using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace iDate.UI
{
    public class SongSelectUI : MonoBehaviour
    {
        public static SongSelectUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject Panel;
        public Transform ContentContainer; // The grid/list parent
        public GameObject SongItemPrefab; // Prefab for individual song entry
        public InputField SearchInput;
        public Button RefreshButton;

        private void Awake()
        {
            Instance = this;
            if (Panel) Panel.SetActive(false); // Hide by default
            
            if (RefreshButton)
            {
                RefreshButton.onClick.AddListener(OnRefreshClicked);
            }
        }

        private void Start()
        {
            if (SongManager.Instance != null)
            {
                SongManager.Instance.OnSongListUpdated += RefreshList;
            }
        }

        private void OnDestroy()
        {
            if (SongManager.Instance != null)
            {
                SongManager.Instance.OnSongListUpdated -= RefreshList;
            }
            Instance = null;
        }

        public void Show()
        {
            if (Panel) Panel.SetActive(true);
            // Refresh logic?
            if (SongManager.Instance != null && SongManager.Instance.Songs.Count == 0)
            {
                SongManager.Instance.RequestSongList();
            }
            else
            {
                RefreshList();
            }
        }

        public void Hide()
        {
            if (Panel) Panel.SetActive(false);
        }

        private void OnRefreshClicked()
        {
            SongManager.Instance?.RequestSongList();
        }

        private void RefreshList()
        {
            if (SongManager.Instance == null) return;
            
            // Clear current
            foreach (Transform child in ContentContainer)
            {
                Destroy(child.gameObject);
            }

            List<SongData> songs = SongManager.Instance.Songs;
            string filter = SearchInput ? SearchInput.text.ToLower() : "";

            foreach (var song in songs)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    if (!song.Title.ToLower().Contains(filter) && !song.Artist.ToLower().Contains(filter))
                        continue;
                }

                CreateSongItem(song);
            }
        }

        private void CreateSongItem(SongData song)
        {
            GameObject item = null;
            if (SongItemPrefab != null)
            {
                item = Instantiate(SongItemPrefab, ContentContainer);
            }
            else
            {
                // Fallback: Create placeholder button
                item = new GameObject($"Song_{song.Id}", typeof(Image), typeof(Button), typeof(LayoutElement));
                item.transform.SetParent(ContentContainer, false);
                
                // Size
                LayoutElement le = item.GetComponent<LayoutElement>();
                le.minHeight = 50;
                le.preferredHeight = 50;
                
                // Color
                item.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                
                // Text
                GameObject textObj = new GameObject("Text", typeof(Text));
                textObj.transform.SetParent(item.transform, false);
                Text t = textObj.GetComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.text = $"{song.Title} - {song.Artist}\nBPM: {song.Bpm} Diff: {song.Difficulty}";
                t.alignment = TextAnchor.MiddleLeft;
                t.color = Color.white;
                RectTransform rt = textObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, 0);
                rt.offsetMax = new Vector2(-10, 0);
            }

            // Click Handler
            Button btn = item.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.AddListener(() => OnSongClicked(song));
            }
        }

        private void OnSongClicked(SongData song)
        {
            Debug.Log($"[SongUI] Clicked {song.Title}");
            SongManager.Instance?.SelectSong(song.Id);
            
            // Highlight UI?
            // Close UI and Start Game?
            // Or just select.
        }
    }
}
