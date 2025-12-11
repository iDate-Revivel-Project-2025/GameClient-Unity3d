using UnityEngine;

namespace iDate.Assets
{
    public class AssetLoader : MonoBehaviour
    {
        public static T Load<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }
    }
}
