using UnityEngine;
using System.Collections.Generic;

namespace iDate.Visuals
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;

        public Transform Target; // Local Player
        public Vector3[] Angles; // Predefined offsets
        public float SmoothSpeed = 5f;
        
        private int _currentAngleIndex = 0;
        private Vector3 _offset;
        private Vector3 _basePosition;
        
        private void Awake()
        {
            Instance = this;
            // Define standard angles if not set
            if (Angles == null || Angles.Length == 0)
            {
                Angles = new Vector3[]
                {
                    new Vector3(0, 2, -4),   // Behind Low
                    new Vector3(3, 4, -4),   // Quarter View
                    new Vector3(-3, 4, -4),  // Quarter View Left
                    new Vector3(0, 5, -5),   // High Angle
                };
            }
            _offset = Angles[0];
        }

        private void Start()
        {
            if (iDate.Gameplay.Conductor.Instance != null)
            {
                iDate.Gameplay.Conductor.Instance.OnBeat += OnBeat;
            }
            
            // Find Local Character if Target is null
            if (Target == null)
            {
                GameObject local = GameObject.Find("LocalCharacter");
                if (local) Target = local.transform;
            }
        }

        private void OnDestroy()
        {
            if (iDate.Gameplay.Conductor.Instance != null)
            {
                iDate.Gameplay.Conductor.Instance.OnBeat -= OnBeat;
            }
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desiredPos = Target.position + _offset;
            Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, SmoothSpeed * Time.deltaTime);
            transform.position = smoothedPos;
            
            transform.LookAt(Target.position + Vector3.up * 1.5f); // Look at head roughly
        }

        private void OnBeat(int beatNum)
        {
            // Simple Bounce Effect
            // We can add a small impulse to rotation or position?
            // Or switch angles every 8 beats?
            
            if (beatNum % 16 == 0)
            {
                // Switch Angle randomly
                _currentAngleIndex = Random.Range(0, Angles.Length);
                _offset = Angles[_currentAngleIndex];
            }
            
            // Small FOV kick?
            StartCoroutine(BeatKick());
        }

        private System.Collections.IEnumerator BeatKick()
        {
            float fov = Camera.main.fieldOfView;
            Camera.main.fieldOfView = fov - 1f; // Zoom in
            yield return new WaitForSeconds(0.1f);
            Camera.main.fieldOfView = fov; // Restore
        }
    }
}
