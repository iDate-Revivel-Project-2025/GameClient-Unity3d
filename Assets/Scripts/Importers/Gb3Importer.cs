using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace iDate.Assets
{
    public class Gb3Importer
    {
        // ... (Previous Classes) ...
        
        // Standard D3DFVF_XYZ | D3DFVF_NORMAL | D3DFVF_TEX1
        // 12 + 12 + 8 = 32 bytes per vertex
        private const int VERTEX_STRIDE = 32;

        public static GameObject LoadModel(string path)
        {
            if (!File.Exists(path)) return null;

            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    // HEADER
                    // 4 chars signature
                    char[] sig = br.ReadChars(4); 
                    int version = br.ReadInt32();
                    int meshCount = br.ReadInt32(); // Assuming count at 0x08

                    for (int i = 0; i < meshCount; i++)
                    {
                        ReadMeshChunk(br, root);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Gb3Importer] Error parsing {path}: {ex.Message}");
                // Return whatever we managed to load
            }

            return root;
        }

        private static void ReadMeshChunk(BinaryReader br, GameObject parent)
        {
            // Chunk Header?
            // Assuming name + size
            // int chunkSize = br.ReadInt32(); 
            // string name = ReadFixedString(br, 32);
            
            // Skip name for now, assume generic mesh
            GameObject meshObj = new GameObject("Mesh_Part");
            meshObj.transform.SetParent(parent.transform);

            // Vertices
            int vCount = br.ReadInt32();
            if (vCount > 65000) return; // Sanity check

            Vector3[] verts = new Vector3[vCount];
            Vector3[] normals = new Vector3[vCount];
            Vector2[] uvs = new Vector2[vCount];

            for (int i = 0; i < vCount; i++)
            {
                // Read XYZ (DirectX = Z-Forward, Y-Up, X-Right. Unity = Z-Forward, Y-Up, X-Right. But Handedness differs)
                // Unity (Left Handed): +X Right, +Y Up, +Z Forward
                // DirectX (Left Handed): Same usually.
                // But G-Blender might use different Scale.
                
                float x = br.ReadSingle();
                float y = br.ReadSingle();
                float z = br.ReadSingle();
                verts[i] = new Vector3(x, y, z);

                float nx = br.ReadSingle();
                float ny = br.ReadSingle();
                float nz = br.ReadSingle();
                normals[i] = new Vector3(nx, ny, nz);

                float u = br.ReadSingle();
                float v = br.ReadSingle();
                uvs[i] = new Vector2(u, 1.0f - v); // Flip UV Y
            }

            // Indices
            int iCount = br.ReadInt32();
            int[] indices = new int[iCount];
            for (int i = 0; i < iCount; i++)
            {
                indices[i] = (int)br.ReadUInt16(); // Usually 16-bit indices
            }
            
            // Triangles needing flip?
            // Unity clips Back faces (CW). DirectX (CW or CCW depending on CullMode).
            // Usually need to swap [0, 1, 2] -> [0, 2, 1] for Unity import from DX.
            for (int i = 0; i < iCount; i += 3)
            {
                int temp = indices[i + 1];
                indices[i + 1] = indices[i + 2];
                indices[i + 2] = temp;
            }

            // Assembly
            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;
            
            mesh.RecalculateBounds();
            
            MeshFilter mf = meshObj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            
            MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("iDate/ToonCharacter")); // Use our shader
        }
    }
}
