using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Network;
using iDate.Gameplay;

namespace iDate.UI
{
    public class ResultUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ScoreText;
        public TextMeshProUGUI RewardsText;
        public Button LobbyButton;
        
        [Header("Animation")]
        public GameObject ContentPanel;
        
        private void Start()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnGameEnded += ShowResults;
            }
            
            LobbyButton.onClick.AddListener(OnLobbyClicked);
            ContentPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnGameEnded -= ShowResults;
            }
        }

        private void ShowResults(int exp, int points)
        {
            ContentPanel.SetActive(true);
            TitleText.text = "GAME CLEAR!";
            
            // Accessing private _currentScore via reflection or change GameplayManager accessor?
            // Or just rely on visual text if GameplayManager exposes it.
            // Let's assume user sees HUD score.
            // Better: Add Property to GameplayManager.
            
            // For now, simple text
            ScoreText.text = "Good Job!"; 
            
            RewardsText.text = $"Exp: +{exp}\nPoints: +{points}";
            
            // Disable HUD?
            // We can do that via RoomManager or GameplayManager state change.
        }

        private void OnLobbyClicked()
        {
            ContentPanel.SetActive(false);
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.LeaveRoom(); // Or Return to Room Lobby?
                // Usually "To Lobby" means leave game to Room.
                // RoomManager handles scene transition.
            }
        }
    }
}
