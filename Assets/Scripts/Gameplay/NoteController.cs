using UnityEngine;

namespace iDate.Gameplay
{
    public class NoteController : MonoBehaviour
    {
        public float TargetBeat;
        public int LaneIndex; // 0=Up, 1=Left, 2=Down, 3=Right (Example)
        
        public bool CanMiss = true;

        // iDate notes usually approach a target circle.

        private Vector3 _startPos;
        private Vector3 _endPos;

        public void Initialize(float beat, int lane, Vector3 spawnPos, Vector3 targetPos)
        {
            TargetBeat = beat;
            LaneIndex = lane;
            _startPos = spawnPos;
            _endPos = targetPos;
        }

        private void Update()
        {
            // Simple interpolation for now based on Beat
            // In iDate, notes circle or approach from sides.
            
            float currentBeat = Conductor.Instance.SongPositionInBeats;
            float diff = TargetBeat - currentBeat; // Beats remaining until hit

            // If diff is large, far away. If 0, at target.
            // Let's assume notes appear 4 beats ahead.
            
            if (diff < -0.5f && CanMiss) 
            {
                // Missed
                Destroy(gameObject);
                GameplayManager.Instance.ReportHit(false, LaneIndex);
            }
            else
            {
                // Move logic
                // Lerp based on (4 - diff) / 4 ?
                // For linear approach:
                float t = 1f - (diff / 4f); // 0 at spawn (4 beats away), 1 at target
                transform.position = Vector3.LerpUnclamped(_startPos, _endPos, t);
            }
        }
    }
}
