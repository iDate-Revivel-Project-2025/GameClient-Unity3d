using UnityEngine;
using iDate.Character;
using iDate.Gameplay;

namespace iDate
{
    /// <summary>
    /// Test harness for all iDate systems
    /// Use this to quickly test character loading, rhythm engine, etc.
    /// </summary>
    public class GameTester : MonoBehaviour
    {
        [Header("Test Modes")]
        public bool TestCharacterSystem = false;
        public bool TestRhythmEngine = false;
        public bool TestRoomSystem = false;

        [Header("Character Test Settings")]
        public int TestGender = 1; // 0 = Male, 1 = Female
        public int TestFaceId = 1;
        public int TestHeadId = 4;
        public int TestUpperId = 4;
        public int TestLowerId = 4;
        public int TestFootId = 4;

        [Header("Rhythm Test Settings")]
        public float TestBPM = 120f;
        public int TestDifficulty = 5;
        public float TestDuration = 30f;

        private void Start()
        {
            Debug.Log("=== iDate Game Tester Started ===");

            if (TestCharacterSystem)
            {
                RunCharacterTest();
            }

            if (TestRhythmEngine)
            {
                RunRhythmTest();
            }

            if (TestRoomSystem)
            {
                RunRoomTest();
            }
        }

        /// <summary>
        /// Test character loading system
        /// </summary>
        [ContextMenu("Test Character System")]
        public void RunCharacterTest()
        {
            Debug.Log("=== Testing Character System ===");

            // Ensure CharacterManager exists
            if (CharacterManager.Instance == null)
            {
                GameObject managerObj = new GameObject("CharacterManager");
                managerObj.AddComponent<CharacterManager>();
            }

            // Create test character data
            CharacterData testChar = new CharacterData
            {
                CharacterId = "TEST_CHAR_001",
                CharacterName = "Test Character",
                Gender = TestGender,
                BaseModel = 1
            };

            // Equip items
            testChar.EquippedItems[EquipSlot.Face] = TestFaceId;
            testChar.EquippedItems[EquipSlot.Head] = TestHeadId;
            testChar.EquippedItems[EquipSlot.Upper] = TestUpperId;
            testChar.EquippedItems[EquipSlot.Lower] = TestLowerId;
            testChar.EquippedItems[EquipSlot.Foot] = TestFootId;

            // Spawn character
            iDate.Character.CharacterController controller = CharacterManager.Instance.SpawnCharacter(testChar, true);

            if (controller != null)
            {
                Debug.Log("✅ Character spawned successfully!");
                Debug.Log($"   Name: {testChar.CharacterName}");
                Debug.Log($"   Gender: {(testChar.Gender == 0 ? "Male" : "Female")}");
                Debug.Log($"   Equipment: {testChar.EquippedItems.Count} items");
            }
            else
            {
                Debug.LogError("❌ Failed to spawn character!");
            }
        }

        /// <summary>
        /// Test rhythm engine
        /// </summary>
        [ContextMenu("Test Rhythm Engine")]
        public void RunRhythmTest()
        {
            Debug.Log("=== Testing Rhythm Engine ===");

            // Ensure RhythmEngine exists
            if (RhythmEngine.Instance == null)
            {
                GameObject engineObj = new GameObject("RhythmEngine");
                engineObj.AddComponent<RhythmEngine>();
            }

            // Create test note chart
            NoteChart testChart = new NoteChart
            {
                SongId = "TEST_SONG_001",
                SongName = "Test Song",
                BPM = TestBPM,
                Difficulty = TestDifficulty,
                Duration = TestDuration
            };

            // Generate test notes
            float beatInterval = 60f / TestBPM;
            RhythmKey[] keys = { RhythmKey.Left, RhythmKey.Down, RhythmKey.Up, RhythmKey.Right };

            for (float time = 2f; time < TestDuration; time += beatInterval)
            {
                Note note = new Note
                {
                    Time = time,
                    Key = keys[Random.Range(0, keys.Length)],
                    IsHold = false
                };
                testChart.Notes.Add(note);
            }

            Debug.Log($"✅ Created test chart with {testChart.Notes.Count} notes");
            Debug.Log($"   BPM: {TestBPM}");
            Debug.Log($"   Duration: {TestDuration}s");
            Debug.Log($"   Difficulty: {TestDifficulty}");

            // Subscribe to events
            RhythmEngine.Instance.OnNoteHit += (note, judgement) =>
            {
                Debug.Log($"Note Hit: {judgement} at {note.Time:F2}s");
            };

            RhythmEngine.Instance.OnComboChanged += (combo) =>
            {
                if (combo > 0 && combo % 10 == 0)
                {
                    Debug.Log($"Combo: {combo}!");
                }
            };

            RhythmEngine.Instance.OnGameEnd += (score) =>
            {
                Debug.Log("=== Game Ended ===");
                Debug.Log($"Final Score: {score.TotalScore}");
                Debug.Log($"Accuracy: {score.Accuracy:F1}%");
                Debug.Log($"Max Combo: {score.MaxCombo}");
                Debug.Log($"Perfect: {score.PerfectCount}");
                Debug.Log($"Great: {score.GreatCount}");
                Debug.Log($"Good: {score.GoodCount}");
                Debug.Log($"Bad: {score.BadCount}");
                Debug.Log($"Miss: {score.MissCount}");
            };

            // Start game
            Debug.Log("Starting rhythm game test...");
            Debug.Log("Press Arrow Keys to hit notes!");
            RhythmEngine.Instance.StartGame(testChart);
        }

