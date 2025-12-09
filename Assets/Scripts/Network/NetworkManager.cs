using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

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
            // Log for debug
             if (p == null) return;
             byte[] bytes = p.GetBytes();
             Debug.Log($"RX: Op={(OpCode)p.OpCode} Size={(bytes != null ? bytes.Length : 0)}");

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
                // Add more handlers
            }

            OnPacketReceived?.Invoke(p);
        }

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
            // According to discovered protocol:
            // "testuser" -> "stteerus" (Swap i, i+2 logic)
            // Packet is OP.ENCRYPTED (0x042E)
            // Payload: [ScrambledUsername] [ScrambledPassword]
            
            string scrUser = CryptoHelper.Scramble(username);
            string scrPass = CryptoHelper.Scramble(password);
            
            Debug.Log($"Login Encrypt: {username}->{scrUser}, {password}->{scrPass}");
            
            using (Packet p = new Packet((ushort)OpCode.ENCRYPTED))
            {
                // We use WriteStringForLogin to append the raw unicode bytes without length prefixes
                // (Assuming the server parses by fixed offset or expects raw stream)
                // Note: The provided emulator ignores the body, so this is safe to add.
                // For a real server, we might need to pad to specific lengths (e.g. 14 chars for user, 10 for pass).
                // Let's assume standardized 40 bytes or similar if it fails.
                // But for now, let's write them.
                
                // WARNING: Standard C# BinaryWriter length-prefixes strings. We implemented WriteStringForLogin to avoid that.
                p.WriteStringForLogin(scrUser);
                p.WriteStringForLogin(scrPass);
                
                Send(p);
            }
        }
    }
}
