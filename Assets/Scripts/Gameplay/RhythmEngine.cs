using System;
using System.Collections.Generic;
using UnityEngine;

namespace iDate.Gameplay
{
    /// <summary>
    /// Judgement types for rhythm game input timing
    /// </summary>
    public enum JudgementType
    {
        Miss = 0,
        Bad = 1,
        Good = 2,
        Great = 3,
        Perfect = 4
    }

    /// <summary>
    /// Input key types for the rhythm game
    /// </summary>
    public enum RhythmKey
    {
        Left = 0,
        Down = 1,
        Up = 2,
        Right = 3,
        Space = 4  // Special key
    }

    /// <summary>
    /// Note data structure
    /// </summary>
    [Serializable]
    public class Note
    {
        public float Time;           // Time in seconds when note should be hit
        public RhythmKey Key;        // Which key to press
        public bool IsHold;          // Is this a hold note?
        public float HoldDuration;   // Duration of hold (if IsHold)
        public bool IsHit;           // Has this note been hit?
        public JudgementType Judgement; // Judgement result
    }

    /// <summary>
    /// Note chart data - contains all notes for a song
    /// </summary>
    [Serializable]
    public class NoteChart
    {
        public string SongId;
        public string SongName;
        public float BPM;
        public int Difficulty;       // 1-10
        public List<Note> Notes = new List<Note>();
        public float Duration;       // Total song duration in seconds
    }

    /// <summary>
    /// Score data for a play session
    /// </summary>
    [Serializable]
    public class ScoreData
    {
        public int TotalScore;
        public int Combo;
        public int MaxCombo;
        public int PerfectCount;
        public int GreatCount;
        public int GoodCount;
        public int BadCount;
        public int MissCount;
        public float Accuracy; // 0-100%
    }

    /// <summary>
    /// Core rhythm game engine
    /// </summary>
    public class RhythmEngine : MonoBehaviour
    {
        public static RhythmEngine Instance { get; private set; }

        [Header("Timing Settings")]
        [Tooltip("Perfect timing window in seconds")]
        public float PerfectWindow = 0.05f;
        
        [Tooltip("Great timing window in seconds")]
        public float GreatWindow = 0.10f;
        
        [Tooltip("Good timing window in seconds")]
        public float GoodWindow = 0.15f;
        
        [Tooltip("Bad timing window in seconds")]
        public float BadWindow = 0.20f;

        [Header("Scoring")]
        public int PerfectScore = 1000;
        public int GreatScore = 800;
        public int GoodScore = 500;
        public int BadScore = 200;
        public int MissScore = 0;

        [Header("Gameplay State")]
        public NoteChart CurrentChart;
        public ScoreData CurrentScore;
        public bool IsPlaying { get; private set; }
        public float SongTime { get; private set; }

        [Header("Audio")]
        public AudioSource MusicSource;

        // Events
        public event Action<Note, JudgementType> OnNoteHit;
        public event Action<int> OnComboChanged;
        public event Action<ScoreData> OnGameEnd;

        private int _currentNoteIndex = 0;
        private List<Note> _activeNotes = new List<Note>();
        private float _startTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            // Update song time
            SongTime = Time.time - _startTime;

            // Check for notes that should be active
            UpdateActiveNotes();

            // Check for input
            ProcessInput();

            // Check for missed notes
            CheckMissedNotes();

            // Check if song is finished
            if (SongTime >= CurrentChart.Duration)
            {
                EndGame();
            }
        }

        /// <summary>
        /// Start playing a note chart
        /// </summary>
        public void StartGame(NoteChart chart)
        {
            if (chart == null || chart.Notes.Count == 0)
            {
                Debug.LogError("Cannot start game with empty chart!");
                return;
            }

            CurrentChart = chart;
            CurrentScore = new ScoreData();
            _currentNoteIndex = 0;
            _activeNotes.Clear();
            SongTime = 0f;
            _startTime = Time.time;
            IsPlaying = true;

            // Start music
            if (MusicSource != null)
            {
                MusicSource.Play();
            }

            Debug.Log($"Started game: {chart.SongName} (BPM: {chart.BPM}, Notes: {chart.Notes.Count})");
        }

