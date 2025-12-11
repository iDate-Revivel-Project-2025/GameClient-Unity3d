using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace iDate.Editor
{
    /// <summary>
    /// Wires up the LoginUI component to the actual UI elements
    /// </summary>
    public class WireUpLoginUI
    {
        [MenuItem("iDate/Wire Up Login UI")]
        public static void WireUp()
        {
            Debug.Log("=== Wiring Up Login UI ===");
            
            // Find the LoginUI component
            iDate.UI.LoginUI loginUI = Object.FindObjectOfType<iDate.UI.LoginUI>();
            
            if (loginUI == null)
            {
                EditorUtility.DisplayDialog("Error", "LoginUI component not found in scene!", "OK");
                return;
            }
            
            // Find all input fields
            TMP_InputField[] inputFields = Object.FindObjectsOfType<TMP_InputField>(true);
            
            // Find all buttons
            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            
            // Wire up input fields
            foreach (TMP_InputField field in inputFields)
            {
                if (field.contentType == TMP_InputField.ContentType.Password)
                {
                    loginUI.PasswordInput = field;
                    Debug.Log($"✓ Assigned Password field: {field.name}");
                }
                else
                {
                    loginUI.UsernameInput = field;
                    Debug.Log($"✓ Assigned Username field: {field.name}");
                }
            }
            
            // Wire up login button (look for button with "login" in name or the first button)
            foreach (Button btn in buttons)
            {
                if (btn.name.ToLower().Contains("login"))
                {
                    loginUI.LoginButton = btn;
                    Debug.Log($"✓ Assigned Login button: {btn.name}");
                    break;
                }
            }
            
            // If no login button found, use first button
            if (loginUI.LoginButton == null && buttons.Length > 0)
            {
                loginUI.LoginButton = buttons[0];
                Debug.Log($"✓ Assigned Login button (first button): {buttons[0].name}");
            }
            
            // Mark as dirty to save changes
            UnityEditor.EditorUtility.SetDirty(loginUI);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("=== Login UI Wired Up ===");
            
            EditorUtility.DisplayDialog("Login UI Wired!", 
                "Login UI has been wired up!\n\n" +
                $"Username: {(loginUI.UsernameInput != null ? loginUI.UsernameInput.name : "NOT FOUND")}\n" +
                $"Password: {(loginUI.PasswordInput != null ? loginUI.PasswordInput.name : "NOT FOUND")}\n" +
                $"Button: {(loginUI.LoginButton != null ? loginUI.LoginButton.name : "NOT FOUND")}\n\n" +
                "Press Play and try logging in!", 
                "OK");
        }
    }
}
