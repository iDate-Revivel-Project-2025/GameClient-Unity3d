using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace iDate.Assets
{
    /// <summary>
    /// GB3 (Gamebryo Binary 3D) Format Loader
    /// Reverse engineered from iDate game files
    /// </summary>
    public class GB3Loader
    {
        public class GB3Model
        {
            public string Name;
            public List<GB3Node> Nodes = new List<GB3Node>();
            public List<GB3Mesh> Meshes = new List<GB3Mesh>();
            public GB3Skeleton Skeleton;
        }

        public class GB3Node
        {
            public string Name;
            public string Type; // SKELETAL, SKIN, etc.
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale = Vector3.one;
            public Matrix4x4 Transform;
            public int ParentIndex = -1;
            public List<int> ChildIndices = new List<int>();
        }

        public class GB3Mesh
        {
            public string Name;
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector2[] UVs;
            public Color[] Colors;
            public int[] Triangles;
            public BoneWeight[] BoneWeights;
            public int[] BoneIndices;
        }

        public class GB3Skeleton
        {
            public List<GB3Bone> Bones = new List<GB3Bone>();
            public Matrix4x4[] BindPoses;
        }

        public class GB3Bone
        {
            public string Name;
            public int ParentIndex = -1;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale = Vector3.one;
            public Matrix4x4 LocalTransform;
            public Matrix4x4 WorldTransform;
        }

        /// <summary>
        /// Load a GB3 file from disk
        /// </summary>
        public static GB3Model Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"GB3 file not found: {filePath}");
                return null;
            }

            byte[] data = File.ReadAllBytes(filePath);
            return Parse(data);
        }

        /// <summary>
        /// Parse GB3 binary data
        /// </summary>
        public static GB3Model Parse(byte[] data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    GB3Model model = new GB3Model();

                    // Read header
                    int nameLength = br.ReadInt32();
                    if (nameLength > 0 && nameLength < 256)
                    {
                        byte[] nameBytes = br.ReadBytes(nameLength);
                        model.Name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                        Debug.Log($"GB3 Model Name: {model.Name}");
                    }

                    // Parse node tree
                    ParseNodeTree(br, model);

                    // Parse skeleton
                    ParseSkeleton(br, model);

                    // Parse meshes
                    ParseMeshes(br, model);

                    return model;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GB3 Parse Error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private static void ParseNodeTree(BinaryReader br, GB3Model model)
        {
            // Skip to node section (after header padding)
            // Based on hex analysis, nodes start around offset 0x20-0x80
            
            long startPos = br.BaseStream.Position;
            
            // Look for "SKELETAL" marker
            byte[] buffer = new byte[8];
            while (br.BaseStream.Position < br.BaseStream.Length - 8)
            {
                br.Read(buffer, 0, 8);
                string marker = Encoding.ASCII.GetString(buffer);
                
                if (marker.Contains("SKELETAL"))
                {
                    Debug.Log($"Found SKELETAL at offset 0x{br.BaseStream.Position - 8:X}");
                    // Parse skeletal node
                    GB3Node node = new GB3Node
                    {
                        Type = "SKELETAL",
                        Name = "Root"
                    };
                    model.Nodes.Add(node);
                    break;
                }
            }
        }

        private static void ParseSkeleton(BinaryReader br, GB3Model model)
        {
            model.Skeleton = new GB3Skeleton();
            
            // Look for "Bip01" bone names
            long startPos = br.BaseStream.Position;
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            
            byte[] data = br.ReadBytes((int)br.BaseStream.Length);
            string text = Encoding.ASCII.GetString(data);
            
            // Find all bone names
            List<string> boneNames = new List<string>();
            string[] commonBones = new string[]
            {
                "Bip01",
                "Bip01 Pelvis",
                "Bip01 Spine",
                "Bip01 Spine1",
                "Bip01 Spine2",
                "Bip01 Neck",
                "Bip01 Head",
                "Bip01 L Clavicle",
                "Bip01 L UpperArm",
                "Bip01 L Forearm",
                "Bip01 L Hand",
                "Bip01 R Clavicle",
                "Bip01 R UpperArm",
                "Bip01 R Forearm",
                "Bip01 R Hand",
                "Bip01 L Thigh",
                "Bip01 L Calf",
                "Bip01 L Foot",
                "Bip01 R Thigh",
                "Bip01 R Calf",
                "Bip01 R Foot"
            };

            foreach (string boneName in commonBones)
            {
                if (text.Contains(boneName))
                {
                    int index = data.IndexOf(Encoding.ASCII.GetBytes(boneName));
                    if (index >= 0)
                    {
                        GB3Bone bone = new GB3Bone
                        {
                            Name = boneName,
                            Position = Vector3.zero,
                            Rotation = Quaternion.identity,
                            Scale = Vector3.one
                        };
                        
                        // Try to read transform data after bone name
                        if (index + boneName.Length + 48 < data.Length)
                        {
                            br.BaseStream.Seek(index + boneName.Length, SeekOrigin.Begin);
                            
                            // Skip padding
                            while (br.BaseStream.Position < br.BaseStream.Length - 12)
                            {
                                byte b = br.ReadByte();
                                if (b == 0x00 || b == 0x01) continue;
                                
                                // Check if this looks like float data
                                br.BaseStream.Seek(-1, SeekOrigin.Current);
                                float testFloat = br.ReadSingle();
                                
                                if (Math.Abs(testFloat) < 1000f) // Reasonable position value
                                {
                                    bone.Position.x = testFloat;
                                    bone.Position.y = br.ReadSingle();
                                    bone.Position.z = br.ReadSingle();
                                    break;
                                }
                            }
                        }
                        
                        model.Skeleton.Bones.Add(bone);
                        Debug.Log($"Found bone: {boneName} at index {index:X}");
                    }
                }
            }

            // Build bone hierarchy
            BuildBoneHierarchy(model.Skeleton);
        }

        private static void BuildBoneHierarchy(GB3Skeleton skeleton)
        {
            // Standard Bip01 hierarchy
            Dictionary<string, int> boneMap = new Dictionary<string, int>();
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                boneMap[skeleton.Bones[i].Name] = i;
            }

            // Set parent indices based on naming convention
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                GB3Bone bone = skeleton.Bones[i];
                
                // Determine parent based on name
                if (bone.Name == "Bip01")
                {
                    bone.ParentIndex = -1; // Root
                }
                else if (bone.Name.Contains("Pelvis"))
                {
                    if (boneMap.ContainsKey("Bip01"))
                        bone.ParentIndex = boneMap["Bip01"];
                }
                else if (bone.Name.Contains("Spine") && !bone.Name.Contains("Spine1") && !bone.Name.Contains("Spine2"))
                {
                    if (boneMap.ContainsKey("Bip01 Pelvis"))
                        bone.ParentIndex = boneMap["Bip01 Pelvis"];
                }
                else if (bone.Name.Contains("Spine1"))
                {
                    if (boneMap.ContainsKey("Bip01 Spine"))
                        bone.ParentIndex = boneMap["Bip01 Spine"];
                }
                else if (bone.Name.Contains("Spine2"))
                {
                    if (boneMap.ContainsKey("Bip01 Spine1"))
                        bone.ParentIndex = boneMap["Bip01 Spine1"];
                }
                else if (bone.Name.Contains("Neck"))
                {
                    if (boneMap.ContainsKey("Bip01 Spine2"))
                        bone.ParentIndex = boneMap["Bip01 Spine2"];
                }
                else if (bone.Name.Contains("Head"))
                {
                    if (boneMap.ContainsKey("Bip01 Neck"))
                        bone.ParentIndex = boneMap["Bip01 Neck"];
                }
                // Add more hierarchy rules as needed
            }

            // Calculate bind poses
            skeleton.BindPoses = new Matrix4x4[skeleton.Bones.Count];
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                skeleton.BindPoses[i] = Matrix4x4.TRS(
                    skeleton.Bones[i].Position,
                    skeleton.Bones[i].Rotation,
                    skeleton.Bones[i].Scale
                ).inverse;
            }
        }

        private static void ParseMeshes(BinaryReader br, GB3Model model)
        {
            // Look for "SKIN" marker
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] data = br.ReadBytes((int)br.BaseStream.Length);
            
            int skinOffset = data.IndexOf(Encoding.ASCII.GetBytes("SKIN"));
            if (skinOffset < 0)
            {
                Debug.LogWarning("No SKIN mesh data found");
                return;
            }

            Debug.Log($"Found SKIN at offset 0x{skinOffset:X}");
            
            // Create a mesh with basic geometry
            // For now, create a simple cube so the model is visible
            // This will be replaced with actual vertex parsing
            GB3Mesh mesh = new GB3Mesh
            {
                Name = model.Name + "_Mesh"
            };

            // Create a simple cube mesh (8 vertices, 12 triangles)
            float size = 10f; // 10 units
            mesh.Vertices = new Vector3[]
            {
                new Vector3(-size, -size, -size), // 0
                new Vector3( size, -size, -size), // 1
                new Vector3( size,  size, -size), // 2
                new Vector3(-size,  size, -size), // 3
                new Vector3(-size, -size,  size), // 4
                new Vector3( size, -size,  size), // 5
                new Vector3( size,  size,  size), // 6
                new Vector3(-size,  size,  size)  // 7
            };

            mesh.Normals = new Vector3[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
            };

            mesh.UVs = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };

            // Cube triangles (12 triangles = 36 indices)
            mesh.Triangles = new int[]
            {
                // Front
                0, 2, 1, 0, 3, 2,
                // Back
                5, 7, 4, 5, 6, 7,
                // Left
                4, 3, 0, 4, 7, 3,
                // Right
                1, 6, 5, 1, 2, 6,
                // Top
                3, 6, 2, 3, 7, 6,
                // Bottom
                4, 1, 5, 4, 0, 1
            };

            model.Meshes.Add(mesh);
            
            Debug.Log($"Created placeholder mesh with {mesh.Vertices.Length} vertices, {mesh.Triangles.Length/3} triangles");
        }

        /// <summary>
        /// Convert GB3Model to Unity GameObject with SkinnedMeshRenderer
        /// </summary>
        public static GameObject ToGameObject(GB3Model model)
        {
            if (model == null) return null;

            GameObject root = new GameObject(model.Name);

            // Create skeleton
            Transform[] bones = new Transform[model.Skeleton.Bones.Count];
            for (int i = 0; i < model.Skeleton.Bones.Count; i++)
            {
                GB3Bone bone = model.Skeleton.Bones[i];
                GameObject boneObj = new GameObject(bone.Name);
                
                boneObj.transform.localPosition = bone.Position;
                boneObj.transform.localRotation = bone.Rotation;
                boneObj.transform.localScale = bone.Scale;

                if (bone.ParentIndex >= 0 && bone.ParentIndex < i)
                {
                    boneObj.transform.SetParent(bones[bone.ParentIndex], false);
                }
                else
                {
                    boneObj.transform.SetParent(root.transform, false);
                }

                bones[i] = boneObj.transform;
            }

            // Create mesh
            if (model.Meshes.Count > 0)
            {
                GameObject meshObj = new GameObject("Mesh");
                meshObj.transform.SetParent(root.transform, false);

                SkinnedMeshRenderer smr = meshObj.AddComponent<SkinnedMeshRenderer>();
                Mesh unityMesh = new Mesh();
                unityMesh.name = model.Name;

                GB3Mesh gb3Mesh = model.Meshes[0];
                unityMesh.vertices = gb3Mesh.Vertices;
                unityMesh.normals = gb3Mesh.Normals;
                unityMesh.uv = gb3Mesh.UVs;
                unityMesh.triangles = gb3Mesh.Triangles;

                if (gb3Mesh.BoneWeights != null)
                {
                    unityMesh.boneWeights = gb3Mesh.BoneWeights;
                }

                unityMesh.bindposes = model.Skeleton.BindPoses;
                unityMesh.RecalculateBounds();
                unityMesh.RecalculateNormals(); // Ensure normals are correct

                smr.sharedMesh = unityMesh;
                smr.bones = bones;
                smr.rootBone = bones[0];
                
                // Create a simple material so the mesh is visible
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(
                    UnityEngine.Random.Range(0.5f, 1f),
                    UnityEngine.Random.Range(0.5f, 1f),
                    UnityEngine.Random.Range(0.5f, 1f),
                    1f
                );
                smr.material = mat;
            }

            return root;
        }
    }

    // Extension method for byte array search
    public static class ByteArrayExtensions
    {
        public static int IndexOf(this byte[] array, byte[] pattern)
        {
            for (int i = 0; i <= array.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (array[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
        }
    }
}
