using System.Collections.Generic;
using UnityEngine;
using iDate.Network;
using System.IO;
using System.Text;
using System;

namespace iDate.Managers
{
    public class ClubData
    {
        public int Id;
        public string Name;
        public int MasterId;
        public int MemberCount;
        public string Description;
    }

    public class ClubManager : MonoBehaviour
    {
        public static ClubManager Instance { get; private set; }

        public List<ClubData> Clubs = new List<ClubData>();
        
        // Events
        public event Action<List<ClubData>> OnClubListUpdated;
        public event Action<bool, int> OnClubCreated; // Success, ClubId
        public event Action<bool> OnClubJoined; // Success

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SendClubListRequest()
        {
             if (NetworkManager.Instance == null) return;
             NetworkManager.Instance.SendPacket(OpCode.CLUB_LIST, new byte[0]);
        }

        public void SendClubCreateRequest(string name, string description)
        {
            if (NetworkManager.Instance == null) return;
            
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(name);
                writer.Write((ushort)nameBytes.Length);
                writer.Write(nameBytes);
                
                byte[] descBytes = Encoding.UTF8.GetBytes(description);
                writer.Write((ushort)descBytes.Length);
                writer.Write(descBytes);
                
                NetworkManager.Instance.SendPacket(OpCode.CLUB_CREATE_REQ, ms.ToArray());
            }
        }
        
        public void SendClubJoinRequest(int clubId)
        {
            if (NetworkManager.Instance == null) return;
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(clubId);
                NetworkManager.Instance.SendPacket(OpCode.CLUB_JOIN_REQ, ms.ToArray());
            }
        }

        // Handlers called by NetworkManager (needs registration in NetworkManager)
        public void HandleClubList(BinaryReader reader)
        {
            Clubs.Clear();
            uint count = reader.ReadUInt32();
            
            for (int i = 0; i < count; i++)
            {
                var c = new ClubData();
                c.Id = reader.ReadInt32();
                
                ushort nameLen = reader.ReadUInt16();
                c.Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));
                
                c.MasterId = reader.ReadInt32();
                c.MemberCount = reader.ReadInt32();
                
                ushort descLen = reader.ReadUInt16();
                c.Description = Encoding.UTF8.GetString(reader.ReadBytes(descLen));
                
                Clubs.Add(c);
            }
            
            OnClubListUpdated?.Invoke(Clubs);
        }
        
        public void HandleClubCreateResponse(BinaryReader reader)
        {
            // Success(4)? OR just ID(4). If ID != 0 -> success
            int clubId = reader.ReadInt32();
            if (clubId != 0) {
                Debug.Log($"Club Created: {clubId}");
                OnClubCreated?.Invoke(true, clubId);
                SendClubListRequest(); // Refresh list
            } else {
                OnClubCreated?.Invoke(false, 0);
            }
        }
        
        public void HandleClubJoinResponse(BinaryReader reader)
        {
            byte success = reader.ReadByte();
            OnClubJoined?.Invoke(success == 1);
            if (success == 1) SendClubListRequest();
        }
    }
}
