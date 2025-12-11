using UnityEngine;

namespace iDate
{
    /// <summary>
    /// Ensures the camera is properly configured for the login screen
    /// Attach this to the Main Camera in the login scene
    /// </summary>
    public class LoginCameraSetup : MonoBehaviour
    {
        private void Start()
        {
            // Ensure camera is orthographic for 2D UI
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f; // Adjust this to show full screen
                cam.transform.position = new Vector3(0, 0, -10);
                cam.transform.rotation = Quaternion.identity;
                
                Debug.Log("[LoginCameraSetup] Camera configured for login screen");
            }
        }
    }
}
