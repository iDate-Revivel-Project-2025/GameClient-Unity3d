using UnityEngine;

namespace iDate.Visuals
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance;

        public GameObject PerfectPrefab;
        public GameObject GreatPrefab;
        public GameObject NormalPrefab;
        public GameObject MissPrefab;

        private void Awake()
        {
            Instance = this;
        }

        public void PlayHitEffect(Vector3 position, string rank)
        {
            GameObject prefab = NormalPrefab; // Default
            Color color = Color.white;

            switch(rank)
            {
                case "Perfect": prefab = PerfectPrefab; color = Color.cyan; break;
                case "Great": prefab = GreatPrefab; color = Color.green; break;
                case "Cool": prefab = NormalPrefab; color = Color.yellow; break;
                case "Bad": prefab = MissPrefab; color = new Color(1f, 0.5f, 0f); break;
                case "Miss": prefab = MissPrefab; color = Color.red; break;
            }

            if (prefab != null)
            {
                Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                // Fallback: Create Primitive
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.transform.position = position;
                p.transform.localScale = Vector3.one * 0.5f;
                var rend = p.GetComponent<Renderer>();
                rend.material.color = color;
                
                Destroy(p, 0.5f);
            }
        }
    }
}
