using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using iDate.Network;
using TMPro;

namespace iDate.UI
{
    public class LobbyLogic : MonoBehaviour
    {
        [Header("UI References")]
        public Transform RoomListContainer; // Needs to be assigned or found
        public Transform UserListContainer;
        public TMP_InputField ChatInput;
        public TextMeshProUGUI ChatLog;
        
        [Header("Prefabs")]
        public GameObject RoomItemPrefab; // We need to generate this or use a generic one

        private void Awake()
        {
            // Auto-bind attempt
            // Look for "ChatInput" or "Ime_..."
            // In the XML, finding specific elements by generic ID is hard without knowing the specific IDs (e.g. Ime_120).
            // We'll rely on Find for common names if the XML has meaningful ones, or just generic search.
            
            // For now, let's find ANY InputField for chat
            if (ChatInput == null) ChatInput = GetComponentInChildren<TMP_InputField>();
            
            // Background is hidden by default in XML to show 3D world.
            // We keep it hidden so we can see the Character Cube.
            // Transform bg = transform.Find("Image_390");
            // if (bg != null) bg.gameObject.SetActive(true);

            if (ChatInput != null)
            {
                ChatInput.onSubmit.AddListener(OnSubmitChat);
            }
        }

        private void OnEnable()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived += OnPacket;
        }

        private void OnDisable()
        {
             if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= OnPacket;
        }

        private void OnSubmitChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            // Send Chat Packet
            // OpCode: ROOM_CHAT or LOBBY_CHAT? 
            // Emulator server.js uses ROOM_CHAT (0x0816) for handling chat commands even in channel/lobby?
            // "case OP.ROOM_CHAT: ... console.log(... Chat from ...)"
            
            // Let's try sending standard chat
            // Payload: [TargetID (4)] [Message (Variable)]
            
            using (Packet p = new Packet((ushort)OpCode.ROOM_CHAT))
            {
                // TargetID 0 for All?
                p.Write((uint)0); // Placeholder for Target
                p.Write(0); // Padding or Target?
                // Actually server reads: senderId = pkt.readUInt32LE(0x04) -> Wait, Server reads FROM packet 0x04.
                // Client sends: [Header 8] [Unknown 4] [Message...]
                // We need to match protocol.
                // Server: senderId = pkt.readUInt32LE(0x04);
                // message = readWString(pkt, 0x0C, 128);
                
                // So Client sends: [Header] [Unknown 4 bytes] [WString]
                
                p.Write((uint)0); // Unknown 4 bytes (maybe target?)
                p.WriteString(text, 128); // Standard WString write
                
                NetworkManager.Instance.Send(p);
            }
            
            ChatInput.text = "";
            ChatInput.ActivateInputField();
        }

        private void OnPacket(Packet p)
        {
            OpCode op = (OpCode)p.OpCode;
            switch(op)
            {
                case OpCode.ROOM_LIST:
                    // Parse Room List
                    Debug.Log("Lobby: Received Room List");
                    break;
                case OpCode.ROOM_CHAT:
                    // Parse Chat
                    // Read Header
                    // uint unk = p.ReadUInt();
                    // string msg = p.ReadString(128);
                    // ChatLog.text += $"\n{msg}";
                    break;
                case OpCode.LOBBY_CHAR_LIST:
                    // Spawn Character
                    Debug.Log("Lobby: Received Character Spawn Data");
                    SpawnLocalCharacter(p);
                    break;
            }
        }

        private void SpawnLocalCharacter(Packet p)
        {
            // Parse Packet based on buildClubSelfSpawn / buildRoomEnterBroadcast
            
            // Offset 0: ID (4)
            // Offset 4: Unknown (4) for padding?
            // Actually Server uses absolute offsets from Body Start (after 8 byte header)
            // p.Read... reads sequentially.
            
            // Server: p.writeUInt32LE(session.characterId, offset + 0x00);
            uint charId = p.ReadUInt();
            
            // Server: p.writeUInt8(0) x4 ... then Gender at +0x05?
            // Let's Skip 4 bytes padding
            uint u1 = p.ReadUInt();
            
            // Server: writeWString(p, offset + 0x08, Name, 16);
            // wait, if offset is +0x08, that WAS where we just read u1.
            // buildClubSelfSpawn:
            // offset = 8.
            // Packet writes ID at 8+0 = 8.
            // Packet writes u8(0) at 8+4 = 12.
            // Packet writes u8(gender) at 8+5 = 13.
            // ...
            // writeWString at 8+8 = 16.
            
            // So:
            // ReadUInt() -> ID (Bytes 8-11)
            // ReadUInt() -> u1 (Bytes 12-15). Gender is inside this.
            
            // ReadString(32 bytes) -> Name (Bytes 16-47) (16 chars * 2)
            string charName = p.ReadString(32); // 16 WChars
            
            // We need to skip to Position Data at Offset 0xB8 (184 dec) relative to body start?
            // "posOffset = 0xB8" -> 184.
            // Header is 8. So absolute index 192.
            // We have read: 4 (ID) + 4 (GenderPack) + 34 (Name: 32 bytes + 2 null?)
            // Actually ReadString handles parsing.
            
            // Let's just hardcode skip to the position loop.
            // We can set read position if Packet supports it, or just read lots of bytes.
            // For now, let's just spawn at (0,0,0) with the name to verify.
            
            Debug.Log($"Spawning Character: {charName} (ID: {charId})");
            
            GameObject pc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pc.name = $"Player_{charName}";
            pc.transform.localScale = new Vector3(50, 50, 50); // Scale up just in case
            
            
            // Try to find a position if we can parse it roughly
            // Position is at roughly byte 192.
            // We are at ~42.
            // Let's assume (0,0,0) first.
            pc.transform.position = Vector3.zero;
            
            // Add a text label
            GameObject textObj = new GameObject("NameTag");
            textObj.transform.SetParent(pc.transform);
            textObj.transform.localPosition = Vector3.up * 2.0f;
            TextMeshPro text = textObj.AddComponent<TextMeshPro>();
            text.text = charName;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.green;

            // Update Camera
            Camera.main.transform.position = new Vector3(0, 5, -10);
            Camera.main.transform.LookAt(pc.transform);
        }
    }
}
