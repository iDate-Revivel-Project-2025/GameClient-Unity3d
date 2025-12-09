using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Network;

namespace iDate.UI
{
    public class LoginLogic : MonoBehaviour
    {
        [Header("UI References (Assign from Imported UI)")]
        public TMP_InputField UsernameInput; // ID 101
        public TMP_InputField PasswordInput; // ID 102
        public Button LoginButton;           // ID 103
        
        [Header("Settings")]
        public string ServerIP = "127.0.0.1";
        public int ServerPort = 5001;

        private void Awake()
        {
            // Auto-bind if not assigned
            if (UsernameInput == null) UsernameInput = transform.Find("Ime_101")?.GetComponent<TMP_InputField>();
            if (PasswordInput == null) PasswordInput = transform.Find("Ime_102")?.GetComponent<TMP_InputField>();
            
            // Try simplified name first, then cmd name
            if (LoginButton == null) 
            {
                Transform btnTr = transform.Find("Button_103") ?? transform.Find("Button_103_Cmd_LOGIN");
                LoginButton = btnTr?.GetComponent<Button>();
            }
        }

        private void Start()
        {
            if (LoginButton != null)
                LoginButton.onClick.AddListener(OnLoginClicked);
                
            // Auto Connect
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Connect(ServerIP, ServerPort);
            }
        }

        private void OnLoginClicked()
        {
            string user = UsernameInput.text;
            string pass = PasswordInput.text;
            
            Debug.Log($"Attempting Login: {user}");
            
            // Use Manager to send login
            if (NetworkManager.Instance != null)
            {
                // Hook into packet event to detect success, or UIManager can listen.
                // For now, let's just send it. The NetworkManager usually fires events.
                NetworkManager.Instance.SendLogin(user, pass);
            }
        }
        
        // Add listener for login success to transition UI
        private void OnEnable()
        {
            if (NetworkManager.Instance != null) 
                NetworkManager.Instance.OnPacketReceived += OnPacket;
        }

        private void OnDisable()
        {
             if (NetworkManager.Instance != null) 
                NetworkManager.Instance.OnPacketReceived -= OnPacket;
        }

        private void OnPacket(Packet p)
        {
            if ((OpCode)p.OpCode == OpCode.LOGIN_RESP)
            {
                Debug.Log("Login Logic: Success!");
                // Transition via UIManager
                if (UIManager.Instance != null)
                {
                    // Delay slightly to let animation play?
                   UIManager.Instance.OnLoginSuccess();
                }
            }
        }
        
        private byte[] BuildLoginPayload(string u, string p)
        {
            // Simple byte writer
            // Using IDO_Format.md or Packet_Protocol.md specs
            return new byte[100]; // Placeholder
        }
    }
}
