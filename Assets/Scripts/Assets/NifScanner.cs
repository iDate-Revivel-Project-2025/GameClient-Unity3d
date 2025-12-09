using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Assets.Scripts.Assets
{
    public class NifScanner : MonoBehaviour
    {
        public string samplePath = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Charactor/Skin/F01_AC_BAG1_0039_00.gb3";

        [ContextMenu("Scan GB3 Structure")]
        public void Scan()
        {
             if (!File.Exists(samplePath)) return;
             byte[] data = File.ReadAllBytes(samplePath);

             Debug.Log($"--- Scanning {Path.GetFileName(samplePath)} ---");

             using (MemoryStream ms = new MemoryStream(data))
             using (BinaryReader br = new BinaryReader(ms))
             {
                 // Read internal name
                 int nameLen = br.ReadInt32();
                 if (nameLen > 0 && nameLen < 256)
                 {
                     string name = Encoding.ASCII.GetString(br.ReadBytes(nameLen)).TrimEnd('\0');
                     Debug.Log($"Header Name: {name}");
                 }

                 // Now blindly look for NIF Magic "NetImmerse File Format" or similar
                 // Or "Gamebryo File Format"
                 // Or common NIF blocks like "NiNode", "NiTriShape"
                 
                 ScanForKeywords(data, new string[] { "NiNode", "NiTriShape", "NiTriStrips", "NiMaterialProperty", "Bip01" });
             }
        }

        void ScanForKeywords(byte[] data, string[] keywords)
        {
            string content = Encoding.ASCII.GetString(data);
            foreach(var kw in keywords)
            {
                int idx = content.IndexOf(kw);
                if (idx != -1)
                {
                    Debug.Log($"Found '{kw}' at offset {idx:X}");
                }
            }
        }
    }
}
