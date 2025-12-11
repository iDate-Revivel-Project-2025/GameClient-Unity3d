using System;
using System.Collections.Generic;
using UnityEngine;
using iDate.Gameplay; // For RoomManager

namespace iDate.Visuals
{
    [RequireComponent(typeof(CharacterVisuals))]
    public class PlayerVisualController : MonoBehaviour
    {
        public string PlayerId;
        private CharacterVisuals _visuals;

        private void Awake()
        {
            _visuals = GetComponent<CharacterVisuals>();
        }

        private void Start()
        {
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnPlayerAppearanceUpdated += OnAppearanceUpdated;
            }
        }

        private void OnDestroy()
        {
             if (RoomManager.Instance != null)
            {
                RoomManager.Instance.OnPlayerAppearanceUpdated -= OnAppearanceUpdated;
            }
        }

        private void OnAppearanceUpdated(string targetId, List<int[]> items)
        {
            if (string.IsNullOrEmpty(PlayerId)) return;

            // Simple ID Check
            if (targetId == PlayerId)
            {
                Debug.Log($"[PlayerVisualController] Received update for {targetId}. Applying to Visuals.");
                _visuals.SetAppearance(items);
            }
        }
    }
}
