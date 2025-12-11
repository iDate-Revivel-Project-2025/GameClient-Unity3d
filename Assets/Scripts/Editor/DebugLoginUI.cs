using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace iDate.Editor
{
    /// <summary>
    /// Debug script to check LoginUI status
    /// </summary>
    public class DebugLoginUI
    {
        [MenuItem("iDate/DEBUG - Check Login UI Status")]
        public static void CheckStatus()
        {
            Debug.Log("=== LOGIN UI DEBUG ===");
            
            // Find LoginUI component
            iDate.UI.LoginUI loginUI = Object.FindObjectOfType<iDate.UI.LoginUI>();
            
            if (loginUI == null)
            {
                Debug.LogError("❌ LoginUI component NOT FOUND in scene!");
                Debug.LogError("Solution: Run 'iDate > ★ ONE-CLICK FIX - Complete Login Setup'");
                EditorUtility.DisplayDialog("LoginUI Missing!", 
                    "LoginUI component not found in scene!\n\n" +
                    "Run: iDate > ★ ONE-CLICK FIX - Complete Login Setup", 
                    "OK");
                return;
            }
            
            Debug.Log($"✓ LoginUI component found on: {loginUI.gameObject.name}");
            
            // Check assignments
            Debug.Log($"Username Input: {(loginUI.UsernameInput != null ? loginUI.UsernameInput.name : "NULL")}");
            Debug.Log($"Password Input: {(loginUI.PasswordInput != null ? loginUI.PasswordInput.name : "NULL")}");
            Debug.Log($"Login Button: {(loginUI.LoginButton != null ? loginUI.LoginButton.name : "NULL")}");
            Debug.Log($"Status Text: {(loginUI.StatusText != null ? loginUI.StatusText.name : "NULL")}");
            
            // Check button listeners
            if (loginUI.LoginButton != null)
            {
                int listenerCount = loginUI.LoginButton.onClick.GetPersistentEventCount();
                Debug.Log($"Login Button has {listenerCount} persistent listeners");
                
                // Check if button is active
                Debug.Log($"Login Button active: {loginUI.LoginButton.gameObject.activeInHierarchy}");
                Debug.Log($"Login Button interactable: {loginUI.LoginButton.interactable}");
            }
            
            // Check NetworkManager
            iDate.Network.NetworkManager nm = Object.FindObjectOfType<iDate.Network.NetworkManager>();
            if (nm == null)
            {
                Debug.LogError("❌ NetworkManager NOT FOUND!");
            }
            else
            {
                Debug.Log("✓ NetworkManager found");
            }
            
            Debug.Log("=== END DEBUG ===");
            
            // Show summary dialog
            string summary = "LoginUI Debug Summary:\n\n";
            summary += $"LoginUI: {(loginUI != null ? "✓ Found" : "❌ Missing")}\n";
            summary += $"Username: {(loginUI?.UsernameInput != null ? "✓" : "❌")}\n";
            summary += $"Password: {(loginUI?.PasswordInput != null ? "✓" : "❌")}\n";
            summary += $"Button: {(loginUI?.LoginButton != null ? "✓" : "❌")}\n";
            summary += $"NetworkManager: {(nm != null ? "✓" : "❌")}\n\n";
            
            if (loginUI != null && loginUI.LoginButton != null)
            {
                summary += "Button looks OK. If login still doesn't work:\n";
                summary += "1. Check Unity Console for errors\n";
                summary += "2. Add Debug.Log in LoginUI.OnLoginClicked\n";
                summary += "3. Verify server is running on port 8132";
            }
            else
            {
                summary += "Run: iDate > Wire Up Login UI";
            }
            
            EditorUtility.DisplayDialog("Login UI Debug", summary, "OK");
        }
    }
}
