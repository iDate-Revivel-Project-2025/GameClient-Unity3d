using UnityEngine;
using UnityEngine.UI;
using iDate.Network;
using TMPro; // Assuming TextMeshPro is used

namespace iDate.UI
{
    public class LoginUI : MonoBehaviour
    {
        public TMP_InputField UsernameInput; // or InputField
        public TMP_InputField PasswordInput;
        public Button LoginButton;
        public TextMeshProUGUI StatusText;

        private void Start()
        {
            if (LoginButton != null)
                LoginButton.onClick.AddListener(OnLoginClicked);
                
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnected += () => UpdateStatus("Connected. Ready to Login.");
                NetworkManager.Instance.OnDisconnected += () => UpdateStatus("Disconnected.");
            }
        }

        private void OnLoginClicked()
        {
            string user = UsernameInput.text;
            string pass = PasswordInput.text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                UpdateStatus("Please enter username and password.");
                return;
            }

            UpdateStatus("Sending credentials...");
            NetworkManager.Instance.SendLogin(user, pass);
        }

        private void UpdateStatus(string msg)
        {
            if (StatusText != null) StatusText.text = msg;
            Debug.Log("[LoginUI] " + msg);
        }
    }
}
