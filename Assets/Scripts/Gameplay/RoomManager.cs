using System;
using System.Collections.Generic;
using UnityEngine;
using iDate.Network;

namespace iDate.Gameplay
{
    /// <summary>
    /// Room settings and configuration
    /// </summary>
    [Serializable]
    public class RoomSettings
    {
        public string RoomName;
        public string Password;
        public int MaxPlayers = 8;
        public int GameMode; // 0 = Normal, 1 = Battle, 2 = Team
        public int Difficulty = 5;
        public string SongId;
        public bool IsPrivate;
    }

    /// <summary>
    /// Player state in a room
    /// </summary>
    [Serializable]
    public class RoomPlayer
    {
        public string PlayerId;
        public string PlayerName;
        public bool IsReady;
        public bool IsHost;
        public int Team; // 0 = Red, 1 = Blue (for team mode)
        public Character.CharacterData Character;
    }

    /// <summary>
    /// Room state
    /// </summary>
    public enum RoomState
    {
        Waiting,    // Waiting for players
        Ready,      // All players ready
        Loading,    // Loading game
        Playing,    // Game in progress
        Results     // Showing results
    }

    /// <summary>
    /// Room manager - handles multiplayer room logic
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [Header("Current Room")]
        public RoomSettings CurrentRoom;
        public RoomState State = RoomState.Waiting;
        public List<RoomPlayer> Players = new List<RoomPlayer>();

        [Header("Local Player")]
        public RoomPlayer LocalPlayer;

