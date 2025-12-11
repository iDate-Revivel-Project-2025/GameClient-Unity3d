using System;
using System.Collections.Generic;
using UnityEngine;

namespace iDate.Visuals
{
    public class CharacterVisuals : MonoBehaviour
    {
        [Header("Configuration")]
        public Transform SkeletonRoot;
        public Renderer BodyRenderer;

        // Categories
        public const int CAT_HAIR = 1;
        public const int CAT_FACE = 2;
        public const int CAT_JACKET = 3;
        public const int CAT_PANTS = 4;
        public const int CAT_SHOES = 5;
        public const int CAT_GLOVES = 6;
        public const int CAT_ACC = 7;

        // Slot Management
        private Dictionary<int, GameObject> _equippedParts = new Dictionary<int, GameObject>();

        // Material/Mesh Caches (Mock)
        
        public void Initialize()
        {
            // Reset to default
            UnequipAll();
        }

        public void Equip(int category, int itemId)
        {
            // 1. Remove existing in slot
            if (_equippedParts.ContainsKey(category))
            {
                if (_equippedParts[category] != null) Destroy(_equippedParts[category]);
                _equippedParts.Remove(category);
            }

            // 2. Load New Asset
            // Try Loading GB3 from StreamingAssets
            string gb3Path = System.IO.Path.Combine(Application.streamingAssetsPath, "Items", category.ToString(), $"{itemId}.gb3");
            
            GameObject instance = null;

            if (System.IO.File.Exists(gb3Path))
            {
                instance = iDate.Assets.Gb3Importer.LoadModel(gb3Path);
            }
            
            if (instance == null)
            {
                 // Fallback to Primitive
                 // Debug.Log($"[CharacterVisuals] Asset not found at {gb3Path}. Using Placeholder.");
                 // In a real build we might not want cubes everywhere, but for dev:
                 // instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                 // Only log to keep scene clean?
            }

            if (instance != null)
            {
                instance.transform.SetParent(GetSlotParent(category), false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                _equippedParts[category] = instance;
            }

            Debug.Log($"[CharacterVisuals] Equipping Cat {category} Item {itemId} on {name} (GB3 Found: {instance != null})");
            
            // Mock Visual Feedback: Change Color based on ItemID?
            if (BodyRenderer != null)
            {
                // Simple hash to color
                UnityEngine.Random.InitState(itemId);
                Color randomColor = UnityEngine.Random.ColorHSV();
                // MaterialPropertyBlock or SharedMaterial?
                // Just log for safety.
            }
        }

        public void UnequipAll()
        {
            foreach(var kv in _equippedParts)
            {
                if (kv.Value != null) Destroy(kv.Value);
            }
            _equippedParts.Clear();
        }

        private Transform GetSlotParent(int category)
        {
            // Return specific bone for attachment
            return SkeletonRoot ? SkeletonRoot : transform;
        }

        public void SetAppearance(List<int[]> items)
        {
            foreach (var item in items)
            {
                if (item.Length >= 2)
                {
                    Equip(item[0], item[1]);
                }
            }
        }
    }
}
