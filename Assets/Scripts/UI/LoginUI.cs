using UnityEngine;
using UnityEngine.UI; // Standard UI
using System.Collections;
using System.Collections.Generic;
using iDate.Network;

public class LoginUI : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject loginPanel;
    public InputField usernameInput;
    public InputField passwordInput;
    public Button loginButton;
    public Text statusText;

    private void Start()
    {
        if (loginButton) loginButton.onClick.AddListener(OnLoginClicked);
        
        NetworkManager.Instance.OnConnected += HandleConnected;
        NetworkManager.Instance.OnDisconnected += HandleDisconnected;
        NetworkManager.Instance.OnPacketReceived += HandlePacket;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnected -= HandleConnected;
            NetworkManager.Instance.OnDisconnected -= HandleDisconnected;
            NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }
    }

    private void HandleConnected()
    {
        SetStatus("Connected! Logging in...");
        // Auto-login sequence as per legacy client behavior usually immediately follows
        // But here we wait for user input in a real scenario.
        // For now, since NetworkManager auto-sends Handshake, we just wait.
    }

    private void HandlePacket(Packet packet)
    {
        // NetworkManager handles the encryption handshake automatically.
        // We just listen for the success OpCode
        if (packet.OpCode == (ushort)OpCode.LOGIN_RESP)
        {
            SetStatus("Login Successful!");
            StartCoroutine(LoadLobby());
        }
    }

    private void HandleDisconnected()
    {
        SetStatus("Disconnected.");
        if (loginButton) loginButton.interactable = true;
    }

    public void OnLoginClicked()
    {
        SetStatus("Connecting...");
        loginButton.interactable = false;
        NetworkManager.Instance.Connect("127.0.0.1", 8132);
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        Debug.Log($"[LoginUI] {msg}");
    }

    IEnumerator LoadLobby()
    {
        yield return new WaitForSeconds(1.0f);
        // TODO: Load Lobby Scene
        SetStatus("Entering Lobby...");
        loginPanel.SetActive(false);
    }
}
