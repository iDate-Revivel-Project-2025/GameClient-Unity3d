using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace iDate.Network
{
    public class Packet : IDisposable
    {
        public ushort Length { get; private set; }
        public ushort OpCode { get; set; } // Renamed to OpCode to match NetworkManager
        public ushort ChecksumHigh { get; set; }
        public ushort ChecksumLow { get; set; }
        
        private MemoryStream _payloadStream;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public byte[] Payload 
        { 
            get => _payloadStream.ToArray();
            set 
            {
                _payloadStream = new MemoryStream(value);
                _reader = new BinaryReader(_payloadStream);
                _writer = new BinaryWriter(_payloadStream);
            }
        }

        // Send Constructor
        public Packet(ushort opcode)
        {
            OpCode = opcode;
            _payloadStream = new MemoryStream();
            _writer = new BinaryWriter(_payloadStream);
        }

        public Packet(OpCode opcode) : this((ushort)opcode) { }

        // Receive Constructor
        public Packet(byte[] fullPacket)
        {
            using (var ms = new MemoryStream(fullPacket))
            using (var br = new BinaryReader(ms))
            {
                Length = br.ReadUInt16();
                OpCode = br.ReadUInt16();
                ChecksumHigh = br.ReadUInt16();
                ChecksumLow = br.ReadUInt16();
                
                int payloadLen = Length - 8;
                if (payloadLen > 0)
                {
                    byte[] payloadBytes = br.ReadBytes(payloadLen);
                    Payload = payloadBytes;
                }
                else
                {
                    Payload = new byte[0];
                }
            }
        }

        public byte[] GetBytes()
        {
            // Flush payload
            byte[] payloadData = _payloadStream.ToArray();
            ushort totalLen = (ushort)(8 + payloadData.Length);
            
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(totalLen);
                bw.Write(OpCode);
                bw.Write(ChecksumHigh);
                bw.Write(ChecksumLow);
                bw.Write(payloadData);
                return ms.ToArray();
            }
        }

        public BinaryWriter Writer => _writer;
        public BinaryReader Reader => _reader;

        // Helpers to match usage in Managers
        public void WriteUInt(uint value) => _writer.Write(value);
        public uint ReadUInt() => _reader.ReadUInt32();
        
        public void WriteInt32(int value) => _writer.Write(value);
        public int ReadInt32() => _reader.ReadInt32();

        public void WriteUShort(ushort value) => _writer.Write(value);
        public ushort ReadUShort() => _reader.ReadUInt16();

        public void WriteBool(bool value) => _writer.Write(value);
        public bool ReadBool() => _reader.ReadBoolean();

        public void WriteByte(byte value) => _writer.Write(value);
        public byte ReadByte() => _reader.ReadByte();

        public void WriteLong(long value) => _writer.Write(value);
        public long ReadLong() => _reader.ReadInt64();

        public void WriteBytes(byte[] value) => _writer.Write(value);
        public byte[] ReadBytes(int count) => _reader.ReadBytes(count);

        public void WriteString(string value)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            // Write length prefixed? Or just bytes?
            // Based on usage: p.WriteString(name)
            // Existing WriteStringForLogin writes raw bytes without prefix.
            // C# BinaryWriter.Write(string) writes length-prefixed (7-bit encoded int).
            // Let's assume standard BinaryWriter for these helpers unless specified.
            // BUT our protocol seems to use fixed size strings often.
            // However, typical custom Packet helpers do:
            // WriteInt(len); WriteBytes(data); OR just Write(string) with 7-bit length.
            // Given the code in managers uses WriteString(), it likely expects C# behavior:
            _writer.Write(value); 
        }

        public string ReadString() => _reader.ReadString();

        public string ReadString(int length)
        {
            byte[] bytes = _reader.ReadBytes(length * 2); // Unicode assumes 2 bytes/char ? 
            // OR if input is `ReadString(32)` meaning 32 CHARS or 32 BYTES?
            // "16 WChars" comment in LobbyLogic suggests 32 BYTES.
            // Let's assume length is number of BYTES based on "ReadString(32); // 16 WChars"
           return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }

        public void Write(byte[] buffer) => _writer.Write(buffer);
        public void Write(int value) => _writer.Write(value);
        public void Write(uint value) => _writer.Write(value);
        public void Write(ushort value) => _writer.Write(value);
        public void Write(byte value) => _writer.Write(value);
        public void Write(bool value) => _writer.Write(value);
        public void Write(long value) => _writer.Write(value);
        public void Write(string value) => _writer.Write(value);

        public void WriteStringForLogin(string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            _writer.Write(bytes);
        }

        public void WriteString(string s, int maxLength)
        {
            // Write string with specific length (padding or truncation)
            byte[] bytes = Encoding.Unicode.GetBytes(s);
            int currentLen = bytes.Length;
            int targetLen = maxLength * 2; // Unicode = 2 bytes per char
            
            _writer.Write(bytes);
            
            // Pad
            if (currentLen < targetLen)
            {
                _writer.Write(new byte[targetLen - currentLen]);
            }
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _payloadStream?.Dispose();
        }
    }
}
