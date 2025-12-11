using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iDate.Network
{
    public class TCPClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private BlowfishCipher _cipher;
        
        // Static Key from RE (needs to be filled with real key bytes)
        // For now using placeholder
        private static readonly byte[] INITIAL_KEY = { 0x01, 0x02, 0x03, 0x04 }; 

        public bool IsConnected => _client != null && _client.Connected;

        public TCPClient()
        {
            _client = new TcpClient();
            _cipher = new BlowfishCipher(INITIAL_KEY);
        }

        public async Task ConnectAsync(string ip, int port)
        {
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
        }

        public async Task SendPacketAsync(Packet packet)
        {
            if (!IsConnected) return;

            // 1. Serialize
            byte[] raw = packet.GetBytes();
            
            // 2. Encrypt (Header + Body? Or Body only? RE indicated full packet or body encryption mostly)
            // Need to verify encryption scope. Assuming Body encryption based on standard patterns.
            // Actually, header usually stays clear for length, but payload is encrypted.
            
            // For now, write raw
            await _stream.WriteAsync(raw, 0, raw.Length);
        }

        public async Task<Packet> ReceivePacketAsync()
        {
            if (!IsConnected) return null;

            byte[] headerBuffer = new byte[8];
            int bytesRead = await _stream.ReadAsync(headerBuffer, 0, 8);
            if (bytesRead < 8) return null;

            // Parse Header
            ushort length = BitConverter.ToUInt16(headerBuffer, 0);
            ushort opcode = BitConverter.ToUInt16(headerBuffer, 2);
            
            // Read Body
            int bodyLen = length - 8;
            byte[] body = new byte[bodyLen];
            if (bodyLen > 0)
            {
                await _stream.ReadAsync(body, 0, bodyLen);
                // Decrypt body here using _cipher
            }

            var packet = new Packet(opcode);
            packet.Payload = body;
            return packet;
        }
    }
}
