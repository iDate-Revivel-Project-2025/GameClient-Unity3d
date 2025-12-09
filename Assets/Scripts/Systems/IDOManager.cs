using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace iDate.Assets
{
    public class IDOManager : MonoBehaviour
    {
        public static IDOManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public byte[] LoadAsset(string path)
        {
            // Handle VFS Mapping
            string realPath = ResolvePath(path);
            
            if (!File.Exists(realPath))
            {
                Debug.LogError($"IDOManager: File not found {realPath}");
                return null;
            }

            return ParseIDO(realPath);
        }

        public string LoadXML(string path)
        {
            byte[] data = LoadAsset(path);
            if (data == null) return null;

            // Try EUC-KR (Korean) decoding, fallback to UTF-8
            // In modern .NET Core/Unity, getting EUC-KR requires System.Text.Encoding.CodePages
            // For now, we assume simple ASCII or try UTF-8
            return Encoding.UTF8.GetString(data); 
        }

        private string ResolvePath(string virtualPath)
        {
            // Map .xml -> .ido
            // Map root paths to GameClient/Media
            // This logic mimics the game's VFS
            string basePath = Path.Combine(Application.dataPath, "../../GameClient/Media");
            
            if (virtualPath.EndsWith(".xml"))
            {
                virtualPath = virtualPath.Replace(".xml", ".ido");
            }
            
            // Should add recursive search or specific folder mapping here
            // For demo, assume full relative path is passed or we brute force search
            return Path.Combine(basePath, virtualPath);
        }

        private byte[] ParseIDO(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // 1. Read Fixed Header (95 bytes)
                byte[] header = br.ReadBytes(0x5F);
                
                // 2. Read Compressed Data
                byte[] compressed = br.ReadBytes((int)(fs.Length - 0x5F));

                // 3. Decompress (Zlib)
                return DecompressZlib(compressed);
            }
        }

        private byte[] DecompressZlib(byte[] data)
        {
            // Zlib (RFC 1950) has a 2-byte header usually (e.g. 78 9C)
            // C# DeflateStream expects Raw Deflate (RFC 1951)
            // We skip the first 2 bytes to make it work with DeflateStream
            
            if (data == null || data.Length < 2) return null;

            using (MemoryStream ms = new MemoryStream(data, 2, data.Length - 2))
            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
            using (MemoryStream outMs = new MemoryStream())
            {
                ds.CopyTo(outMs);
                return outMs.ToArray();
            }
        }
    }
}
