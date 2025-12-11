using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoupleUI : MonoBehaviour
{
    public GameObject ProposalPanel;
    public TextMeshProUGUI ProposalText;
    public Button AcceptButton;
    public Button RejectButton;
    public TextMeshProUGUI NotificationText;

    private uint _pendingProposerId;

    private void Start()
    {
        if (CoupleManager.Instance != null)
        {
            CoupleManager.Instance.OnProposalReceived += ShowProposal;
            CoupleManager.Instance.OnProposalResult += ShowResult;
            CoupleManager.Instance.OnBreakUp += ShowBreakUp;
        }

        if (AcceptButton) AcceptButton.onClick.AddListener(OnAccept);
        if (RejectButton) RejectButton.onClick.AddListener(OnReject);

        if (ProposalPanel) ProposalPanel.SetActive(false);
        if (NotificationText) NotificationText.text = "";
    }

    private void OnDestroy()
    {
        if (CoupleManager.Instance != null)
        {
            CoupleManager.Instance.OnProposalReceived -= ShowProposal;
            CoupleManager.Instance.OnProposalResult -= ShowResult;
            CoupleManager.Instance.OnBreakUp -= ShowBreakUp;
        }
    }

    public void ShowProposal(uint proposerId, string name)
    {
        _pendingProposerId = proposerId;
        if (ProposalPanel)
        {
            ProposalPanel.SetActive(true);
            if (ProposalText) ProposalText.text = $"{name} has proposed to you!\nDo you accept?";
        }
    }

    public void OnAccept()
    {
        if (_pendingProposerId != 0)
        {
            CoupleManager.Instance.SendResponse(_pendingProposerId, true);
        }
        CloseProposal();
    }

    public void OnReject()
    {
        if (_pendingProposerId != 0)
        {
            CoupleManager.Instance.SendResponse(_pendingProposerId, false);
        }
        CloseProposal();
    }

    public void CloseProposal()
    {
        if (ProposalPanel) ProposalPanel.SetActive(false);
        _pendingProposerId = 0;
    }

    public void ShowResult(bool success, uint partnerId)
    {
        string msg = success ? "Proposal Accepted! You are now a couple!" : "Proposal Rejected.";
        ShowNotification(msg);
    }

    public void ShowBreakUp(uint partnerId)
    {
        ShowNotification("You have broken up.");
    }

    private void ShowNotification(string msg)
    {
        if (NotificationText)
        {
            NotificationText.text = msg;
            StartCoroutine(ClearNotification(3f));
        }
        Debug.Log($"[CoupleUI] {msg}");
    }

    private IEnumerator ClearNotification(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (NotificationText) NotificationText.text = "";
    }
}
