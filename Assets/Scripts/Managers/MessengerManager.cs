using System;
using System.IO;
using System.Text;
using UnityEngine;
using iDate.Network;

namespace iDate.Managers
{
    public class MessengerManager : MonoBehaviour
    {
        public static MessengerManager Instance;

        public event Action<uint, string, string> OnMessageReceived; // SenderID, SenderName, Message
        public event Action<bool, string> OnMessageSentResult; // Success, SystemMessage

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Register handlers via NetworkManager events or direct injection?
            // NetworkManager.cs has a HandlePacket switch.
            // Ideally we shouldn't edit NetworkManager.cs for every manager.
            // But currently NetworkManager dispatches explicitly.
            // Using "OnPacketReceived" event we can handle it here.
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived += HandlePacket;
            }
        }
        
        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            }
        }

        public void SendMessage(string targetName, string message)
        {
            using (Packet p = new Packet((ushort)OpCode.MSG_SEND_REQ))
            {
                // Format: [TargetNameLen:2] [TargetName:Var] [MessageLen:2] [Message:Var]
                // Lengths are in CHARACTERS (as server expects *2 bytes)
                
                byte[] nameBytes = Encoding.Unicode.GetBytes(targetName);
                byte[] msgBytes = Encoding.Unicode.GetBytes(message);
                
                // Server expects Char Count (UShort)
                p.Writer.Write((ushort)targetName.Length);
                p.Writer.Write(nameBytes);
                
                p.Writer.Write((ushort)message.Length);
                p.Writer.Write(msgBytes);
                
                NetworkManager.Instance.Send(p);
                Debug.Log($"[Messenger] Sent message to {targetName}");
            }
        }

        private void HandlePacket(Packet p)
        {
             // Check OpCode
             switch ((OpCode)p.OpCode)
             {
                 case OpCode.MSG_RECV:
                     HandleMsgRecv(p);
                     break;
                 case OpCode.MSG_SEND_RESP:
                     HandleMsgSendResp(p);
                     break;
             }
        }

        private void HandleMsgRecv(Packet p)
        {
            try
            {
                // [SenderID:4] [SenderNameLen:2] [SenderName:Var] [MessageLen:2] [Message:Var]
                // Reader position needs to be managed if p.Reader is shared or reset?
                // Packet constructor makes a new Reader.
                // But Payload starts at offset 0 of p.Reader (if constructed from payload).
                // Wait, ReceiveLoop: `Packet p = new Packet(fullPacket);`
                // Packet(byte[]): `Payload = payloadBytes;` -> `_reader = new BinaryReader(_payloadStream);`
                // So Reader starts at 0 of Payload. Correct.
                
                // Assuming we haven't read header in Reader (which we haven't, header parsed in constructor).
                
                // However, NetworkManager.cs sometimes seeks to 8?
                // "if (p.Reader.BaseStream.Position < 8) p.Reader.BaseStream.Seek(8, SeekOrigin.Begin);"
                // This implies NetworkManager thinks Reader includes Header?
                // Let's check Packet.cs Receive Constructor again.
                // It reads Header from `br` (local). Then reads Payload.
                // Then `Payload` property setter creates `_payloadStream` from `value` (payload bytes).
                // So Reader starts at 0 of Payload.
                // The `Seek(8)` in NetworkManager was likely a mistake or needed if Packet wrap different logic?
                // `NetworkManager.cs` line 219: `if (p.Reader.BaseStream.Position < 8) ...`
                // If Reader is on Payload (size 10), seek 8 is byte 8 of payload.
                // If header was stripped, this is index 8 of body.
                // If header was NOT stripped, index 8 is start of body.
                
                // My Packet.cs logic:
                // `Payload = payloadBytes` -> `_reader` is on Payload only.
                // So index 0 is start of body.
                // So NO seek of 8 needed.
                
                // Parse:
                uint senderId = p.Reader.ReadUInt32();
                
                ushort nameLen = p.Reader.ReadUInt16();
                byte[] nameBytes = p.Reader.ReadBytes(nameLen * 2);
                string senderName = Encoding.Unicode.GetString(nameBytes);
                
                ushort msgLen = p.Reader.ReadUInt16();
                byte[] msgBytes = p.Reader.ReadBytes(msgLen * 2);
                string message = Encoding.Unicode.GetString(msgBytes);
                
                Debug.Log($"[Messenger] Received from {senderName}: {message}");
                OnMessageReceived?.Invoke(senderId, senderName, message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Messenger] Error parsing MSG_RECV: {ex.Message}");
            }
        }

        private void HandleMsgSendResp(Packet p)
        {
            try
            {
                // [Success:1] [MsgLen:2] [Msg:Var]
                bool success = p.Reader.ReadByte() == 1;
                
                ushort len = p.Reader.ReadUInt16();
                byte[] bytes = p.Reader.ReadBytes(len * 2);
                string msg = Encoding.Unicode.GetString(bytes);
                
                Debug.Log($"[Messenger] Send Result: {success} - {msg}");
                OnMessageSentResult?.Invoke(success, msg);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Messenger] Error parsing MSG_SEND_RESP: {ex.Message}");
            }
        }
    }
}
