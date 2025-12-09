using UnityEngine;
using UnityEngine.UI;
using iDate.Network;

namespace iDate.UI
{
    /// <summary>
    /// Manages the entire UI flow of the game
    /// Login → Server Select → Lobby → Game
    /// </summary>
    public class UIFlowManager : MonoBehaviour
    {
        public static UIFlowManager Instance;

        [Header("UI Panels")]
        public GameObject LoginPanel;
        public GameObject ServerSelectPanel;
        public GameObject LobbyPanel;
        public GameObject GamePanel;
        public GameObject LoadingPanel;

        [Header("Current State")]
        public UIState CurrentState = UIState.Login;

        public enum UIState
        {
            Login,
            ServerSelect,
            Lobby,
            InGame,
            Loading
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnected += OnNetworkConnected;
                NetworkManager.Instance.OnDisconnected += OnNetworkDisconnected;
            }
        }

        private void Start()
        {
            // Start at login screen
            ShowState(UIState.Login);
        }

        /// <summary>
        /// Show a specific UI state
        /// </summary>
        public void ShowState(UIState state)
        {
            CurrentState = state;

            // Hide all panels
            if (LoginPanel) LoginPanel.SetActive(false);
            if (ServerSelectPanel) ServerSelectPanel.SetActive(false);
            if (LobbyPanel) LobbyPanel.SetActive(false);
            if (GamePanel) GamePanel.SetActive(false);
            if (LoadingPanel) LoadingPanel.SetActive(false);

            // Show the requested panel
            switch (state)
            {
                case UIState.Login:
                    if (LoginPanel) LoginPanel.SetActive(true);
                    Debug.Log("UI: Showing Login Screen");
                    break;

                case UIState.ServerSelect:
                    if (ServerSelectPanel) ServerSelectPanel.SetActive(true);
                    Debug.Log("UI: Showing Server Select");
                    break;

                case UIState.Lobby:
                    if (LobbyPanel) LobbyPanel.SetActive(true);
                    Debug.Log("UI: Showing Lobby");
                    break;

                case UIState.InGame:
                    if (GamePanel) GamePanel.SetActive(true);
                    Debug.Log("UI: Showing Game");
                    break;

                case UIState.Loading:
                    if (LoadingPanel) LoadingPanel.SetActive(true);
                    Debug.Log("UI: Showing Loading");
                    break;
            }
        }

        /// <summary>
        /// Called when login is successful
        /// </summary>
        public void OnLoginSuccess()
        {
            Debug.Log("UIFlow: Login successful, moving to Server Select");
            ShowState(UIState.ServerSelect);
        }

        /// <summary>
        /// Called when server/channel is selected
        /// </summary>
        public void OnChannelSelected()
        {
            Debug.Log("UIFlow: Channel selected, moving to Lobby");
            ShowState(UIState.Lobby);
        }

        /// <summary>
        /// Called when entering a game room
        /// </summary>
        public void OnEnterGame()
        {
            Debug.Log("UIFlow: Entering game");
            ShowState(UIState.InGame);
        }

        /// <summary>
        /// Called when leaving game back to lobby
        /// </summary>
        public void OnLeaveGame()
        {
            Debug.Log("UIFlow: Leaving game, returning to Lobby");
            ShowState(UIState.Lobby);
        }

        /// <summary>
        /// Called when logging out
        /// </summary>
        public void OnLogout()
        {
            Debug.Log("UIFlow: Logging out");
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Disconnect();
            }
            ShowState(UIState.Login);
        }

        /// <summary>
        /// Show loading screen with message
        /// </summary>
        public void ShowLoading(string message = "Loading...")
        {
            ShowState(UIState.Loading);
            // TODO: Update loading text
        }

        private void OnNetworkConnected()
        {
            Debug.Log("UIFlow: Network connected");
        }

        private void OnNetworkDisconnected()
        {
            Debug.Log("UIFlow: Network disconnected, returning to login");
            ShowState(UIState.Login);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnected -= OnNetworkConnected;
                NetworkManager.Instance.OnDisconnected -= OnNetworkDisconnected;
            }
        }
    }
}
