using UnityEngine;
using UnityEngine.UI;
using Assets;

public class TextureTest : MonoBehaviour
{
    // Assign a RawImage in Inspector
    public RawImage debugImage; 

    void Start()
    {
        // Try loading a known file
        // F01_PA_FACE_0001_01.ido
        string path = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Charactor/Texture/F01_PA_FACE_0001_01.ido";
        
        Texture2D tex = IDOLoader.LoadTexture(path);
        if (tex != null && debugImage != null)
        {
            debugImage.texture = tex;
            debugImage.SetNativeSize();
            Debug.Log("Texture applied to Debug Image.");
        }
        else
        {
            Debug.LogError("Texture failed to load.");
        }
    }
}
