using UnityEngine;

namespace iDate.Gameplay
{
    public class Conductor : MonoBehaviour
    {
        public static Conductor Instance;

        public AudioSource MusicSource;
        public float Bpm = 120;
        public float SongOffset = 0; // In seconds

        public float SongPosition; // In seconds
        public float SongPositionInBeats;
        public float DspSongTime;

        private float _secPerBeat;

        private void Awake()
        {
            Instance = this;
            _secPerBeat = 60f / Bpm;
        }

        public void LoadSong(iDate.UI.SongData song)
        {
            Debug.Log($"[Conductor] Loading song: {song.Title} ({song.Bpm} BPM)");
            
            // Load Audio
            // Path from server is relative to Resources? Or a full path?
            // Assuming Resources path for now: "Songs/FastLane"
            AudioClip clip = Resources.Load<AudioClip>(song.ResourcePath);
            if (clip != null)
            {
                MusicSource.clip = clip;
                Bpm = song.Bpm;
                _secPerBeat = 60f / Bpm;
                SongOffset = 0; // Reset or read from DB if we add it
                Debug.Log("[Conductor] Song Loaded Successfully.");
            }
            else
            {
                Debug.LogError($"[Conductor] Failed to load audio clip at: {song.ResourcePath}");
            }
        }

        public void StartMusic()
        {
            if (MusicSource == null || MusicSource.clip == null) return;
            
            DspSongTime = (float)AudioSettings.dspTime;
            MusicSource.Play();
        }

        public event System.Action<int> OnBeat;
        private int _lastBeat = 0;

        private void Update()
        {
            if (MusicSource != null && MusicSource.isPlaying)
            {
                // Calculate position relative to DSP time for precision
                SongPosition = (float)(AudioSettings.dspTime - DspSongTime - SongOffset);
                SongPositionInBeats = SongPosition / _secPerBeat;

                // Beat Event
                int currentBeatCil = Mathf.FloorToInt(SongPositionInBeats);
                if (currentBeatCil > _lastBeat)
                {
                    _lastBeat = currentBeatCil;
                    OnBeat?.Invoke(_lastBeat);
                }
            }
        }
    }
}
