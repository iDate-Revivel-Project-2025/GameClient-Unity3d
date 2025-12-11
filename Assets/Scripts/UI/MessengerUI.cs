using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Managers;

namespace iDate.UI
{
    public class MessengerUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject windowPanel;
        public TMP_InputField targetInput;
        public TMP_InputField messageInput;
        public Button sendButton;
        public Button closeButton;
        public TextMeshProUGUI logText;
        public ScrollRect logScroll;

        private void Start()
        {
            if (sendButton) sendButton.onClick.AddListener(OnSendClicked);
            if (closeButton) closeButton.onClick.AddListener(Close);
            
            // Subscribe to Manager
            if (MessengerManager.Instance != null)
            {
                MessengerManager.Instance.OnMessageReceived += HandleMessageReceived;
                MessengerManager.Instance.OnMessageSentResult += HandleMessageSentResult;
            }
        }

        private void OnDestroy()
        {
            if (MessengerManager.Instance != null)
            {
                MessengerManager.Instance.OnMessageReceived -= HandleMessageReceived;
                MessengerManager.Instance.OnMessageSentResult -= HandleMessageSentResult;
            }
        }

        public void Toggle()
        {
            if (windowPanel) windowPanel.SetActive(!windowPanel.activeSelf);
        }

        public void Open()
        {
            if (windowPanel) windowPanel.SetActive(true);
        }

        public void Close()
        {
            if (windowPanel) windowPanel.SetActive(false);
        }

        private void OnSendClicked()
        {
            string target = targetInput.text;
            string msg = messageInput.text;

            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(msg))
            {
                AppendLog("<color=red>Target and Message required.</color>");
                return;
            }

            if (MessengerManager.Instance != null)
            {
                MessengerManager.Instance.SendMessage(target, msg);
                // Don't clear message yet, wait for success? 
                // Or clear immediately for better UX?
                messageInput.text = "";
                AppendLog($"<color=yellow>To {target}: {msg}</color>");
            }
        }

        private void HandleMessageReceived(uint senderId, string senderName, string message)
        {
            // If window is closed, maybe flash icon? (Not implemented yet)
            // For now just append to log if open, or always append.
            AppendLog($"<color=green>[{senderName}]: {message}</color>");
            
            // Auto open?
            if (windowPanel && !windowPanel.activeSelf)
            {
                // Optional: Open();
            }
        }

        private void HandleMessageSentResult(bool success, string msg)
        {
            if (success)
            {
                AppendLog($"<color=cyan>[System]: {msg}</color>");
            }
            else
            {
                AppendLog($"<color=red>[Error]: {msg}</color>");
            }
        }

        private void AppendLog(string text)
        {
            if (logText)
            {
                logText.text += text + "\n";
                // Auto scroll?
                if (logScroll) logScroll.verticalNormalizedPosition = 0;
            }
        }
    }
}
