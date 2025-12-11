using UnityEngine;
using System.Collections.Generic;
using iDate.Network;
using iDate.Shop;
using System.Text;

namespace iDate.UI
{
    public class LobbyUI : MonoBehaviour
    {
        public Transform RoomListContainer;
        public Transform RankingListContainer;
        public GameObject RankingEntryPrefab;
        public UnityEngine.UI.Text LocalStatsText; // "Lv.X Name (Exp) | Cash: Y Points: Z"

        private void Start()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived += HandlePacket;
                NetworkManager.Instance.OnTopRankingReceived += UpdateRankingList;
                
                // Auto-request rankings on load
                NetworkManager.Instance.SendTopRankingRequest();
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnStatsUpdated += UpdateStatsUI;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
                NetworkManager.Instance.OnTopRankingReceived -= UpdateRankingList;
            }
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnStatsUpdated -= UpdateStatsUI;
            }
        }

        private void UpdateStatsUI(int cash, int points, int level, uint exp)
        {
            if (LocalStatsText != null)
            {
                // Note: Name is not passed in OnStatsUpdated, might need to store it or accept it's missing for now.
                // Assuming "Player" or just stats.
                LocalStatsText.text = $"Lv.{level} (Exp: {exp}) | Cash: {cash} | Points: {points}";
            }
        }

        private void UpdateRankingList(List<(uint id, string name, int level, uint exp)> chars)
        {
            if (RankingListContainer == null) return;
            
            foreach(Transform child in RankingListContainer) Destroy(child.gameObject);
            
            foreach(var c in chars)
            {
                // Create Entry
                GameObject go = null;
                if (RankingEntryPrefab != null)
                {
                    go = Instantiate(RankingEntryPrefab, RankingListContainer);
                }
                else
                {
                    // Fallback Text
                    go = new GameObject("RankEntry", typeof(UnityEngine.UI.Text));
                    go.transform.SetParent(RankingListContainer, false);
                }
                
                var txt = go.GetComponent<UnityEngine.UI.Text>();
                if (txt != null)
                {
                    txt.text = $"Lv.{c.level} {c.name} (Exp: {c.exp})";
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txt.color = Color.white;
                }
            }
        }

        private void HandlePacket(Packet p)
        {
            if (p.OpCode == (ushort)OpCode.ROOM_LIST)
            {
                // Parse Room List (based on RE)
                ParseRoomList(p.Payload);
            }
        }
        
        // ... (ParseRoomList remains)

        private void ParseRoomList(byte[] data)
        {
            // Placeholder: Clear existing
            foreach(Transform child in RoomListContainer) Destroy(child.gameObject);

            // RE Logic: Loop through data chunks
            // struct RoomInfo { short id; char name[?]; ... }
            
            Debug.Log("LobbyUI: Received Room List Data: " + data.Length + " bytes");
            
            // NOTE: Need exact struct parsing from opcode_map/RE report to implement fully.
            // For now, logging.
        }
    }
}
