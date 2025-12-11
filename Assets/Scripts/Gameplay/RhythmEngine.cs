using UnityEngine;
using System.Collections.Generic;
using System;

namespace iDate.Gameplay
{
    public enum JudgementType
    {
        Miss = 0,
        Bad,
        Good,
        Great,
        Perfect
    }

    public enum RhythmKey
    {
        Up,
        Down,
        Left,
        Right,
        Space
    }

    public class Note
    {
        public float Time;
        public RhythmKey Key;
        public bool IsHit;
        public bool IsHold; // Added
    }

    public class ScoreData
    {
        public int TotalScore;
        public int PerfectCount;
        public int GreatCount;
        public int GoodCount;
        public int BadCount;
        public int MissCount;
        public int MaxCombo;
        public int Combo; // Added current combo
        public float Accuracy;
    }

    public class RhythmEngine : MonoBehaviour
    {
        // ... (existing code)
        public static RhythmEngine Instance;

        public bool IsPlaying;
        public float SongTime;
        public ScoreData CurrentScore = new ScoreData();

        // Events
        public event Action<Note, JudgementType> OnNoteHit;
        public event Action<int> OnComboChanged;
        public event Action<ScoreData> OnGameEnd;

        private List<Note> _activeNotes = new List<Note>();

        private void Awake()
        {
            Instance = this;
        }

        public List<Note> GetActiveNotes()
        {
            return _activeNotes;
        }
        
        // Stub methods
        public void ReportHit(Note note, JudgementType type)
        {
             OnNoteHit?.Invoke(note, type);
             if (type == JudgementType.Miss || type == JudgementType.Bad)
             {
                 CurrentScore.Combo = 0;
             }
             else
             {
                 CurrentScore.Combo++;
                 if (CurrentScore.Combo > CurrentScore.MaxCombo) CurrentScore.MaxCombo = CurrentScore.Combo;
             }
             OnComboChanged?.Invoke(CurrentScore.Combo);
        }

        public void StartGame(NoteChart chart)
        {
            IsPlaying = true;
            _activeNotes = new List<Note>(chart.Notes); // Copy
            CurrentScore = new ScoreData();
            // Load logic
        }

        public void StopGame()
        {
            IsPlaying = false;
            OnGameEnd?.Invoke(CurrentScore);
        }
    }

    public class NoteChart
    {
        public string SongId; // Added
        public string SongName;
        public float BPM; // Added
        public int Difficulty;
        public float Duration; // Added
        public List<Note> Notes = new List<Note>();
    }
}
