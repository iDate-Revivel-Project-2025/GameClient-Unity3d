using System;
using iDate.UI;
using iDate.Shop;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace iDate.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isRunning;

        // Queue for processing packets on the main thread
        private ConcurrentQueue<Packet> _receiveQueue = new ConcurrentQueue<Packet>();
        
        // Events
        public event Action OnConnected;
        public event Action<Packet> OnPacketReceived;
        public event Action OnDisconnected;
#pragma warning disable 67
        public event Action<List<iDate.UI.SongData>> OnSongListReceived;
#pragma warning restore 67

        // Constants
        private const string IP = "127.0.0.1";
        private const int PORT = 8132;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Auto connect for now
            Connect(IP, PORT);
        }

        public void Connect(string ip, int port)
        {
            if (_client != null && _client.Connected) 
            {
               Debug.LogWarning("NetworkManager: Already connected.");
               return;
            }

            try
            {
                Debug.Log($"Connecting to {ip}:{port}...");
                _client = new TcpClient();
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                _isRunning = true;

                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.Start();

                // Notify connected
                OnConnected?.Invoke();
                Debug.Log("Connected to server!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                OnDisconnected?.Invoke();
            }
        }

        // Handler Dictionary
        private Dictionary<ushort, Action<Packet>> _handlers = new Dictionary<ushort, Action<Packet>>();

        public void RegisterHandler(OpCode op, Action<Packet> handler)
        {
            ushort code = (ushort)op;
            if (_handlers.ContainsKey(code))
            {
                _handlers[code] += handler;
            }
            else
            {
                _handlers[code] = handler;
            }
        }
        
        public void UnregisterHandler(OpCode op, Action<Packet> handler)
        {
            ushort code = (ushort)op;
            if (_handlers.ContainsKey(code))
            {
                _handlers[code] -= handler;
            }
        }
        
        // Alias for Send to match other scripts
        public void SendPacket(Packet p)
        {
            Send(p);
        }



        private void ReceiveLoop()
        {
            byte[] headerBuffer = new byte[8];

            try
            {
                while (_isRunning && _client.Connected)
                {
                    // Read Header
                    if (!ReadExactly(headerBuffer, 8)) break;

                    ushort size = BitConverter.ToUInt16(headerBuffer, 0);
                    
                    // Sanity check
                    if (size < 8)
                    {
                         Debug.LogError($"Invalid packet size: {size}");
                         break;
                    }

                    // Read Body
                    int bodySize = size - 8;
                    byte[] bodyBuffer = new byte[bodySize];
                    
                    if (bodySize > 0)
                    {
                        if (!ReadExactly(bodyBuffer, bodySize)) break;
                    }

                    // Combine headers and body to create full packet data for parsing
                    byte[] fullPacket = new byte[size];
                    Array.Copy(headerBuffer, 0, fullPacket, 0, 8);
                    if (bodySize > 0)
                    {
                        Array.Copy(bodyBuffer, 0, fullPacket, 8, bodySize);
                    }

                    // Enqueue
                    Packet p = new Packet(fullPacket);
                    _receiveQueue.Enqueue(p);
                }
            }
            catch (Exception ex)
            {
                if (_isRunning) Debug.LogError($"Receive Loop Error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private bool ReadExactly(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        private void Update()
        {
            while (_receiveQueue.TryDequeue(out Packet p))
            {
                HandlePacket(p);
            }
        }

        private bool _handshakeSent = false;

        private void HandlePacket(Packet p)
        {
            // Log for debug
             if (p == null) return;
             byte[] bytes = p.GetBytes();
             Debug.Log($"RX: Op={(OpCode)p.OpCode} Size={(bytes != null ? bytes.Length : 0)}");

            // Check Registered Handlers
            if (_handlers.TryGetValue(p.OpCode, out var handler))
            {
                handler?.Invoke(p);
            }

            // Invoke Legacy Event for managers who subscribe to everything
            OnPacketReceived?.Invoke(p);

            // Basic Handlers or Dispatch
            switch ((OpCode)p.OpCode)
            {
                case OpCode.HANDSHAKE:
                    if (!_handshakeSent)
                    {
                        Debug.Log("Handshake received, sending reply...");
                        SendHandshake();
                        _handshakeSent = true;
                    }
                    else
                    {
                        Debug.Log("Handshake echo received.");
                    }
                    break;
                case OpCode.ENCRYPT_INIT:
                    // Server sent encryption init (key is usually 0 in emu)
                    // We need to send Login now?
                    Debug.Log("Encryption Init received. Sending Login...");
                    SendLogin();
                    break;
                case OpCode.LOGIN_RESP:
                    Debug.Log("Login Successful!");
                    break;
                case OpCode.GAME_SCORE:
                    // Parse Score: [Header] [PlayerID(4)] [Score(4)] [Combo(2)] [Judge(1)]
                    // Total payload size = 11 (after header)
                    // Packet reader is at Payload start.
                    try 
                    {
                        uint pid = p.Reader.ReadUInt32();
                        int score = p.Reader.ReadInt32();
                        short combo = p.Reader.ReadInt16();
                        byte judge = p.Reader.ReadByte();
                        
                        iDate.Gameplay.GameplayManager.Instance?.OnScoreReceived(pid, score, combo, judge);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Bad Score Pkt: {ex.Message}");
                    }
                    break;
                case OpCode.INVENTORY:
                    // Packet Structure:
                    // Header (8 bytes)
                    // Count (4 bytes)
                    // Items... (28 bytes each)
                    
                    try
                    {
                        // ensure we are past header (8)
                        if (p.Reader.BaseStream.Position < 8) p.Reader.BaseStream.Seek(8, SeekOrigin.Begin);
                        
                        int count = p.Reader.ReadInt32();
                        Debug.Log($"[Network] Received Inventory: {count} items.");

                        List<iDate.UI.InventoryItemData> items = new List<iDate.UI.InventoryItemData>();
                        
                        for (int i = 0; i < count; i++)
                        {
                            iDate.UI.InventoryItemData item = new iDate.UI.InventoryItemData();
                            // 28 Bytes per item
                            item.UniqueID = p.Reader.ReadUInt32();      // 0
                            item.ItemTypeID = p.Reader.ReadUInt32();    // 4
                            item.Category = p.Reader.ReadInt32();       // 8
                            item.VariantID = p.Reader.ReadInt32();      // 12
                            int slot = p.Reader.ReadInt32();            // 16
                            // item.Slot = slot; // If needed in UI
                            int equipped = p.Reader.ReadInt32();        // 20
                            item.IsEquipped = (equipped != 0);
                            int duration = p.Reader.ReadInt32();        // 24
                             
                            items.Add(item);
                        }
                        
                        // Dispatch
                        if (iDate.UI.InventoryUI.Instance != null)
                        {
                            iDate.UI.InventoryUI.Instance.Populate(items);
                        }
                        else
                        {
                             Debug.LogWarning("InventoryUI Instance not found.");
                        }
                    }
                    catch (Exception ex)
                    {
                         Debug.LogWarning("Inv Parse Error: " + ex.Message);
                    }
                    break;
                case OpCode.ROOM_CHAT:
                    // Server Format: [Header] [SenderID(4)] [MessageString...]
                    try
                    {
                        uint senderId = p.Reader.ReadUInt32();
                        
                        // Read remaining bytes as Unicode string
                        // Start index = 4 (relative to payload start)
                        // But Reader is at 4 now.
                        
                        // Read string manually or use Unicode
                        // C# BinaryReader ReadString is 7-bit encoded len prefixed.
                        // We need raw.
                        
                        // Get remaining bytes
                        int remain = (int)(p.Reader.BaseStream.Length - p.Reader.BaseStream.Position);
                        byte[] msgBytes = p.Reader.ReadBytes(remain);
                        string msg = System.Text.Encoding.Unicode.GetString(msgBytes);
                        
                        // Map ID to Name?
                        // For now just ID
                        string name = $"Player_{senderId}";
                        // If it's us?
                        
                        // Dispatch
                        // Use MainThreadDispatch if needed, but Unity logic often runs in Update via Queue here
                        iDate.UI.ChatUI.Instance?.AddMessage(name, msg);
                    }
                    catch (Exception ex) 
                    {
                        Debug.LogWarning("Chat Parse Error: " + ex.Message);
                    }
                    break;
                case OpCode.SONG_LIST:
                    HandleSongList(p);
                    break;
                case OpCode.SHOP_LIST:
                    HandleShopList(p);
                    break;
                case OpCode.BUY_ITEM_RESP:
                    HandleBuyItemResponse(p);
                    break;

                // Social Handlers
                case OpCode.FRIEND_LIST_RESP:
                    iDate.Managers.SocialManager.Instance?.HandleFriendListResponse(p.Reader);
                    break;
                case OpCode.FRIEND_ADD_RESP:
                    iDate.Managers.SocialManager.Instance?.HandleAddFriendResponse(p.Reader);
                    break;
                case OpCode.FRIEND_REMOVE_RESP:
                    iDate.Managers.SocialManager.Instance?.HandleRemoveFriendResponse(p.Reader);
                    break;

                // --- Club System ---
                case OpCode.CLUB_LIST:
                    iDate.Managers.ClubManager.Instance?.HandleClubList(p.Reader);
                    break;
                case OpCode.CLUB_CREATE_REQ:
                    iDate.Managers.ClubManager.Instance?.HandleClubCreateResponse(p.Reader);
                    break;
                case OpCode.CLUB_JOIN_REQ:
                    iDate.Managers.ClubManager.Instance?.HandleClubJoinResponse(p.Reader);
                    break;
                
                // --- New Verification Handlers ---
                case OpCode.STAGE_INFO: // 0x0596 - Currency/Exp Update
                    HandleStageInfo(p);
                    break;
                case OpCode.GAME_END:
                    HandleGameEnd(p);
                    break;
                case OpCode.APPEARANCE_BROADCAST: // 0x0C30 - Visual Update
                    HandleAppearanceBroadcast(p);
                    break;
                case OpCode.TOP_RANKING_RESP:
                    HandleTopRankingResponse(p);
                    break;
                
                // --- Messenger System ---
                case OpCode.MSG_RECV:
                case OpCode.MSG_SEND_RESP:
                    // Dispatch entire Packet object
                    // Note: Manager should handle OpCode check if needed, or we just call specific method.
                    // But MessengerManager handles both in HandlePacket(p).
                    // Or we can expose specific methods.
                    // Let's use generic HandlePacket if exposed, or specific.
                    // I exposed HandlePacket(Packet p) in MessengerManager (private). I should make it public or expose specific.
                    // Wait, I made HandlePacket private in MessengerManager using event.
                    // But I subscribed to NetworkManager.OnPacketReceived in MessengerManager!
                    // SO I DO NOT NEED TO ADD DISPATCH HERE for MessengerManager if it listens to OnPacketReceived!
                    
                    // Allow MessengerManager to handle via event.
                    break;

                // --- Couple System ---
                case OpCode.COUPLE_REQ:
                case OpCode.COUPLE_RESP:
                case OpCode.COUPLE_BREAK:
                    // CoupleManager subscribes to OnPacketReceived?
                    // In my previous read of CoupleManager, it TRIED to use RegisterHandler.
                    // I will fix CoupleManager to subscribe to OnPacketReceived.
                    // So I don't need to add dispatch here either!
                    break;
                
                // --- New Verification Handlers ---
        }
    } // Close HandlePacket method

        public void Send(Packet p)
        {
            if (_client == null || !_client.Connected) return;

            try
            {
                byte[] data = p.GetBytes();
                _stream.Write(data, 0, data.Length);
                Debug.Log($"TX: Op={(OpCode)p.OpCode} Size={data.Length}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send Error: {ex.Message}");
                Disconnect();
            }
        }

        public void SendPacket(OpCode op, byte[] payload)
        {
            using (Packet p = new Packet(op))
            {
                p.WriteBytes(payload);
                Send(p);
            }
        }

        public void Disconnect()
        {
            _isRunning = false;
            _client?.Close();
            _client = null;
            OnDisconnected?.Invoke();
             Debug.Log("Disconnected.");
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        // --- Specific Packet Senders (Move to a helper later?) ---

        public void SendHandshake()
        {
            // 08 00 64 00 00 00 00 00
            using (Packet p = new Packet((ushort)OpCode.HANDSHAKE))
            {
                Send(p);
            }
        }

        public void SendLogin(string username = "testuser", string password = "pass1234")
        {
            // According to the encryption spec:
            // - If ENCRYPT_INIT is NOT sent, login uses swap algorithm only
            // - Username and password are encoded as UTF-16LE and encrypted with swap
            // - Standard lengths: username 28 bytes (14 chars), password 20 bytes (10 chars)
            
            const int USERNAME_LEN = 28; // 14 UTF-16LE characters
            const int PASSWORD_LEN = 20; // 10 UTF-16LE characters
            
            byte[] encryptedUser = CryptoUtils.EncodeString(username, USERNAME_LEN);
            byte[] encryptedPass = CryptoUtils.EncodeString(password, PASSWORD_LEN);
            
            Debug.Log($"[Login] Sending encrypted credentials for user: {username}");
            
            using (Packet p = new Packet((ushort)OpCode.ENCRYPTED))
            {
                // Write encrypted username and password directly
                p.WriteBytes(encryptedUser);
                p.WriteBytes(encryptedPass);
                
                Send(p);
            }
        }

        public void SendScore(int score, short combo, byte judge)
        {
            // OP.GAME_SCORE = 0x0924
            using (Packet p = new Packet((ushort)OpCode.GAME_SCORE))
            {
                p.Writer.Write((int)score);
                p.Writer.Write((short)combo);
                p.Writer.Write((byte)judge);
                // Padding if needed?
                Send(p);
            }
        }

        public void SendEquip(uint newItemId)
        {
             // 0x0BE0
             // Payload: 6 uints (Hair, Face, Jacket, Gloves, Pants, Shoes)
             using (Packet p = new Packet((ushort)OpCode.CHAR_EQUIP))
             {
                 // Mock sending same ID to all slots to verify reception
                 for(int i=0; i<6; i++) p.Writer.Write(newItemId);
                 Send(p);
             }
         }

         public void SendChat(string message)
         {
             // OP.ROOM_CHAT (0x0816)
             // Payload: Message (Unicode)
             using (Packet p = new Packet((ushort)OpCode.ROOM_CHAT))
             {
                 // Write RAW unicode bytes without length prefix (standard for this protocol variant)
                 byte[] data = System.Text.Encoding.Unicode.GetBytes(message);
                 p.Writer.Write(data);
                 Send(p);
             }
         }

        public void SendSongListRequest()
        {
            if (_stream == null || !_stream.CanWrite) return;

            // Packet: Header (8) only
            byte[] packet = new byte[8];
            
            // Size
            System.BitConverter.GetBytes((ushort)8).CopyTo(packet, 0);
            // OpCode
            System.BitConverter.GetBytes((ushort)OpCode.SONG_LIST_REQ).CopyTo(packet, 2);
            // Checksum/Flags
            System.BitConverter.GetBytes((uint)0).CopyTo(packet, 4);

            _stream.Write(packet, 0, packet.Length);
            Debug.Log("[Network] Sent SONG_LIST_REQ");
        }

        public void SendShopListRequest()
        {
            if (_stream == null || !_stream.CanWrite) return;
            var p = new Packet(OpCode.SHOP_LIST_REQ);
            Send(p);
            Debug.Log("[Network] Sent SHOP_LIST_REQ");
        }

        public void SendBuyItemRequest(uint itemId)
        {
            if (_stream == null || !_stream.CanWrite) return;
            var p = new Packet(OpCode.BUY_ITEM_REQ);
            p.Writer.Write((uint)itemId);
            Send(p);
            Debug.Log($"[Network] Sent BUY_ITEM_REQ for Item {itemId}");
        }

        private void HandleSongList(Packet p)
        {
            try 
            {
                byte[] raw = p.GetBytes();
                // Count at offset 8
                int count = System.BitConverter.ToInt32(raw, 8);
                Debug.Log($"[Network] Received Song List: {count} songs.");

                List<SongData> songs = new List<SongData>();
                int offset = 12;
                int ITEM_SIZE = 272;

                for (int i = 0; i < count; i++)
                {
                    SongData s = new SongData();
                    s.Id = System.BitConverter.ToUInt32(raw, offset + 0);
                    
                    // Title (64 bytes / 32 chars)
                    s.Title = Encoding.Unicode.GetString(raw, offset + 4, 64).Trim('\0');
                    
                    // Artist (64 bytes)
                    s.Artist = Encoding.Unicode.GetString(raw, offset + 4 + 64, 64).Trim('\0');
                    
                    // Path (128 bytes)
                    s.ResourcePath = Encoding.Unicode.GetString(raw, offset + 4 + 64 + 64, 128).Trim('\0');
                    
                    s.Bpm = System.BitConverter.ToSingle(raw, offset + 4 + 64 + 64 + 128);
                    s.Duration = System.BitConverter.ToInt32(raw, offset + 4 + 64 + 64 + 128 + 4);
                    s.Difficulty = System.BitConverter.ToInt32(raw, offset + 4 + 64 + 64 + 128 + 4 + 4);

                    songs.Add(s);
                    offset += ITEM_SIZE;
                }

                if (SongManager.Instance != null)
                {
                    SongManager.Instance.SetSongs(songs);
                }
            } 
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Error parsing SongList: {ex.Message}");
            }
        }

        private void HandleShopList(Packet p)
        {
            try
            {
                byte[] raw = p.GetBytes();
                int count = System.BitConverter.ToInt32(raw, 8);
                Debug.Log($"[Network] Received Shop List: {count} items.");

                List<ShopItemData> items = new List<ShopItemData>();
                int offset = 12;
                int ITEM_SIZE = 208;

                for (int i = 0; i < count; i++)
                {
                    ShopItemData item = new ShopItemData();
                    item.Id = System.BitConverter.ToUInt32(raw, offset + 0);
                    item.Name = Encoding.Unicode.GetString(raw, offset + 4, 64).Trim('\0');
                    
                    item.Category = System.BitConverter.ToInt32(raw, offset + 68);
                    item.Price = System.BitConverter.ToInt32(raw, offset + 72);
                    item.CurrencyType = System.BitConverter.ToInt32(raw, offset + 76);
                    
                    item.ResourcePath = Encoding.Unicode.GetString(raw, offset + 80, 64).Trim('\0');
                    item.IconPath = Encoding.Unicode.GetString(raw, offset + 144, 64).Trim('\0');

                    items.Add(item);
                    offset += ITEM_SIZE;
                }

                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.SetShopItems(items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Error parsing ShopList: {ex.Message}");
            }
        }

        private void HandleBuyItemResponse(Packet p)
        {
            try
            {
                // Packet Reader is at offset 8 (Payload) because of how we wrap?
                // Actually my manual parsing uses raw bytes for lists.
                // For simple packets, Reader is easier if it works.
                // Packet constructor: `Reader = new BinaryReader(new MemoryStream(bytes));`
                // It reads from 0 (Header).
                // So parsing with Reader requires valid position.
                // Let's use raw for consistency if unsure about Reader Pos.
                
                byte[] raw = p.GetBytes();
                // 12 bytes payload (Success, ItemID, NewBalance)
                // Header (8)
                int successVal = System.BitConverter.ToInt32(raw, 8);
                uint itemId = System.BitConverter.ToUInt32(raw, 12);
                int newBalance = System.BitConverter.ToInt32(raw, 16);

                bool success = (successVal == 1);
                
                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.HandlePurchaseResult(success, itemId, newBalance);
                }
            }
            catch (Exception ex)
            {
                 Debug.LogError($"[Network] Error parsing BuyItemResp: {ex.Message}");
            }
        }

        private void HandleStageInfo(Packet p)
        {
            try
            {
                // Packet Structure (from PacketBuilder.ts / RE):
                // Header (8 bytes)
                // Items... (36 bytes each)
                //   Offset 0: Type (4 bytes)
                //   Offset 4: Value (4 bytes)
                //   Offset 8..35: Padding/Unknown
                // Types: 1=Points, 2=Cash, 3=Exp?, 4=Level? (Hypothesis or expand server to send more)
                
                // Currently Server sends Type 1 (Points) and 2 (Cash).
                
                byte[] raw = p.GetBytes();
                int offset = 8;
                int ITEM_SIZE = 36;
                
                int cash = 0;
                int points = 0;
                int level = 0;
                uint exp = 0;

                while (offset + 8 <= raw.Length)
                {
                    int type = BitConverter.ToInt32(raw, offset);
                    int val = BitConverter.ToInt32(raw, offset + 4);
                    
                    switch (type)
                    {
                        case 1: points = val; break;
                        case 2: cash = val; break;
                        case 3: level = val; break; // Hypothesis
                        case 4: exp = (uint)val; break; // Hypothesis
                    }
                    
                    offset += ITEM_SIZE;
                }

                Debug.Log($"[Network] StageInfo: Cash={cash} P={points} Lvl={level} Exp={exp}");

                // Dispatch
                ShopManager.Instance?.UpdateStats(cash, points, level, exp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Error parsing StageInfo: {ex.Message}");
            }
        }

        private void HandleAppearanceBroadcast(Packet p)
        {
             try
             {
                 // [Header:8][PlayerID:4][Count:4][Item...]
                 byte[] raw = p.GetBytes();
                 if (raw.Length < 16) return;

                 uint playerId = BitConverter.ToUInt32(raw, 8);
                 int count = BitConverter.ToInt32(raw, 12);
                 Debug.Log($"[Network] Appearance Update for Player {playerId}: {count} items.");

                 // Check if it is us?
                 // If not, update Room Character.
                 
                 // Payload: [Category:4][TypeId:4] pairs (8 bytes each)
                 List<int[]> items = new List<int[]>();
                 int offset = 16;
                 
                 for(int i=0; i<count; i++) 
                 {
                     int cat = BitConverter.ToInt32(raw, offset);
                     int type = BitConverter.ToInt32(raw, offset + 4);
                     items.Add(new int[] { cat, type });
                     offset += 8;
                 }

                 // Dispatch
                 if (iDate.Gameplay.RoomManager.Instance != null)
                 {
                     iDate.Gameplay.RoomManager.Instance.UpdatePlayerAppearance(playerId.ToString(), items);
                 }
                 else
                 {
                     Debug.LogWarning("RoomManager Instance not found during Appearance Broadcast.");
                 }
             }
             catch (Exception ex)
             {
                 Debug.LogError($"[Network] Error parsing AppearanceBroadcast: {ex.Message}");
             }
        }
        public delegate void TopRankingHandler(List<(uint id, string name, int level, uint exp)> chars);
        public event TopRankingHandler OnTopRankingReceived;
        
        public delegate void GameEndedHandler(int exp, int points);
        public event GameEndedHandler OnGameEnded;

        public void SendTopRankingRequest()
        {
            SendPacket(new Packet(OpCode.TOP_RANKING_REQ));
        }

        private void HandleTopRankingResponse(Packet p)
        {
             // [Header][Count] [Char...]
             // Item Size = 76 (4+64+4+4)
             byte[] raw = p.GetBytes();
             if (raw.Length < 12) return;
             
             uint count = BitConverter.ToUInt32(raw, 8);
             int ITEM_SIZE = 76;
             
             List<(uint, string, int, uint)> list = new List<(uint, string, int, uint)>();
             
             int offset = 12;
             for(int i=0; i<count; i++)
             {
                 if (raw.Length < offset + ITEM_SIZE) break;
                 
                 uint id = BitConverter.ToUInt32(raw, offset);
                 string name = System.Text.Encoding.Unicode.GetString(raw, offset + 4, 64).TrimEnd('\0');
                 int level = BitConverter.ToInt32(raw, offset + 68);
                 uint exp = BitConverter.ToUInt32(raw, offset + 72);
                 
                 list.Add((id, name, level, exp));
                 offset += ITEM_SIZE;
             }
             
             Debug.Log($"[Network] Received {count} Top Ranked Players.");
             OnTopRankingReceived?.Invoke(list);
        }
        
        private void HandleGameEnd(Packet p)
        {
            // [Header][Exp:4][Points:4]
            byte[] raw = p.GetBytes();
            if (raw.Length < 12) return;
            
            int exp = BitConverter.ToInt32(raw, 4);
            int points = BitConverter.ToInt32(raw, 8);
            
            Debug.Log($"[Network] Game Ended. Exp: {exp}, Points: {points}");
            OnGameEnded?.Invoke(exp, points);
        }
    }
}
