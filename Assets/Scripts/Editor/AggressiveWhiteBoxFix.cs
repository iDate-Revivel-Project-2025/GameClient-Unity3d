using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace iDate.Editor
{
    /// <summary>
    /// Aggressive white box finder - checks ALL UI components
    /// </summary>
    public class AggressiveWhiteBoxFix
    {
        [MenuItem("iDate/DEBUG - List All UI Elements")]
        public static void ListAllUIElements()
        {
            Debug.Log("=== ALL UI ELEMENTS IN SCENE ===");
            
            // Find all GameObjects in scene
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                // Check for Image component
                Image img = obj.GetComponent<Image>();
                if (img != null)
                {
                    Debug.Log($"Image: {GetPath(obj)} | Color: {img.color} | Sprite: {(img.sprite != null ? img.sprite.name : "NULL")} | Active: {obj.activeInHierarchy}");
                }
                
                // Check for RawImage component
                RawImage rawImg = obj.GetComponent<RawImage>();
                if (rawImg != null)
                {
                    Debug.Log($"RawImage: {GetPath(obj)} | Color: {rawImg.color} | Texture: {(rawImg.texture != null ? rawImg.texture.name : "NULL")}");
                }
                
                // Check for Canvas component
                Canvas canvas = obj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"Canvas: {GetPath(obj)} | RenderMode: {canvas.renderMode} | SortOrder: {canvas.sortingOrder}");
                }
                
                // Check for TMP_InputField
                TMP_InputField inputField = obj.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    Debug.Log($"InputField: {GetPath(obj)} | TargetGraphic: {inputField.targetGraphic}");
                }
            }
            
            Debug.Log("=== END OF LIST ===");
        }
        
        [MenuItem("iDate/FIX - Make ALL White Images Transparent")]
        public static void MakeAllWhiteTransparent()
        {
            Debug.Log("=== Making ALL white images transparent ===");
            
            int fixedCount = 0;
            
            // Find ALL Image components
            Image[] allImages = Object.FindObjectsOfType<Image>(true); // true = include inactive
            
            foreach (Image img in allImages)
            {
                // Check if it has no sprite AND is white-ish
                if (img.sprite == null)
                {
                    Color c = img.color;
                    if (c.r > 0.5f && c.g > 0.5f && c.b > 0.5f && c.a > 0.1f)
                    {
                        img.color = new Color(c.r, c.g, c.b, 0); // Make transparent
                        Debug.Log($"✓ Fixed: {GetPath(img.gameObject)} | Was: rgba({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})");
                        fixedCount++;
                    }
                }
            }
            
            // Also check RawImages
            RawImage[] allRawImages = Object.FindObjectsOfType<RawImage>(true);
            foreach (RawImage img in allRawImages)
            {
                if (img.texture == null)
                {
                    Color c = img.color;
                    if (c.r > 0.5f && c.g > 0.5f && c.b > 0.5f && c.a > 0.1f)
                    {
                        img.color = new Color(c.r, c.g, c.b, 0);
                        Debug.Log($"✓ Fixed RawImage: {GetPath(img.gameObject)} | Was: rgba({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})");
                        fixedCount++;
                    }
                }
            }
            
            Debug.Log($"=== Fixed {fixedCount} elements ===");
            
            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("Fixed!", 
                    $"Made {fixedCount} white elements transparent!\n\nCheck the Console for details.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Nothing Found", 
                    "No white elements found. Check Console for full list of UI elements.", 
                    "OK");
            }
        }
        
        private static string GetPath(GameObject obj)
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
