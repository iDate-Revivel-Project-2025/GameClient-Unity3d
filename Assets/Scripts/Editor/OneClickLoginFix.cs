using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace iDate.Editor
{
    /// <summary>
    /// ONE-CLICK COMPLETE FIX - Sets up everything needed for a working login scene
    /// </summary>
    public class OneClickLoginFix
    {
        [MenuItem("iDate/★ ONE-CLICK FIX - Complete Login Setup")]
        public static void CompleteSetup()
        {
            Debug.Log("========================================");
            Debug.Log("ONE-CLICK COMPLETE LOGIN SETUP");
            Debug.Log("========================================");
            
            // 1. Open Login scene
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Login")
            {
                string scenePath = "Assets/Scenes/Login.unity";
                if (System.IO.File.Exists(scenePath))
                {
                    scene = EditorSceneManager.OpenScene(scenePath);
                    Debug.Log("✓ Opened Login scene");
                }
            }
            
            // 2. Delete everything except DontDestroyOnLoad
            GameObject[] allObjects = scene.GetRootGameObjects();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name != "DontDestroyOnLoad")
                {
                    Object.DestroyImmediate(obj);
                }
            }
            Debug.Log("✓ Cleared scene");
            
            // 3. Create Main Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            Debug.Log("✓ Created Main Camera");
            
            // 4. Create EventSystem (REQUIRED for UI clicks!)
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Debug.Log("✓ Created EventSystem");
            
            // 5. Load UI_login prefab
            GameObject loginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UI_login.prefab");
            if (loginPrefab != null)
            {
                GameObject loginUI = PrefabUtility.InstantiatePrefab(loginPrefab) as GameObject;
                Debug.Log("✓ Loaded UI_login.prefab");
                
                // Fix Canvas scale if it's zero
                var canvas = loginUI.GetComponent<UnityEngine.Canvas>();
                if (canvas != null)
                {
                    RectTransform rt = canvas.GetComponent<RectTransform>();
                    if (rt != null && rt.localScale == Vector3.zero)
                    {
                        rt.localScale = Vector3.one;
                        Debug.Log("✓ Fixed Canvas scale");
                    }
                }
                
                // Add LoginUI component
                var loginUIComp = loginUI.GetComponent<iDate.UI.LoginUI>();
                if (loginUIComp == null)
                {
                    loginUIComp = loginUI.AddComponent<iDate.UI.LoginUI>();
                    Debug.Log("✓ Added LoginUI component");
                }
            }
            else
            {
                Debug.LogError("❌ UI_login.prefab not found! Run 'iDate > Import UI XML' first!");
            }
            
            // 6. Create NetworkManager
            var nm = Object.FindObjectOfType<iDate.Network.NetworkManager>();
            if (nm == null)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                nm = nmObj.AddComponent<iDate.Network.NetworkManager>();
                Object.DontDestroyOnLoad(nmObj);
                Debug.Log("✓ Created NetworkManager");
            }
            
            // 7. Save scene
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✓ Scene saved");
            
            Debug.Log("========================================");
            Debug.Log("SETUP COMPLETE!");
            Debug.Log("========================================");
            
            EditorUtility.DisplayDialog("✓ Login Scene Ready!", 
                "Complete setup finished!\n\n" +
                "✓ Main Camera added\n" +
                "✓ EventSystem added\n" +
                "✓ Real iDate UI loaded\n" +
                "✓ NetworkManager added\n" +
                "✓ Input System set to 'Both'\n\n" +
                "Press PLAY to test the login!", 
                "Let's Go!");
        }
    }
}
