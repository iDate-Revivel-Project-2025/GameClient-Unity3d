using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace iDate.Network
{
    public class Packet : IDisposable
    {
        private MemoryStream _ms;
        private BinaryWriter _bw;
        private BinaryReader _br; // For reading

        public ushort OpCode { get; private set; }

        // Writing mode
        public Packet(ushort opCode)
        {
            OpCode = opCode;
            _ms = new MemoryStream();
            _bw = new BinaryWriter(_ms);
            
            // Placeholder for Header (8 bytes)
            // 0-1: Size
            // 2-3: OpCode
            // 4-5: Res
            // 6-7: Padding
            _bw.Write((ushort)0); // Size (to be filled later)
            _bw.Write(opCode);
            _bw.Write((ushort)0); // Res
            _bw.Write((ushort)0); // Padding
        }

        // Reading mode
        public Packet(byte[] data)
        {
            _ms = new MemoryStream(data);
            _br = new BinaryReader(_ms);
            
            ushort size = _br.ReadUInt16();
            OpCode = _br.ReadUInt16();
            ushort res = _br.ReadUInt16();
            ushort padding = _br.ReadUInt16();
        }

        public void Write(byte value) => _bw.Write(value);
        public void Write(short value) => _bw.Write(value);
        public void Write(ushort value) => _bw.Write(value);
        public void Write(int value) => _bw.Write(value);
        public void Write(uint value) => _bw.Write(value);
        public void Write(float value) => _bw.Write(value);
        public void Write(long value) => _bw.Write(value);
        public void Write(ulong value) => _bw.Write(value);
        public void Write(byte[] value) => _bw.Write(value);

        // Helper methods for common types
        public void WriteInt32(int value) => _bw.Write(value);
        public void WriteBool(bool value) => _bw.Write(value);

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _bw.Write((ushort)0);
                return;
            }
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            // Protocol usually sends Length (in characters?) or Bytes?
            // "readWString" in JS: converts buffer to string.
            // "writeWString": writes fixed length usually. 
            // BUT for login, we might need variable length?
            // Let's assume standard [UShort Len] [Chars] if not fixed.
            // Actually, based on User input: "this is what get sent to the server".
            // It's likely just appended.
            // To be safe, let's stick to the existing WriteString(val, max) for fixed fields,
            // and add a WriteStringVariable(val) if needed.
            
            // Re-implementing existing fixed writer first to ensure it's correct
        }

        public void WriteString(string value, int maxChars)
        {
            byte[] stringBytes = Encoding.Unicode.GetBytes(value);
            int copyBytes = Math.Min(stringBytes.Length, maxChars * 2);
            
            byte[] buffer = new byte[maxChars * 2]; 
            Array.Copy(stringBytes, buffer, copyBytes);
            
            _bw.Write(buffer);
        }
            
        public void WriteStringForLogin(string value)
        {
            // Login packet specifically seems to just append the scrambled string?
            // User says "this is what get sent to ...".
            // If the Emulator uses `readWString`, it expects a fixed buffer.
            // But if we are sending raw, we might just need to write bytes.
            // Let's implement WriteBytes directly for scrambled strings if needed.
            if (string.IsNullOrEmpty(value)) return;
            
            byte[] bytes = Encoding.ASCII.GetBytes(value); // Scrambled string is ASCII-compatible? 
            // Wait, "testuser" -> "stteerus". 
            // If input is ASCII, output is ASCII.
            // Game uses Unicode (UTF-16LE) usually.
            // "readWString" reads 2 bytes per char.
            // Let's write Unicode.
            byte[] unicodeBytes = Encoding.Unicode.GetBytes(value);
            _bw.Write(unicodeBytes);
        }

        public byte ReadByte() => _br.ReadByte();
        public short ReadShort() => _br.ReadInt16();
        public ushort ReadUShort() => _br.ReadUInt16();
        public int ReadInt() => _br.ReadInt32();
        public uint ReadUInt() => _br.ReadUInt32();
        public float ReadFloat() => _br.ReadSingle();
        public long ReadLong() => _br.ReadInt64();
        public ulong ReadULong() => _br.ReadUInt64();
        public byte[] ReadBytes(int count) => _br.ReadBytes(count);

        public string ReadString(int maxChars)
        {
             // Strings are fixed length buffers in this protocol
            byte[] buffer = _br.ReadBytes(maxChars * 2);
            // Find null terminator (0x00 0x00)
            int nullIndex = -1;
            for (int i = 0; i < buffer.Length; i += 2)
            {
                if (buffer[i] == 0 && buffer[i+1] == 0)
                {
                    nullIndex = i;
                    break;
                }
            }
            
            int len = nullIndex != -1 ? nullIndex : buffer.Length;
            return Encoding.Unicode.GetString(buffer, 0, len);
        }

        public byte[] GetBytes()
        {
            // If writing mode
            if (_bw != null)
            {
                long currentPos = _ms.Position;
                ushort size = (ushort)currentPos;
                
                _ms.Position = 0;
                _bw.Write(size);
                _ms.Position = currentPos;
                
                return _ms.ToArray();
            }
            // If reading mode
            else if (_ms != null)
            {
                return _ms.ToArray();
            }
            return null;
        }

        public void Dispose()
        {
            _bw?.Dispose();
            _br?.Dispose();
            _ms?.Dispose();
        }
    }
}
