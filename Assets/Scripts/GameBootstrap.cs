using UnityEngine;
using iDate.Network;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (NetworkManager.Instance != null) return;

        Debug.Log("Bootstrapping game...");
        GameObject go = new GameObject("GameSystems");
        go.AddComponent<NetworkManager>();
        go.AddComponent<UIManager>();
        
        // Ensure EventSystem exists
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Use New Input System Module
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            DontDestroyOnLoad(es);
        }

        // go.AddComponent<LoginController>(); // Removed Debug UI
        go.AddComponent<AssetManager>();
        go.AddComponent<Assets.Scripts.Data.ItemDatabase>();
        go.AddComponent<Assets.Scripts.Assets.GB3Inspector>();
        go.AddComponent<ConfigInspector>();
        go.AddComponent<UIAnalyzer>();
        go.AddComponent<Assets.Scripts.Assets.NifScanner>();
        go.AddComponent<TextureDumper>();
        go.AddComponent<GameClient>();
        DontDestroyOnLoad(go);
    }
}
