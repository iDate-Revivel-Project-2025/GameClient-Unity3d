using UnityEngine;
using UnityEngine.UI;
using TMPro;
using iDate.Managers;

public class ProfileUI : MonoBehaviour
{
    public GameObject ProfilePanel;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI LevelText;
    public TextMeshProUGUI GenderText; // 0=M, 1=F
    public TextMeshProUGUI StatusText;
    public Button CloseButton;

    public Button ProposeButton;
    private uint _currentProfileId;

    private void Start()
    {
        if (SocialManager.Instance != null)
        {
            SocialManager.Instance.OnProfileReceived += ShowProfile;
        }
        
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(Close);
        }

        if (ProposeButton != null)
        {
            ProposeButton.onClick.AddListener(Propose);
        }
        
        if (ProfilePanel != null) ProfilePanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (SocialManager.Instance != null)
        {
            SocialManager.Instance.OnProfileReceived -= ShowProfile;
        }
    }
    
    public void ShowProfile(ProfileData p)
    {
        if (ProfilePanel == null) return;
        
        _currentProfileId = p.Id;
        ProfilePanel.SetActive(true);
        if (NameText) NameText.text = p.Name;
        if (LevelText) LevelText.text = $"Lv. {p.Level}";
        if (GenderText) GenderText.text = (p.Gender == 1) ? "Female" : "Male"; // Assuming 0=Male, 1=Female
        if (StatusText) StatusText.text = p.IsOnline ? "Online" : "Offline";
    }

    public void Propose()
    {
        if (_currentProfileId == 0) return;
        if (CoupleManager.Instance != null)
        {
            CoupleManager.Instance.SendProposal(_currentProfileId);
            // Maybe close profile or show feedback?
            Debug.Log($"[ProfileUI] Proposed to {_currentProfileId}");
        }
    }
    
    public void Close()
    {
        if (ProfilePanel) ProfilePanel.SetActive(false);
    }
}
