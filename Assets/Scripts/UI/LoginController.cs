using UnityEngine;
using iDate.Network;

public class LoginController : MonoBehaviour
{
    private string _status = "Disconnected";

    private void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnected += () => _status = "Connected";
            NetworkManager.Instance.OnPacketReceived += OnPacket;
            NetworkManager.Instance.OnDisconnected += () => _status = "Disconnected";
        }
    }

    private void OnPacket(Packet p)
    {
        if ((OpCode)p.OpCode == OpCode.LOGIN_RESP)
        {
            _status = "Logged In!";
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label($"<b>Status:</b> {_status}");
        
        if (GUILayout.Button("Reconnect"))
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.Disconnect();
                NetworkManager.Instance.Connect("127.0.0.1", 8132);
            }
        }

        GUILayout.EndArea();
    }
}
