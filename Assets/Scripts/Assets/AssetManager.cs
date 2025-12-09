using UnityEngine;

public class AssetManager : MonoBehaviour
{
    public static AssetManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public GameObject LoadCharacter(string characterId)
    {
        // Example path construction
        // TODO: Map characterId to actual filenames using a database
        string path = "/Users/hades/UnityGames/iDate Revival 2025/GameClient/Media/Charactor/Skin/F01_AC_BAG1_0001_00.gb3"; 
        
        // GB3Loader is a static utility class
        var model = iDate.Assets.GB3Loader.Load(path);
        if (model != null)
        {
            return iDate.Assets.GB3Loader.ToGameObject(model);
        }
        
        return null;
    }
}
