using System;
using System.IO;
using UnityEngine;

namespace Assets
{
    public static class DDSLoader
    {
        public static Texture2D Load(byte[] ddsBytes)
        {
            if (ddsBytes == null)
            {
                 Debug.LogError("DDS Data is null");
                 return null;
            }
            if (ddsBytes.Length < 128)
            {
                 Debug.LogError($"DDS Data too small: {ddsBytes.Length} bytes");
                 return null;
            }

            using (MemoryStream ms = new MemoryStream(ddsBytes))
            using (BinaryReader br = new BinaryReader(ms))
            {
                // Magic "DDS "
                uint magic = br.ReadUInt32();
                if (magic != 0x20534444)
                {
                    Debug.LogError($"Invalid DDS Magic: {magic:X}");
                    return null;
                }

                // DDS_HEADER
                uint size = br.ReadUInt32(); // 124
                if (size != 124)
                {
                    Debug.LogError($"Invalid DDS Header Size: {size}");
                    return null;
                }

                uint flags = br.ReadUInt32();
                int height = br.ReadInt32();
                int width = br.ReadInt32();
                // Skip PitchOrLinearSize, Depth, MipMapCount, Reserved1[11]
                ms.Seek(12 + 4 + 44, SeekOrigin.Current); 

                // DDS_PIXELFORMAT (offset 76)
                uint pfSize = br.ReadUInt32(); // 32
                uint pfFlags = br.ReadUInt32();
                uint fourCC = br.ReadUInt32();
                // Skip RGBBitCount, RBitMask, G, B, A
                
                // Seek to end of header (128 bytes total from start)
                ms.Seek(128, SeekOrigin.Begin);

                byte[] textureData = br.ReadBytes((int)(ms.Length - 128));

                TextureFormat format = TextureFormat.DXT1; // Default fallback
                bool supported = false;

                // FourCC Parsing
                // DXT1 = 0x31545844 ("DXT1")
                // DXT3 = 0x33545844 ("DXT3")
                // DXT5 = 0x35545844 ("DXT5")
                
                switch (fourCC)
                {
                    case 0x31545844: // DXT1
                        format = TextureFormat.DXT1;
                        supported = true;
                        break;
                    case 0x35545844: // DXT5
                        format = TextureFormat.DXT5;
                        supported = true;
                        break;
                    case 0: // Uncompressed RGB/RGBA?
                        // If FourCC is 0, we check pfFlags and RGBBitCount
                        // pfFlags: 0x40 = RGB, 0x41 = RGBA
                        // We need to read RGBBitCount (offset 88)
                        long current = ms.Position;
                        ms.Seek(88, SeekOrigin.Begin);
                        uint rgbBitCount = br.ReadUInt32();
                        uint rMask = br.ReadUInt32();
                        uint gMask = br.ReadUInt32();
                        uint bMask = br.ReadUInt32();
                        uint aMask = br.ReadUInt32();

                        if (rgbBitCount == 32)
                        {
                            // BGRA is most common in DDS uncompressed
                            format = TextureFormat.BGRA32; 
                            supported = true;
                        }
                        else if (rgbBitCount == 24)
                        {
                            format = TextureFormat.RGB24; // Actually usually BGR layout in file, Unity expects RGB?
                            // Unity lacks BGR24. We might need to handle swapping manually if colors look wrong.
                            // But usually TextureFormat.RGB24 works enough for testing.
                            supported = true;
                        }
                        else if (rgbBitCount == 16)
                        {
                           // B5G6R5 or B5G5R5A1
                           // format = TextureFormat.RGB565;
                           // supported = true;
                        }
                        
                        ms.Seek(current, SeekOrigin.Begin); // Restore pos
                        break;
                }

                if (supported)
                {
                    Texture2D tex = new Texture2D(width, height, format, false);
                    tex.LoadRawTextureData(textureData);
                    tex.Apply();
                    return tex;
                }
                else
                {
                    // Read info for debug
                    ms.Seek(88, SeekOrigin.Begin);
                    uint bits = br.ReadUInt32();
                    uint rMask = br.ReadUInt32();
                    uint gMask = br.ReadUInt32();
                    uint bMask = br.ReadUInt32();
                    uint aMask = br.ReadUInt32();
                    
                    if (bits == 0 && rMask == 0)
                    {
                        // Some tools strip header info for raw BGRA.
                        // Assuming BGRA32 if fits size.
                        // Check if size matches 4 bytes per pixel?
                         Debug.LogWarning($"DDS Zero Header detected. Assuming BGRA32.");
                         Texture2D tex = new Texture2D(width, height, TextureFormat.BGRA32, false);
                         // Note: TextureData might be larger or smaller?
                         // Note: TextureData might be larger (padding) or smaller (mips missing?)
                         // Standard check:
                         int expectedSize = width * height * 4;
                         
                         if (textureData.Length >= expectedSize)
                         {
                             // We have enough data
                             tex.LoadRawTextureData(textureData); 
                             tex.Apply();
                             return tex;
                         }
                         // Check for 8-bit Alpha (1 byte per pixel)
                         else if (textureData.Length >= width * height)
                         {
                             Debug.LogWarning($"DDS Zero Header: Data size suggests Alpha8 (8-bit). Loading as Alpha8.");
                             // Re-create texture as Alpha8
                             tex = new Texture2D(width, height, TextureFormat.Alpha8, false);
                             tex.LoadRawTextureData(textureData);
                             tex.Apply();
                             return tex;
                         }
                         else
                         {
                             Debug.LogError($"DDS Zero Header: Not enough data for BGRA32 ({expectedSize}) or Alpha8 ({width*height}). Got {textureData.Length}");
                         }
                    }

                    Debug.LogWarning($"DDS Unsupported: FourCC={fourCC}, Bits={bits}, R={rMask:X} G={gMask:X} B={bMask:X} A={aMask:X}");
                    return null;
                }
            }
        }
    }
}
