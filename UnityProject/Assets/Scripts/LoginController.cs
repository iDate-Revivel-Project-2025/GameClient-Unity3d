using UnityEngine;
using UnityEngine.UI;

using iDate.Network;

public class LoginController : MonoBehaviour
{
    [Header("UI References")]
    public InputField idInput;
    public InputField passwordInput;
    public Button loginButton;
    public MainMenuController mainController;

    void Start()
    {
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginClicked);
        }
    }

    public void OnLoginClicked()
    {
        string id = idInput.text;
        string pw = passwordInput.text;

        Debug.Log($"Attempting login with ID: {id}");

        // Validate (Mock)
        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(pw))
        {
            Debug.Log("Credentials Valid. Connecting to Server...");
            
            // 1. Connect (Sends Handshake Op 2000)
            NetworkManager.Instance.Connect();
            
            if (mainController != null)
            {
                mainController.ShowServerSelect();
            }
        }
        else
        {
            Debug.LogError("Invalid Credentials");
        }
    }
}
