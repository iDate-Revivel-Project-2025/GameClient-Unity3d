using System;
using System.IO;
using System.Text;
using UnityEngine;
using iDate.Network;

public class CoupleManager : MonoBehaviour
{
    public static CoupleManager Instance;

    public event Action<uint, string> OnProposalReceived; // proposerId, proposerName
    public event Action<bool, uint> OnProposalResult; // success, partnerId
    public event Action<uint> OnBreakUp;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPacketReceived += HandlePacket;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }
    }
    
    private void HandlePacket(Packet p)
    {
        switch ((OpCode)p.OpCode)
        {
            case OpCode.COUPLE_REQ:
                HandleCoupleRequest(p);
                break;
            case OpCode.COUPLE_RESP:
                HandleCoupleResponse(p);
                break;
            case OpCode.COUPLE_BREAK:
                HandleCoupleBreak(p);
                break;
        }
    }

    public void SendProposal(uint targetId)
    {
        // [4 bytes TargetID]
        using (Packet p = new Packet(OpCode.COUPLE_REQ))
        {
            p.Writer.Write(targetId);
            NetworkManager.Instance.Send(p);
            Debug.Log($"[CoupleManager] Sent proposal to {targetId}");
        }
    }

    public void SendResponse(uint proposerId, bool accepted)
    {
        // [4 bytes ProposerID] [1 byte Accepted(1)/Rejected(0)]
        using (Packet p = new Packet(OpCode.COUPLE_RESP))
        {
            p.Writer.Write(proposerId);
            p.Writer.Write(accepted ? (byte)1 : (byte)0);
            NetworkManager.Instance.Send(p);
            Debug.Log($"[CoupleManager] Sent response to {proposerId}: {accepted}");
        }
    }

    public void BreakUp()
    {
        // No payload needed, server knows user
        using (Packet p = new Packet(OpCode.COUPLE_BREAK))
        {
            NetworkManager.Instance.Send(p);
            Debug.Log("[CoupleManager] Sent Break Up Request");
        }
    }

    private void HandleCoupleRequest(Packet p)
    {
        // [4 bytes SenderID] [2 bytes NameLen] [Name:Var]
        try
        {
            uint senderId = p.Reader.ReadUInt32();
            ushort nameLen = p.Reader.ReadUInt16();
            
            // Read name bytes
            byte[] nameBytes = p.Reader.ReadBytes(nameLen * 2);
            string senderName = Encoding.Unicode.GetString(nameBytes);

            Debug.Log($"[CoupleManager] Proposal received from {senderName} ({senderId})");
            OnProposalReceived?.Invoke(senderId, senderName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoupleManager] Error parsing COUPLE_REQ: {e.Message}");
        }
    }

    private void HandleCoupleResponse(Packet p)
    {
        // [4 bytes PartnerID] [1 byte Result(1=Success, 0=Fail)]
        try
        {
            uint partnerId = p.Reader.ReadUInt32();
            bool success = p.Reader.ReadByte() == 1;

            Debug.Log($"[CoupleManager] Proposal result with {partnerId}: {success}");
            OnProposalResult?.Invoke(success, partnerId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoupleManager] Error parsing COUPLE_RESP: {e.Message}");
        }
    }

    private void HandleCoupleBreak(Packet p)
    {
        // [1 byte Success/Reason]
        try
        {
            byte result = p.Reader.ReadByte(); 
            // Just assuming break up happened
            uint partnerId = 0; 
            
            Debug.Log($"[CoupleManager] Break up notification received. (Res: {result})");
            OnBreakUp?.Invoke(partnerId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CoupleManager] Error parsing COUPLE_BREAK: {e.Message}");
        }
    }
}
