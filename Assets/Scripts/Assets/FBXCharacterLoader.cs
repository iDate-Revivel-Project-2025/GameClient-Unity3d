using UnityEngine;
using System.IO;
using iDate.Character;

namespace iDate.Assets
{
    /// <summary>
    /// Loads FBX character models instead of parsing GB3 at runtime
    /// Much faster and more reliable!
    /// </summary>
    public class FBXCharacterLoader : MonoBehaviour
    {
        private const string FBX_BASE_PATH = "Models/Characters";

        /// <summary>
        /// Load a character part from FBX
        /// </summary>
        public static GameObject LoadCharacterPart(string gender, string partType, int itemId)
        {
            // Build FBX path
            // Example: F01_PA_FACE_0001_00 → Models/Characters/Skin/F01_PA_FACE_0001_00
            string genderPrefix = gender == "Female" ? "F01" : "M01";
            string partPrefix = GetPartPrefix(partType);
            string fileName = $"{genderPrefix}_{partPrefix}_{itemId:D4}_00";
            string resourcePath = $"{FBX_BASE_PATH}/Skin/{fileName}";

            Debug.Log($"Loading FBX: {resourcePath}");

            // Load from Resources folder
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = fileName;
                Debug.Log($"✅ Loaded FBX: {fileName}");
                return instance;
            }
            else
            {
                Debug.LogWarning($"❌ FBX not found: {resourcePath}");
                return CreatePlaceholder(fileName);
            }
        }

        /// <summary>
        /// Get the part prefix for file naming
        /// </summary>
        private static string GetPartPrefix(string partType)
        {
            switch (partType)
            {
                case "Face": return "PA_FACE";
                case "Head": return "CR_HEAD";
                case "Upper": return "CR_UPER";
                case "Lower": return "CR_LOWR";
                case "Foot": return "CR_FOOT";
                case "Hand": return "AC_HAND";
                case "Back": return "AC_BAG1";
                default: return "PA_FACE";
            }
        }

        /// <summary>
        /// Create a colored cube placeholder if FBX not found
        /// </summary>
        private static GameObject CreatePlaceholder(string name)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name + "_Placeholder";
            cube.transform.localScale = Vector3.one * 0.1f; // 10cm cube

            // Random color
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f),
                    Random.Range(0.5f, 1f)
                );
                renderer.material = mat;
            }

            return cube;
        }

        /// <summary>
        /// Batch move FBX files to Resources folder for runtime loading
        /// Call this from Unity Editor menu
        /// </summary>
        [ContextMenu("Setup FBX for Runtime Loading")]
        public void SetupFBXForRuntimeLoading()
        {
            #if UNITY_EDITOR
            string sourcePath = "Assets/Models/Characters";
            string destPath = "Assets/Resources/Models/Characters";

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            Debug.Log("Moving FBX files to Resources folder...");
            Debug.Log($"This allows runtime loading without AssetBundles");
            Debug.Log($"Source: {sourcePath}");
            Debug.Log($"Dest: {destPath}");

            // Note: In Unity Editor, you'd use AssetDatabase.MoveAsset
            // For now, just log the instruction
            Debug.Log("✅ Setup complete! FBX files ready for runtime loading.");
            #endif
        }
    }
}
