using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace iDate.Editor
{
    /// <summary>
    /// Final fix for Login UI - ensures input fields are clickable and interactive
    /// </summary>
    public class FinalLoginUIFix
    {
        [MenuItem("iDate/FINAL FIX - Make Login UI Interactive")]
        public static void MakeLoginInteractive()
        {
            Debug.Log("=== Final Login UI Fix ===");
            
            // 1. Ensure EventSystem exists (required for UI interaction!)
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Debug.Log("✓ Created EventSystem (required for UI clicks!)");
            }
            else
            {
                Debug.Log("✓ EventSystem already exists");
            }
            
            // 2. Find all Canvas objects
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                // Ensure GraphicRaycaster exists
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"✓ Added GraphicRaycaster to {canvas.name}");
                }
                
                // Fix Canvas settings
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // Ensure proper sorting order
                canvas.sortingOrder = 0;
                
                Debug.Log($"✓ Fixed Canvas: {canvas.name}");
            }
            
            // 3. Find and fix all TMP_InputFields
            TMP_InputField[] inputFields = Object.FindObjectsOfType<TMP_InputField>(true);
            Debug.Log($"Found {inputFields.Length} input fields");
            
            foreach (TMP_InputField field in inputFields)
            {
                // Ensure the field is active
                field.gameObject.SetActive(true);
                
                // Ensure interactable
                field.interactable = true;
                
                // Ensure it has a target graphic
                if (field.targetGraphic == null)
                {
                    Image img = field.GetComponent<Image>();
                    if (img != null)
                    {
                        field.targetGraphic = img;
                        img.raycastTarget = true;
                    }
                }
                
                // Ensure text component exists
                if (field.textComponent == null)
                {
                    TextMeshProUGUI text = field.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        field.textComponent = text;
                        field.textViewport = text.rectTransform.parent as RectTransform;
                    }
                }
                
                // Set proper content type
                if (field.name.ToLower().Contains("pass") || field.name.ToLower().Contains("pw"))
                {
                    field.contentType = TMP_InputField.ContentType.Password;
                }
                
                Debug.Log($"✓ Fixed input field: {field.name} (Active: {field.gameObject.activeInHierarchy}, Interactable: {field.interactable})");
            }
            
            // 4. Find and fix all Buttons
            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            Debug.Log($"Found {buttons.Length} buttons");
            
            foreach (Button btn in buttons)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                
                if (btn.targetGraphic == null)
                {
                    Image img = btn.GetComponent<Image>();
                    if (img != null)
                    {
                        btn.targetGraphic = img;
                        img.raycastTarget = true;
                    }
                }
                
                Debug.Log($"✓ Fixed button: {btn.name}");
            }
            
            // 5. Fix visual artifacts and blocking UI elements
            Image[] allImages = Object.FindObjectsOfType<Image>(true);
            foreach (Image img in allImages)
            {
                RectTransform rt = img.rectTransform;
                
                // Fix white line/box artifacts - make input field backgrounds transparent
                if (img.transform.parent != null)
                {
                    TMP_InputField parentInput = img.GetComponentInParent<TMP_InputField>();
                    if (parentInput != null && img.gameObject == parentInput.gameObject)
                    {
                        // This is an input field's background - make it mostly transparent
                        img.color = new Color(0, 0, 0, 0.3f); // Dark semi-transparent
                        img.raycastTarget = true; // But keep it clickable
                        Debug.Log($"✓ Fixed input field background: {img.name}");
                    }
                }
                
                // If it's a large background image, disable raycast to prevent blocking
                if (rt.sizeDelta.x > 800 && rt.sizeDelta.y > 600)
                {
                    img.raycastTarget = false;
                    Debug.Log($"✓ Disabled raycast on large image: {img.name} (to prevent blocking clicks)");
                }
            }
            
            // 6. Save scene
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("✓ Scene saved");
            
            Debug.Log("=== Final Login UI Fix COMPLETE ===");
            
            EditorUtility.DisplayDialog("Success!", 
                "Login UI is now interactive!\n\n" +
                "✓ EventSystem added\n" +
                "✓ All input fields enabled\n" +
                "✓ All buttons enabled\n" +
                "✓ Raycasting configured\n" +
                "✓ Background images won't block clicks\n\n" +
                "Press PLAY and try clicking the input fields!", 
                "Test It!");
        }
    }
}
