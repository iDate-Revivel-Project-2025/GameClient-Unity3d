using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using iDate.Network;

public class MailItem
{
    public uint Id;
    public uint SenderId;
    public string SenderName;
    public string Subject;
    public string Content;
    public bool IsRead;
    public long SentAt; // Timestamp
}

public class MailManager : MonoBehaviour
{
    public static MailManager Instance { get; private set; }

    public event Action<List<MailItem>> OnMailListReceived;
    public event Action<bool> OnMailSent;
    public event Action<bool> OnMailDeleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        NetworkManager.Instance.RegisterHandler(OpCode.MAIL_LIST, HandleMailList);
        NetworkManager.Instance.RegisterHandler(OpCode.MAIL_SEND_RESP, HandleMailSendResp);
        NetworkManager.Instance.RegisterHandler(OpCode.MAIL_DELETE_RESP, HandleMailDeleteResp);
    }

    public void RequestMailList()
    {
        Packet p = new Packet(OpCode.MAIL_LIST);
        NetworkManager.Instance.SendPacket(p);
    }

    public void SendMail(string targetName, string subject, string content)
    {
        Packet p = new Packet(OpCode.MAIL_SEND_REQ);
        // Format: [Header][TargetName:64][Subject:64][Content:128]
        // We must pad strings manually or Packet class helper needed?
        // Let's implement manual write with padding in Packet or here.
        // Assuming Packet.WriteString handles length prefix? No, server expects Fixed Size Buffers or specialized parsing.
        // The server parser `packet.toString('utf16le', offset, offset + 64)` expects fixed bytes.
        
        // Write fixed strings
        WriteFixedString(p, targetName, 64);
        WriteFixedString(p, subject, 64);
        WriteFixedString(p, content, 128);
        
        NetworkManager.Instance.SendPacket(p);
    }

    public void ReadMail(uint mailId)
    {
        Packet p = new Packet(OpCode.MAIL_READ_REQ);
        p.WriteUInt(mailId);
        NetworkManager.Instance.SendPacket(p);
    }

    public void DeleteMail(uint mailId)
    {
        Packet p = new Packet(OpCode.MAIL_DELETE_REQ);
        p.WriteUInt(mailId);
        NetworkManager.Instance.SendPacket(p);
    }

    private void HandleMailList(Packet p)
    {
        uint count = p.ReadUInt();
        List<MailItem> mail = new List<MailItem>();

        for (int i = 0; i < count; i++)
        {
            MailItem m = new MailItem();
            m.Id = p.ReadUInt();
            m.SenderId = p.ReadUInt();
            m.SenderName = ReadFixedString(p, 64);
            m.Subject = ReadFixedString(p, 64);
            m.Content = ReadFixedString(p, 128);
            m.IsRead = p.ReadByte() == 1;
            m.SentAt = p.ReadLong();
            mail.Add(m);
        }

        OnMailListReceived?.Invoke(mail);
    }

    private void HandleMailSendResp(Packet p)
    {
        bool success = p.ReadUShort() == 1; // Assuming server sent ushort 1 or 0? 
        // Server: resp.writeUInt16LE(success ? 1 : 0, 4);
        OnMailSent?.Invoke(success);
        if (success) RequestMailList(); // Refresh
    }

    private void HandleMailDeleteResp(Packet p)
    {
        bool success = p.ReadUShort() == 1;
        OnMailDeleted?.Invoke(success);
        if (success) RequestMailList();
    }

    // Helpers for Fixed Structure
    private void WriteFixedString(Packet p, string s, int sizeBytes)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(s);
        byte[] buffer = new byte[sizeBytes];
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, sizeBytes));
        p.WriteBytes(buffer);
    }

    private string ReadFixedString(Packet p, int sizeBytes)
    {
        byte[] bytes = p.ReadBytes(sizeBytes);
        return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
    }
}
