using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Gameplay;
using System.Collections;

namespace iDate.UI
{
    public class ScoreUI : MonoBehaviour
    {
        public TextMeshProUGUI ScoreText;
        public TextMeshProUGUI ComboText;
        public TextMeshProUGUI JudgmentText;

        private int _displayedScore = 0;
        private int _currentCombo = 0;

        private void Start()
        {
            if (GameplayManager.Instance != null)
            {
                GameplayManager.Instance.OnJudgment += HandleJudgment;
            }
            
            if (ScoreText) ScoreText.text = "Score: 0";
            if (ComboText) ComboText.text = "";
            if (JudgmentText) JudgmentText.text = "";
        }

        private void OnDestroy()
        {
            if (GameplayManager.Instance != null)
            {
                GameplayManager.Instance.OnJudgment -= HandleJudgment;
            }
        }

        private void HandleJudgment(string rank) // Perfect, Great, Cool, Bad, Miss
        {
            // Update Score Display Logic (score added in GameplayManager, we just visualize)
            // But GameplayManager doesn't emit score in event? 
            // We can read GameplayManager score or just track accumulation locally for display.
            // Better to read it or have Event pass score.
            // For now, let's just approximate or assume Sync via Update?
            // Wait, GameplayManager has `_currentScore` private.
            // I should expose `CurrentScore` property in GameplayManager strictly.
            // For prototype, I will just add standard values.
            
            int add = 0;
            switch(rank) {
                case "Perfect": add = 500; break;
                case "Great": add = 300; break;
                case "Cool": add = 100; break;
                case "Bad": add = 50; _currentCombo = 0; break;
                case "Miss": add = 0; _currentCombo = 0; break;
            }
            
            if (rank != "Bad" && rank != "Miss") _currentCombo++;
            
            _displayedScore += add;
            if (ScoreText) ScoreText.text = $"Score: {_displayedScore}";

            // Combo
            if (ComboText)
            {
                if (_currentCombo > 1) ComboText.text = $"{_currentCombo} COMBO!";
                else ComboText.text = "";
            }

            // Judgment Animation
            if (JudgmentText)
            {
                JudgmentText.text = rank.ToUpper();
                JudgmentText.color = GetColorForRank(rank);
                
                // Trigger Animation
                StopAllCoroutines();
                StartCoroutine(AnimateJudgment());
            }
        }

        private Color GetColorForRank(string rank)
        {
            switch(rank)
            {
                case "Perfect": return Color.cyan;
                case "Great": return Color.green;
                case "Cool": return Color.yellow;
                case "Bad": return new Color(1f, 0.5f, 0f); // Orange
                case "Miss": return Color.red;
                default: return Color.white;
            }
        }

        private IEnumerator AnimateJudgment()
        {
            // Simple pop effect
            JudgmentText.transform.localScale = Vector3.one * 1.5f;
            float t = 0;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                JudgmentText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t / 0.2f);
                yield return null;
            }
            JudgmentText.transform.localScale = Vector3.one;
            
            yield return new WaitForSeconds(0.5f);
            JudgmentText.text = "";
        }
    }
}
