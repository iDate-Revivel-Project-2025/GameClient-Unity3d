using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Assets
{
    public class GB3Inspector : MonoBehaviour
    {
        // Path to a sample GB3 file
        public string gb3Path = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Charactor/Skin/F01_AC_BAG1_0001_00.gb3";

        [ContextMenu("Analyze GB3")]
        public void Analyze()
        {
            if (!File.Exists(gb3Path))
            {
                Debug.LogError($"File not found: {gb3Path}");
                return;
            }

            byte[] data = File.ReadAllBytes(gb3Path);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Analyzing {Path.GetFileName(gb3Path)} ({data.Length} bytes)");

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                // 1. Read Header String (Length-prefixed?)
                try 
                {
                    int len = br.ReadInt32();
                    if (len > 0 && len < 256)
                    {
                        byte[] nameBytes = br.ReadBytes(len);
                        string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                        sb.AppendLine($"Internal Name: {name}");
                    }
                    
                    // Scanning for 'Bip01' or bone names
                    ScanForStrings(data, sb);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Read Error: {ex.Message}");
                }
            }

            Debug.Log(sb.ToString());
        }

        private void ScanForStrings(byte[] data, StringBuilder sb)
        {
             sb.AppendLine("--- String Scan ---");
             int minLen = 4;
             int maxLen = 64;
             
             int currentLen = 0;
             long startPos = 0;

             for(int i=0; i<data.Length; i++)
             {
                 char c = (char)data[i];
                 if (IsValidChar(c))
                 {
                     if (currentLen == 0) startPos = i;
                     currentLen++;
                 }
                 else
                 {
                     if (currentLen >= minLen && currentLen <= maxLen)
                     {
                         string s = Encoding.ASCII.GetString(data, (int)startPos, currentLen);
                         // Filter for relevant node names
                         if (s.StartsWith("Bip") || s.Contains("Mesh") || s.Contains("Skin") || s.Contains("f01"))
                         {
                             sb.AppendLine($"[{startPos:X}] {s}");
                         }
                     }
                     currentLen = 0;
                 }
             }
        }

        private bool IsValidChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || c == ' ';
        }
    }
}
