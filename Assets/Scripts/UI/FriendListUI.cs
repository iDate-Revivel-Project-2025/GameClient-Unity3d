using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using iDate.Managers;

namespace iDate.UI
{
    public class FriendListUI : MonoBehaviour
    {
        public static FriendListUI Instance { get; private set; }

        public GameObject Panel;
        public Transform ContentRoot;
        public GameObject FriendItemPrefab;
        public TMP_InputField AddFriendInput;
        public Button AddButton;
        
        // Internal cache
        private List<GameObject> _items = new List<GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            Panel.SetActive(false); // Hide by default
            
            AddButton.onClick.AddListener(OnAddClicked);
        }

        private void Start()
        {
            if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnFriendListUpdated += UpdateList;
                SocialManager.Instance.OnAddFriendResult += OnAddResult;
            }
        }

        private void OnDestroy()
        {
             if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnFriendListUpdated -= UpdateList;
                SocialManager.Instance.OnAddFriendResult -= OnAddResult;
            }
        }
        
        public void Toggle()
        {
            Panel.SetActive(!Panel.activeSelf);
            if (Panel.activeSelf)
            {
                SocialManager.Instance?.SendFriendListRequest();
            }
        }

        private void OnAddClicked()
        {
            string name = AddFriendInput.text;
            if (!string.IsNullOrEmpty(name))
            {
                SocialManager.Instance?.SendAddFriendRequest(name);
                AddFriendInput.text = "";
            }
        }
        
        private void OnAddResult(string msg, bool success)
        {
            Debug.Log($"Add Friend Result: {msg}");
            // Show toast or popup?
        }

        private void UpdateList(List<FriendData> friends)
        {
            // Clear old
            foreach(var item in _items) Destroy(item);
            _items.Clear();
            
            foreach(var f in friends)
            {
                var go = Instantiate(FriendItemPrefab, ContentRoot);
                _items.Add(go);
                
                // Set Text
                var text = go.GetComponentInChildren<Text>();
                if (text)
                {
                    string status = f.IsOnline ? "<color=green>Online</color>" : "<color=gray>Offline</color>";
                    text.text = $"{f.Name} {status}";
                }
                
                // Set Remove Button
                var removeBtnTrans = go.transform.Find("RemoveBtn");
                if (removeBtnTrans)
                {
                    var btn = removeBtnTrans.GetComponent<Button>();
                    btn.onClick.AddListener(() => {
                        SocialManager.Instance?.SendRemoveFriendRequest(f.Id);
                    });
                }
                
                // Set Inspect Button
                var inspBtnTrans = go.transform.Find("InspBtn");
                if (inspBtnTrans)
                {
                    var btn = inspBtnTrans.GetComponent<Button>();
                    btn.onClick.AddListener(() => {
                        SocialManager.Instance?.RequestProfile(f.Id);
                    });
                }
            }
        }
    }
}
