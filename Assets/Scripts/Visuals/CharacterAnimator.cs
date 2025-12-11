using UnityEngine;
using iDate.Gameplay;

namespace iDate.Visuals
{
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        private Animator _animator;
        private bool _isDancing = false;
        
        // Animation Hashes for Performance
        private int _hashBeat;
        private int _hashMiss;
        private int _hashWin;
        private int _hashStart;
        
        // Configuration
        public bool IsLocalPlayer = false;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            
            _hashBeat = Animator.StringToHash("Beat");
            _hashMiss = Animator.StringToHash("Miss");
            _hashWin = Animator.StringToHash("Win");
            _hashStart = Animator.StringToHash("Start");
        }

        private void Start()
        {
            // Subscribe to Conductor for Rhythm
            if (Conductor.Instance != null)
            {
                Conductor.Instance.OnBeat += HandleBeat;
            }
            
            // Subscribe to Gameplay Events (only if Local, or if we replicate events)
            // For now, assume this is mostly for Local Player feedback
            if (IsLocalPlayer && GameplayManager.Instance != null)
            {
                GameplayManager.Instance.OnJudgment += HandleJudgment;
                GameplayManager.Instance.OnGameStarting += HandleGameStart;
            }
        }

        private void OnDestroy()
        {
            if (Conductor.Instance != null) Conductor.Instance.OnBeat -= HandleBeat;
            if (GameplayManager.Instance != null)
            {
                GameplayManager.Instance.OnJudgment -= HandleJudgment;
                GameplayManager.Instance.OnGameStarting -= HandleGameStart;
            }
        }

        private void HandleGameStart()
        {
            _isDancing = true;
            _animator.SetTrigger(_hashStart);
            _animator.SetBool("IsDancing", true);
        }

        private void HandleBeat(int beatIndex)
        {
            // Trigger a "Bob" or "Step" every beat to keep in sync
            // Or just ensure the dance loop speed matches BPM?
            // For simple implementation: Trigger a Beat state.
            
            // Only bob if we are in game
            if (_isDancing)
            {
                 // Sync Logic Idea: Reset state time?
                 // Simple approach: Trigger "Beat" which blends a head bob or step
                 _animator.SetTrigger(_hashBeat);
            }
        }

        private void HandleJudgment(string rank)
        {
            if (rank == "Miss")
            {
                _animator.SetTrigger(_hashMiss);
                // Maybe stop dancing for a moment?
                // _animator.SetBool("IsDancing", false);
            }
            else
            {
                // Happy/Win reaction on high combo?
                // For now, keep dancing.
            }
        }
        
        // Public method for Network Remote Players to trigger animations
        public void NetworkTrigger(string triggerName)
        {
            _animator.SetTrigger(triggerName);
        }
    }
}
