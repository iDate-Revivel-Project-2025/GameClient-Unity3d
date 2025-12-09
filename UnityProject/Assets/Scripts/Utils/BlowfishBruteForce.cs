using System;
using System.IO;
using UnityEngine;

public class BlowfishBruteForce : MonoBehaviour
{
    private uint[] P;
    private uint[] S;

    // Known Cipher/Plain Pair for Login.ido wrapping
    // Cipher (offset 32): dac5 a119 1825 c060 -> 19a1c5da 60c02518 (Little Endian)
    // Plain (DDS header): 4444 5320 7c00 0000 -> 20534444 0000007c
    private uint cipherL = 0x19a1c5da;
    private uint cipherR = 0x60c02518;
    private uint plainL = 0x20534444;
    private uint plainR = 0x0000007c;

    void Start()
    {
        LoadTables();
        StartCoroutine(BruteForce());
    }

    void LoadTables()
    {
        TextAsset pAsset = Resources.Load<TextAsset>("p_array");
        TextAsset sAsset = Resources.Load<TextAsset>("s_boxes");

        if (pAsset == null || sAsset == null)
        {
            Debug.LogError("Failed to load tables! Make sure p_array.bytes and s_boxes.bytes are in Resources.");
            return; 
        }

        P = BytesToUInts(pAsset.bytes);
        S = BytesToUInts(sAsset.bytes);
    }

    uint[] BytesToUInts(byte[] bytes)
    {
        uint[] uints = new uint[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, uints, 0, bytes.Length);
        return uints;
    }

    System.Collections.IEnumerator BruteForce()
    {
        Debug.Log("Starting Brute Force...");
        uint seed = 0;
        int batch = 10000;
        
        // Optimize: Local copies to avoid 'this' overhead in tight loop?
        // Allocating Blowfish instance per trial is slow.
        // We need a lightweight 'TrySeed' that modifies a TEMP P array.
        
        uint[] localP = new uint[18];
        uint[] localS = new uint[1024];

        while (seed < uint.MaxValue)
        {
            for (int i = 0; i < batch; i++)
            {
                if (TrySeed(seed, localP, localS))
                {
                    Debug.LogError($"FOUND SEED: 0x{seed:X8}");
                    yield break;
                }
                seed++;
                if (seed == 0) break; // Wrapped
            }
            if (seed % 1000000 == 0)
            {
                 Debug.Log($"Scanned {seed}...");
                 yield return null; // Unfreeze Frame
            }
        }
    }

    private bool TrySeed(uint seed, uint[] tempP, uint[] tempS)
    {
        // 1. KeyGen
        // Logic from sub_51418D
        // Generates 16 byte key (4 uints)
        
        uint k0, k1, k2, k3;
        
        // Indices
        // sizea 0..3 for k0
        // sizea 4..7 for k1 ...
        
        // Unroll loop for speed
        k0 = GetKeyPart(seed, 0);
        k1 = GetKeyPart(seed, 1);
        k2 = GetKeyPart(seed, 2);
        k3 = GetKeyPart(seed, 3);
        
        // 2. SetKey (Init P/S)
        // Copy P/S from master
        Array.Copy(P, tempP, 18);
        Array.Copy(S, tempS, 1024);
        
        // XOR P with Key
        // Key loops (16 bytes = 4 uints? No, key is bytes).
        // P array is 18 UINTS.
        // Key is 16 BYTES. (4 UINTS).
        // XOR logic:
        // P[0] ^= k0
        // P[1] ^= k1
        // P[2] ^= k2
        // P[3] ^= k3
        // P[4] ^= k0 ...
        
        // Careful with Endianness. 
        // My Python script: data = (data << 8) | key[idx].
        // This builds Big Endian UINTs from key bytes.
        // But GetKeyPart returns a UINT (from S-Box which is Host Endian).
        // "memcpy(&Blocka[v4], &Src, v5)".
        // Key buffer is Byte array.
        // If system is Little Endian, 'Src' (int) copied to byte array -> Little Endian bytes.
        // Then 'data' constructed Big Endianly from those bytes.
        // Effectively: Bytes Swap if Little Endian?
        // Let's assume KeyPart returns the correct UINT for P-Array directly? 
        // No, Blowfish standard uses Big Endian Key Bytes to XOR P-Array.
        // But `sub_5140C2` takes `char* key`.
        // Loop: `data = (data << 8) | key[idx]`.
        // The Key Bytes come from S-Boxes.
        // If `memcpy` put `0xAABBCCDD` into `key[0..3]`: `DD CC BB AA` (buffers).
        // Then `data` construction:
        // `(DD<<24) | (CC<<16) | (BB<<8) | AA`.
        // Which is `0xDDCCBBAA`.
        // This is `SWAP_ENDIAN(0xAABBCCDD)`.
        
        // So k0_be = Swap(k0_le).
        
        uint[] key = { Swap(k0), Swap(k1), Swap(k2), Swap(k3) };
        
        for (int i = 0; i < 18; i++)
        {
            tempP[i] ^= key[i % 4];
        }

        // Encrypt P-Array (Init)
        uint L = 0, R = 0;
        for (int i = 0; i < 18; i += 2)
        {
            EncryptBlock(ref L, ref R, tempP, tempS);
            tempP[i] = L;
            tempP[i + 1] = R;
        }

        for (int i = 0; i < 1024; i += 2)
        {
            EncryptBlock(ref L, ref R, tempP, tempS);
            tempS[i] = L;
            tempS[i + 1] = R;
        }

        // 3. Decrypt Cipher Block and Check
        uint dL = cipherL;
        uint dR = cipherR;
        DecryptBlock(ref dL, ref dR, tempP, tempS);
        
        return (dL == plainL && dR == plainR);
    }
    
