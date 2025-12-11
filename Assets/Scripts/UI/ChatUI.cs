using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Network;
using System.Text;
using System.Collections.Generic;

namespace iDate.UI
{
    public class ChatUI : MonoBehaviour
    {
        public static ChatUI Instance;

        public TMP_InputField ChatInput;
        public Transform MessageContainer;
        public GameObject MessagePrefab;
        public ScrollRect ScrollView;

        private void Awake()
        {
            Instance = this;
            if (ChatInput) ChatInput.onSubmit.AddListener(SendChatMessage);
        }

        public void AddMessage(string senderName, string message, Color? color = null)
        {
            if (!MessagePrefab || !MessageContainer) return;

            GameObject obj = Instantiate(MessagePrefab, MessageContainer);
            TextMeshProUGUI txt = obj.GetComponent<TextMeshProUGUI>();
            
            if (txt)
            {
                txt.text = $"<b>{senderName}:</b> {message}";
                if (color.HasValue) txt.color = color.Value;
            }

            // Auto scroll
            Canvas.ForceUpdateCanvases();
            if (ScrollView) ScrollView.verticalNormalizedPosition = 0f;
        }

        private void SendChatMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;

            // Send via Network
            NetworkManager.Instance.SendChat(msg);

            // Clear input
            ChatInput.text = "";
            ChatInput.ActivateInputField();
        }
    }
}
