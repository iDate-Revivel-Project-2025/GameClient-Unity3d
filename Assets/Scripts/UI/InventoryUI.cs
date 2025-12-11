using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using iDate.Network;
using TMPro;

namespace iDate.UI
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance;

        public Transform GridRoot;
        public GameObject SlotPrefab;
        public Button EquipButton;

        private uint _selectedItemId;
        private int _selectedSlotId;

        private void Awake()
        {
            Instance = this;
            if (EquipButton) EquipButton.onClick.AddListener(SendEquipRequest);
        }

        public void Populate(List<InventoryItemData> items)
        {
            // Clear existing
            foreach (Transform child in GridRoot) Destroy(child.gameObject);

            // Spawn new
            foreach (var item in items)
            {
                GameObject obj;
                if (SlotPrefab != null)
                {
                     obj = Instantiate(SlotPrefab, GridRoot);
                }
                else
                {
                     // Fallback
                     obj = new GameObject("Slot_" + item.UniqueID);
                     obj.transform.SetParent(GridRoot, false);
                     Image img = obj.AddComponent<Image>();
                     img.color = item.IsEquipped ? Color.yellow : Color.white;
                     obj.AddComponent<Button>();
                     
                     GameObject txtObj = new GameObject("Label");
                     txtObj.transform.SetParent(obj.transform, false);
                     var tm = txtObj.AddComponent<TextMeshProUGUI>();
                     tm.fontSize = 12;
                     tm.alignment = TextAlignmentOptions.Center;
                     tm.color = Color.black;
                }

                var btn = obj.GetComponent<Button>();
                var txt = obj.GetComponentInChildren<TextMeshProUGUI>();
                
                if(txt) txt.text = $"{item.ItemTypeID}";
                
                uint uid = item.UniqueID;
                int slot = item.Slot;
                
                // Update color based on bool
                var imgComp = obj.GetComponent<Image>();
                if(imgComp) imgComp.color = item.IsEquipped ? Color.yellow : Color.white;
                
                btn.onClick.AddListener(() => OnItemClicked(uid, slot));
            }
        }

        private void OnItemClicked(uint uid, int slot)
        {
            _selectedItemId = uid;
            _selectedSlotId = slot;
            Debug.Log($"Selected Item: {uid} (Slot {slot})");
        }

        private void SendEquipRequest()
        {
            if (_selectedItemId == 0) return;

            // Packet Structure: [Header] [Hair] [Face] [Jacket] [Gloves] [Pants] [Shoes]
            // We need to know current equipped state of other slots to not unequip them!
            // ... (rest of logic)
            // For now, simpler test: Just set Hair/Face to selected ID (Hack)
            
            if (iDate.Character.AvatarManager.Instance != null)
            {
                var avatarMgr = Object.FindFirstObjectByType<Character.AvatarManager>();
                if (avatarMgr) avatarMgr.EquipItemById(_selectedItemId);
            }

            NetworkManager.Instance.SendEquip(_selectedItemId);
        }
    }

    public struct InventoryItemData
    {
        public uint UniqueID;
        public int Category;
        public uint ItemTypeID;
        public int VariantID;
        public int Slot;
        public bool IsEquipped;
    }
}