    private uint GetKeyPart(uint seed, int part) // part 0..3 (for 4 uints)
    {
        // 4 bytes per part.
        // i = part*4 + 0..3
        uint val = 0;
        for (int b = 0; b < 4; b++)
        {
            int sizea = part * 4 + b;
            
            // Indices
            uint block = seed; 
            // v8 indices
            // v8[0] = (Block >> 1) & 3
            // v8[1] = (Block >> 11) & 3
            // v8[2] = (Block >> 21) & 3
            int[] v8 = { (int)((block >> 1) & 3), (int)((block >> 11) & 3), (int)((block >> 21) & 3) };
            int[] v7 = { (int)((block >> 3) & 0xFF), (int)((block >> 13) & 0xFF), (int)((block >> 23) & 0xFF) };
            
            int idx1 = v8[(sizea / 3) % 3];
            int idx2 = v7[sizea % 3];
            
            uint s_val = S[256 * idx1 + idx2];
            
            // s_val is the byte source?
            // "memcpy(&key[...], &s_val, 4)".
            // So s_val is a 4-byte chunk of the key.
            // Wait. My Python script was: key += struct.pack('<I', s_val).
            // It appends 4 bytes.
            // The loop runs 16 times? No.
            // Python Loop: `for sizea in range(16)`.
            // BUT: `if sizea >= 3: break`.
            // It breaks after 3?
            // Wait. "v4 += 4" logic.
            // In C `sub_51418D`:
            // `for ( Sizea = 0; v4 < Size; v4 += 4 )`
            // Size = 16.
            // v4=0 (Sizea=0). v4=4 (Sizea=1). v4=8 (Sizea=2). v4=12 (Sizea=3).
            // v4=16 (Stop).
            // So Sizea runs 0, 1, 2, 3.
            // 4 Iterations.
            // Each iteration copies 4 bytes (1 uint).
            // So Total Key Bytes = 16.
            
            // In my GetKeyPart, I assumed GetKeyPart returns 1 uint.
            // So I just need to call it with part=0,1,2,3.
            // Ah, the inner loop `for (int b=0; b<4)` constructs ONE uint from BYTES?
            // NO. `s_val` IS the UINT key part.
            // One iteration of loop gives ONE UINT.
            
            return s_val; // Mistake in previous thought process found.
        }
        return 0;
    }
    
    // Corrected GetKeyPart
    private uint GetKeyPart(uint seed, int sizea)
    {
         uint block = seed; 
         int[] v8 = { (int)((block >> 1) & 3), (int)((block >> 11) & 3), (int)((block >> 21) & 3) };
         int[] v7 = { (int)((block >> 3) & 0xFF), (int)((block >> 13) & 0xFF), (int)((block >> 23) & 0xFF) };
         
         int idx1 = v8[(sizea / 3) % 3];
         int idx2 = v7[sizea % 3];
         
         return S[256 * idx1 + idx2];
    }
    
    // ... Implement EncryptBlock/DecryptBlock/F/Swap ...
    
    uint Swap(uint v)
    {
        return ((v & 0x000000FF) << 24) |
               ((v & 0x0000FF00) << 8) |
               ((v & 0x00FF0000) >> 8) |
               ((v & 0xFF000000) >> 24);
    }
    
    uint F(uint x, uint[] tempS)
    {
        uint d = x & 0xFF; x >>= 8;
        uint c = x & 0xFF; x >>= 8;
        uint b = x & 0xFF; x >>= 8;
        uint a = x & 0xFF;
        uint y = (tempS[a] + tempS[256 + b]);
        y = y ^ tempS[512 + c];
        y = (y + tempS[768 + d]);
        return y;
    }

    void EncryptBlock(ref uint L, ref uint R, uint[] tempP, uint[] tempS)
    {
        for (int i = 0; i < 16; i++) {
            L ^= tempP[i];
            R ^= F(L, tempS);
            uint t = L; L = R; R = t;
        }
        uint t2 = L; L = R; R = t2;
        R ^= tempP[16];
        L ^= tempP[17];
    }

    void DecryptBlock(ref uint L, ref uint R, uint[] tempP, uint[] tempS)
    {
        for (int i = 17; i > 1; i--) {
            L ^= tempP[i];
            R ^= F(L, tempS);
            uint t = L; L = R; R = t;
        }
        uint t2 = L; L = R; R = t2;
        R ^= tempP[1];
        L ^= tempP[0];
    }
}
