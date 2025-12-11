using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace iDate.UI // Keeping it in UI or creating a shared Data namespace? keeping UI for now to match NetworkManager reference
{
    [System.Serializable]
    public struct SongData
    {
        public uint Id;
        public string Title;
        public string Artist;
        public string ResourcePath;
        public float Bpm;
        public int Duration;
        public int Difficulty;
    }

    public class SongManager : MonoBehaviour
    {
        public static SongManager Instance { get; private set; }

        [SerializeField]
        private List<SongData> _songs = new List<SongData>();
        public List<SongData> Songs => _songs;

        public SongData? SelectedSong { get; private set; }

        public delegate void SongListUpdatedHandler();
        public event SongListUpdatedHandler OnSongListUpdated;

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

        public void RequestSongList()
        {
            Debug.Log("[SongManager] Requesting Song List...");
            if (iDate.Network.NetworkManager.Instance != null)
            {
                iDate.Network.NetworkManager.Instance.SendSongListRequest();
            }
        }

        public void SetSongs(List<SongData> songs)
        {
            _songs = songs;
            Debug.Log($"[SongManager] Loaded {_songs.Count} songs.");
            OnSongListUpdated?.Invoke();
        }

        public void SelectSong(uint songId)
        {
            foreach (var song in _songs)
            {
                if (song.Id == songId)
                {
                    SelectedSong = song;
                    Debug.Log($"[SongManager] Selected song: {song.Title}");
                    return;
                }
            }
        }
    }
}
