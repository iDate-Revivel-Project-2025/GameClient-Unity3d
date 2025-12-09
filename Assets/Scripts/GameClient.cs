using UnityEngine;
using UnityEngine.UI;
using Assets;
using iDate.Network;
using System.Collections.Generic;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance;

    [Header("UI Reference")]
    public GameObject loginPanel;
    public GameObject lobbyPanel; // Placeholder for now

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 1. Initialize Asset System
        Debug.Log("Initializing Asset System...");
        // Ensure IDOLoader / GB3Loader are ready (lazy init usually fine)

        // 2. Initialize Network System
        Debug.Log("Initializing Network...");
        NetworkManager.Instance.OnConnected += OnConnected;
        NetworkManager.Instance.OnPacketReceived += OnPacket;
        NetworkManager.Instance.OnDisconnected += OnDisconnected;

        // 3. Initialize Item DB
        // Already handled by bootstrap adding ItemDatabase
        
        // 4. Show Login
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen("login");
        }
    }

    private void OnConnected()
    {
        Debug.Log("Client Connected. Waiting for Handshake/Login...");
    }

    private void OnDisconnected()
    {
        Debug.Log("Client Disconnected.");
    }

    private void OnPacket(Packet p)
    {
        switch((OpCode)p.OpCode)
        {
            case OpCode.LOGIN_RESP:
                Debug.Log("Login Authenticated! Requesting Server List...");
                NetworkManager.Instance.Send(new Packet((ushort)OpCode.SERVER_LIST));
                StartCoroutine(LoadLobbySequence());
                break;
        }
    }

    [ContextMenu("Force Load Lobby")]
    public void ForceLoadLobby()
    {
        StartCoroutine(LoadLobbySequence());
    }

    private System.Collections.IEnumerator LoadLobbySequence()
    {
         // Simulate loading
         yield return new WaitForSeconds(0.5f);
         
         // In real client, we would load the scene or instantiate the Lobby UI prefab.
         // For now, let's try to verify if we have the assets to BUILD the lobby.
         
         Texture2D lobbyTex = IDOLoader.LoadTexture("/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Interface/Interface04/LobbyUI.ido");
         if (lobbyTex != null)
         {
             Debug.Log($"Lobby Texture Loaded: {lobbyTex.width}x{lobbyTex.height}");
             // Create a Sprite from it just to show we can
             Sprite s = Sprite.Create(lobbyTex, new Rect(0,0,lobbyTex.width, lobbyTex.height), Vector2.zero);
             
             // Create a simple UI Image on screen if LobbyPanel is active
             GameObject canvas = GameObject.Find("Canvas"); // Standard hack for no-ref
             if (canvas)
             {
                 GameObject imgObj = new GameObject("LobbyBackground");
                 imgObj.transform.SetParent(canvas.transform, false);
                 Image img = imgObj.AddComponent<Image>();
                 img.sprite = s;
                 img.SetNativeSize();
             }
         }
         else
         {
             Debug.LogError("Failed to load Lobby Texture.");
         }
    }
}
