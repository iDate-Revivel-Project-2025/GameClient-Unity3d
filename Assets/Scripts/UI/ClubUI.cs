using UnityEngine;
using UnityEngine.UI;
using iDate.Managers;
using System.Collections.Generic;

namespace iDate.UI
{
    public class ClubUI : MonoBehaviour
    {
        public Transform ListContent;
        public GameObject ClubItemPrefab; // Needs to be assigned or created programmatically
        
        public InputField InputName;
        public InputField InputDesc;
        public Button BtnCreate;
        public Button BtnRefresh;
        public Button BtnJoin; // Dynamic or separate
        
        private int _selectedClubId = -1;

        private void Start()
        {
            BtnCreate.onClick.AddListener(OnCreateClicked);
            BtnRefresh.onClick.AddListener(OnRefreshClicked);
            // ClubItemPrefab usually needs a script to handle selection
        }

        private void OnEnable()
        {
            if (ClubManager.Instance != null)
            {
                ClubManager.Instance.OnClubListUpdated += RefreshList;
                ClubManager.Instance.OnClubCreated += OnCreated;
            }
        }

        private void OnDisable()
        {
            if (ClubManager.Instance != null)
            {
                ClubManager.Instance.OnClubListUpdated -= RefreshList;
                ClubManager.Instance.OnClubCreated -= OnCreated;
            }
        }
        
        private void OnCreated(bool success, int id)
        {
            if (success) {
                InputName.text = "";
                InputDesc.text = "";
            } else {
                Debug.LogError("Club Create Failed");
            }
        }

        private void OnRefreshClicked()
        {
            ClubManager.Instance.SendClubListRequest();
        }

        private void OnCreateClicked()
        {
            if (string.IsNullOrEmpty(InputName.text)) return;
            ClubManager.Instance.SendClubCreateRequest(InputName.text, InputDesc.text);
        }

        private void RefreshList(List<ClubData> clubs)
        {
            // Clear current list logic (destroy children)
            foreach (Transform child in ListContent) {
                Destroy(child.gameObject);
            }
            
            foreach (var club in clubs)
            {
                // Instantiate item
                if (ClubItemPrefab != null) {
                    var go = Instantiate(ClubItemPrefab, ListContent);
                    var txt = go.GetComponentInChildren<Text>();
                    if (txt) txt.text = $"{club.Name} (Members: {club.MemberCount})";
                    
                    var btn = go.GetComponent<Button>();
                    if (btn) btn.onClick.AddListener(() => {
                        _selectedClubId = club.Id;
                        Debug.Log($"Selected Club: {club.Name}");
                        if (BtnJoin != null) {
                             BtnJoin.onClick.RemoveAllListeners();
                             BtnJoin.onClick.AddListener(() => ClubManager.Instance.SendClubJoinRequest(club.Id));
                        }
                    });
                }
            }
        }
    }
}
