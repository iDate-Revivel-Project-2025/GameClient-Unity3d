using UnityEngine;
using System.Text;
using Assets;

public class ConfigInspector : MonoBehaviour
{
    public string path = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Interface/InterfaceInfo/Item_male_uper.ido";

    [ContextMenu("Inspect Config")]
    public void Inspect()
    {
        byte[] data = IDOLoader.LoadRaw(path);
        if (data != null)
        {
            // Try decoding EUC-KR (Code Page 51949) or just ASCII/UTF8
            // Unity doesn't always have EUC-KR. Let's try default or ASCII first.
            string content = Encoding.ASCII.GetString(data);
            Debug.Log($"Config Content (ASCII): \n{content}");
            
            // If it looks like XML, we are good.
        }
    }
}
