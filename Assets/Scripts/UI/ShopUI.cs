using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using iDate.Shop;
using TMPro;

namespace iDate.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject ShopPanel;
        public Transform ItemContainer;
        public GameObject ShopItemPrefab; // Needs to be assigned or created code-side
        public Button CloseButton;
        public Button RefreshButton;
        public Text CurrencyText; // "Cash: X  Points: Y"
        
        // Aliases for ProjectSetup generator compatibility
        public GameObject Panel { get { return ShopPanel; } set { ShopPanel = value; } }
        public Transform ItemsGrid { get { return ItemContainer; } set { ItemContainer = value; } }
        public Button BuyButton; // To be assigned
        public TextMeshProUGUI ItemNameText;
        public TextMeshProUGUI ItemPriceText;

        private List<GameObject> _spawnedItems = new List<GameObject>();

        private void Start()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopListUpdated += RefreshList;
                ShopManager.Instance.OnPurchaseResult += OnPurchaseResult;
                ShopManager.Instance.OnCurrencyUpdated += UpdateCurrencyText;
            }

            if (CloseButton != null) CloseButton.onClick.AddListener(Hide);
            if (RefreshButton != null) RefreshButton.onClick.AddListener(RequestRefresh);

            Hide(); // Default hidden
        }

        private void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnShopListUpdated -= RefreshList;
                ShopManager.Instance.OnPurchaseResult -= OnPurchaseResult;
                ShopManager.Instance.OnCurrencyUpdated -= UpdateCurrencyText;
            }
        }

        public void Show()
        {
            if (ShopPanel != null) ShopPanel.SetActive(true);
            RequestRefresh();
        }

        public void Hide()
        {
            if (ShopPanel != null) ShopPanel.SetActive(false);
        }

        private void RequestRefresh()
        {
            ShopManager.Instance?.RequestShopList();
        }

        private void RefreshList()
        {
            // Clear old
            foreach (var obj in _spawnedItems) Destroy(obj);
            _spawnedItems.Clear();

            if (ShopManager.Instance == null) return;

            foreach (var item in ShopManager.Instance.ShopItems)
            {
                CreateShopItem(item);
            }
        }

        private void CreateShopItem(ShopItemData item)
        {
            GameObject go = null;
            if (ShopItemPrefab != null)
            {
                go = Instantiate(ShopItemPrefab, ItemContainer);
            }
            else
            {
                // Fallback: Create simple button
                go = new GameObject($"ShopItem_{item.Id}", typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(ItemContainer, false);
                
                var layout = go.GetComponent<LayoutElement>();
                layout.minHeight = 50;
                layout.preferredHeight = 100;

                // Texts
                GameObject textObj = new GameObject("InfoText", typeof(Text));
                textObj.transform.SetParent(go.transform, false);
                Text t = textObj.GetComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.alignment = TextAnchor.MiddleCenter;
                t.resizeTextForBestFit = true;
                t.color = Color.black;
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                
                string currency = (item.CurrencyType == 1) ? "Cash" : "Points";
                t.text = $"{item.Name}\n{item.Price} {currency}";
                
                // Button
                Button b = go.GetComponent<Button>();
                b.onClick.AddListener(() => OnItemClicked(item));
            }

            // If using Prefab, we'd look for components to set text.
            _spawnedItems.Add(go);
        }

        private void OnItemClicked(ShopItemData item)
        {
            // Simple logic: Immediate buy prompt (or just buy)
            Debug.Log($"[ShopUI] Buying {item.Name}...");
            ShopManager.Instance?.BuyItem(item.Id);
        }

        private void OnPurchaseResult(bool success, uint itemId, int newBalance)
        {
            if (success)
            {
                Debug.Log("[ShopUI] Purchase Successful!");
                // Optionally show a popup
            }
            else
            {
                Debug.Log("[ShopUI] Purchase Failed (Funds?).");
            }
        }

        private void UpdateCurrencyText(int cash, int points)
        {
            if (CurrencyText != null)
            {
                CurrencyText.text = $"Cash: {cash}  Points: {points}";
            }
        }
    }
}
