using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Gameplay;

namespace iDate.UI
{
    /// <summary>
    /// UI controller for the rhythm game
    /// </summary>
    public class RhythmGameUI : MonoBehaviour
    {
        [Header("Note Highway")]
        public RectTransform NoteHighway;
        public GameObject NotePrefab;
        public float NoteSpeed = 200f; // Pixels per second
        public float HitLineY = -200f; // Y position of hit line

        [Header("Key Positions")]
        public RectTransform[] KeyLanes; // 4 lanes for arrow keys

        [Header("Score Display")]
        public TextMeshProUGUI ScoreText;
        public TextMeshProUGUI ComboText;
        public TextMeshProUGUI AccuracyText;

        [Header("Judgement Display")]
        public TextMeshProUGUI JudgementText;
        public float JudgementDisplayTime = 0.5f;

        [Header("Results Panel")]
        public GameObject ResultsPanel;
        public TextMeshProUGUI FinalScoreText;
        public TextMeshProUGUI PerfectCountText;
        public TextMeshProUGUI GreatCountText;
        public TextMeshProUGUI GoodCountText;
        public TextMeshProUGUI BadCountText;
        public TextMeshProUGUI MissCountText;
        public TextMeshProUGUI MaxComboText;
        public TextMeshProUGUI FinalAccuracyText;

        private Dictionary<Note, GameObject> _noteObjects = new Dictionary<Note, GameObject>();
        private float _judgementTimer = 0f;
        private Color[] _judgementColors = new Color[]
        {
            Color.gray,      // Miss
            Color.red,       // Bad
            Color.yellow,    // Good
            Color.green,     // Great
            Color.cyan       // Perfect
        };

        private void Start()
        {
            // Subscribe to rhythm engine events
            if (RhythmEngine.Instance != null)
            {
                RhythmEngine.Instance.OnNoteHit += OnNoteHit;
                RhythmEngine.Instance.OnComboChanged += OnComboChanged;
                RhythmEngine.Instance.OnGameEnd += OnGameEnd;
            }

            // Hide results panel
            if (ResultsPanel != null)
            {
                ResultsPanel.SetActive(false);
            }

            // Initialize UI
            UpdateScoreDisplay();
        }

        private void Update()
        {
            if (RhythmEngine.Instance == null || !RhythmEngine.Instance.IsPlaying)
                return;

            // Update note positions
            UpdateNotePositions();

            // Spawn new notes
            SpawnNotes();

            // Update judgement text fade
            if (_judgementTimer > 0f)
            {
                _judgementTimer -= Time.deltaTime;
                if (JudgementText != null)
                {
                    float alpha = _judgementTimer / JudgementDisplayTime;
                    Color c = JudgementText.color;
                    c.a = alpha;
                    JudgementText.color = c;
                }
            }

            // Update score display
            UpdateScoreDisplay();
        }

        /// <summary>
        /// Spawn note objects for active notes
        /// </summary>
        private void SpawnNotes()
        {
            if (RhythmEngine.Instance == null) return;

            List<Note> activeNotes = RhythmEngine.Instance.GetActiveNotes();
            
            foreach (Note note in activeNotes)
            {
                if (!_noteObjects.ContainsKey(note))
                {
                    SpawnNoteObject(note);
                }
            }
        }

        /// <summary>
        /// Spawn a visual note object
        /// </summary>
        private void SpawnNoteObject(Note note)
        {
            if (NotePrefab == null || NoteHighway == null) return;

            GameObject noteObj = Instantiate(NotePrefab, NoteHighway);
            RectTransform rt = noteObj.GetComponent<RectTransform>();

            if (rt != null)
            {
                // Position based on key lane
                int laneIndex = (int)note.Key;
                if (laneIndex < KeyLanes.Length && KeyLanes[laneIndex] != null)
                {
                    rt.anchoredPosition = new Vector2(
                        KeyLanes[laneIndex].anchoredPosition.x,
                        1000f // Start off-screen at top
                    );
                }

                // Set color based on key
                Image img = noteObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = GetKeyColor(note.Key);
                }
            }

            _noteObjects[note] = noteObj;
        }

        /// <summary>
        /// Update positions of all note objects
        /// </summary>
        private void UpdateNotePositions()
        {
            if (RhythmEngine.Instance == null) return;

            float songTime = RhythmEngine.Instance.SongTime;
            List<Note> toRemove = new List<Note>();

            foreach (var kvp in _noteObjects)
            {
                Note note = kvp.Key;
                GameObject noteObj = kvp.Value;

                if (noteObj == null || note.IsHit)
                {
                    toRemove.Add(note);
                    continue;
                }

                RectTransform rt = noteObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    // Calculate Y position based on time until hit
                    float timeUntilHit = note.Time - songTime;
                    float yPos = HitLineY + (timeUntilHit * NoteSpeed);
                    
                    Vector2 pos = rt.anchoredPosition;
                    pos.y = yPos;
                    rt.anchoredPosition = pos;
                }
            }

