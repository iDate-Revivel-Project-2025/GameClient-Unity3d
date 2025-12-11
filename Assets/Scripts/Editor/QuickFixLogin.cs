using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace iDate.Editor
{
    public class QuickFixLogin
    {
        [MenuItem("iDate/Quick Fix - Add Camera to Login")]
        public static void AddCamera()
        {
            // Add camera to current scene
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                cam.transform.position = new Vector3(0, 0, -10);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = false;
                
                Debug.Log("✓ Created Main Camera");
                
                // Save scene
                EditorSceneManager.SaveOpenScenes();
                
                EditorUtility.DisplayDialog("Success", 
                    "Camera added to Login scene!\n\nPress Play to test.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", 
                    "Camera already exists in scene.", 
                    "OK");
            }
        }
    }
}
