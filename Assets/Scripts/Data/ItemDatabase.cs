using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [System.Serializable]
    public class ItemData
    {
        public ushort Category;
        public ushort ItemTypeId;
        public short VariantId;
        public short Validity;
        public byte TypeFlag;
        public int SetItemId;
        public string Name;

        public uint GetUniqueKey()
        {
            // Composite key strategy from server: (Category << 16) | ItemTypeId
            // But variant matters too? Server uses: const key = (category << 16) | itemTypeId; 
            // Server inventory map uses this.
            return (uint)((Category << 16) | ItemTypeId);
        }
    }

    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        public string databasePath = "/Users/hades/UnityGames/iDate Revival 2025/iDateEmulator(Server)/server/ShopItem.ido";
        public List<ItemData> loadedItems = new List<ItemData>();
        
        private const int RECORD_SIZE = 456; // 0x1C8

        private void Awake()
        {
            Instance = this;
            LoadDatabase();
        }

        public void LoadDatabase()
        {
            if (!File.Exists(databasePath))
            {
                Debug.LogError($"ShopItem.ido not found at {databasePath}");
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(databasePath);
                int count = data.Length / RECORD_SIZE;
                loadedItems.Clear();

                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    for (int i = 0; i < count; i++)
                    {
                        ms.Seek(i * RECORD_SIZE, SeekOrigin.Begin);

                        ItemData item = new ItemData();
                        item.Category = br.ReadUInt16();      // 0x00
                        item.ItemTypeId = br.ReadUInt16();    // 0x02
                        item.VariantId = br.ReadInt16();      // 0x04
                        item.Validity = br.ReadInt16();       // 0x06
                        
                        // Skip to 0x0C
                        ms.Seek((i * RECORD_SIZE) + 0x0C, SeekOrigin.Begin);
                        item.TypeFlag = br.ReadByte();

                        // Skip to 0x38
                        ms.Seek((i * RECORD_SIZE) + 0x38, SeekOrigin.Begin);
                        item.SetItemId = br.ReadInt32();

                        // Skip to 0x64 for Name
                        ms.Seek((i * RECORD_SIZE) + 0x64, SeekOrigin.Begin);
                        byte[] nameBytes = br.ReadBytes(100);
                        
                        // Parse UTF-16LE string (null terminated)
                        item.Name = ParseName(nameBytes);

                        loadedItems.Add(item);
                    }
                }

                Debug.Log($"[ItemDatabase] Loaded {loadedItems.Count} items.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ItemDatabase] Failed to load: {ex.Message}");
            }
        }

        private string ParseName(byte[] buffer)
        {
            // Search for null terminator (double zero in UTF-16)
            int len = buffer.Length;
            for (int i = 0; i < buffer.Length; i += 2)
            {
                if (buffer[i] == 0 && buffer[i+1] == 0)
                {
                    len = i;
                    break;
                }
            }
            return Encoding.Unicode.GetString(buffer, 0, len).Trim();
        }

        public ItemData GetItem(ushort category, ushort typeId)
        {
            return loadedItems.Find(x => x.Category == category && x.ItemTypeId == typeId);
        }
    }
}
