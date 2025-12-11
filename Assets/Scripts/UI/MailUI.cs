using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MailUI : MonoBehaviour
{
    public GameObject MailPanel;
    public Transform MailListContainer;
    public GameObject MailItemPrefab;

    [Header("Compose")]
    public GameObject ComposePanel;
    public TMP_InputField TargetInput;
    public TMP_InputField SubjectInput;
    public TMP_InputField ContentInput;

    [Header("Read")]
    public GameObject ReadPanel;
    public TextMeshProUGUI ReadSubject;
    public TextMeshProUGUI ReadContent;
    public TextMeshProUGUI ReadSender;

    private void Start()
    {
        MailManager.Instance.OnMailListReceived += UpdateMailList;
        MailManager.Instance.OnMailSent += OnMailSent;
        HideAll();
    }

    private void OnDestroy()
    {
        if (MailManager.Instance != null)
        {
            MailManager.Instance.OnMailListReceived -= UpdateMailList;
            MailManager.Instance.OnMailSent -= OnMailSent;
        }
    }

    public void ToggleMail()
    {
        if (MailPanel.activeSelf) HideAll();
        else
        {
            HideAll();
            MailPanel.SetActive(true);
            MailManager.Instance.RequestMailList();
        }
    }

    public void OpenCompose()
    {
        ComposePanel.SetActive(true);
        TargetInput.text = "";
        SubjectInput.text = "";
        ContentInput.text = "";
    }

    public void SendMail()
    {
        if (string.IsNullOrEmpty(TargetInput.text) || string.IsNullOrEmpty(ContentInput.text)) return;
        MailManager.Instance.SendMail(TargetInput.text, SubjectInput.text, ContentInput.text);
    }

    private void OnMailSent(bool success)
    {
        if (success)
        {
            ComposePanel.SetActive(false);
            MailManager.Instance.RequestMailList();
        }
        else
        {
            Debug.LogError("Failed to send mail");
        }
    }

    private void UpdateMailList(List<MailItem> items)
    {
        foreach (Transform child in MailListContainer) Destroy(child.gameObject);

        foreach (var item in items)
        {
            GameObject obj = Instantiate(MailItemPrefab, MailListContainer);
            // Assuming Prefab has a simple script or just finding Text components
            // For prototype, strictly find components
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = item.Subject; 
            if (texts.Length > 1) texts[1].text = item.SenderName;
            
            Button btn = obj.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => OpenMail(item));
            
            // Delete Button?
            Button[] btns = obj.GetComponentsInChildren<Button>();
            if (btns.Length > 1)
            {
                btns[1].onClick.AddListener(() => MailManager.Instance.DeleteMail(item.Id));
            }
        }
    }

    public void OpenMail(MailItem item)
    {
        ReadPanel.SetActive(true);
        ReadSubject.text = item.Subject;
        ReadContent.text = item.Content;
        ReadSender.text = $"From: {item.SenderName}";
        
        if (!item.IsRead) MailManager.Instance.ReadMail(item.Id);
    }

    public void CloseRead()
    {
        ReadPanel.SetActive(false);
    }

    public void CloseCompose()
    {
        ComposePanel.SetActive(false);
    }

    private void HideAll()
    {
        MailPanel.SetActive(false);
        ComposePanel.SetActive(false);
        ReadPanel.SetActive(false);
    }
}
