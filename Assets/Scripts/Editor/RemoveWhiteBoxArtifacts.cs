using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace iDate.Editor
{
    /// <summary>
    /// Removes white box artifacts from the UI by making untextured white images transparent
    /// </summary>
    public class RemoveWhiteBoxArtifacts
    {
        [MenuItem("iDate/Remove White Box Artifacts")]
        public static void RemoveWhiteBoxes()
        {
            Debug.Log("=== Removing White Box Artifacts ===");
            
            // Find all Image components in the scene
            Image[] allImages = Object.FindObjectsOfType<Image>(true);
            int fixedCount = 0;
            
            foreach (Image img in allImages)
            {
                bool isWhite = false;
                string reason = "";
                
                // Check if it's a white or near-white image
                if (img.sprite == null)
                {
                    // No sprite - check color
                    if (img.color.r > 0.9f && img.color.g > 0.9f && img.color.b > 0.9f && img.color.a > 0.5f)
                    {
                        isWhite = true;
                        reason = $"white color (a={img.color.a:F2})";
                    }
                }
                
                if (isWhite)
                {
                    // Make it fully transparent
                    Color newColor = img.color;
                    newColor.a = 0;
                    img.color = newColor;
                    Debug.Log($"✓ Made transparent: {GetGameObjectPath(img.gameObject)} - {reason}");
                    fixedCount++;
                }
            }
            
            // Also check RawImage components
            UnityEngine.UI.RawImage[] allRawImages = Object.FindObjectsOfType<UnityEngine.UI.RawImage>(true);
            foreach (UnityEngine.UI.RawImage img in allRawImages)
            {
                // Check if it's a white image with no texture
                if (img.texture == null && img.color == Color.white)
                {
                    // Make it transparent
                    img.color = new Color(1, 1, 1, 0);
                    Debug.Log($"✓ Made transparent (RawImage): {GetGameObjectPath(img.gameObject)}");
                    fixedCount++;
                }
            }
            
            Debug.Log($"=== Fixed {fixedCount} white box artifacts ===");
            
            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("White Boxes Removed!", 
                    $"Removed {fixedCount} white box artifacts!\n\n" +
                    "Press Play to see the clean UI.", 
                    "Great!");
            }
            else
            {
                EditorUtility.DisplayDialog("No White Boxes Found", 
                    "No white box artifacts were found in the scene.", 
                    "OK");
            }
        }
        
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
