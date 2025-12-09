using System;
using System.Net.Sockets;
using UnityEngine;

namespace iDate.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        private TcpClient _client;
        private NetworkStream _stream;
        private string _ip = "127.0.0.1";
        private int _port = 10001; // Default from analysis? Need to verify port.

        // Crypto State
        private uint _seed = 0; // Default seed
        private ushort _sequence = 0;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            DontDestroyOnLoad(gameObject);
        }

        public void Connect()
        {
            try
            {
                Debug.Log($"Connecting to {_ip}:{_port}...");
                _client = new TcpClient();
                _client.Connect(_ip, _port);
                _stream = _client.GetStream();
                
                _sequence = 0; // Reset sequence on connect
                _seed = 0; // Reset seed
                
                Debug.Log("Connected!");

                // Send Handshake? (OpCode 2000)
                SendConnectPacket();
            }
            catch (Exception e)
            {
                Debug.LogError($"Connection failed: {e.Message}");
            }
        }

        private void SendConnectPacket()
        {
            // OpCode 2000: ConnectServer
            using (var packet = new Packet(2000))
            {
                // Body is Empty?
                // sub_4058F0 doesn't write body, just sends header.
                Send(packet);
            }
        }

        public void Send(Packet packet)
        {
            if (_client == null || !_client.Connected)
            {
                Debug.LogWarning("Cannot send: Not connected.");
                return;
            }

            // 1. Increment Seq
            _sequence++;

            // 2. Seal Packet (CRC)
            packet.Seal(_seed, _sequence);

            // 3. Send Bytes
            byte[] data = packet.GetBytes();
            _stream.Write(data, 0, data.Length);
            
            Debug.Log($"Sent Packet Op:{packet.OpCode} Size:{data.Length} Seq:{_sequence}");
        }

        void OnApplicationQuit()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}
