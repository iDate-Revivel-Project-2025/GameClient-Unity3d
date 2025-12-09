using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using iDate.Network;
using TMPro;

namespace iDate.UI
{
    public class ServerSelectLogic : MonoBehaviour
    {
        private List<Button> _channelButtons = new List<Button>();

        private void Awake()
        {
            // Auto-discover channel buttons
            // Usually named Button_XXX or based on hierarchy
            // In iDate UI, they are often sequential.
            // visual inspection of screenshot shows ~10 rows.
            
            // Let's find all buttons and assume they are channels for now
            // Or look for specific IDs if we knew them.
            // From UI_server.xml decompile, we could check IDs, but let's grab all active buttons.
            
            var buttons = GetComponentsInChildren<Button>();
            foreach (var btn in buttons)
            {
                // Filter out non-channel buttons (like 'Close' or 'Modify') if possible
                // For now, assume the list items are the main interactive elements
                _channelButtons.Add(btn);
                
                // Add click listener
                int index = _channelButtons.Count - 1; // 0-based
                btn.onClick.AddListener(() => OnChannelClicked(index));
            }
        }

        private void Start()
        {
            // We might have already received the SERVER_LIST packet in GameClient?
            // If so, we should display it. If not, request it.
            // The emulator sent it immediately after login.
        }

        private void OnChannelClicked(int index)
        {
            Debug.Log($"Selected Channel {index}");
            
            // Send Channel Enter Packet
            // OpCode: CH_ENTER (0x07DA)
            // Payload: Channel ID (byte or short?) -> Usually 1-based index
            
            using (Packet p = new Packet((ushort)OpCode.CH_ENTER))
            {
                p.Write((int)(index + 1)); // Provide ID directly as Int (4 bytes)
                NetworkManager.Instance.Send(p);
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

        private void OnPacket(Packet p)
        {
            // Parse OpCode
            OpCode op = (OpCode)p.OpCode;

            if (op == OpCode.CH_ENTER)
            {
                Debug.Log("Channel Joined. Requesting Lobby Init...");
                // Send Lobby Init Request
                using (Packet req = new Packet((ushort)OpCode.LOBBY_INIT))
                {
                    req.Write((int)0); // Unsure of payload, usually empty or ID? Server expects nothing but packet size check usually for simple ones.
                    // Server code: case OP.LOBBY_INIT: just runs. Handled by simple opcode match usually.
                    // Let's send empty body for safety or 0 padding. 
                    // Server parsing LOBBY_INIT doesn't check length explicitly in the switch start.
                    // But standard packet writes at least 8 bytes.
                    NetworkManager.Instance.Send(req);
                }
            }
            else if (op == OpCode.LOBBY_INIT || op == OpCode.CLUB_LIST)
            {
                Debug.Log("Entering Lobby...");
                // Transition
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowScreen("lobby");
                }
            }
        }
    }
}
