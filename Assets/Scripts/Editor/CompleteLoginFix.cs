using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace iDate.Editor
{
    /// <summary>
    /// Complete automated fix for the Login scene
    /// This will delete all custom UI and set up the real iDate login from UI_login.prefab
    /// </summary>
    public class CompleteLoginFix
    {
        [MenuItem("iDate/COMPLETE FIX - Setup Real iDate Login")]
        public static void CompletelyFixLogin()
        {
            // 1. Ensure we're in the Login scene
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Login")
            {
                // Try to open it
                string scenePath = "Assets/Scenes/Login.unity";
                if (System.IO.File.Exists(scenePath))
                {
                    scene = EditorSceneManager.OpenScene(scenePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Login scene not found at Assets/Scenes/Login.unity", "OK");
                    return;
                }
            }
            
            Debug.Log("=== Starting Complete Login Fix ===");
            
            // 2. Delete ALL GameObjects except DontDestroyOnLoad
            GameObject[] allObjects = scene.GetRootGameObjects();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name != "DontDestroyOnLoad")
                {
                    Object.DestroyImmediate(obj);
                    Debug.Log($"Deleted: {obj.name}");
                }
            }
            
            // 3. Create Main Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = false;
            Debug.Log("✓ Created Main Camera");
            
            // 4. Load and instantiate UI_login prefab
            GameObject loginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UI_login.prefab");
            if (loginPrefab != null)
            {
                GameObject loginUI = PrefabUtility.InstantiatePrefab(loginPrefab) as GameObject;
                Debug.Log("✓ Instantiated UI_login prefab");
                
                // 5. Find and configure Canvas
                Canvas canvas = loginUI.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    // Fix the scale issue - ensure it's not zero!
                    RectTransform rt = canvas.GetComponent<RectTransform>();
                    if (rt != null && rt.localScale == Vector3.zero)
                    {
                        rt.localScale = Vector3.one;
                        Debug.Log("✓ Fixed Canvas scale (was zero!)");
                    }
                }
                
                // 6. Try to add LoginUI component and wire it up
                var loginUIComp = loginUI.GetComponent<iDate.UI.LoginUI>();
                if (loginUIComp == null)
                {
                    loginUIComp = loginUI.AddComponent<iDate.UI.LoginUI>();
                    Debug.Log("✓ Added LoginUI component");
                }
                
                // Try to auto-wire UI elements
                var inputFields = loginUI.GetComponentsInChildren<TMPro.TMP_InputField>(true);
                var buttons = loginUI.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                
                if (inputFields.Length >= 2)
                {
                    // Find by name or position
                    foreach (var field in inputFields)
                    {
                        if (field.name.ToLower().Contains("id") || field.name.ToLower().Contains("user"))
                        {
                            loginUIComp.UsernameInput = field;
                        }
                        else if (field.name.ToLower().Contains("pass") || field.name.ToLower().Contains("pw"))
                        {
                            loginUIComp.PasswordInput = field;
                        }
                    }
                    
                    // Fallback: use first two
                    if (loginUIComp.UsernameInput == null) loginUIComp.UsernameInput = inputFields[0];
                    if (loginUIComp.PasswordInput == null && inputFields.Length > 1) loginUIComp.PasswordInput = inputFields[1];
                    
                    Debug.Log($"✓ Assigned input fields: Username={loginUIComp.UsernameInput?.name}, Password={loginUIComp.PasswordInput?.name}");
                }
                
                if (buttons.Length > 0)
                {
                    // Find login button
                    foreach (var btn in buttons)
                    {
                        if (btn.name.ToLower().Contains("login"))
                        {
                            loginUIComp.LoginButton = btn;
                            break;
                        }
                    }
                    
                    // Fallback: use first button
                    if (loginUIComp.LoginButton == null) loginUIComp.LoginButton = buttons[0];
                    
                    Debug.Log($"✓ Assigned login button: {loginUIComp.LoginButton?.name}");
                }
            }
            else
            {
                Debug.LogError("❌ UI_login.prefab not found! Run 'iDate > Import UI XML' first.");
                EditorUtility.DisplayDialog("Error", 
                    "UI_login.prefab not found!\n\nPlease run 'iDate > Import UI XML' first to import the original game UI.", 
                    "OK");
                return;
            }
            
            // 7. Ensure NetworkManager exists
            var nm = Object.FindObjectOfType<iDate.Network.NetworkManager>();
            if (nm == null)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                nm = nmObj.AddComponent<iDate.Network.NetworkManager>();
                Object.DontDestroyOnLoad(nmObj);
                Debug.Log("✓ Created NetworkManager");
            }
            
            // 8. Save scene
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✓ Scene saved");
            
            Debug.Log("=== Complete Login Fix DONE ===");
            
            EditorUtility.DisplayDialog("Success!", 
                "Login scene has been completely fixed!\n\n" +
                "✓ Deleted all custom UI\n" +
                "✓ Added Main Camera\n" +
                "✓ Loaded real iDate UI from UI_login.prefab\n" +
                "✓ Fixed Canvas scale\n" +
                "✓ Added NetworkManager\n\n" +
                "Press PLAY to see the original iDate login screen!", 
                "Awesome!");
        }
    }
}
