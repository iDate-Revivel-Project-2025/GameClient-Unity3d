using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace iDate.Editor
{
    public class FixLoginScene : EditorWindow
    {
        [MenuItem("iDate/Fix Login Scene")]
        public static void Fix()
        {
            // 1. Open or create Login scene
            Scene scene;
            string scenePath = "Assets/Scenes/Login.unity";
            
            if (System.IO.File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
            
            // 2. Delete any existing LoginCanvas or Canvas
            GameObject[] allObjects = scene.GetRootGameObjects();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Canvas") || obj.name.Contains("LoginCanvas"))
                {
                    DestroyImmediate(obj);
                    Debug.Log($"Deleted: {obj.name}");
                }
            }
            
            // 3. Ensure camera exists
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                cam.transform.position = new Vector3(0, 0, -10);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                Debug.Log("Created Main Camera");
            }
            
            // 4. Load and instantiate UI_login prefab
            GameObject loginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UI_login.prefab");
            if (loginPrefab != null)
            {
                GameObject loginUI = PrefabUtility.InstantiatePrefab(loginPrefab) as GameObject;
                Debug.Log("Instantiated UI_login prefab");
                
                // 5. Add LoginUI component if it doesn't exist
                var loginUIComp = loginUI.GetComponent<iDate.UI.LoginUI>();
                if (loginUIComp == null)
                {
                    loginUIComp = loginUI.AddComponent<iDate.UI.LoginUI>();
                    
                    // Try to find and assign UI elements
                    var inputFields = loginUI.GetComponentsInChildren<TMPro.TMP_InputField>(true);
                    if (inputFields.Length >= 2)
                    {
                        loginUIComp.UsernameInput = inputFields[0];
                        loginUIComp.PasswordInput = inputFields[1];
                    }
                    
                    var buttons = loginUI.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                    if (buttons.Length > 0)
                    {
                        loginUIComp.LoginButton = buttons[0];
                    }
                    
                    Debug.Log("Added LoginUI component and assigned fields");
                }
            }
            else
            {
                Debug.LogError("UI_login.prefab not found! Run 'iDate > Import UI XML' first.");
                return;
            }
            
            // 6. Add NetworkManager if it doesn't exist
            var nm = FindObjectOfType<iDate.Network.NetworkManager>();
            if (nm == null)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                nm = nmObj.AddComponent<iDate.Network.NetworkManager>();
                Object.DontDestroyOnLoad(nmObj);
                Debug.Log("Created NetworkManager");
            }
            
            // 7. Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"✓ Login scene fixed and saved to {scenePath}");
            
            EditorUtility.DisplayDialog("Success", 
                "Login scene has been set up with the REAL iDate UI!\n\n" +
                "Press Play to see the original iDate login screen.", 
                "OK");
        }
    }
}