        /// <summary>
        /// Stop the current game
        /// </summary>
        public void StopGame()
        {
            IsPlaying = false;
            
            if (MusicSource != null)
            {
                MusicSource.Stop();
            }

            Debug.Log("Game stopped");
        }

        /// <summary>
        /// Update list of active notes (notes that are visible/hittable)
        /// </summary>
        private void UpdateActiveNotes()
        {
            // Add notes that are coming up
            while (_currentNoteIndex < CurrentChart.Notes.Count)
            {
                Note note = CurrentChart.Notes[_currentNoteIndex];
                
                // Check if note should be active (within hit window + approach time)
                float approachTime = 2.0f; // Notes appear 2 seconds before hit time
                if (note.Time - SongTime <= approachTime)
                {
                    _activeNotes.Add(note);
                    _currentNoteIndex++;
                }
                else
                {
                    break;
                }
            }

            // Remove notes that are no longer active
            _activeNotes.RemoveAll(n => n.IsHit || (SongTime - n.Time) > BadWindow);
        }

        /// <summary>
        /// Process player input
        /// </summary>
        private void ProcessInput()
        {
            // Use new Input System
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // Check each key
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                ProcessKeyPress(RhythmKey.Left);
            
            if (keyboard.downArrowKey.wasPressedThisFrame)
                ProcessKeyPress(RhythmKey.Down);
            
            if (keyboard.upArrowKey.wasPressedThisFrame)
                ProcessKeyPress(RhythmKey.Up);
            
            if (keyboard.rightArrowKey.wasPressedThisFrame)
                ProcessKeyPress(RhythmKey.Right);
            
            if (keyboard.spaceKey.wasPressedThisFrame)
                ProcessKeyPress(RhythmKey.Space);
        }

        /// <summary>
        /// Process a key press
        /// </summary>
        private void ProcessKeyPress(RhythmKey key)
        {
            // Find the closest note for this key
            Note closestNote = null;
            float closestDistance = float.MaxValue;

            foreach (Note note in _activeNotes)
            {
                if (note.IsHit || note.Key != key) continue;

                float distance = Mathf.Abs(note.Time - SongTime);
                if (distance < closestDistance && distance <= BadWindow)
                {
                    closestDistance = distance;
                    closestNote = note;
                }
            }

            if (closestNote != null)
            {
                HitNote(closestNote, closestDistance);
            }
            else
            {
                // Bad press (no note nearby)
                Debug.Log($"Bad press: {key}");
                ResetCombo();
            }
        }

        /// <summary>
        /// Hit a note
        /// </summary>
        private void HitNote(Note note, float timingError)
        {
            // Calculate judgement
            JudgementType judgement = CalculateJudgement(timingError);
            
            note.IsHit = true;
            note.Judgement = judgement;

            // Update score
            UpdateScore(judgement);

            // Fire event
            OnNoteHit?.Invoke(note, judgement);

            Debug.Log($"Hit note at {note.Time:F2}s with {judgement} (error: {timingError * 1000:F0}ms)");
        }

        /// <summary>
        /// Calculate judgement based on timing error
        /// </summary>
        private JudgementType CalculateJudgement(float timingError)
        {
            if (timingError <= PerfectWindow)
                return JudgementType.Perfect;
            else if (timingError <= GreatWindow)
                return JudgementType.Great;
            else if (timingError <= GoodWindow)
                return JudgementType.Good;
            else if (timingError <= BadWindow)
                return JudgementType.Bad;
            else
                return JudgementType.Miss;
        }

