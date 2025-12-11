using UnityEngine;
using iDate.Network;
using System.IO;

namespace iDate.Gameplay
{
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager Instance;

        public GameObject NotePrefab;
        public Transform[] LaneTargets; // Locations for Up/Left/Down/Right targets
        public Transform SpawnOrigin;

        // Score thresholds (in seconds)
        public float PerfectWindow = 0.05f;
        public float GreatWindow = 0.10f;
        public float CoolWindow = 0.15f;
        public float BadWindow = 0.25f;

        private int _currentScore = 0;
        private int _currentCombo = 0;

        public event System.Action<string> OnJudgment; // Perfect, Great, Cool, Bad, Miss
        public event System.Action OnGameStarting;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (iDate.UI.SongManager.Instance != null && iDate.UI.SongManager.Instance.SelectedSong.HasValue)
            {
                Conductor.Instance.LoadSong(iDate.UI.SongManager.Instance.SelectedSong.Value);
            }
            else
            {
                Debug.LogWarning("[GameplayManager] No song selected in SongManager. Using default Conductor settings.");
            }
            
            // Start Music after slight delay or immediately
            Conductor.Instance.StartMusic();
            OnGameStarting?.Invoke();
        }

        private void Update()
        {
            // Input Handling (New Input System or Legacy)
            // Using Legacy for simplicity in prototype
            if (Input.GetKeyDown(KeyCode.UpArrow)) CheckHit(0);
            if (Input.GetKeyDown(KeyCode.LeftArrow)) CheckHit(1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) CheckHit(2);
            if (Input.GetKeyDown(KeyCode.RightArrow)) CheckHit(3);
        }

        public void CheckHit(int lane)
        {
            // Find closest note in this lane
            // Optimization: Keep a list of active notes per lane.
            // For prototype: FindObjects (Slow)
            
            NoteController[] notes = FindObjectsByType<NoteController>(FindObjectsSortMode.None);
            NoteController targetInfo = null;
            float minDiff = float.MaxValue;

            float currentSongTime = Conductor.Instance.SongPosition;

            foreach (var note in notes)
            {
                if (note.LaneIndex != lane) continue;
                
                // Calculate time difference
                // Note Beat * SecPerBeat = Note Time
                float noteTime = note.TargetBeat * (60f / Conductor.Instance.Bpm);
                float diff = Mathf.Abs(noteTime - currentSongTime);

                if (diff < minDiff)
                {
                    minDiff = diff;
                    targetInfo = note;
                }
            }

            if (targetInfo != null)
            {
                // Determine Judgment
                string rank = "Miss";
                int scoreAdd = 0;
                byte judgeId = 0; // 1=Perf, 2=Great, 3=Cool, 4=Bad, 5=Miss

                if (minDiff <= PerfectWindow) { rank = "Perfect"; scoreAdd = 500; judgeId = 1; }
                else if (minDiff <= GreatWindow) { rank = "Great"; scoreAdd = 300; judgeId = 2; }
                else if (minDiff <= CoolWindow) { rank = "Cool"; scoreAdd = 100; judgeId = 3; }
                else if (minDiff <= BadWindow) { rank = "Bad"; scoreAdd = 50; judgeId = 4; _currentCombo = 0; } // Bad often breaks combo? Or just gives low score? Let's say Bad breaks combo or just 0 combo add. Usually Bad breaks combo in rhythmic games.
                else { rank = "Miss"; judgeId = 5; _currentCombo = 0; }
                
                Debug.Log($"Hit Lane {lane}: {rank} ({minDiff:F3}s)");
                
                if (rank != "Miss" && rank != "Bad")
                {
                     _currentCombo++;
                }

                targetInfo.CanMiss = false; // Prevent double miss processing
                
                // VFX
                if (iDate.Visuals.VFXManager.Instance != null)
                {
                    iDate.Visuals.VFXManager.Instance.PlayHitEffect(targetInfo.transform.position, rank);
                }
                
                Destroy(targetInfo.gameObject); // Visual hit effect would replace this
                
                // Send Score
                _currentScore += scoreAdd;
                NetworkManager.Instance.SendScore(_currentScore, (short)_currentCombo, judgeId);
                OnJudgment?.Invoke(rank);
            }
        }
        
        public void OnScoreReceived(uint playerId, int score, short combo, byte judge)
        {
            Debug.Log($"Enemy {playerId} Scored: {score} (x{combo})");
            // TODO: Update HUD for opponent
        }

        public void ReportHit(bool success, int lane)
        {
            if (!success) 
            {
                 Debug.Log("Miss!");
                 OnJudgment?.Invoke("Miss");
                 _currentCombo = 0;
            }
        }
    }
}
