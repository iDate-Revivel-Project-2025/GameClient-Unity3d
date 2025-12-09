using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip bgmCliip;
    private AudioSource audioSource;

    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject serverSelectPanel;

    void Start()
    {
        // Setup Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgmCliip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        
        if (bgmCliip != null)
        {
            audioSource.Play();
        }

        // Init State
        ShowLogin();
    }

    public void ShowLogin()
    {
        loginPanel.SetActive(true);
        serverSelectPanel.SetActive(false);
    }

    public void ShowServerSelect()
    {
        loginPanel.SetActive(false);
        serverSelectPanel.SetActive(true);
    }
}
