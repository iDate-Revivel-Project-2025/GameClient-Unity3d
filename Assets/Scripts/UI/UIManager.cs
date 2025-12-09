using UnityEngine;
using System.Collections.Generic;
using iDate.Network;
using iDate.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private GameObject _currentScreen;
    private Dictionary<string, GameObject> _loadedPrefabs = new Dictionary<string, GameObject>();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowScreen(string screenName)
    {
        if (_currentScreen != null) Destroy(_currentScreen);

        string prefabPath = $"UI/UI_{screenName}"; // Assumes in Resources or we load from AssetDatabase (Editor only)?
        // At runtime, we need them in Resources folder to use Resources.Load
        // Our Importer saves to Assets/Prefabs/UI. We should move them to Assets/Resources/UI/ or use AssetBundle.
        // For rapid dev in Editor, we can try to find them.
        
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        
#if UNITY_EDITOR
        if (prefab == null)
        {
             // Fallback for Editor: Load generic or use AssetDatabase (not possible in runtime script)
             // We'll rely on the user moving them or we can implement a simple loader content variable.
             Debug.LogWarning($"Could not load generic resource {prefabPath}. Ensure Prefabs are in Assets/Resources/UI/");
             return;
        }
#endif

        if (prefab != null)
        {
            _currentScreen = Instantiate(prefab);
            _currentScreen.name = screenName;
            
            // Attach Logic based on Screen
            BindLogic(screenName, _currentScreen);
        }
    }

    private void BindLogic(string screenName, GameObject go)
    {
        switch (screenName.ToLower())
        {
            case "login":
                go.AddComponent<LoginLogic>();
                break;
            case "server":
            case "server_channel":
                go.AddComponent<ServerSelectLogic>();
                break;
            case "lobby":
            case "waitroom":
                go.AddComponent<LobbyLogic>();
                break;
        }
    }

    public void OnLoginSuccess()
    {
        ShowScreen("server_channel"); // Prepare next screen
    }
}
