using System.Collections.Generic;
using UnityEngine;
using iDate.Network;
using System.IO;
using System.Text;
using System;

namespace iDate.Managers
{
    public class FriendData
    {
        public uint Id;
        public string Name;
        public bool IsOnline;
    }

    public class ProfileData
    {
        public uint Id;
        public string Name;
        public int Level;
        public int Gender;
        public bool IsOnline;
    }

    public class SocialManager : MonoBehaviour
    {
        public static SocialManager Instance { get; private set; }

        public List<FriendData> Friends = new List<FriendData>();
        
        // Events
        public event Action<List<FriendData>> OnFriendListUpdated;
        public event Action<string, bool> OnAddFriendResult; // Msg, success
        public event Action<ProfileData> OnProfileReceived;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SendAddFriendRequest(string name)
        {
            if (NetworkManager.Instance == null) return;
            
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(name);
                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);
                
                NetworkManager.Instance.SendPacket(OpCode.FRIEND_ADD_REQ, ms.ToArray());
            }
        }
        
        public void SendRemoveFriendRequest(uint friendId)
        {
            if (NetworkManager.Instance == null) return;

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((uint)friendId);
                NetworkManager.Instance.SendPacket(OpCode.FRIEND_REMOVE_REQ, ms.ToArray());
            }
        }
        
        public void SendFriendListRequest()
        {
             if (NetworkManager.Instance == null) return;
             NetworkManager.Instance.SendPacket(OpCode.FRIEND_LIST_REQ, new byte[0]);
        }
        
        public void RequestProfile(uint targetId)
        {
            if (NetworkManager.Instance == null) return;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(targetId);
                NetworkManager.Instance.SendPacket(OpCode.CHAR_INFO, ms.ToArray()); // 0x0546
            }
        }

        // Handlers called by NetworkManager
        public void HandleFriendListResponse(BinaryReader reader)
        {
            Friends.Clear();
            ushort count = reader.ReadUInt16();
            
            for (int i = 0; i < count; i++)
            {
                var f = new FriendData();
                f.Id = reader.ReadUInt32();
                // Status byte
                byte status = reader.ReadByte(); // In Server logic, we wrote ID(4), Status(1), NameLen(2)...
                // Wait. Server Logic:
                // writeUInt32LE(f.id, off); off += 4;
                // writeUInt8(1, off); off += 1;
                // writeUInt16LE(nameBytes.length, off); off += 2;
                // nameBytes.copy...
                
                // So Reader Order: ID(4), Status(1), NameLen(2), NameBytes
                // PREVIOUS CODE: f.Id=ReadUInt32, nameLen=ReadUInt16... 
                // Previous client verification might differ.
                // Let's Check Server `SocialHandlers.ts` again?
                // Line 84: ID, Line 87: Status(1), Line 90: NameLen(2)
                // So Client MUST match: ID, Status, NameLen, Name
                
                f.IsOnline = (status == 1);
                
                ushort nameLen = reader.ReadUInt16();
                byte[] nameBytes = reader.ReadBytes(nameLen);
                f.Name = Encoding.UTF8.GetString(nameBytes);
                
                Friends.Add(f);
            }
            
            OnFriendListUpdated?.Invoke(Friends);
            Debug.Log($"[Social] Loaded {count} friends.");
        }
        
        public void HandleAddFriendResponse(BinaryReader reader)
        {
            byte success = reader.ReadByte();
            if (success == 1)
            {
                Debug.Log("[Social] Friend added successfully!");
                OnAddFriendResult?.Invoke("Friend added!", true);
                SendFriendListRequest(); // Refresh
            }
            else
            {
                Debug.Log("[Social] Failed to add friend (not found or already friends).");
                OnAddFriendResult?.Invoke("Failed to add friend.", false);
            }
        }
        
        public void HandleRemoveFriendResponse(BinaryReader reader)
        {
            byte success = reader.ReadByte();
             if (success == 1)
             {
                 SendFriendListRequest(); // Refresh
             }
        }
        
        public void HandleProfileResponse(BinaryReader reader)
        {
            // [ID:4][Gender:1][Level:4][Status:1][NameLen:2][Name:N]
            ProfileData p = new ProfileData();
            p.Id = reader.ReadUInt32();
            p.Gender = reader.ReadByte();
            p.Level = reader.ReadInt32();
            byte status = reader.ReadByte();
            p.IsOnline = (status == 1);
            
            ushort nameLen = reader.ReadUInt16();
            byte[] nameBytes = reader.ReadBytes(nameLen);
            p.Name = Encoding.UTF8.GetString(nameBytes);
            
            Debug.Log($"[Social] Received Profile for {p.Name} (Lvl {p.Level})");
            OnProfileReceived?.Invoke(p);
        }
    }
}