            // Remove destroyed/hit notes
            foreach (Note note in toRemove)
            {
                if (_noteObjects.ContainsKey(note))
                {
                    if (_noteObjects[note] != null)
                    {
                        Destroy(_noteObjects[note]);
                    }
                    _noteObjects.Remove(note);
                }
            }
        }

        /// <summary>
        /// Handle note hit event
        /// </summary>
        private void OnNoteHit(Note note, JudgementType judgement)
        {
            // Remove note object
            if (_noteObjects.ContainsKey(note))
            {
                GameObject noteObj = _noteObjects[note];
                if (noteObj != null)
                {
                    // Play hit animation
                    PlayHitEffect(noteObj, judgement);
                    Destroy(noteObj, 0.2f);
                }
                _noteObjects.Remove(note);
            }

            // Show judgement text
            ShowJudgement(judgement);
        }

        /// <summary>
        /// Show judgement text
        /// </summary>
        private void ShowJudgement(JudgementType judgement)
        {
            if (JudgementText == null) return;

            JudgementText.text = judgement.ToString().ToUpper();
            JudgementText.color = _judgementColors[(int)judgement];
            _judgementTimer = JudgementDisplayTime;
        }

        /// <summary>
        /// Play hit effect animation
        /// </summary>
        private void PlayHitEffect(GameObject noteObj, JudgementType judgement)
        {
            // Simple scale animation without LeanTween
            StartCoroutine(ScaleAnimation(noteObj));
        }

        private System.Collections.IEnumerator ScaleAnimation(GameObject obj)
        {
            if (obj == null) yield break;
            
            Vector3 originalScale = obj.transform.localScale;
            Vector3 targetScale = originalScale * 1.5f;
            float duration = 0.1f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                obj.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            // Scale down to zero
            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                obj.transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
                yield return null;
            }
        }

        /// <summary>
        /// Handle combo changed event
        /// </summary>
        private void OnComboChanged(int combo)
        {
            if (ComboText != null)
            {
                if (combo > 0)
                {
                    ComboText.text = $"COMBO: {combo}";
                    ComboText.gameObject.SetActive(true);
                    
                    // Pulse animation
                    StartCoroutine(PulseAnimation(ComboText.gameObject));
                }
                else
                {
                    ComboText.gameObject.SetActive(false);
                }
            }
        }

        private System.Collections.IEnumerator PulseAnimation(GameObject obj)
        {
            if (obj == null) yield break;
            
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = Vector3.one * 1.2f;
            float duration = 0.2f;
            float elapsed = 0f;

            obj.transform.localScale = targetScale;

            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                obj.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            obj.transform.localScale = originalScale;
        }

        /// <summary>
        /// Update score display
        /// </summary>
        private void UpdateScoreDisplay()
        {
            if (RhythmEngine.Instance == null) return;

            ScoreData score = RhythmEngine.Instance.CurrentScore;
            if (score == null) return;

            if (ScoreText != null)
            {
                ScoreText.text = $"SCORE: {score.TotalScore:N0}";
            }

            if (AccuracyText != null)
            {
                int totalNotes = score.PerfectCount + score.GreatCount + score.GoodCount + 
                                score.BadCount + score.MissCount;
                if (totalNotes > 0)
                {
                    int hitNotes = score.PerfectCount + score.GreatCount + score.GoodCount + score.BadCount;
                    float accuracy = (hitNotes / (float)totalNotes) * 100f;
                    AccuracyText.text = $"ACCURACY: {accuracy:F1}%";
                }
            }
        }

        /// <summary>
        /// Handle game end event
        /// </summary>
        private void OnGameEnd(ScoreData finalScore)
        {
            // Show results panel
            if (ResultsPanel != null)
            {
                ResultsPanel.SetActive(true);

                if (FinalScoreText != null)
                    FinalScoreText.text = $"SCORE: {finalScore.TotalScore:N0}";

                if (PerfectCountText != null)
                    PerfectCountText.text = $"Perfect: {finalScore.PerfectCount}";

                if (GreatCountText != null)
                    GreatCountText.text = $"Great: {finalScore.GreatCount}";

                if (GoodCountText != null)
                    GoodCountText.text = $"Good: {finalScore.GoodCount}";

                if (BadCountText != null)
                    BadCountText.text = $"Bad: {finalScore.BadCount}";

                if (MissCountText != null)
                    MissCountText.text = $"Miss: {finalScore.MissCount}";

                if (MaxComboText != null)
                    MaxComboText.text = $"Max Combo: {finalScore.MaxCombo}";

                if (FinalAccuracyText != null)
                    FinalAccuracyText.text = $"Accuracy: {finalScore.Accuracy:F1}%";
            }

            // Clear note objects
            foreach (var noteObj in _noteObjects.Values)
            {
                if (noteObj != null)
                    Destroy(noteObj);
            }
            _noteObjects.Clear();
        }

        /// <summary>
        /// Get color for a specific key
        /// </summary>
        private Color GetKeyColor(RhythmKey key)
        {
            return key switch
            {
                RhythmKey.Left => new Color(1f, 0.3f, 0.3f),    // Red
                RhythmKey.Down => new Color(0.3f, 0.3f, 1f),    // Blue
                RhythmKey.Up => new Color(0.3f, 1f, 0.3f),      // Green
                RhythmKey.Right => new Color(1f, 1f, 0.3f),     // Yellow
                RhythmKey.Space => new Color(1f, 0.3f, 1f),     // Magenta
                _ => Color.white
            };
        }

        /// <summary>
        /// Close results panel and return to lobby
        /// </summary>
        public void CloseResults()
        {
            if (ResultsPanel != null)
            {
                ResultsPanel.SetActive(false);
            }

            // Return to lobby (to be implemented)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen("lobby");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (RhythmEngine.Instance != null)
            {
                RhythmEngine.Instance.OnNoteHit -= OnNoteHit;
                RhythmEngine.Instance.OnComboChanged -= OnComboChanged;
                RhythmEngine.Instance.OnGameEnd -= OnGameEnd;
            }
        }
    }
}
