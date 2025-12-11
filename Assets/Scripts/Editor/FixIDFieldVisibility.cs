using UnityEngine;
using UnityEditor;
using TMPro;

namespace iDate.Editor
{
    /// <summary>
    /// Makes the ID input field visible (it's hidden by default in login.xml)
    /// </summary>
    public class FixIDFieldVisibility
    {
        [MenuItem("iDate/Fix ID Field Visibility")]
        public static void MakeIDFieldVisible()
        {
            Debug.Log("=== Making ID Field Visible ===");
            
            // Find all TMP_InputField components
            TMP_InputField[] allInputFields = Object.FindObjectsOfType<TMP_InputField>(true); // true = include inactive
            
            int fixedCount = 0;
            
            foreach (TMP_InputField field in allInputFields)
            {
                // Check if this is likely the ID field (not password type)
                if (field.contentType != TMP_InputField.ContentType.Password)
                {
                    // Make it visible and active
                    field.gameObject.SetActive(true);
                    
                    // Ensure the text component is visible
                    if (field.textComponent != null)
                    {
                        field.textComponent.gameObject.SetActive(true);
                        field.textComponent.color = Color.white; // Make text visible
                    }
                    
                    // Ensure placeholder is visible
                    if (field.placeholder != null)
                    {
                        field.placeholder.gameObject.SetActive(true);
                    }
                    
                    Debug.Log($"✓ Made visible: {GetPath(field.gameObject)}");
                    fixedCount++;
                }
            }
            
            Debug.Log($"=== Made {fixedCount} ID fields visible ===");
            
            // Save the scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            EditorUtility.DisplayDialog("ID Field Fixed!", 
                $"Made {fixedCount} ID input field(s) visible!\n\n" +
                "The ID field should now be visible and clickable.\n\n" +
                "Press Play to test!", 
                "OK");
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
