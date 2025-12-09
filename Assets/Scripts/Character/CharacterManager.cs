using System.Collections.Generic;
using UnityEngine;

namespace iDate.Character
{
    /// <summary>
    /// Manages all characters in the scene (local player + other players)
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [Header("Character Prefab")]
        public GameObject CharacterPrefab;

        [Header("Spawn Settings")]
        public Transform SpawnPoint;
        public float SpawnSpacing = 2f;

        private Dictionary<string, CharacterController> _characters = new Dictionary<string, CharacterController>();
        private CharacterController _localPlayer;

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

        /// <summary>
        /// Spawn a character
        /// </summary>
        public CharacterController SpawnCharacter(CharacterData data, bool isLocalPlayer = false)
        {
            if (_characters.ContainsKey(data.CharacterId))
            {
                Debug.LogWarning($"Character {data.CharacterId} already exists!");
                return _characters[data.CharacterId];
            }

            // Calculate spawn position
            Vector3 spawnPos = SpawnPoint != null ? SpawnPoint.position : Vector3.zero;
            spawnPos += Vector3.right * (_characters.Count * SpawnSpacing);

            // Create character GameObject
            GameObject charObj;
            if (CharacterPrefab != null)
            {
                charObj = Instantiate(CharacterPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                charObj = new GameObject($"Character_{data.CharacterName}");
                charObj.transform.position = spawnPos;
            }

            // Add CharacterController component
            CharacterController controller = charObj.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = charObj.AddComponent<CharacterController>();
            }

            // Set data and load
            controller.Data = data;
            controller.LoadCharacter();

            // Register
            _characters[data.CharacterId] = controller;

            if (isLocalPlayer)
            {
                _localPlayer = controller;
                Debug.Log($"Local player set: {data.CharacterName}");
            }

            Debug.Log($"Spawned character: {data.CharacterName} at {spawnPos}");
            return controller;
        }

        /// <summary>
        /// Remove a character
        /// </summary>
        public void RemoveCharacter(string characterId)
        {
            if (_characters.ContainsKey(characterId))
            {
                CharacterController controller = _characters[characterId];
                _characters.Remove(characterId);
                
                if (controller == _localPlayer)
                {
                    _localPlayer = null;
                }

                Destroy(controller.gameObject);
                Debug.Log($"Removed character: {characterId}");
            }
        }

        /// <summary>
        /// Get character by ID
        /// </summary>
        public CharacterController GetCharacter(string characterId)
        {
            return _characters.ContainsKey(characterId) ? _characters[characterId] : null;
        }

        /// <summary>
        /// Get local player character
        /// </summary>
        public CharacterController GetLocalPlayer()
        {
            return _localPlayer;
        }

        /// <summary>
        /// Get all characters
        /// </summary>
        public List<CharacterController> GetAllCharacters()
        {
            return new List<CharacterController>(_characters.Values);
        }

        /// <summary>
        /// Clear all characters
        /// </summary>
        public void ClearAll()
        {
            foreach (var controller in _characters.Values)
            {
                if (controller != null)
                {
                    Destroy(controller.gameObject);
                }
            }
            _characters.Clear();
            _localPlayer = null;
        }

        [ContextMenu("Spawn Test Characters")]
        public void SpawnTestCharacters()
        {
            // Spawn local player
            CharacterData player = new CharacterData
            {
                CharacterId = "PLAYER001",
                CharacterName = "Player",
                Gender = 1,
                BaseModel = 1
            };
            player.EquippedItems[EquipSlot.Face] = 1;
            player.EquippedItems[EquipSlot.Head] = 4;
            player.EquippedItems[EquipSlot.Upper] = 4;
            player.EquippedItems[EquipSlot.Lower] = 4;
            player.EquippedItems[EquipSlot.Foot] = 4;

            SpawnCharacter(player, true);

            // Spawn some other players
            for (int i = 0; i < 3; i++)
            {
                CharacterData npc = new CharacterData
                {
                    CharacterId = $"NPC{i:D3}",
                    CharacterName = $"Player {i + 2}",
                    Gender = i % 2,
                    BaseModel = 1
                };
                npc.EquippedItems[EquipSlot.Face] = 1;
                npc.EquippedItems[EquipSlot.Head] = 4 + i;
                npc.EquippedItems[EquipSlot.Upper] = 4;
                npc.EquippedItems[EquipSlot.Lower] = 4;
                npc.EquippedItems[EquipSlot.Foot] = 4;

                SpawnCharacter(npc, false);
            }
        }
    }
}