        /// <summary>
        /// Update score based on judgement
        /// </summary>
        private void UpdateScore(JudgementType judgement)
        {
            int points = judgement switch
            {
                JudgementType.Perfect => PerfectScore,
                JudgementType.Great => GreatScore,
                JudgementType.Good => GoodScore,
                JudgementType.Bad => BadScore,
                _ => MissScore
            };

            // Apply combo multiplier
            float comboMultiplier = 1.0f + (CurrentScore.Combo * 0.01f); // 1% per combo
            points = Mathf.RoundToInt(points * comboMultiplier);

            CurrentScore.TotalScore += points;

            // Update judgement counts
            switch (judgement)
            {
                case JudgementType.Perfect:
                    CurrentScore.PerfectCount++;
                    CurrentScore.Combo++;
                    break;
                case JudgementType.Great:
                    CurrentScore.GreatCount++;
                    CurrentScore.Combo++;
                    break;
                case JudgementType.Good:
                    CurrentScore.GoodCount++;
                    CurrentScore.Combo++;
                    break;
                case JudgementType.Bad:
                    CurrentScore.BadCount++;
                    ResetCombo();
                    break;
                case JudgementType.Miss:
                    CurrentScore.MissCount++;
                    ResetCombo();
                    break;
            }

            // Update max combo
            if (CurrentScore.Combo > CurrentScore.MaxCombo)
            {
                CurrentScore.MaxCombo = CurrentScore.Combo;
            }

            OnComboChanged?.Invoke(CurrentScore.Combo);
        }

        /// <summary>
        /// Reset combo counter
        /// </summary>
        private void ResetCombo()
        {
            CurrentScore.Combo = 0;
            OnComboChanged?.Invoke(0);
        }

        /// <summary>
        /// Check for notes that were missed
        /// </summary>
        private void CheckMissedNotes()
        {
            foreach (Note note in _activeNotes)
            {
                if (!note.IsHit && (SongTime - note.Time) > BadWindow)
                {
                    // Note was missed
                    note.IsHit = true;
                    note.Judgement = JudgementType.Miss;
                    
                    CurrentScore.MissCount++;
                    ResetCombo();
                    
                    OnNoteHit?.Invoke(note, JudgementType.Miss);
                    Debug.Log($"Missed note at {note.Time:F2}s");
                }
            }
        }

        /// <summary>
        /// End the game and calculate final score
        /// </summary>
        private void EndGame()
        {
            IsPlaying = false;

            // Calculate accuracy
            int totalNotes = CurrentChart.Notes.Count;
            int hitNotes = CurrentScore.PerfectCount + CurrentScore.GreatCount + 
                          CurrentScore.GoodCount + CurrentScore.BadCount;
            
            CurrentScore.Accuracy = totalNotes > 0 ? (hitNotes / (float)totalNotes) * 100f : 0f;

            OnGameEnd?.Invoke(CurrentScore);

            Debug.Log($"Game ended! Score: {CurrentScore.TotalScore}, Accuracy: {CurrentScore.Accuracy:F1}%, Max Combo: {CurrentScore.MaxCombo}");
        }

        /// <summary>
        /// Get active notes (for UI display)
        /// </summary>
        public List<Note> GetActiveNotes()
        {
            return new List<Note>(_activeNotes);
        }

        /// <summary>
        /// Create a test note chart
        /// </summary>
        public static NoteChart CreateTestChart()
        {
            NoteChart chart = new NoteChart
            {
                SongId = "TEST001",
                SongName = "Test Song",
                BPM = 120,
                Difficulty = 5,
                Duration = 30f
            };

            // Generate simple test pattern
            float beatInterval = 60f / chart.BPM; // Time between beats
            RhythmKey[] keys = { RhythmKey.Left, RhythmKey.Down, RhythmKey.Up, RhythmKey.Right };

            for (float time = 2f; time < chart.Duration; time += beatInterval)
            {
                Note note = new Note
                {
                    Time = time,
                    Key = keys[UnityEngine.Random.Range(0, keys.Length)],
                    IsHold = false
                };
                chart.Notes.Add(note);
            }

            Debug.Log($"Created test chart with {chart.Notes.Count} notes");
            return chart;
        }

        [ContextMenu("Start Test Game")]
        public void StartTestGame()
        {
            NoteChart testChart = CreateTestChart();
            StartGame(testChart);
        }
    }
}
