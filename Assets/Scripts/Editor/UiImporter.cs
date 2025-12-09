#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace iDate.Editor
{
    public class UiImporter : EditorWindow
    {
        private string xmlFolder = "Assets/Resources/UI_XML";
        
        [MenuItem("iDate/Import UI XML")]
        public static void ShowWindow()
        {
            GetWindow<UiImporter>("UI Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Import iDate UI", EditorStyles.boldLabel);
            xmlFolder = EditorGUILayout.TextField("XML Folder", xmlFolder);

            if (GUILayout.Button("Import ALL Layouts"))
            {
                BulkImport();
            }
            
            if (GUILayout.Button("Import 'login.xml' Only"))
            {
                ImportUI("login.xml");
            }
        }

        public static void AutoImport()
        {
            UiImporter importer = ScriptableObject.CreateInstance<UiImporter>();
            importer.BulkImport();
            Debug.Log("Auto Import Finished");
        }

        private void BulkImport()
        {
            AssetDatabase.Refresh();
            if (!Directory.Exists(xmlFolder)) return;
            string[] files = Directory.GetFiles(xmlFolder, "*.xml");
            foreach (var f in files)
            {
                ImportUI(Path.GetFileName(f));
            }
        }

        private void ImportUI(string filename)
        {
            string fullPath = Path.Combine(xmlFolder, filename);
            if (!File.Exists(fullPath)) 
            {
                Debug.LogError("File not found: " + fullPath);
                return;
            }

            string xmlContent = File.ReadAllText(fullPath);
            XmlDocument doc = new XmlDocument();
            try { doc.LoadXml(xmlContent); } 
            catch { Debug.LogError($"XML Parse Error in {filename}"); return; }

            // 1. Setup Canvas Prefab Root
            string baseName = Path.GetFileNameWithoutExtension(filename);
            GameObject root = new GameObject($"UI_{baseName}");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = root.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1024, 768);
            cs.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            
            // 2. Parse Textures Map
            Dictionary<int, Texture2D> textureMap = new Dictionary<int, Texture2D>();
            XmlNode dlgInfo = doc.SelectSingleNode("//DlgInfo");
            if (dlgInfo == null) return;
            
            foreach (XmlNode texNode in dlgInfo.SelectNodes("TextureInfo"))
            {
                if (texNode.Attributes["TextureIndex"] == null || texNode.Attributes["TexturePath"] == null) continue;
                int index = int.Parse(texNode.Attributes["TextureIndex"].Value);
                string path = texNode.Attributes["TexturePath"].Value;
                // remove extension
                string texName = Path.GetFileNameWithoutExtension(path);
                
                // Find in Assets/Textures
                string[] guids = AssetDatabase.FindAssets($"{texName} t:Texture2D", new[] { "Assets/Textures" });
                if (guids.Length > 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    textureMap[index] = tex;
                }
                else
                {
                    // Debug.LogWarning($"Texture not found: {texName} (Index {index}) in {filename}");
                }
            }

            // 3. Parse Components
            foreach (XmlNode compNode in dlgInfo.SelectNodes("Component"))
            {
                CreateComponent(compNode, root.transform, textureMap);
            }
            
            // Save as Prefab
            string prefabPath = $"Assets/Prefabs/UI/{root.name}.prefab";
            Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root); // Cleanup scene
            
            Debug.Log($"Created Prefab: {prefabPath}");
        }

        private void CreateComponent(XmlNode node, Transform parent, Dictionary<int, Texture2D> texMap)
        {
            if (node.Attributes["Type"] == null || node.Attributes["Id"] == null) return;
            string type = node.Attributes["Type"].Value;
            string id = node.Attributes["Id"].Value;
            
            float x = node.Attributes["Xpos"] != null ? float.Parse(node.Attributes["Xpos"].Value) : 0;
            float y = node.Attributes["Ypos"] != null ? float.Parse(node.Attributes["Ypos"].Value) : 0;
            float w = node.Attributes["Width"] != null ? float.Parse(node.Attributes["Width"].Value) : 100;
            float h = node.Attributes["Height"] != null ? float.Parse(node.Attributes["Height"].Value) : 100;
            bool visible = node.Attributes["Visible"]?.Value.ToLower() == "true";

            GameObject obj = new GameObject($"{type}_{id}");
            obj.transform.SetParent(parent, false);
            obj.SetActive(visible);
            
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);

            if (type == "Image" || type == "Button")
            {
                RawImage img = obj.AddComponent<RawImage>();
                img.raycastTarget = (type == "Button");
                img.color = Color.clear; // Default to transparent. Only show if texture loads.
                
                XmlNode mainBtn = node.SelectSingleNode("SubElement[@ElementName='Main Button']");
                if (mainBtn != null)
                {
                    int texNum = int.Parse(mainBtn.Attributes["TextureNum"].Value);
                    if (texMap.ContainsKey(texNum))
                    {
                        Texture2D tex = texMap[texNum];
                        if (tex != null)
                        {
                            img.texture = tex;
                            img.color = Color.white;
                        
                            if (mainBtn.Attributes["Left"] != null)
                            {
                                float l = float.Parse(mainBtn.Attributes["Left"].Value);
                                float t = float.Parse(mainBtn.Attributes["Top"].Value);
                                float r = float.Parse(mainBtn.Attributes["Right"].Value);
                                float b = float.Parse(mainBtn.Attributes["Bottom"].Value);
                                
                                float uvX = l / tex.width;
                                float uvW = (r - l) / tex.width;
                                float uvH = (b - t) / tex.height;
                                float uvY = 1.0f - (b / tex.height);
                                
                                img.uvRect = new Rect(uvX, uvY, uvW, uvH);
                            }
                        }
                        else
                        {
                            // Texture in map but null? Treat as missing.
                            img.color = new Color(1, 1, 1, 0); 
                            Debug.LogWarning($"Texture {texNum} loaded as NULL for {type}_{id}");
                        }
                    }
                    else
                    {
                        // Texture missing? Make transparent so it doesn't block view.
                        img.color = new Color(1, 1, 1, 0); 
                    }
                }
                
                if (type == "Button")
                {
                    obj.AddComponent<Button>();
                    if (node.Attributes["Command"] != null)
                        obj.name += $"_Cmd_{node.Attributes["Command"].Value}";
                }
            }
            else if (type == "Ime" || type == "Edit")
            {
                var input = obj.AddComponent<TMP_InputField>();
                
                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(obj.transform, false);
                RawImage bgImg = bg.AddComponent<RawImage>();
                bgImg.color = new Color(0,0,0,0.5f);
                RectTransform bgRT = bg.GetComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
                
                input.targetGraphic = bgImg;
                
                GameObject textArea = new GameObject("TextArea");
                textArea.transform.SetParent(obj.transform, false);
                RectTransform areaRT = textArea.AddComponent<RectTransform>();
                areaRT.anchorMin = Vector2.zero; areaRT.anchorMax = Vector2.one; areaRT.sizeDelta = new Vector2(-10, -10);
                
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(textArea.transform, false);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.color = Color.white;
                text.fontSize = 14;
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one; textRT.sizeDelta = Vector2.zero;
                
                input.textViewport = areaRT;
                input.textComponent = text;
                
                if (node.Attributes["TexType"]?.Value == "PassWord")
                    input.contentType = TMP_InputField.ContentType.Password;
            }
            
            // Auto Z-Order Hack
            // If it's an InputField, bring to front.
            // If it's a large image (> 800 width), send to back.
            if (type == "Ime" || type == "Edit")
            {
                obj.transform.SetAsLastSibling();
            }
            else if (w > 800 && h > 600)
            {
                obj.transform.SetAsFirstSibling();
            }
        }
    }
}
#endif