        // Events
        public event Action<RoomSettings> OnRoomCreated;
        public event Action<RoomSettings> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<RoomPlayer> OnPlayerJoined;
        public event Action<RoomPlayer> OnPlayerLeft;
        public event Action<RoomPlayer> OnPlayerReadyChanged;
        public event Action OnGameStarting;
        public event Action<RoomState> OnStateChanged;

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
            }
        }

        private void Start()
        {
            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived += OnPacketReceived;
            }
        }

        /// <summary>
        /// Create a new room
        /// </summary>
        public void CreateRoom(RoomSettings settings)
        {
            CurrentRoom = settings;
            State = RoomState.Waiting;
            Players.Clear();

            // Add local player as host
            LocalPlayer = new RoomPlayer
            {
                PlayerId = "LOCAL", // Will be set by server
                PlayerName = "Player",
                IsHost = true,
                IsReady = false
            };
            Players.Add(LocalPlayer);

            // Send create room packet
            SendCreateRoomPacket(settings);

            OnRoomCreated?.Invoke(settings);
            Debug.Log($"Created room: {settings.RoomName}");
        }

        /// <summary>
        /// Join an existing room
        /// </summary>
        public void JoinRoom(int roomId, string password = "")
        {
            // Send join room packet
            SendJoinRoomPacket(roomId, password);
            Debug.Log($"Joining room: {roomId}");
        }

        /// <summary>
        /// Leave current room
        /// </summary>
        public void LeaveRoom()
        {
            if (CurrentRoom == null) return;

            // Send leave room packet
            SendLeaveRoomPacket();

            CurrentRoom = null;
            State = RoomState.Waiting;
            Players.Clear();
            LocalPlayer = null;

            OnRoomLeft?.Invoke();
            Debug.Log("Left room");
        }

        /// <summary>
        /// Set local player ready state
        /// </summary>
        public void SetReady(bool ready)
        {
            if (LocalPlayer == null) return;

            LocalPlayer.IsReady = ready;

            // Send ready packet
            SendReadyPacket(ready);

            OnPlayerReadyChanged?.Invoke(LocalPlayer);
            Debug.Log($"Set ready: {ready}");

            // Check if all players are ready
            CheckAllReady();
        }

        /// <summary>
        /// Start the game (host only)
        /// </summary>
        public void StartGame()
        {
            if (LocalPlayer == null || !LocalPlayer.IsHost)
            {
                Debug.LogWarning("Only host can start the game!");
                return;
            }

            if (!AllPlayersReady())
            {
                Debug.LogWarning("Not all players are ready!");
                return;
            }

            // Send start game packet
            SendStartGamePacket();

            ChangeState(RoomState.Loading);
            OnGameStarting?.Invoke();
            Debug.Log("Starting game...");
        }

        /// <summary>
        /// Check if all players are ready
        /// </summary>
        private bool AllPlayersReady()
        {
            foreach (var player in Players)
            {
                if (!player.IsReady) return false;
            }
            return Players.Count > 0;
        }

        /// <summary>
        /// Check if all players are ready and update state
        /// </summary>
        private void CheckAllReady()
        {
            if (AllPlayersReady())
            {
                ChangeState(RoomState.Ready);
            }
            else
            {
                ChangeState(RoomState.Waiting);
            }
        }

        /// <summary>
        /// Change room state
        /// </summary>
        private void ChangeState(RoomState newState)
        {
            if (State == newState) return;

            State = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"Room state changed: {newState}");
        }

        /// <summary>
        /// Handle incoming network packets
        /// </summary>
        private void OnPacketReceived(Packet packet)
        {
            switch ((OpCode)packet.OpCode)
            {
                case OpCode.ROOM_CREATE_RESP:
                    HandleRoomCreateResponse(packet);
                    break;
                case OpCode.ROOM_JOIN_RESP:
                    HandleRoomJoinResponse(packet);
                    break;
                case OpCode.ROOM_PLAYER_JOIN:
                    HandlePlayerJoined(packet);
                    break;
                case OpCode.ROOM_PLAYER_LEAVE:
                    HandlePlayerLeft(packet);
                    break;
                case OpCode.ROOM_PLAYER_READY:
                    HandlePlayerReady(packet);
                    break;
                case OpCode.GAME_START:
                    HandleGameStart(packet);
                    break;
            }
        }

        // Packet Handlers

        private void HandleRoomCreateResponse(Packet packet)
        {
            // Parse room ID from server
            // For now, just confirm creation
            Debug.Log("Room created successfully");
        }

        private void HandleRoomJoinResponse(Packet packet)
        {
            // Parse room data and player list
            // For now, simulate joining
            CurrentRoom = new RoomSettings
            {
                RoomName = "Test Room",
                MaxPlayers = 8
            };

            LocalPlayer = new RoomPlayer
            {
                PlayerId = "LOCAL",
                PlayerName = "Player",
                IsHost = false,
                IsReady = false
            };
            Players.Add(LocalPlayer);

            OnRoomJoined?.Invoke(CurrentRoom);
            Debug.Log("Joined room successfully");
        }

        private void HandlePlayerJoined(Packet packet)
        {
            // Parse player data
            RoomPlayer newPlayer = new RoomPlayer
            {
                PlayerId = "PLAYER_" + Players.Count,
                PlayerName = $"Player {Players.Count + 1}",
                IsHost = false,
                IsReady = false
            };

            Players.Add(newPlayer);
            OnPlayerJoined?.Invoke(newPlayer);
            Debug.Log($"Player joined: {newPlayer.PlayerName}");
        }

        private void HandlePlayerLeft(Packet packet)
        {
            // Parse player ID and remove
            // For now, remove last player
            if (Players.Count > 1)
            {
                RoomPlayer player = Players[Players.Count - 1];
                Players.RemoveAt(Players.Count - 1);
                OnPlayerLeft?.Invoke(player);
                Debug.Log($"Player left: {player.PlayerName}");
            }
        }

        private void HandlePlayerReady(Packet packet)
        {
            // Parse player ID and ready state
            // For now, just log
            Debug.Log("Player ready state changed");
            CheckAllReady();
        }

        private void HandleGameStart(Packet packet)
        {
            ChangeState(RoomState.Loading);
            OnGameStarting?.Invoke();
            Debug.Log("Game starting!");

            // Load game scene
            LoadGameScene();
        }

        // Packet Senders

        private void SendCreateRoomPacket(RoomSettings settings)
        {
            if (NetworkManager.Instance == null) return;

            using (Packet packet = new Packet((ushort)OpCode.ROOM_CREATE))
            {
                packet.WriteString(settings.RoomName);
                packet.WriteString(settings.Password);
                packet.WriteInt32(settings.MaxPlayers);
                packet.WriteInt32(settings.GameMode);
                packet.WriteInt32(settings.Difficulty);
                packet.WriteString(settings.SongId ?? "");
                packet.WriteBool(settings.IsPrivate);

                NetworkManager.Instance.Send(packet);
            }
        }

        private void SendJoinRoomPacket(int roomId, string password)
        {
            if (NetworkManager.Instance == null) return;

            using (Packet packet = new Packet((ushort)OpCode.ROOM_JOIN))
            {
                packet.WriteInt32(roomId);
                packet.WriteString(password);

                NetworkManager.Instance.Send(packet);
            }
        }

        private void SendLeaveRoomPacket()
        {
            if (NetworkManager.Instance == null) return;

            using (Packet packet = new Packet((ushort)OpCode.ROOM_LEAVE))
            {
                NetworkManager.Instance.Send(packet);
            }
        }

        private void SendReadyPacket(bool ready)
        {
            if (NetworkManager.Instance == null) return;

            using (Packet packet = new Packet((ushort)OpCode.ROOM_READY))
            {
                packet.WriteBool(ready);

                NetworkManager.Instance.Send(packet);
            }
        }

        private void SendStartGamePacket()
        {
            if (NetworkManager.Instance == null) return;

            using (Packet packet = new Packet((ushort)OpCode.GAME_START))
            {
                packet.WriteString(CurrentRoom.SongId ?? "TEST001");
                packet.WriteInt32(CurrentRoom.Difficulty);

                NetworkManager.Instance.Send(packet);
            }
        }

        /// <summary>
        /// Load the game scene
        /// </summary>
        private void LoadGameScene()
        {
            // Load game scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }

        /// <summary>
        /// Get player by ID
        /// </summary>
        public RoomPlayer GetPlayer(string playerId)
        {
            return Players.Find(p => p.PlayerId == playerId);
        }

        /// <summary>
        /// Get player count
        /// </summary>
        public int GetPlayerCount()
        {
            return Players.Count;
        }

        /// <summary>
        /// Check if local player is host
        /// </summary>
        public bool IsHost()
        {
            return LocalPlayer != null && LocalPlayer.IsHost;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPacketReceived -= OnPacketReceived;
            }
        }
    }
}
