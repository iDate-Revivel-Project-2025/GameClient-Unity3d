using System;

namespace iDate.Network
{
    public class Blowfish
    {
        private uint[] P;
        private uint[,] S;

        public Blowfish(byte[] key)
        {
            P = (uint[])BlowfishConstants.P.Clone();
            S = new uint[4, 256];
            // Copy S-boxes if populated, or Initialize logic
            // Assuming S-Boxes are populated in Constants or we rely on standard Init
            
            Initialize(key);
        }

        private void Initialize(byte[] key)
        {
            int keyLen = key.Length;
            int keyPos = 0;
            uint data = 0;

            for (int i = 0; i < 18; i++)
            {
                data = 0;
                for (int k = 0; k < 4; k++)
                {
                    data = (data << 8) | key[keyPos];
                    keyPos = (keyPos + 1) % keyLen;
                }
                P[i] ^= data;
            }

            uint L = 0, R = 0;
            for (int i = 0; i < 18; i += 2)
            {
                Encipher(ref L, ref R);
                P[i] = L;
                P[i + 1] = R;
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 256; j += 2)
                {
                    Encipher(ref L, ref R);
                    S[i, j] = L;
                    S[i, j + 1] = R;
                }
            }
        }

        private void Encipher(ref uint xl, ref uint xr)
        {
            uint Xl = xl;
            uint Xr = xr;
            
            for (int i = 0; i < 16; i++)
            {
                Xl ^= P[i];
                Xr ^= F(Xl);
                uint temp = Xl; Xl = Xr; Xr = temp;
            }
            uint temp2 = Xl; Xl = Xr; Xr = temp2;
            Xr ^= P[16];
            Xl ^= P[17];
            xl = Xl;
            xr = Xr;
        }

        private void Decipher(ref uint xl, ref uint xr)
        {
            uint Xl = xl;
            uint Xr = xr;
            
            Xl ^= P[17];
            Xr ^= P[16];
            uint temp = Xl; Xl = Xr; Xr = temp;

            for (int i = 15; i >= 0; i--)
            {
                uint temp2 = Xl; Xl = Xr; Xr = temp2;
                Xl ^= P[i];
                Xr ^= F(Xl);
            }
            xl = Xl;
            xr = Xr;
        }
        
        public byte[] Encrypt(byte[] data)
        {
            return Transform(data, true);
        }
        
        public byte[] Decrypt(byte[] data)
        {
            return Transform(data, false);
        }

        private byte[] Transform(byte[] data, bool encrypt)
        {
            int length = data.Length;
            int paddedLength = (length + 7) / 8 * 8;
            byte[] result = new byte[paddedLength];
            Array.Copy(data, result, length);

            for (int i = 0; i < paddedLength; i += 8)
            {
                uint xl = (uint)((result[i] << 24) | (result[i+1] << 16) | (result[i+2] << 8) | result[i+3]);
                uint xr = (uint)((result[i+4] << 24) | (result[i+5] << 16) | (result[i+6] << 8) | result[i+7]);
                
                if (encrypt) Encipher(ref xl, ref xr);
                else Decipher(ref xl, ref xr);
                
                result[i] = (byte)(xl >> 24); result[i+1] = (byte)(xl >> 16); result[i+2] = (byte)(xl >> 8); result[i+3] = (byte)xl;
                result[i+4] = (byte)(xr >> 24); result[i+5] = (byte)(xr >> 16); result[i+6] = (byte)(xr >> 8); result[i+7] = (byte)xr;
            }
            return result;
        }

        private uint F(uint x)
        {
           ushort a = (ushort)(x >> 24);
           ushort b = (ushort)((x >> 16) & 0xFF);
           ushort c = (ushort)((x >> 8) & 0xFF);
           ushort d = (ushort)(x & 0xFF);
           return ((S[0, a] + S[1, b]) ^ S[2, c]) + S[3, d];
        }
    }
}