        /// <summary>
        /// Test room system
        /// </summary>
        [ContextMenu("Test Room System")]
        public void RunRoomTest()
        {
            Debug.Log("=== Testing Room System ===");

            // Ensure RoomManager exists
            if (RoomManager.Instance == null)
            {
                GameObject roomObj = new GameObject("RoomManager");
                roomObj.AddComponent<RoomManager>();
            }

            // Subscribe to events
            RoomManager.Instance.OnRoomCreated += (settings) =>
            {
                Debug.Log($"✅ Room Created: {settings.RoomName}");
            };

            RoomManager.Instance.OnPlayerJoined += (player) =>
            {
                Debug.Log($"Player Joined: {player.PlayerName}");
            };

            RoomManager.Instance.OnPlayerReadyChanged += (player) =>
            {
                Debug.Log($"Player Ready Changed: {player.PlayerName} = {player.IsReady}");
            };

            RoomManager.Instance.OnStateChanged += (state) =>
            {
                Debug.Log($"Room State: {state}");
            };

            // Create test room
            RoomSettings testRoom = new RoomSettings
            {
                RoomName = "Test Room",
                Password = "",
                MaxPlayers = 8,
                GameMode = 0,
                Difficulty = 5,
                SongId = "TEST_SONG_001",
                IsPrivate = false
            };

            RoomManager.Instance.CreateRoom(testRoom);
            Debug.Log("Room created. Use context menu to test ready/start.");
        }

        [ContextMenu("Set Player Ready")]
        public void SetPlayerReady()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.SetReady(true);
                Debug.Log("Local player set to ready");
            }
        }

        [ContextMenu("Start Game (Host)")]
        public void StartGame()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.StartGame();
                Debug.Log("Starting game...");
            }
        }

        /// <summary>
        /// Test GB3 loading
        /// </summary>
        [ContextMenu("Test GB3 Loading")]
        public void TestGB3Loading()
        {
            Debug.Log("=== Testing GB3 Loading ===");

            string testPath = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Charactor/Skin/F01_PA_FACE_0001_00.gb3";

            if (!System.IO.File.Exists(testPath))
            {
                Debug.LogError($"Test file not found: {testPath}");
                return;
            }

            var model = iDate.Assets.GB3Loader.Load(testPath);

            if (model != null)
            {
                Debug.Log($"✅ GB3 Loaded: {model.Name}");
                Debug.Log($"   Nodes: {model.Nodes.Count}");
                Debug.Log($"   Meshes: {model.Meshes.Count}");
                Debug.Log($"   Bones: {model.Skeleton.Bones.Count}");

                // Create GameObject
                GameObject obj = iDate.Assets.GB3Loader.ToGameObject(model);
                if (obj != null)
                {
                    obj.transform.position = Vector3.zero;
                    Debug.Log($"✅ GameObject created: {obj.name}");
                }
            }
            else
            {
                Debug.LogError("❌ Failed to load GB3 file");
            }
        }

        /// <summary>
        /// Run all tests
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== Running All Tests ===");
            RunCharacterTest();
            RunRhythmTest();
            RunRoomTest();
            TestGB3Loading();
        }

        /// <summary>
        /// Show test controls
        /// </summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("=== iDate Game Tester ===");

            if (GUILayout.Button("Test Character System"))
            {
                RunCharacterTest();
            }

            if (GUILayout.Button("Test Rhythm Engine"))
            {
                RunRhythmTest();
            }

            if (GUILayout.Button("Test Room System"))
            {
                RunRoomTest();
            }

            if (GUILayout.Button("Test GB3 Loading"))
            {
                TestGB3Loading();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Run All Tests"))
            {
                RunAllTests();
            }

            GUILayout.Space(20);

            // Rhythm engine controls
            if (RhythmEngine.Instance != null && RhythmEngine.Instance.IsPlaying)
            {
                GUILayout.Label($"Song Time: {RhythmEngine.Instance.SongTime:F1}s");
                GUILayout.Label($"Score: {RhythmEngine.Instance.CurrentScore.TotalScore}");
                GUILayout.Label($"Combo: {RhythmEngine.Instance.CurrentScore.Combo}");

                if (GUILayout.Button("Stop Game"))
                {
                    RhythmEngine.Instance.StopGame();
                }
            }

            GUILayout.EndArea();
        }
    }
}
