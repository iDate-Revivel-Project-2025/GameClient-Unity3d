using System;
using System.Text;

namespace iDate.Network
{
    /// <summary>
    /// Crypto utilities for iDate packet encryption/decryption
    /// 
    /// The encryption uses two layers:
    /// 1. Blowfish cipher (using custom P-array and S-boxes)
    /// 2. Swap algorithm: Within groups of 4 UTF-16LE characters (8 bytes), swap character pairs:
    ///    - Character 0 <-> Character 2 (bytes 0-1 <-> bytes 4-5)
    ///    - Character 1 <-> Character 3 (bytes 2-3 <-> bytes 6-7)
    /// 
    /// Example: "test" (t(0) e(1) s(2) t(3)) -> "stte" (s(2) t(3) t(0) e(1))
    /// The swap algorithm is symmetric, so encrypt and decrypt use the same function.
    /// </summary>
    public static class CryptoUtils
    {
        /// <summary>
        /// Decrypt/descramble a buffer using the swap algorithm
        /// </summary>
        /// <param name="buf">Input buffer to decrypt</param>
        /// <param name="start">Starting offset in buffer</param>
        /// <param name="length">Length of data to decrypt</param>
        /// <returns>Decrypted buffer</returns>
        public static byte[] DecryptSwap(byte[] buf, int start, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(buf, start, result, 0, length);
            
            // Swap algorithm: In groups of 4 UTF-16LE characters (8 bytes), swap character pairs:
            // Character 0 <-> Character 2, Character 1 <-> Character 3
            // Each UTF-16LE character is 2 bytes
            for (int i = 0; i < length; i += 8)
            {
                if (i + 7 < length)
                {
                    // Save original character pairs (each char is 2 bytes)
                    byte temp0 = result[i];     // Char 0, byte 0
                    byte temp1 = result[i + 1]; // Char 0, byte 1
                    byte temp2 = result[i + 2]; // Char 1, byte 0
                    byte temp3 = result[i + 3]; // Char 1, byte 1
                    
                    // Swap: char 0 <-> char 2, char 1 <-> char 3
                    result[i] = result[i + 4];     // Char 0 <- Char 2, byte 0
                    result[i + 1] = result[i + 5]; // Char 0 <- Char 2, byte 1
                    result[i + 2] = result[i + 6]; // Char 1 <- Char 3, byte 0
                    result[i + 3] = result[i + 7]; // Char 1 <- Char 3, byte 1
                    result[i + 4] = temp0;         // Char 2 <- Char 0, byte 0
                    result[i + 5] = temp1;         // Char 2 <- Char 0, byte 1
                    result[i + 6] = temp2;         // Char 3 <- Char 1, byte 0
                    result[i + 7] = temp3;         // Char 3 <- Char 1, byte 1
                }
            }
            
            return result;
        }

        /// <summary>
        /// Encrypt/scramble a buffer using the swap algorithm (same as decrypt, it's symmetric)
        /// </summary>
        public static byte[] EncryptSwap(byte[] buf, int start, int length)
        {
            // The swap is symmetric, so encrypt and decrypt are the same
            return DecryptSwap(buf, start, length);
        }

        /// <summary>
        /// Encode a string to UTF-16LE and encrypt using swap algorithm
        /// </summary>
        /// <param name="str">String to encode</param>
        /// <param name="maxLen">Maximum buffer length</param>
        /// <returns>Encrypted buffer</returns>
        public static byte[] EncodeString(string str, int maxLen)
        {
            // Convert string to UTF-16LE
            byte[] encoded = Encoding.Unicode.GetBytes(str);
            byte[] buf = new byte[maxLen];
            Array.Clear(buf, 0, maxLen);
            Array.Copy(encoded, 0, buf, 0, Math.Min(encoded.Length, maxLen));
            
            // Encrypt using swap algorithm
            return EncryptSwap(buf, 0, maxLen);
        }

        /// <summary>
        /// Decode a UTF-16LE string from an encrypted buffer
        /// </summary>
        /// <param name="buf">Source buffer</param>
        /// <param name="start">Starting offset</param>
        /// <param name="maxLen">Maximum length to read</param>
        /// <returns>Decoded string</returns>
        public static string DecodeString(byte[] buf, int start, int maxLen)
        {
            // Only use swap algorithm (for login strings)
            byte[] decrypted = DecryptSwap(buf, start, maxLen);
            
            // Convert from UTF-16LE and remove null terminators
            return Encoding.Unicode.GetString(decrypted).Replace("\0", "");
        }
    }
}
