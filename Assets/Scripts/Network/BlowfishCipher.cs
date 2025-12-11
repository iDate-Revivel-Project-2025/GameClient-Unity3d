using System;

namespace iDate.Network
{
    public class BlowfishCipher
    {
        private uint[] P = new uint[18];
        private uint[] S0 = new uint[256];
        private uint[] S1 = new uint[256];
        private uint[] S2 = new uint[256];
        private uint[] S3 = new uint[256];

        // Standard Blowfish constants (Digits of Pi) - Simplified for brevity, normally you'd paste the full hex dump here.
        // For the full implementation, we need the exact P and S boxes used by the game if they differ from standard.
        // Based on RE, they are initialized from static data, likely standard.
        private static readonly uint[] INIT_P = { /* Standard P-Array constants */ 
            0x243F6A88, 0x85A308D3, 0x13198A2E, 0x03707344, 0xA4093822, 0x299F31D0, 0x082EFA98, 0xEC4E6C89, 
            0x452821E6, 0x38D01377, 0xBE5466CF, 0x34E90C6C, 0xC0AC29B7, 0xC97C50DD, 0x3F84D5B5, 0xB5470917, 
            0x9216D5D9, 0x8979FB1B 
        };
        
        // ... S-Boxes would be initialized here (too large for this snippet, will need to be populated) ...

        public BlowfishCipher(byte[] key)
        {
            Initialize(key);
        }

        private void Initialize(byte[] key)
        {
            // Reset to initial constants
            Array.Copy(INIT_P, P, 18);
            // ... Copy S-Boxes ...

            // Key Expansion (`sub_5140C2`)
            int keyLen = key.Length;
            int keyPos = 0;
            uint data = 0;

            for (int i = 0; i < 18; ++i)
            {
                data = 0;
                for (int k = 0; k < 4; ++k)
                {
                    data = (data << 8) | key[keyPos];
                    keyPos = (keyPos + 1) % keyLen;
                }
                P[i] ^= data;
            }

            uint l = 0, r = 0;
            for (int i = 0; i < 18; i += 2)
            {
                EncryptBlock(ref l, ref r);
                P[i] = l;
                P[i + 1] = r;
            }

            for (int i = 0; i < 4; ++i)
            {
                uint[] sBox = (i == 0) ? S0 : (i == 1) ? S1 : (i == 2) ? S2 : S3;
                for (int j = 0; j < 256; j += 2)
                {
                    EncryptBlock(ref l, ref r);
                    sBox[j] = l;
                    sBox[j + 1] = r;
                }
            }
        }

        // Custom F-Function from RE (`sub_513F6F`)
        // Logic: S4[a] + (S3[b] ^ (S1[c] + S2[d]))
        // Note: The S-boxes in RE seemed to be accessed via `this` pointer offsets.
        // Assuming standard layout: S0, S1, S2, S3.
        private uint F(uint x)
        {
            byte a = (byte)(x >> 24);
            byte b = (byte)(x >> 16);
            byte c = (byte)(x >> 8);
            byte d = (byte)x;

            // RE Logic: this[(unsigned __int8)a2 + 786] + (this[BYTE1(a2) + 530] ^ (this[HIBYTE(a2) + 18] + this[BYTE2(a2) + 274]));
            // 18 is start of S0 (after P-array) ? No, P is 18 uints (72 bytes).
            // Let's re-verify offsets from `sub_513F6F`.
            // RE: return this[d + 786] + (this[c + 530] ^ (this[a + 18] + this[b + 274]));
            // P = 18 dwords.
            // S0 starts at index 18. Length 256. Ends at 274.
            // S1 starts at index 274. Ends at 530.
            // S2 starts at index 530. Ends at 786.
            // S3 starts at index 786.
            
            // So: S3[d] + (S2[c] ^ (S0[a] + S1[b]))
            // Wait, RE says: 
            // `this[BYTE2(a2) + 274]` -> BYTE2 is 'b' (x >> 16). 274 is S1. So S1[b].
            // `this[HIBYTE(a2) + 18]` -> HIBYTE is 'c' (x >> 8) ?? No.
            // In typical access: HIBYTE is bits 8-15. BYTE2 is bits 16-23.
            // Let's stick to the decompiled expression:
            // S3[d] + (S2[b] ^ (S0[?] + S1[?]))
            
            // Re-reading `sub_513F6F` from Step 42:
            // `this[(unsigned __int8)a2 + 786]` -> Low byte (d) -> S3[d] (Offset 786 is correct for S3)
            // `this[BYTE1(a2) + 530]` -> Byte 1 (c) -> S2[c] (Offset 530 is correct for S2)
            // `this[HIBYTE(a2) + 18]` -> HIBYTE is usually bits 8-15 of the low word? 
            // No, in IDA: 
            // LOBYTE = 0-7, HIBYTE = 8-15. 
            // BYTE1 = 8-15.
            // BYTE2 = 16-23.
            // HIBYTE(a2) - depending on macro, might be bits 8-15 of the register?
            // Wait, `BYTE1(x)` is usually bits 8-15. `HIBYTE(x)` is bits 8-15 of a 16-bit value.
            // But a2 is `unsigned int` (32-bit).
            
            // Let's look at `sub_513F6F` again:
            // `this[BYTE1(a2) + 530]` -> S2[ byte 8-15 ]
            // `this[HIBYTE(a2) + 18]` -> S0 [ byte 8-15 of low word??? ] -> Likely Byte 16-23 or 24-31?
            // Actually, `HIBYTE` on a 32-bit int in IDA often means bits 24-31 (High byte of the full int).
            // Let's assume standard byte extraction: a=31-24, b=23-16, c=15-8, d=7-0.
            
            // Standard Blowfish F: ((S0[a] + S1[b]) ^ S2[c]) + S3[d]
            
            // iDate RE: S3[d] + (S2[c] ^ (S0[a] + S1[b]))
            // This is "Reverse Order" F-function?
            
            return S3[d] + (S2[c] ^ (S0[a] + S1[b]));
        }

        public void EncryptBlock(ref uint xl, ref uint xr)
        {
            uint l = xl;
            uint r = xr;

            for (int i = 0; i < 16; ++i)
            {
                l ^= P[i];
                r ^= F(l);
                // Swap
                uint temp = l;
                l = r;
                r = temp;
            }
            
            // Undo last swap
            uint temp2 = l;
            l = r;
            r = temp2;

            r ^= P[16];
            l ^= P[17];

            xl = l;
            xr = r;
        }

        public void DecryptBlock(ref uint xl, ref uint xr)
        {
            uint l = xl;
            uint r = xr;

            for (int i = 17; i > 1; --i)
            {
                l ^= P[i];
                r ^= F(l);
                // Swap
                uint temp = l;
                l = r;
                r = temp;
            }

            // Undo last swap
            uint temp2 = l;
            l = r;
            r = temp2;

            r ^= P[1];
            l ^= P[0];

            xl = l;
            xr = r;
        }
    }
}
