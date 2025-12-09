using UnityEngine;

public class TextureDumper : MonoBehaviour
{
    [ContextMenu("Dump Lobby Texture")]
    public void Dump()
    {
        string path = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Interface/Interface04/LobbyUI.ido";
        Texture2D tex = Assets.IDOLoader.LoadTexture(path);
        
        if (tex != null)
        {
             byte[] png = tex.EncodeToPNG();
             string outPath = Application.dataPath + "/LobbyUI_Dump.png";
             System.IO.File.WriteAllBytes(outPath, png);
             Debug.Log($"Dumped texture to {outPath}");
        }
    }
}
