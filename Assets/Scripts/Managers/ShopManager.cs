using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using iDate.Network;

namespace iDate.Shop
{
    [System.Serializable]
    public struct ShopItemData
    {
        public uint Id;
        public string Name;
        public int Category; // 0=Hair, 1=Face etc same as Server
        public int Price;
        public int CurrencyType; // 0=Points, 1=Cash
        public string ResourcePath;
        public string IconPath;
    }

    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [SerializeField]
        private List<ShopItemData> _shopItems = new List<ShopItemData>();
        public List<ShopItemData> ShopItems => _shopItems;

        public delegate void ShopListUpdatedHandler();
        public event ShopListUpdatedHandler OnShopListUpdated;

        public delegate void PurchaseResultHandler(bool success, uint itemId, int newBalance);
        public event PurchaseResultHandler OnPurchaseResult;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RequestShopList()
        {
            Debug.Log("[ShopManager] Requesting Shop List...");
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendShopListRequest();
            }
        }

        public void SetShopItems(List<ShopItemData> items)
        {
            _shopItems = items;
            Debug.Log($"[ShopManager] Loaded {_shopItems.Count} shop items.");
            OnShopListUpdated?.Invoke();
        }

        public void BuyItem(uint itemId)
        {
            Debug.Log($"[ShopManager] Requesting purchase of Item {itemId}...");
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendBuyItemRequest(itemId);
            }
        }

        public delegate void CurrencyUpdateHandler(int cash, int points);
        public event CurrencyUpdateHandler OnCurrencyUpdated;

        public delegate void StatsUpdateHandler(int cash, int points, int level, uint exp);
        public event StatsUpdateHandler OnStatsUpdated;

        public void HandlePurchaseResult(bool success, uint itemId, int newBalance)
        {
            Debug.Log($"[ShopManager] Purchase Result: {(success ? "Success" : "Failed")} for Item {itemId}. New Balance: {newBalance}");
            OnPurchaseResult?.Invoke(success, itemId, newBalance);
        }

        public void UpdateCurrency(int cash, int points)
        {
            // Legacy/Convenience
            Debug.Log($"[ShopManager] Currency Udpate: Cash={cash} Points={points}");
            OnCurrencyUpdated?.Invoke(cash, points);
        }

        public void UpdateStats(int cash, int points, int level, uint exp)
        {
            Debug.Log($"[ShopManager] Stats Update: Lv.{level} ({exp}) Cash={cash} Points={points}");
            OnStatsUpdated?.Invoke(cash, points, level, exp);
            UpdateCurrency(cash, points); // keep legacy UI working
        }
    }
}
