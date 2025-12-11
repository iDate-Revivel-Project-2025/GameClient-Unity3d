using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Gameplay;
using System.Collections.Generic;

namespace iDate.UI
{
    public class RoomUI : MonoBehaviour
    {
        [Header("Room Info")]
        public TextMeshProUGUI RoomNameText;
        public TextMeshProUGUI ModeText;
        
        [Header("Controls")]
        public Button ReadyButton;
        public TextMeshProUGUI ReadyButtonText;
        public Button StartButton;
        public Button LeaveButton;

        [Header("Settings (Host Only)")]
        public TMP_Dropdown GameModeDropdown;
        public TMP_Dropdown DifficultyDropdown;
        public TMP_InputField StageIdInput;
        public TMP_InputField MusicIdInput;
        
        [Header("Player List")]
        public Transform SlotContainer;
        public GameObject PlayerListItemPrefab; // Assume a simple Text or Prefab
        
        private void Start()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnRoomJoined += UpdateRoomInfo;
                RoomManager.Instance.OnPlayerJoined += OnPlayerListChanged;
                RoomManager.Instance.OnPlayerLeft += OnPlayerListChanged;
                RoomManager.Instance.OnPlayerReadyChanged += OnPlayerReadyChanged;
                RoomManager.Instance.OnRoomSettingsChanged += UpdateRoomInfo;
                
                // Initial State
                if (RoomManager.Instance.CurrentRoom != null)
                {
                    UpdateRoomInfo(RoomManager.Instance.CurrentRoom);
                    RefreshPlayerList();
                }
            }
            
            ReadyButton.onClick.AddListener(OnReadyClicked);
            StartButton.onClick.AddListener(OnStartClicked);
            LeaveButton.onClick.AddListener(OnLeaveClicked);
            
            // Setup Options
            SetupDropdowns();
            
            // Listeners
            if (GameModeDropdown != null) GameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            if (DifficultyDropdown != null) DifficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            if (StageIdInput != null) StageIdInput.onEndEdit.AddListener(OnStageIdChanged);
            if (MusicIdInput != null) MusicIdInput.onEndEdit.AddListener(OnMusicIdChanged);

            UpdateButtons();
        }

        private void SetupDropdowns()
        {
            if (GameModeDropdown != null)
            {
                GameModeDropdown.ClearOptions();
                GameModeDropdown.AddOptions(new List<string> { "Normal", "Battle", "Team" });
            }
            if (DifficultyDropdown != null)
            {
                DifficultyDropdown.ClearOptions();
                DifficultyDropdown.AddOptions(new List<string> { "Easy", "Normal", "Hard", "Insane" });
            }
        }

        private void OnDestroy()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnRoomJoined -= UpdateRoomInfo;
                RoomManager.Instance.OnPlayerJoined -= OnPlayerListChanged;
                RoomManager.Instance.OnPlayerLeft -= OnPlayerListChanged;
                RoomManager.Instance.OnPlayerReadyChanged -= OnPlayerReadyChanged;
                RoomManager.Instance.OnRoomSettingsChanged -= UpdateRoomInfo;
            }
        }

        private void UpdateRoomInfo(RoomSettings settings)
        {
            if (settings == null) return;
            RoomNameText.text = settings.RoomName;
            ModeText.text = $"Mode: {settings.GameMode} | Diff: {settings.Difficulty}";
            
            // Update UI elements values without triggering events?
            // Unity UI events trigger on value change. We might loop.
            // But we can check if value is different.
            if (GameModeDropdown != null) GameModeDropdown.SetValueWithoutNotify(settings.GameMode);
            // Difficulty mapping: 1, 5, 8, 10
            // Simplified: 0, 1, 2, 3
            if (DifficultyDropdown != null) DifficultyDropdown.SetValueWithoutNotify(settings.Difficulty); 
            
            if (StageIdInput != null) StageIdInput.SetTextWithoutNotify(settings.StageId.ToString());
            if (MusicIdInput != null) MusicIdInput.SetTextWithoutNotify(settings.SongId);

            gameObject.SetActive(true); // Show this UI
            UpdateButtons();
        }

        private void OnPlayerListChanged(RoomPlayer player)
        {
            RefreshPlayerList();
            UpdateButtons();
        }
        
        private void OnPlayerReadyChanged(RoomPlayer player)
        {
            // Update visual ready state in list
            RefreshPlayerList(); 
            // Also update our own button text if it was us
            if (player == RoomManager.Instance.LocalPlayer)
            {
                ReadyButtonText.text = player.IsReady ? "Unready" : "Ready";
            }
            UpdateButtons();
        }

        private void RefreshPlayerList()
        {
            // Simple clear and rebuild for prototype
            foreach (Transform child in SlotContainer)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var player in RoomManager.Instance.Players)
            {
                // Instantiate Prefab or create Text
                // For simplicity in code-only creation:
                GameObject go = new GameObject("Player_" + player.PlayerName);
                go.transform.SetParent(SlotContainer, false);
                var text = go.AddComponent<TextMeshProUGUI>();
                
                string readyStatus = player.IsReady ? "<color=green>[READY]</color>" : "<color=red>[WAITING]</color>";
                if (player.IsHost) readyStatus = "<color=yellow>[HOST]</color>";
                
                text.text = $"{player.PlayerName} {readyStatus}";
                text.fontSize = 24;
                text.color = Color.white;
            }
        }

        private void UpdateButtons()
        {
            if (RoomManager.Instance.LocalPlayer == null) return;
            
            bool isHost = RoomManager.Instance.LocalPlayer.IsHost;
            StartButton.gameObject.SetActive(isHost);
            ReadyButton.gameObject.SetActive(!isHost); // Host doesn't ready up usually, or Host Ready = Start?
            // Actually in iDate Host handles Start. Users handle Ready.
            
            // If Host, enable Start only if everyone else is ready? 
            // We can leave that validation to Server or check locally.
        }

        private void OnReadyClicked()
        {
            bool currentState = RoomManager.Instance.LocalPlayer.IsReady;
            RoomManager.Instance.SetReady(!currentState);
        }

        private void OnStartClicked()
        {
            RoomManager.Instance.StartGame();
        }

        private void OnLeaveClicked()
        {
            RoomManager.Instance.LeaveRoom();
            gameObject.SetActive(false); // Hide self
        }

        private void OnGameModeChanged(int index)
        {
            if (RoomManager.Instance.IsHost())
                RoomManager.Instance.SetGameMode(index, 0); // 0 = 4k default
        }

        private void OnDifficultyChanged(int index)
        {
            if (RoomManager.Instance.IsHost())
                RoomManager.Instance.SetDifficulty(index); // Mapping 0-3 directly
        }

        private void OnStageIdChanged(string value)
        {
            if (RoomManager.Instance.IsHost() && int.TryParse(value, out int result))
                RoomManager.Instance.SetStage(result);
        }

        private void OnMusicIdChanged(string value)
        {
             if (RoomManager.Instance.IsHost() && int.TryParse(value, out int result))
                RoomManager.Instance.SetMusic(result);
        }
    }
}
