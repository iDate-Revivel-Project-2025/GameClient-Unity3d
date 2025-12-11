using UnityEngine;
using System.Collections.Generic;
using iDate.Assets;

namespace iDate.Character
{
    public enum EquipmentSlot
    {
        Hair,
        Face,
        Top,
        Bottom,
        Shoes,
        Accessory,
        Pet
    }

    public class AvatarManager : MonoBehaviour
    {
        [Header("Skeleton Reference")]
        public Transform RootBone; // The Hips/Root of the master skeleton

        [Header("Equipped Parts")]
        public Dictionary<EquipmentSlot, GameObject> EquippedItems = new Dictionary<EquipmentSlot, GameObject>();

        // Cache of Master Bones for re-parenting/skinning
        private Dictionary<string, Transform> _boneMap = new Dictionary<string, Transform>();

        public static AvatarManager Instance;

        private void Awake()
        {
            Instance = this;
            if (RootBone != null)
            {
                MapBones(RootBone);
            }
        }

        private void MapBones(Transform current)
        {
            if (!_boneMap.ContainsKey(current.name))
            {
                _boneMap.Add(current.name, current);
            }

            foreach (Transform child in current)
            {
                MapBones(child);
            }
        }

        public void EquipItem(EquipmentSlot slot, string modelPath)
        {
            // 1. Unequip existing
            UnequipItem(slot);

            // 2. Load New Model
            // NOTE: In a real scenario, this would load a SkinnedMesh and we'd merge bones.
            // For now, using our Prop Importer (Gb3Importer).
            GameObject newItem = Gb3Importer.LoadModel(modelPath);
            
            if (newItem == null)
            {
                Debug.LogWarning($"[AvatarManager] Failed to load {modelPath}");
                return;
            }

            // 3. Attach to Skeleton
            // Strategy A: "Socketing" (Simple parenting)
            // Strategy B: "Skinned Mesh Merge" (Complex)
            
            // Implementing Strategy A for now (Prop/Proxy mode):
            newItem.transform.SetParent(transform, false);
            
            // Register
            EquippedItems[slot] = newItem;
            Debug.Log($"[AvatarManager] Equipped {slot}: {modelPath}");
            Debug.Log($"[AvatarManager] Equipped {slot}: {modelPath}");
        }

        public void EquipItemById(uint itemId)
        {
            // TODO: Map ItemID -> Asset Path via CSV/Database lookup
            // For prototype, we assume ItemID = "Model_{ID}" in Resources
            // Mapping Logic Stub:
            // Slot logic?
            
            string path = $"Models/{itemId}"; // e.g., Models/1001.gb3
            
            // Heuristic for slot based on ID range?
            // 1000-1999: Hair
            // 2000-2999: Face
            // 3000-3999: Top
            // ...
            EquipmentSlot slot = EquipmentSlot.Top; 
            if (itemId < 2000) slot = EquipmentSlot.Hair;
            else if (itemId < 3000) slot = EquipmentSlot.Face;
            else if (itemId < 4000) slot = EquipmentSlot.Top;
            
            EquipItem(slot, path);
        }

        public void UnequipItem(EquipmentSlot slot)
        {
            if (EquippedItems.ContainsKey(slot))
            {
                GameObject obj = EquippedItems[slot];
                if (obj != null) Destroy(obj);
                EquippedItems.Remove(slot);
            }
        }

        // Test Method
        [ContextMenu("Test Equip")]
        public void TestEquip()
        {
            EquipItem(EquipmentSlot.Top, "Assets/Resources/TestTop.gb3");
        }
    }
}
