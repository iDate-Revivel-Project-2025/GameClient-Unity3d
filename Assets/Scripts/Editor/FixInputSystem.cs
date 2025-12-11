using UnityEngine;
using UnityEditor;
using System.IO;

namespace iDate.Editor
{
    /// <summary>
    /// Fixes the Input System conflict that prevents UI from working
    /// </summary>
    public class FixInputSystem
    {
        [MenuItem("iDate/Fix Input System Conflict")]
        public static void FixInputSystemSettings()
        {
            Debug.Log("=== Fixing Input System Conflict ===");
            
            string projectSettingsPath = "ProjectSettings/ProjectSettings.asset";
            
            if (File.Exists(projectSettingsPath))
            {
                string content = File.ReadAllText(projectSettingsPath);
                
                // Change activeInputHandler from 1 (new) or 2 (both) to 2 (both)
                // This allows legacy Input to work alongside new Input System
                if (content.Contains("activeInputHandler: 1"))
                {
                    content = content.Replace("activeInputHandler: 1", "activeInputHandler: 2");
                    File.WriteAllText(projectSettingsPath, content);
                    Debug.Log("✓ Changed activeInputHandler to 2 (Both)");
                }
                else if (content.Contains("activeInputHandler: 2"))
                {
                    Debug.Log("✓ Already set to 'Both' (activeInputHandler: 2)");
                }
                else
                {
                    Debug.LogWarning("Could not find activeInputHandler setting");
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Input System Fixed!", 
                    "Active Input Handling changed to 'Both'\n\n" +
                    "Unity will now RESTART to apply changes.\n\n" +
                    "After restart, the UI will be clickable!", 
                    "OK - Restart Unity");
                
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
            else
            {
                Debug.LogError("ProjectSettings.asset not found!");
            }
        }
    }
}
