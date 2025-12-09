using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Assets
{
    public class IDOLoader : MonoBehaviour
    {
        // Based on 'idoc' Rust tool: Header is 0x5F (95) bytes.
        private const int HEADER_SIZE = 0x5F;

        public static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"File not found: {path}");
                return null;
            }

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length <= HEADER_SIZE) 
                    {
                        Debug.LogError($"File too small: {path}");
                        return null;
                    }

                    // 1. Read/Skip Header
                    byte[] header = new byte[HEADER_SIZE];
                    fs.Read(header, 0, HEADER_SIZE);

                    // Debug: Print header to see if it makes sense
                    // string headerStr = Encoding.ASCII.GetString(header);
                    // Debug.Log($"IDO Header: {headerStr}");

                    // 2. Read the rest (Zlib Payload)
                    byte[] payload = new byte[fs.Length - HEADER_SIZE];
                    fs.Read(payload, 0, payload.Length);

                    // 3. Decompress (Zlib -> Raw)
                    byte[] decompressed = DecompressZlib(payload);

                    if (decompressed == null || decompressed.Length == 0)
                    {
                        Debug.LogError("Failed to decompress IDO payload.");
                        return null;
                    }

            // 4. Detect Type & Load
            if (decompressed.Length > 4)
            {
                Debug.Log($"IDO Decompressed Magic: {decompressed[0]:X2} {decompressed[1]:X2} {decompressed[2]:X2} {decompressed[3]:X2}");
            }
            return CreateTextureFromData(decompressed, Path.GetFileName(path));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IDO Load Error: {ex.Message}");
                return null;
            }
        }

        public static byte[] LoadRaw(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"File not found: {path}");
                return null;
            }

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (fs.Length <= HEADER_SIZE) 
                    {
                        Debug.LogError($"File too small: {path}");
                        return null;
                    }

                    // 1. Read/Skip Header
                    fs.Seek(HEADER_SIZE, SeekOrigin.Begin);

                    // 2. Read the rest (Zlib Payload)
                    byte[] payload = new byte[fs.Length - HEADER_SIZE];
                    fs.Read(payload, 0, payload.Length);

                    // 3. Decompress (Zlib -> Raw)
                    return DecompressZlib(payload);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IDO Load Error: {ex.Message}");
                return null;
            }
        }

        private static byte[] DecompressZlib(byte[] data)
        {
            // Zlib format (RFC 1950) = 2 bytes header + Deflate stream + 4 bytes ADLER32.
            // .NET DeflateStream expects raw Deflate (RFC 1951).
            // We usually just skip the first 2 bytes.
            
            if (data == null || data.Length < 2) return null;

            try
            {
                // Basic Zlib header check (CMF + FLG)
                // CMF: 0x78 (Standard)
                // FLG: 0x01, 0x9C, 0xDA (Compression levels)
                // Common: 78 9C
                
                // Skip 2 bytes
                using (MemoryStream inStream = new MemoryStream(data, 2, data.Length - 2))
                using (DeflateStream deflate = new DeflateStream(inStream, CompressionMode.Decompress))
                using (MemoryStream outStream = new MemoryStream())
                {
                    deflate.CopyTo(outStream);
                    return outStream.ToArray();
                }
            }
            catch
            {
                // Fallback or error
                return null;
            }
        }

        private static Texture2D CreateTextureFromData(byte[] data, string debugName)
        {
            // Check Magic Bytes
            if (data.Length > 4)
            {
                if (data[0] == 'D' && data[1] == 'D' && data[2] == 'S' && data[3] == ' ')
                {
                    Texture2D ddsTex = DDSLoader.Load(data);
                    if (ddsTex == null) Debug.LogError("DDSLoader returned null!");
                    return ddsTex;
                }
                else if (data[0] == 'B' && data[1] == 'M')
                {
                    // BMP
                    // LoadImage might support BMP? usually only PNG/JPG.
                    Debug.Log($"BMP Texture detected for {debugName}.");
                }
                else if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                {
                    // PNG
                    Debug.Log($"PNG Texture detected for {debugName}.");
                }
                // TGA footer check?
            }

            // Try generic LoadImage (Works for PNG/JPG, maybe TGA/BMP depending on version)
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(data))
            {
                Debug.Log($"Successfully loaded texture: {debugName} ({tex.width}x{tex.height})");
                return tex;
            }
            else
            {
                Debug.LogError($"Failed to LoadImage for {debugName}. Header: {BitConverter.ToString(data, 0, Math.Min(16, data.Length))}");
                return null;
            }
        }
    }
}
