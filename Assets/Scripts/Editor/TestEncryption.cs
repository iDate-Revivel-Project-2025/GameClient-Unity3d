using UnityEngine;
using UnityEditor;
using iDate.Network;
using System.Text;

namespace iDate.Editor
{
    /// <summary>
    /// Test script to verify which encryption algorithm is active
    /// </summary>
    public class TestEncryption
    {
        [MenuItem("iDate/TEST - Check Encryption Algorithm")]
        public static void TestEncryptionAlgorithm()
        {
            Debug.Log("=== TESTING ENCRYPTION ALGORITHM ===");
            
            // Test with "hades"
            string testInput = "hades";
            int bufferSize = 28; // Standard username length
            
            // Encode and encrypt using current CryptoUtils
            byte[] encrypted = CryptoUtils.EncodeString(testInput, bufferSize);
            
            // Show hex dump
            StringBuilder hexDump = new StringBuilder();
            for (int i = 0; i < encrypted.Length && i < 20; i++)
            {
                hexDump.Append(encrypted[i].ToString("x2"));
                if (i < encrypted.Length - 1) hexDump.Append(" ");
            }
            
            Debug.Log($"Input: '{testInput}'");
            Debug.Log($"Encrypted (hex): {hexDump}");
            
            // Decrypt it back
            string decrypted = CryptoUtils.DecodeString(encrypted, 0, bufferSize);
            Debug.Log($"Decrypted: '{decrypted}'");
            
            // Analyze the pattern
            // If using character-swap: "hades" -> should see pattern like "deha..."
            // If using 32-bit block swap: "hades" -> should see pattern like "deha..." but different order
            
            // Check specific bytes to identify algorithm
            string analysis = "";
            if (encrypted.Length >= 10)
            {
                // Check first 10 bytes
                // "hades" in UTF-16LE: 68 00 61 00 64 00 65 00 73 00
                // After char-swap:    64 00 65 00 68 00 61 00 73 00 (dehas)
                // After 32-bit swap:  64 00 65 00 68 00 61 00 73 00 (same as char-swap for this input!)
                
                // Let's check with a different pattern
                if (encrypted[0] == 0x64 && encrypted[2] == 0x65)
                {
                    analysis = "Pattern matches: First chars are 'd' 'e' - encryption is working";
                }
                else if (encrypted[0] == 0x68 && encrypted[2] == 0x61)
                {
                    analysis = "Pattern matches: First chars are 'h' 'a' - NO encryption (or double encryption)";
                }
                else
                {
                    analysis = $"Unexpected pattern: {encrypted[0]:x2} {encrypted[2]:x2}";
                }
            }
            
            Debug.Log($"Analysis: {analysis}");
            Debug.Log("=== END TEST ===");
            
            // Show dialog with results
            EditorUtility.DisplayDialog("Encryption Test Results", 
                $"Input: '{testInput}'\n\n" +
                $"Encrypted (hex): {hexDump}\n\n" +
                $"Decrypted: '{decrypted}'\n\n" +
                $"{analysis}\n\n" +
                "Check Console for full details.", 
                "OK");
        }
        
        [MenuItem("iDate/TEST - Simulate Login Packet")]
        public static void TestLoginPacket()
        {
            Debug.Log("=== SIMULATING LOGIN PACKET ===");
            
            string username = "hades";
            string password = "hades";
            
            const int USERNAME_LEN = 28;
            const int PASSWORD_LEN = 20;
            
            byte[] encryptedUser = CryptoUtils.EncodeString(username, USERNAME_LEN);
            byte[] encryptedPass = CryptoUtils.EncodeString(password, PASSWORD_LEN);
            
            // Show what would be sent
            StringBuilder userHex = new StringBuilder();
            for (int i = 0; i < encryptedUser.Length && i < 20; i++)
            {
                userHex.Append(encryptedUser[i].ToString("x2"));
            }
            
            StringBuilder passHex = new StringBuilder();
            for (int i = 0; i < encryptedPass.Length && i < 20; i++)
            {
                passHex.Append(encryptedPass[i].ToString("x2"));
            }
            
            Debug.Log($"Username '{username}' encrypts to: {userHex}");
            Debug.Log($"Password '{password}' encrypts to: {passHex}");
            
            // Compare with server's expected pattern
            Debug.Log("\nServer is receiving (from logs): 7300000000000000000000000000000064006500000000000000000068006100");
            Debug.Log($"Client would send:                 {userHex}");
            
            bool matches = userHex.ToString().StartsWith("73000000000000000000000000000000640065");
            
            Debug.Log($"\nMatch: {matches}");
            
            if (matches)
            {
                Debug.Log("✓ Client encryption matches server logs!");
            }
            else
            {
                Debug.Log("✗ Client encryption DOES NOT match server logs!");
                Debug.Log("This means Unity HAS recompiled with the new algorithm.");
            }
            
            Debug.Log("=== END SIMULATION ===");
            
            EditorUtility.DisplayDialog("Login Packet Simulation", 
                $"Username: {username}\n" +
                $"Encrypted: {userHex}\n\n" +
                $"Matches server logs: {matches}\n\n" +
                "Check Console for details.", 
                "OK");
        }
    }
}
