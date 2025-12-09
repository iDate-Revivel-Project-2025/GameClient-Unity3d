using UnityEngine;
using System.Text;
using System.IO;
using Assets;

public class UIAnalyzer : MonoBehaviour
{
    // Paths to analyze
    public string loginConfigPath = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Interface/InterfaceInfo/login.ido";
    public string lobbyTexturePath = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Interface/Interface04/LobbyUI.ido";

    [ContextMenu("Analyze Login Config")]
    public void AnalyzeLogin()
    {
        byte[] data = IDOLoader.LoadRaw(loginConfigPath);
        if (data == null) return;

        // Attempt verify if it's XML
        string str = Encoding.Default.GetString(data); // Try default system encoding first
        Debug.Log($"[UIAnalyzer] Login Config (Default): {str.Substring(0, Mathf.Min(500, str.Length))}");

        // Try EUC-KR if available, otherwise UTF-16
        try {
            // 51949 is EUC-KR, but specific to Windows. In .NET Core/Unity we might need 'korean' or 949.
            // Encoding.GetEncoding("euc-kr") often requires I18N dlls.
            // Let's brute force valid ascii ranges.
        } catch {}
    }

    [ContextMenu("Analyze Lobby Texture")]
    public void AnalyzeLobbyTexture()
    {
        Texture2D tex = IDOLoader.LoadTexture(lobbyTexturePath);
        if (tex != null)
        {
            Debug.Log($"[UIAnalyzer] LobbyUI loaded: {tex.width}x{tex.height} format={tex.format}");
        }
    }
}
