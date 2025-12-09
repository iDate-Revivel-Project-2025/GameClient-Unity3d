using System;
using System.IO;
using System.Text;

namespace iDate.Network
{
    public class Packet : IDisposable
    {
        private MemoryStream _ms;
        private BinaryWriter _bw;
        private BinaryReader _br;

        public ushort OpCode { get; private set; }

        // Standard CRC32 Polynomial
        private const uint CRC32_POLY = 0xEDB88320;
        private static uint[] _crcTable;

        static Packet()
        {
            InitializeCRCTable();
        }

        private static void InitializeCRCTable()
        {
            _crcTable = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 8; j > 0; j--)
                {
                    if ((crc & 1) == 1)
                        crc = (crc >> 1) ^ CRC32_POLY;
                    else
                        crc >>= 1;
                }
                _crcTable[i] = crc;
            }
        }

        private static uint ComputeCRC32(byte[] buffer, int offset, int count, uint seed)
        {
            uint crc = ~seed;
            for (int i = offset; i < offset + count; i++)
            {
                byte index = (byte)((crc ^ buffer[i]) & 0xFF);
                crc = (crc >> 8) ^ _crcTable[index];
            }
            return ~crc;
        }

        // Writing mode
        public Packet(ushort opCode)
        {
            OpCode = opCode;
            _ms = new MemoryStream();
            _bw = new BinaryWriter(_ms);
            
            // Header (8 bytes)
            // 0-1: Size
            // 2-3: OpCode
            // 4-5: Res (High CRC)
            // 6-7: Padding (Low CRC / Seq)
            _bw.Write((ushort)0); // Size
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
            _br.ReadUInt16(); // Res
            _br.ReadUInt16(); // Padding
        }

        public void Seal(uint seed, ushort sequence)
        {
            if (_bw == null) return;

            long pos = _ms.Position;
            ushort size = (ushort)pos;

            // 1. Write final Size
            _ms.Position = 0;
            _bw.Write(size);

            // 2. Prepare for CRC: Res=0, Padding=Seq
            _ms.Position = 4;
            _bw.Write((ushort)0);    // Res
            _bw.Write(sequence);     // Padding = Sequence

            // 3. Compute CRC over entire packet
            byte[] bytes = _ms.ToArray(); // Note: inefficient for allocs but safe
            // CRC is computed over the whole buffer (0 to size)
            uint crc = ComputeCRC32(bytes, 0, (int)size, seed);

            // 4. Write CRC into Res/Padding
            _ms.Position = 4;
            _bw.Write((ushort)(crc >> 16)); // High Word -> Res
            _bw.Write((ushort)(crc & 0xFFFF)); // Low Word -> Padding
            
            _ms.Position = pos; // Restore pos
        }

        public void Write(byte value) => _bw?.Write(value);
        public void Write(short value) => _bw?.Write(value);
        public void Write(ushort value) => _bw?.Write(value);
        public void Write(int value) => _bw?.Write(value);
        public void Write(uint value) => _bw?.Write(value);
        public void Write(string value)
        {
             if (_bw == null) return;
             if (string.IsNullOrEmpty(value)) return;
             byte[] bytes = Encoding.Unicode.GetBytes(value);
             _bw.Write(bytes);
        }

        public byte[] GetBytes()
        {
            return _ms?.ToArray();
        }

        public void Dispose()
        {
            _bw?.Dispose();
            _br?.Dispose();
            _ms?.Dispose();
        }
    }
}
