using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using iDate.Network;
using iDate.UI;
using iDate.Shop;
using iDate.Gameplay;
using iDate.Visuals;
using UnityEngine.UI;
using TMPro;

namespace iDate.Editor
{
    public class ProjectSetup : EditorWindow
    {
        [MenuItem("iDate/Setup Project")]
        public static void Setup()
        {
            SetupDirectories();
            SetupLoginScene();
            // SetupGameScene(); // DISABLED: Causes UI overlay issues - setup manually if needed
            SetupBuildSettings();
            
            EditorUtility.DisplayDialog("iDate Setup", "Login Scene Generated Successfully!\nGame scene setup disabled to prevent UI conflicts.", "OK");
        }

        private static void SetupDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        }

        private static void SetupLoginScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // 1. NetworkManager Setup
            GameObject nmGO = new GameObject("NetworkManager");
            NetworkManager nm = nmGO.AddComponent<NetworkManager>();
            Object.DontDestroyOnLoad(nmGO);
            
            // Add all required managers
            if (nm.GetComponent<iDate.Gameplay.RoomManager>() == null) nm.gameObject.AddComponent<iDate.Gameplay.RoomManager>();
            if (nm.GetComponent<iDate.Managers.ClubManager>() == null) nm.gameObject.AddComponent<iDate.Managers.ClubManager>();
            
            // 2. UI Canvas - Responsive and properly scaled
            GameObject canvasGO = new GameObject("LoginCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Ensure it's on top
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 768);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // 3. Create transparent input fields matching original positions
            // Username field - positioned at original XML coordinates (195, 470)
            GameObject userObj = CreateTransparentInputField("UsernameField", 195, 470, 200, 30, canvasGO.transform);
            TMP_InputField userInput = userObj.GetComponent<TMP_InputField>();
            
            // Password field - positioned at original XML coordinates (195, 505)
            GameObject passObj = CreateTransparentInputField("PasswordField", 195, 505, 200, 30, canvasGO.transform);
            TMP_InputField passInput = passObj.GetComponent<TMP_InputField>();
            passInput.contentType = TMP_InputField.ContentType.Password;
            
            // 4. Create transparent button overlay for LOGIN button (417, 461, 107x67)
            GameObject loginBtnObj = CreateTransparentButton("LoginButton", 417, 461, 107, 67, canvasGO.transform);
            Button loginBtn = loginBtnObj.GetComponent<Button>();
            
            // 5. Add LoginUI component to manage the UI
            LoginUI loginUI = canvasGO.AddComponent<LoginUI>();
            loginUI.UsernameInput = userInput;
            loginUI.PasswordInput = passInput;
            loginUI.LoginButton = loginBtn;
            
            // 6. Add status text (optional, for debugging)
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(canvasGO.transform, false);
            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 14;
            statusText.color = Color.yellow;
            statusText.alignment = TextAlignmentOptions.Center;
            RectTransform statusRT = statusObj.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0.5f, 0);
            statusRT.anchorMax = new Vector2(0.5f, 0);
            statusRT.pivot = new Vector2(0.5f, 0);
            statusRT.anchoredPosition = new Vector2(0, 20);
            statusRT.sizeDelta = new Vector2(400, 30);
            loginUI.StatusText = statusText;
            
            // Save scene
            string path = "Assets/Scenes/Login.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[ProjectSetup] Login scene created at {path}");
        }

        private static void SetupGameScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Managers
            GameObject gameGO = new GameObject("GameplayManager");
            gameGO.AddComponent<GameplayManager>();
            gameGO.AddComponent<Conductor>();

            // Visual Managers
            GameObject vfxGO = new GameObject("VFXManager");
            vfxGO.AddComponent<VFXManager>();
            
            // Camera Controller (Attach to Main Camera)
            GameObject camGO = GameObject.Find("Main Camera");
            if (camGO == null) 
            {
                camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            camGO.AddComponent<CameraController>();

            // HUD UI
            GameObject hudGO = new GameObject("HUDCanvas");
            Canvas hudCanvas = hudGO.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudGO.AddComponent<CanvasScaler>();
            hudGO.AddComponent<GraphicRaycaster>();
            
            SetupScoreUI(hudGO);
            SetupResultUI(hudGO);
            SetupMessengerUI(hudGO); // Add Messenger to HUD
            SetupCoupleUI(hudGO);    // Ensure CoupleUI is also added if not already (it was missing in snippet)

            // Lobby UI (Overlay)
            GameObject canvasGO = new GameObject("LobbyCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            canvasGO.AddComponent<LobbyUI>();
            canvasGO.AddComponent<SongSelectUI>(); // Add component, will wire below

            // Song Select Panel
            GameObject songSelGO = new GameObject("SongSelectPanel");
            songSelGO.transform.SetParent(canvasGO.transform, false);
            Image ssBg = songSelGO.AddComponent<Image>();
            ssBg.color = new Color(0.1f, 0, 0.1f, 0.95f);
            RectTransform ssRT = songSelGO.GetComponent<RectTransform>();
            ssRT.anchorMin = new Vector2(0.2f, 0.2f);
            ssRT.anchorMax = new Vector2(0.8f, 0.8f);
            ssRT.offsetMin = Vector2.zero;
            ssRT.offsetMax = Vector2.zero;

            // Header
            CreateText("Select Song", 180, songSelGO.transform, 32);

            // Scroll View for Songs
            GameObject ssScroll = new GameObject("SongsScroll");
            ssScroll.transform.SetParent(songSelGO.transform, false);
            ScrollRect ssSR = ssScroll.AddComponent<ScrollRect>();
            RectTransform ssScrollRT = ssScroll.GetComponent<RectTransform>();
            ssScrollRT.anchorMin = new Vector2(0.05f, 0.1f);
            ssScrollRT.anchorMax = new Vector2(0.95f, 0.85f);
            ssScrollRT.offsetMin = Vector2.zero;
            ssScrollRT.offsetMax = Vector2.zero;

            // Content
            GameObject ssContent = new GameObject("Content");
            ssContent.transform.SetParent(ssScroll.transform, false);
            VerticalLayoutGroup ssVLG = ssContent.AddComponent<VerticalLayoutGroup>();
            ssVLG.childControlHeight = true;
            ssVLG.childForceExpandHeight = false;
            ssVLG.spacing = 5;
            ContentSizeFitter ssCSF = ssContent.AddComponent<ContentSizeFitter>();
            ssCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform ssContentRT = ssContent.GetComponent<RectTransform>();
            ssContentRT.anchorMin = new Vector2(0, 1);
            ssContentRT.anchorMax = new Vector2(1, 1);
            ssContentRT.pivot = new Vector2(0.5f, 1);
            ssSR.content = ssContentRT;

            // Refresh Button
            GameObject refBtnGO = new GameObject("RefreshButton");
            refBtnGO.transform.SetParent(songSelGO.transform, false);
            Image refBtnImg = refBtnGO.AddComponent<Image>();
            refBtnImg.color = Color.yellow;
            Button refBtn = refBtnGO.AddComponent<Button>();
            RectTransform refBtnRT = refBtnGO.GetComponent<RectTransform>();
            refBtnRT.anchorMin = new Vector2(0.5f, 0);
            refBtnRT.anchorMax = new Vector2(0.5f, 0);
            refBtnRT.pivot = new Vector2(0.5f, 0);
            refBtnRT.anchoredPosition = new Vector2(0, 10);
            refBtnRT.sizeDelta = new Vector2(120, 40);
            CreateText("Refresh", 0, refBtnGO.transform, 20, Color.black);

            // Wire UI
            SongSelectUI ssUI = canvasGO.GetComponent<SongSelectUI>();
            ssUI.Panel = songSelGO;
            ssUI.ContentContainer = ssContent.transform;
            ssUI.RefreshButton = refBtn;
            // SongItemPrefab is null, will use fallback

            songSelGO.SetActive(false); // Hide initially

            canvasGO.AddComponent<LobbyUI>();
            
            // Inventory UI (Hidden by default?)
            GameObject invGO = new GameObject("InventoryPanel");
            invGO.transform.SetParent(canvasGO.transform, false);
            Image invBg = invGO.AddComponent<Image>();
            invBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            RectTransform invRT = invGO.GetComponent<RectTransform>();
            invRT.sizeDelta = new Vector2(400, 300);
            
            // Grid
            GameObject gridGO = new GameObject("Grid");
            gridGO.transform.SetParent(invGO.transform, false);
            GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(50, 50);
            RectTransform gridRT = gridGO.GetComponent<RectTransform>();
            gridRT.anchorMin = Vector2.zero;
            gridRT.anchorMax = Vector2.one;
            gridRT.offsetMin = new Vector2(10, 50); // Space for button
            gridRT.offsetMax = new Vector2(-10, -10);
            
            // Equip Button
            GameObject eqBtnGO = new GameObject("EquipButton");
            eqBtnGO.transform.SetParent(invGO.transform, false);
            Image eqBtnImg = eqBtnGO.AddComponent<Image>();
            eqBtnImg.color = Color.cyan;
            Button eqBtn = eqBtnGO.AddComponent<Button>();
            RectTransform eqBtnRT = eqBtnGO.GetComponent<RectTransform>();
            eqBtnRT.anchoredPosition = new Vector2(0, -120); // Bottom
            eqBtnRT.sizeDelta = new Vector2(100, 30);
            CreateText("Equip", 0, eqBtnGO.transform, 18, Color.black);

            InventoryUI invUI = invGO.AddComponent<InventoryUI>();
            invUI.GridRoot = gridGO.transform;
            // Create a simple prefab for slots on the fly or resource load
            // For now, let's make a rudimentary prefab to assign?
            // Actually, we can't create assets easily here without saving them.
            // Setup should just assign the button, and Instance handles prefab logic if null?
            // InventoryUI script assumes SlotPrefab. Let's create a placeholder method in InventoryUI to generate it if missing.
            
            // --- SHOP BUTTON ---
            GameObject shopBtnGO = new GameObject("OpenShopButton");
            shopBtnGO.transform.SetParent(canvasGO.transform, false);
            Image shopBtnImg = shopBtnGO.AddComponent<Image>();
            shopBtnImg.color = new Color(1f, 0.5f, 0f); // Orange
            Button shopBtn = shopBtnGO.AddComponent<Button>();
            RectTransform shopBtnRT = shopBtnGO.GetComponent<RectTransform>();
            shopBtnRT.anchorMin = new Vector2(0.5f, 1f); // Top Center
            shopBtnRT.anchorMax = new Vector2(0.5f, 1f);
            shopBtnRT.pivot = new Vector2(0.5f, 1f);
            shopBtnRT.anchoredPosition = new Vector2(100, -10); // Next to something?
            shopBtnRT.sizeDelta = new Vector2(100, 40);
            CreateText("SHOP", 0, shopBtnGO.transform, 20, Color.black);
            
            shopBtn.onClick.AddListener(() => {
                ShopUI sUI = canvasGO.GetComponent<ShopUI>();
                if (sUI != null) sUI.Show();
            });

            // Call Helper for Shop Panel Setup (defined below)
            SetupShopUI(canvasGO);
            SetupRoomUI(canvasGO);
            SetupMailUI(canvasGO);
            SetupProfileUI(canvasGO);
            SetupCoupleUI(canvasGO);

            // --- RANKING LIST UI (Left Side) ---
            GameObject rankGO = new GameObject("RankingPanel");
            rankGO.transform.SetParent(canvasGO.transform, false);
            Image rBg = rankGO.AddComponent<Image>();
            rBg.color = new Color(0.1f, 0.1f, 0.3f, 0.9f);
            RectTransform rRT = rankGO.GetComponent<RectTransform>();
            rRT.sizeDelta = new Vector2(250, 400);
            rRT.anchoredPosition = new Vector2(-300, 0); // Left

            // Header
            CreateText("Top Players", 170, rankGO.transform, 24);

            // Scroll View
            GameObject rScroll = new GameObject("RankingScroll");
            rScroll.transform.SetParent(rankGO.transform, false);
            ScrollRect rSR = rScroll.AddComponent<ScrollRect>();
            RectTransform rScrollRT = rScroll.GetComponent<RectTransform>();
            rScrollRT.anchorMin = new Vector2(0.05f, 0.05f);
            rScrollRT.anchorMax = new Vector2(0.95f, 0.85f);
            rScrollRT.offsetMin = Vector2.zero;
            rScrollRT.offsetMax = Vector2.zero;
            
            GameObject rContent = new GameObject("Content");
            rContent.transform.SetParent(rScroll.transform, false);
            VerticalLayoutGroup rVLG = rContent.AddComponent<VerticalLayoutGroup>();
            rVLG.childControlHeight = true;
            rVLG.childForceExpandHeight = false;
            rVLG.spacing = 2;
            ContentSizeFitter rCSF = rContent.AddComponent<ContentSizeFitter>();
            rCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform rContentRT = rContent.GetComponent<RectTransform>();
            rContentRT.anchorMin = new Vector2(0, 1);
            rContentRT.anchorMax = new Vector2(1, 1);
            rContentRT.pivot = new Vector2(0.5f, 1);
            rSR.content = rContentRT;

            // Template
            GameObject rItem = new GameObject("RankingItemTemplate");
            rItem.transform.SetParent(rContent.transform, false);
            Text rItemTxt = rItem.AddComponent<Text>(); 
            rItemTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rItemTxt.text = "Rank 1";
            rItemTxt.color = Color.white;
            rItemTxt.fontSize = 14;
            rItem.SetActive(false);

            // Wire to LobbyUI
            LobbyUI lobbyUI = canvasGO.GetComponent<LobbyUI>();
            lobbyUI.RankingListContainer = rContent.transform;
            lobbyUI.RankingEntryPrefab = rItem;

            // --- LOCAL STATS TEXT ---
            GameObject statsGO = new GameObject("LocalStatsText");
            statsGO.transform.SetParent(canvasGO.transform, false);
            Text statsTxt = statsGO.AddComponent<Text>();
            statsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsTxt.text = "Waiting for stats...";
            statsTxt.alignment = TextAnchor.MiddleCenter;
            statsTxt.color = Color.yellow;
            statsTxt.fontSize = 20;

            RectTransform statsRT = statsGO.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0, 1);
            statsRT.anchorMax = new Vector2(1, 1);
            statsRT.pivot = new Vector2(0.5f, 1);
            statsRT.anchoredPosition = new Vector2(0, -60); // Below Header? Or Top
            statsRT.sizeDelta = new Vector2(0, 40);

            lobbyUI.LocalStatsText = statsTxt;

            // --- FRIEND LIST UI ---
            GameObject friendsGO = new GameObject("FriendListPanel");
            friendsGO.transform.SetParent(canvasGO.transform, false);
            Image fBg = friendsGO.AddComponent<Image>();
            fBg.color = new Color(0.1f, 0.1f, 0.3f, 0.95f);
            RectTransform fRT = friendsGO.GetComponent<RectTransform>();
            fRT.sizeDelta = new Vector2(250, 400);
            fRT.anchoredPosition = new Vector2(300, 0); // To the right
            
            // Header
            CreateText("Friends", 170, friendsGO.transform, 24);
            
            // Add Input
            GameObject fInputGO = CreateInputField("AddFriendInput", 130, friendsGO.transform);
            
            // Add Button
            GameObject fAddBtnGO = new GameObject("AddBtn");
            fAddBtnGO.transform.SetParent(friendsGO.transform, false);
            Image fAddImg = fAddBtnGO.AddComponent<Image>();
            fAddImg.color = Color.green;
            Button fAddBtn = fAddBtnGO.AddComponent<Button>();
            RectTransform fAddRT = fAddBtnGO.GetComponent<RectTransform>();
            fAddRT.anchoredPosition = new Vector2(80, 130);
            fAddRT.sizeDelta = new Vector2(40, 30);
            CreateText("+", 0, fAddBtnGO.transform, 20, Color.black);
            
            // Scroll View
            GameObject fScroll = new GameObject("FriendScroll");
            fScroll.transform.SetParent(friendsGO.transform, false);
            ScrollRect fSR = fScroll.AddComponent<ScrollRect>();
            RectTransform fScrollRT = fScroll.GetComponent<RectTransform>();
            fScrollRT.anchorMin = new Vector2(0.05f, 0.05f);
            fScrollRT.anchorMax = new Vector2(0.95f, 0.75f); // Below header/input
            fScrollRT.offsetMin = Vector2.zero;
            fScrollRT.offsetMax = Vector2.zero;
            
            GameObject fContent = new GameObject("Content");
            fContent.transform.SetParent(fScroll.transform, false);
            VerticalLayoutGroup fVLG = fContent.AddComponent<VerticalLayoutGroup>();
            fVLG.childControlHeight = true;
            fVLG.childForceExpandHeight = false;
            fVLG.spacing = 2;
            ContentSizeFitter fCSF = fContent.AddComponent<ContentSizeFitter>();
            fCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform fContentRT = fContent.GetComponent<RectTransform>();
            fContentRT.anchorMin = new Vector2(0, 1);
            fContentRT.anchorMax = new Vector2(1, 1);
            fContentRT.pivot = new Vector2(0.5f, 1);
            fSR.content = fContentRT;

            // Friend Item Template
            GameObject fItem = new GameObject("FriendItemTemplate");
            fItem.transform.SetParent(fContent.transform, false);
            Image fItemImg = fItem.AddComponent<Image>();
            fItemImg.color = new Color(1,1,1,0.1f);
            LayoutElement fItemLE = fItem.AddComponent<LayoutElement>();
            fItemLE.minHeight = 30;
            
            GameObject fNameObj = new GameObject("Name");
            fNameObj.transform.SetParent(fItem.transform, false);
            Text fNameTxt = fNameObj.AddComponent<Text>(); // Legacy Text for prototype simplicity or switch to TMP
            fNameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // or Arial
            fNameTxt.text = "Friend Name";
            fNameTxt.alignment = TextAnchor.MiddleLeft;
            fNameTxt.color = Color.white;
            RectTransform fNameRT = fNameObj.GetComponent<RectTransform>();
            fNameRT.anchorMin = Vector2.zero;
            fNameRT.anchorMax = Vector2.one;
            fNameRT.offsetMin = new Vector2(5,0);
            fNameRT.offsetMax = new Vector2(-40,0);
            
            GameObject fRemBtnObj = new GameObject("RemoveBtn");
            fRemBtnObj.transform.SetParent(fItem.transform, false);
            Image fRemImg = fRemBtnObj.AddComponent<Image>();
            fRemImg.color = Color.red;
            Button fRemBtn = fRemBtnObj.AddComponent<Button>();
            RectTransform fRemRT = fRemBtnObj.GetComponent<RectTransform>();
            fRemRT.anchorMin = new Vector2(1, 0.5f);
            fRemRT.anchorMax = new Vector2(1, 0.5f);
            fRemRT.pivot = new Vector2(1, 0.5f);
            fRemRT.sizeDelta = new Vector2(30, 20);
            fRemRT.anchoredPosition = new Vector2(-5, 0);

            GameObject fInspBtnObj = new GameObject("InspBtn");
            fInspBtnObj.transform.SetParent(fItem.transform, false);
            Image fInspImg = fInspBtnObj.AddComponent<Image>();
            fInspImg.color = Color.blue;
            Button fInspBtn = fInspBtnObj.AddComponent<Button>();
            RectTransform fInspRT = fInspBtnObj.GetComponent<RectTransform>();
            fInspRT.anchorMin = new Vector2(1, 0.5f);
            fInspRT.anchorMax = new Vector2(1, 0.5f);
            fInspRT.pivot = new Vector2(1, 0.5f);
            fInspRT.sizeDelta = new Vector2(30, 20);
            fInspRT.anchoredPosition = new Vector2(-40, 0); // Next to Remove
            
            // Wire Component
            FriendListUI fUI = friendsGO.AddComponent<FriendListUI>();
            fUI.Panel = friendsGO;
            fUI.ContentRoot = fContent.transform;
            fUI.FriendItemPrefab = fItem; // Template as prefab
            fUI.AddFriendInput = fInputGO.GetComponent<TMP_InputField>();
            // Ah, CreateInputField uses TMP_InputField. FriendListUI defined InputField (Legacy).
            // Need to update FriendListUI or cast? 
            // Better to update FriendListUI to use TMP_InputField.
            // For now, let's just make FriendListUI compatible or assume I change CreateInputField logic.
            // Wait, CreateInputField returns TMP_InputField. 
            // I should verify FriendListUI definition.
            
            fUI.AddButton = fAddBtn;
            
            fItem.SetActive(false); // Hide template
            
            // Friend Button in Lobby
            GameObject fBtnGO = new GameObject("FriendButton");
            fBtnGO.transform.SetParent(canvasGO.transform, false);
            Image fBtnImg = fBtnGO.AddComponent<Image>();
            fBtnImg.color = new Color(0.2f, 0.8f, 0.2f);
            Button fBtn = fBtnGO.AddComponent<Button>();
            RectTransform fBtnRT = fBtnGO.GetComponent<RectTransform>();
            fBtnRT.anchorMin = new Vector2(0.5f, 1f);
            fBtnRT.anchorMax = new Vector2(0.5f, 1f);
            fBtnRT.pivot = new Vector2(0.5f, 1f);
            fBtnRT.anchoredPosition = new Vector2(220, -10); // Right of Shop
            fBtnRT.sizeDelta = new Vector2(100, 40);
            CreateText("SOCIAL", 0, fBtnGO.transform, 20, Color.black);
            
            fBtn.onClick.AddListener(() => {
                fUI.Toggle();
            });

            // Chat UI
            GameObject chatGO = new GameObject("ChatPanel");
            chatGO.transform.SetParent(canvasGO.transform, false);
            Image chatBg = chatGO.AddComponent<Image>();
            chatBg.color = new Color(0, 0, 0, 0.5f);
            RectTransform chatRT = chatGO.GetComponent<RectTransform>();
            chatRT.anchorMin = new Vector2(0, 0);
            chatRT.anchorMax = new Vector2(0.3f, 0.4f); // Bottom Left Corner
            chatRT.pivot = new Vector2(0, 0);
            chatRT.anchoredPosition = new Vector2(10, 10);
            
            // Scroll View (Simplified)
            GameObject scrollGO = new GameObject("Scroll View");
            scrollGO.transform.SetParent(chatGO.transform, false);
            ScrollRect sr = scrollGO.AddComponent<ScrollRect>();
            RectTransform srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = Vector2.zero;
            srRT.anchorMax = Vector2.one;
            srRT.offsetMin = new Vector2(0, 30); // Space for input
            srRT.offsetMax = Vector2.zero;
            
            // Content
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            sr.content = contentRT;
            
            // Input
            GameObject chatInputGO = CreateInputField("ChatInput", 15, chatGO.transform); // Y offset checks
            RectTransform ciRT = chatInputGO.GetComponent<RectTransform>();
            ciRT.anchorMin = new Vector2(0, 0);
            ciRT.anchorMax = new Vector2(1, 0);
            ciRT.pivot = new Vector2(0.5f, 0);
            ciRT.anchoredPosition = new Vector2(0, 0);
            ciRT.sizeDelta = new Vector2(0, 30);
            
            // Message Prefab (On the fly)
            // Just use a Text object template for instantiation?
            // ChatUI script needs it assigned.
            // Let's create a template object, disable it, and assign it as prefab.
            GameObject msgTemplate = new GameObject("MessageTemplate");
            msgTemplate.transform.SetParent(contentGO.transform, false);
            TextMeshProUGUI msgTxt = msgTemplate.AddComponent<TextMeshProUGUI>();
            msgTxt.fontSize = 14;
            msgTxt.color = Color.white;
            msgTemplate.SetActive(false); // Hide template

            ChatUI chatUI = chatGO.AddComponent<ChatUI>();
            chatUI.ChatInput = chatInputGO.GetComponent<TMP_InputField>();
            chatUI.MessageContainer = contentGO.transform;
            chatUI.MessagePrefab = msgTemplate;
            chatUI.ScrollView = sr;
            
            invUI.EquipButton = eqBtn;
            
            // Slot Prefab Mock (Internal logic in UI script would be better, but we need to assign it)
            // Let's rely on InventoryUI having a fallback or we make a prefab asset.
            // Skipping Prefab assignment, User/Runtime will handle or we add logic in InventoryUI Start().

            SetupLocalCharacter();

            string path = "Assets/Scenes/Game.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        }

        private static void SetupLocalCharacter()
        {
            GameObject charGO = new GameObject("LocalCharacter");
            charGO.transform.position = Vector3.zero;
            charGO.transform.rotation = Quaternion.identity;
            
            charGO.AddComponent<CharacterVisuals>();
            var anim = charGO.AddComponent<CharacterAnimator>();
            anim.IsLocalPlayer = true;
            charGO.AddComponent<PlayerVisualController>();
        }
        
        private static void SetupBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Login.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static GameObject CreateInputField(string name, float y, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            
            Image img = root.AddComponent<Image>();
            img.color = Color.white;
            
            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.targetGraphic = img;
            
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 28); // Minimal for tiny window
            rt.anchoredPosition = new Vector2(0, y);
            
            // Text Area
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.color = Color.black;
            text.fontSize = 11;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(5, 0, 5, 0);
            
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(5, 0);
            textRT.offsetMax = new Vector2(-5, 0);
            
            input.textViewport = textRT;
            input.textComponent = text;
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = name;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholder.fontSize = 11;
            placeholder.alignment = TextAlignmentOptions.Left;
            placeholder.margin = new Vector4(5, 0, 5, 0);
            
            RectTransform phRT = placeholderObj.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(5, 0);
            phRT.offsetMax = new Vector2(-5, 0);
            
            input.placeholder = placeholder;
            
            return root;
        }
        
        // XML-to-Unity UI Coordinate Conversion Helpers
        // Original game uses top-left origin (0,0), Unity uses anchored positioning
        private static GameObject CreateInputFieldAtPosition(string name, float xmlX, float xmlY, float width, float height, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            
            Image img = root.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f); // More opaque white
            img.raycastTarget = true; // Ensure it can receive clicks
            
            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.targetGraphic = img;
            
            // Convert from XML top-left coordinates to Unity
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); // Top-left anchor
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(xmlX, -xmlY); // Y inverted
            // Make input fields taller for better clickability
            rt.sizeDelta = new Vector2(width, Mathf.Max(height, 25)); // Minimum 25px height
            
            // Text Area
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.color = Color.black;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(8, 0, 8, 0);
            
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 0);
            textRT.offsetMax = new Vector2(-8, 0);
            
            input.textViewport = textRT;
            input.textComponent = text;
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = name;
            placeholder.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            placeholder.fontSize = 14;
            placeholder.alignment = TextAlignmentOptions.Left;
            placeholder.margin = new Vector4(8, 0, 8, 0);
            
            RectTransform phRT = placeholderObj.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(8, 0);
            phRT.offsetMax = new Vector2(-8, 0);
            
            input.placeholder = placeholder;
            
            return root;
        }
        
        private static GameObject CreateButtonAtPosition(string name, float xmlX, float xmlY, float width, float height, Transform parent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            
            Button btn = btnObj.AddComponent<Button>();
            
            // Convert from XML top-left coordinates to Unity
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1); // Top-left anchor
            btnRT.anchorMax = new Vector2(0, 1);
            btnRT.pivot = new Vector2(0, 1);
            btnRT.anchoredPosition = new Vector2(xmlX, -xmlY); // Y inverted
            btnRT.sizeDelta = new Vector2(width, height);
            
            return btnObj;
        }
        
        // Transparent UI overlay helpers - these create invisible UI elements
        // that overlay on top of the background image's painted UI
        private static GameObject CreateTransparentInputField(string name, float xmlX, float xmlY, float width, float height, Transform parent)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            
            Image img = root.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.1f); // Almost transparent - just visible enough to see where it is
            img.raycastTarget = true;
            
            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.targetGraphic = img;
            
            // Convert from XML top-left coordinates to Unity
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); // Top-left anchor
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(xmlX, -xmlY); // Y inverted
            rt.sizeDelta = new Vector2(width, height);
            
            // Text Area
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(root.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.color = Color.white; // White text for visibility
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(5, 0, 5, 0);
            
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(5, 0);
            textRT.offsetMax = new Vector2(-5, 0);
            
            input.textViewport = textRT;
            input.textComponent = text;
            
            // No placeholder - background image shows the labels
            
            return root;
        }
        
        private static GameObject CreateTransparentButton(string name, float xmlX, float xmlY, float width, float height, Transform parent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.01f); // Almost completely transparent
            btnImg.raycastTarget = true; // But still clickable
            
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            
            // Convert from XML top-left coordinates to Unity
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1); // Top-left anchor
            btnRT.anchorMax = new Vector2(0, 1);
            btnRT.pivot = new Vector2(0, 1);
            btnRT.anchoredPosition = new Vector2(xmlX, -xmlY); // Y inverted
            btnRT.sizeDelta = new Vector2(width, height);
            
            return btnObj;
        }
        
        private static void CreateText(string content, float y, Transform parent, float size, Color? c = null)
        {
            GameObject obj = new GameObject("Label_" + content);
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = size;
            txt.color = c ?? Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(300, 100);
        }

        private static void SetupShopUI(GameObject canvasGO)
        {
            GameObject shopGO = new GameObject("ShopPanel");
            shopGO.transform.SetParent(canvasGO.transform, false);
            Image bg = shopGO.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            
            RectTransform rt = shopGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.1f);
            rt.anchorMax = new Vector2(0.9f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            CreateText("Item Shop", 250, shopGO.transform, 36);

            // Grid
            GameObject scroll = new GameObject("ShopScroll");
            scroll.transform.SetParent(shopGO.transform, false);
            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            RectTransform scrollRT = scroll.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.05f, 0.1f);
            scrollRT.anchorMax = new Vector2(0.6f, 0.8f);
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scroll.transform, false);
            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 120);
            grid.spacing = new Vector2(10, 10);
            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            sr.content = contentRT;

            // Details Panel (Right side)
            GameObject details = new GameObject("DetailsPanel");
            details.transform.SetParent(shopGO.transform, false);
            Image dBg = details.AddComponent<Image>();
            dBg.color = new Color(0,0,0,0.3f);
            RectTransform dRT = details.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0.65f, 0.1f);
            dRT.anchorMax = new Vector2(0.95f, 0.8f);
            dRT.offsetMin = Vector2.zero;
            dRT.offsetMax = Vector2.zero;

            CreateText("Select Item", 100, details.transform, 24);

            // Buy Button
            GameObject buyBtnGO = new GameObject("BuyButton");
            buyBtnGO.transform.SetParent(details.transform, false);
            Image bImg = buyBtnGO.AddComponent<Image>();
            bImg.color = Color.yellow;
            Button buyBtn = buyBtnGO.AddComponent<Button>();
            RectTransform buyRT = buyBtnGO.GetComponent<RectTransform>();
            buyRT.anchorMin = new Vector2(0.5f, 0.1f);
            buyRT.anchorMax = new Vector2(0.5f, 0.1f);
            buyRT.anchoredPosition = new Vector2(0, 20);
            buyRT.sizeDelta = new Vector2(120, 40);
            CreateText("BUY", 0, buyBtnGO.transform, 20, Color.black);

            // Close Button
            GameObject closeBtnGO = new GameObject("CloseShop");
            closeBtnGO.transform.SetParent(shopGO.transform, false);
            Image cImg = closeBtnGO.AddComponent<Image>();
            cImg.color = Color.red;
            Button closeBtn = closeBtnGO.AddComponent<Button>();
            RectTransform cRT = closeBtnGO.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(1, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.anchoredPosition = new Vector2(-20, -20);
            cRT.sizeDelta = new Vector2(30, 30);
            CreateText("X", 0, closeBtnGO.transform, 20, Color.white);

            closeBtn.onClick.AddListener(() => shopGO.SetActive(false));

            ShopUI ui = canvasGO.AddComponent<ShopUI>();
            ui.Panel = shopGO;
            ui.ItemsGrid = content.transform;
            ui.BuyButton = buyBtn;
            ui.ItemNameText = details.GetComponentInChildren<TextMeshProUGUI>(); // Assuming CreateText adds TMP
            ui.ItemPriceText = null; // Need to create specific texts for Name/Price/Type
            
            // Create specific labels in Details
            GameObject nameLbl = new GameObject("NameLabel");
            nameLbl.transform.SetParent(details.transform, false);
            var nt = nameLbl.AddComponent<TextMeshProUGUI>();
            nt.text = "Item Name";
            nt.alignment = TextAlignmentOptions.Center;
            ((RectTransform)nameLbl.transform).anchoredPosition = new Vector2(0, 100);
            ui.ItemNameText = nt;

            GameObject priceLbl = new GameObject("PriceLabel");
            priceLbl.transform.SetParent(details.transform, false);
            var pt = priceLbl.AddComponent<TextMeshProUGUI>();
            pt.text = "Price: 0";
            pt.alignment = TextAlignmentOptions.Center;
            ((RectTransform)priceLbl.transform).anchoredPosition = new Vector2(0, 50);
            ui.ItemPriceText = pt;

            shopGO.SetActive(false);
        }

        private static void SetupRoomUI(GameObject canvasGO)
        {
            GameObject panel = new GameObject("RoomPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.2f, 0.98f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            RoomUI ui = panel.AddComponent<RoomUI>();
            
            // Header
            CreateText("Room Name", 350, panel.transform, 40);
            ui.RoomNameText = panel.transform.Find("Label_Room Name").GetComponent<TextMeshProUGUI>();
            
            // Info Text (Mode display for non-hosts)
            CreateText("Mode: Normal", 300, panel.transform, 24);
            ui.ModeText = panel.transform.Find("Label_Mode: Normal").GetComponent<TextMeshProUGUI>();

            // Settings (Right Side)
            float rightX = 250;
            GameObject modeDD = CreateDropdown("GameMode", 200, panel.transform);
            ((RectTransform)modeDD.transform).anchoredPosition = new Vector2(rightX, 200);
            ui.GameModeDropdown = modeDD.GetComponent<TMP_Dropdown>();
            
            GameObject diffDD = CreateDropdown("Difficulty", 150, panel.transform);
            ((RectTransform)diffDD.transform).anchoredPosition = new Vector2(rightX, 150);
            ui.DifficultyDropdown = diffDD.GetComponent<TMP_Dropdown>();
            
            GameObject stageInp = CreateInputField("StageID", 100, panel.transform);
            ((RectTransform)stageInp.transform).anchoredPosition = new Vector2(rightX, 100);
            ui.StageIdInput = stageInp.GetComponent<TMP_InputField>();
            
            GameObject musicInp = CreateInputField("MusicID", 50, panel.transform);
            ((RectTransform)musicInp.transform).anchoredPosition = new Vector2(rightX, 50);
            ui.MusicIdInput = musicInp.GetComponent<TMP_InputField>();

            // Slots (Left Side)
            GameObject slots = new GameObject("Slots");
            slots.transform.SetParent(panel.transform, false);
            slots.AddComponent<VerticalLayoutGroup>();
            RectTransform sRT = slots.AddComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0.5f, 0.5f);
            sRT.anchorMax = new Vector2(0.5f, 0.5f);
            sRT.sizeDelta = new Vector2(300, 300);
            sRT.anchoredPosition = new Vector2(-150, 0); // Moved Left
            
            ui.SlotContainer = slots.transform;

            // Buttons (Bottom Center)
            GameObject startBtn = CreateButton("Start", -200, panel.transform, () => {});
            ui.StartButton = startBtn.GetComponent<Button>();
            
            GameObject readyBtn = CreateButton("Ready", -250, panel.transform, () => {});
            ui.ReadyButton = readyBtn.GetComponent<Button>();
            ui.ReadyButtonText = readyBtn.GetComponentInChildren<TextMeshProUGUI>();
            
            GameObject leaveBtn = CreateButton("Leave", -300, panel.transform, () => {});
            ui.LeaveButton = leaveBtn.GetComponent<Button>();
            
            panel.SetActive(false); // Hidden by default
        }

        private static void SetupScoreUI(GameObject canvasGO)
        {
            GameObject panel = new GameObject("ScorePanel");
            panel.transform.SetParent(canvasGO.transform, false);
            
            RectTransform pRT = panel.AddComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero;
            pRT.anchorMax = Vector2.one;
            pRT.offsetMin = Vector2.zero;
            pRT.offsetMax = Vector2.zero;
            
            ScoreUI ui = panel.AddComponent<ScoreUI>();
            
            // Score (Top Right)
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(panel.transform, false);
            var st = scoreObj.AddComponent<TextMeshProUGUI>();
            st.fontSize = 40;
            st.color = Color.white;
            st.alignment = TextAlignmentOptions.Right;
            st.text = "Score: 0";
            
            RectTransform sRT = scoreObj.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(1, 1);
            sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(1, 1);
            sRT.anchoredPosition = new Vector2(-20, -20);
            sRT.sizeDelta = new Vector2(300, 50);
            ui.ScoreText = st;
            
            // Combo (Center)
            GameObject comboObj = new GameObject("ComboText");
            comboObj.transform.SetParent(panel.transform, false);
            var ct = comboObj.AddComponent<TextMeshProUGUI>();
            ct.fontSize = 60;
            ct.color = Color.cyan;
            ct.alignment = TextAlignmentOptions.Center;
            ct.text = "";
            
            RectTransform cRT = comboObj.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.5f, 0.5f);
            cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.anchoredPosition = new Vector2(0, 50);
            cRT.sizeDelta = new Vector2(400, 100);
            ui.ComboText = ct;
            
            // Judgment (Center, below Combo)
            GameObject judgeObj = new GameObject("JudgmentText");
            judgeObj.transform.SetParent(panel.transform, false);
            var jt = judgeObj.AddComponent<TextMeshProUGUI>();
            jt.fontSize = 80;
            jt.color = Color.green;
            jt.alignment = TextAlignmentOptions.Center;
            jt.text = "";
            
            RectTransform jRT = judgeObj.GetComponent<RectTransform>();
            jRT.anchorMin = new Vector2(0.5f, 0.5f);
            jRT.anchorMax = new Vector2(0.5f, 0.5f);
            jRT.anchoredPosition = new Vector2(0, -20);
            jRT.sizeDelta = new Vector2(400, 100);
            ui.JudgmentText = jt;
            
            panel.SetActive(false);
        }

        private static void SetupMailUI(GameObject canvasGO)
        {
            GameObject mailGO = new GameObject("MailPanel");
            mailGO.transform.SetParent(canvasGO.transform, false);
            Image bg = mailGO.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.4f, 0.95f);
            
            RectTransform rt = mailGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 400);
            
            CreateText("Mailbox", 180, mailGO.transform, 32);
            
            // Scroll View
            GameObject scroll = new GameObject("MailScroll");
            scroll.transform.SetParent(mailGO.transform, false);
            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            RectTransform sRT = scroll.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0.05f, 0.05f);
            sRT.anchorMax = new Vector2(0.95f, 0.8f);
            sRT.offsetMin = Vector2.zero;
            sRT.offsetMax = Vector2.zero;
            
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scroll.transform, false);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            sr.content = cRT;
            
            // Mail Item Template
            GameObject itemTempl = new GameObject("MailItemTemplate");
            itemTempl.transform.SetParent(content.transform, false);
            Image itemImg = itemTempl.AddComponent<Image>();
            itemImg.color = new Color(1,1,1,0.1f);
            LayoutElement le = itemTempl.AddComponent<LayoutElement>();
            le.minHeight = 40;
            Button itemBtn = itemTempl.AddComponent<Button>();
            
            GameObject subjObj = new GameObject("Subject");
            subjObj.transform.SetParent(itemTempl.transform, false);
            TextMeshProUGUI subjTxt = subjObj.AddComponent<TextMeshProUGUI>();
            subjTxt.fontSize = 18;
            subjTxt.color = Color.white;
            subjTxt.alignment = TextAlignmentOptions.Left;
            ((RectTransform)subjObj.transform).anchorMin = Vector2.zero;
            ((RectTransform)subjObj.transform).anchorMax = Vector2.one;
            ((RectTransform)subjObj.transform).offsetMin = new Vector2(10, 0);
            ((RectTransform)subjObj.transform).offsetMax = new Vector2(-100, 0);
            
            GameObject senderObj = new GameObject("Sender");
            senderObj.transform.SetParent(itemTempl.transform, false);
            TextMeshProUGUI senderTxt = senderObj.AddComponent<TextMeshProUGUI>();
            senderTxt.fontSize = 14;
            senderTxt.color = Color.yellow;
            senderTxt.alignment = TextAlignmentOptions.Right;
            ((RectTransform)senderObj.transform).anchorMin = new Vector2(1, 0);
            ((RectTransform)senderObj.transform).anchorMax = new Vector2(1, 1);
            ((RectTransform)senderObj.transform).offsetMin = new Vector2(-100, 0);
            ((RectTransform)senderObj.transform).offsetMax = new Vector2(-10, 0);
            
            // Delete Button on template?
            GameObject delBtnObj = new GameObject("DeleteBtn");
            delBtnObj.transform.SetParent(itemTempl.transform, false);
            Image delImg = delBtnObj.AddComponent<Image>();
            delImg.color = Color.red;
            Button delBtn = delBtnObj.AddComponent<Button>();
            ((RectTransform)delBtnObj.transform).anchorMin = new Vector2(1, 0.5f);
            ((RectTransform)delBtnObj.transform).anchorMax = new Vector2(1, 0.5f);
            ((RectTransform)delBtnObj.transform).anchoredPosition = new Vector2(25, 0); // Outside right? No inside.
            ((RectTransform)delBtnObj.transform).anchoredPosition = new Vector2(-120, 0);
            ((RectTransform)delBtnObj.transform).sizeDelta = new Vector2(20, 20);

            itemTempl.SetActive(false);
            
            // Compose Panel
            GameObject compGO = new GameObject("ComposePanel");
            compGO.transform.SetParent(mailGO.transform, false);
            Image cBg = compGO.AddComponent<Image>();
            cBg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            ((RectTransform)compGO.transform).anchorMin = Vector2.zero;
            ((RectTransform)compGO.transform).anchorMax = Vector2.one;
            ((RectTransform)compGO.transform).offsetMin = Vector2.zero;
            ((RectTransform)compGO.transform).offsetMax = Vector2.zero;
            
            GameObject tInp = CreateInputField("TargetInput", 120, compGO.transform);
            GameObject sInp = CreateInputField("SubjectInput", 80, compGO.transform);
            GameObject bInp = CreateInputField("BodyInput", -20, compGO.transform);
            ((RectTransform)bInp.transform).sizeDelta = new Vector2(300, 150);
            
            GameObject sendBtnGO = CreateButton("Send", -150, compGO.transform, () => {});
            
            GameObject closeComp = CreateButton("X", 0, compGO.transform, () => compGO.SetActive(false));
            ((RectTransform)closeComp.transform).anchoredPosition = new Vector2(230, 180);
            ((RectTransform)closeComp.transform).sizeDelta = new Vector2(30, 30);
            
            compGO.SetActive(false);
            
            // Read Panel
            GameObject readGO = new GameObject("ReadPanel");
            readGO.transform.SetParent(mailGO.transform, false);
            Image rBg = readGO.AddComponent<Image>();
            rBg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            ((RectTransform)readGO.transform).anchorMin = Vector2.zero;
            ((RectTransform)readGO.transform).anchorMax = Vector2.one;
            ((RectTransform)readGO.transform).offsetMin = Vector2.zero;
            ((RectTransform)readGO.transform).offsetMax = Vector2.zero;
            
            CreateText("Message", 180, readGO.transform, 24);
            GameObject rSubj = new GameObject("ReadSubject"); rSubj.transform.SetParent(readGO.transform, false);
            GameObject rContent = new GameObject("ReadContent"); rContent.transform.SetParent(readGO.transform, false);
            GameObject rSender = new GameObject("ReadSender"); rSender.transform.SetParent(readGO.transform, false);
            
            TextMeshProUGUI rSubjTxt = rSubj.AddComponent<TextMeshProUGUI>(); rSubjTxt.fontSize=20; rSubjTxt.color=Color.white;
            TextMeshProUGUI rContentTxt = rContent.AddComponent<TextMeshProUGUI>(); rContentTxt.fontSize=16; rContentTxt.color=Color.white;
            TextMeshProUGUI rSenderTxt = rSender.AddComponent<TextMeshProUGUI>(); rSenderTxt.fontSize=14; rSenderTxt.color=Color.yellow;
            
            ((RectTransform)rSubj.transform).anchoredPosition = new Vector2(0, 140);
            ((RectTransform)rContent.transform).anchoredPosition = new Vector2(0, 0); ((RectTransform)rContent.transform).sizeDelta = new Vector2(400, 200);
            ((RectTransform)rSender.transform).anchoredPosition = new Vector2(0, 110);
            
            GameObject closeRead = CreateButton("Close", -170, readGO.transform, () => readGO.SetActive(false));
            readGO.SetActive(false);

            // Tools (Cancel/Compose)
            GameObject composeBtn = CreateButton("Compose", -180, mailGO.transform, () => compGO.SetActive(true));
            ((RectTransform)composeBtn.transform).anchoredPosition = new Vector2(-100, -180);
            
            GameObject closeMail = CreateButton("Close", -180, mailGO.transform, () => mailGO.SetActive(false));
            ((RectTransform)closeMail.transform).anchoredPosition = new Vector2(100, -180);

            // Wire UI
            MailUI mUI = mailGO.AddComponent<MailUI>();
            mUI.MailPanel = mailGO;
            mUI.MailListContainer = content.transform;
            mUI.MailItemPrefab = itemTempl;
            mUI.ComposePanel = compGO;
            mUI.TargetInput = tInp.GetComponent<TMP_InputField>();
            mUI.SubjectInput = sInp.GetComponent<TMP_InputField>();
            mUI.ContentInput = bInp.GetComponent<TMP_InputField>();
            mUI.ReadPanel = readGO;
            mUI.ReadSubject = rSubjTxt;
            mUI.ReadContent = rContentTxt;
            mUI.ReadSender = rSenderTxt;
            
            // Wire Send Button in Compose
            sendBtnGO.GetComponent<Button>().onClick.AddListener(() => mUI.SendMail());

            mailGO.SetActive(false);
            
            // Add Mail Button to Lobby
            GameObject mailBtnGO = new GameObject("MailButton");
            mailBtnGO.transform.SetParent(canvasGO.transform, false);
            Image mbImg = mailBtnGO.AddComponent<Image>();
            mbImg.color = Color.magenta;
            Button mb = mailBtnGO.AddComponent<Button>();
            RectTransform mbRT = mailBtnGO.GetComponent<RectTransform>();
            mbRT.anchorMin = new Vector2(0.5f, 1f);
            mbRT.anchorMax = new Vector2(0.5f, 1f);
            mbRT.pivot = new Vector2(0.5f, 1f);
            mbRT.anchoredPosition = new Vector2(340, -10); // Right of Friend
            mbRT.sizeDelta = new Vector2(100, 40);
            CreateText("MAIL", 0, mailBtnGO.transform, 20, Color.white);
            
            mb.onClick.AddListener(() => mUI.ToggleMail());
        }

        private static void SetupResultUI(GameObject canvasGO)
        {
            GameObject panel = new GameObject("ResultPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            
            // BG
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            RectTransform pRT = panel.GetComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero;
            pRT.anchorMax = Vector2.one;
            pRT.offsetMin = Vector2.zero;
            pRT.offsetMax = Vector2.zero;
            
            ResultUI ui = panel.AddComponent<ResultUI>();
            ui.ContentPanel = panel;
            
            // Title
            CreateText("GAME CLEAR!", 100, panel.transform, 60);
            ui.TitleText = panel.transform.Find("Label_GAME CLEAR!").GetComponent<TextMeshProUGUI>();
            
            // Score
            CreateText("Score: 0", 0, panel.transform, 40);
            ui.ScoreText = panel.transform.Find("Label_Score: 0").GetComponent<TextMeshProUGUI>();
            
            // Rewards
            CreateText("Exp: +0\nPoints: +0", -100, panel.transform, 30);
            ui.RewardsText = panel.transform.Find("Label_Exp: +0\nPoints: +0").GetComponent<TextMeshProUGUI>();
            
            // Button
            GameObject btn = CreateButton("Lobby", -200, panel.transform, () => {});
            ui.LobbyButton = btn.GetComponent<Button>();
            
            panel.SetActive(false);
        }
        
        private static void SetupProfileUI(GameObject canvasGO)
        {
            GameObject panel = new GameObject("ProfilePanel");
            panel.transform.SetParent(canvasGO.transform, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.3f, 0.95f);
            
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 400);
            
            CreateText("Profile", 170, panel.transform, 32);
            
            GameObject nameObj = new GameObject("NameText"); nameObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI nt = nameObj.AddComponent<TextMeshProUGUI>(); nt.fontSize = 24; nt.color=Color.white; nt.alignment=TextAlignmentOptions.Center;
            ((RectTransform)nameObj.transform).anchoredPosition = new Vector2(0, 100);
            
            GameObject lvlObj = new GameObject("LevelText"); lvlObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI lt = lvlObj.AddComponent<TextMeshProUGUI>(); lt.fontSize = 20; lt.color=Color.yellow; lt.alignment=TextAlignmentOptions.Center;
            ((RectTransform)lvlObj.transform).anchoredPosition = new Vector2(0, 60);

            GameObject genderObj = new GameObject("GenderText"); genderObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI gt = genderObj.AddComponent<TextMeshProUGUI>(); gt.fontSize = 20; gt.color=Color.white; gt.alignment=TextAlignmentOptions.Center;
            ((RectTransform)genderObj.transform).anchoredPosition = new Vector2(0, 20);
            
            GameObject statusObj = new GameObject("StatusText"); statusObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI st = statusObj.AddComponent<TextMeshProUGUI>(); st.fontSize = 18; st.color=Color.green; st.alignment=TextAlignmentOptions.Center;
            ((RectTransform)statusObj.transform).anchoredPosition = new Vector2(0, -20);
            
            GameObject proposeBtn = CreateButton("Propose", -60, panel.transform, () => {});
            
            GameObject closeBtn = CreateButton("Close", -150, panel.transform, () => panel.SetActive(false));
            
            ProfileUI uiComp = panel.AddComponent<ProfileUI>();
            uiComp.ProfilePanel = panel;
            uiComp.ProposeButton = proposeBtn.GetComponent<Button>();
            uiComp.NameText = nt;
            uiComp.LevelText = lt;
            uiComp.GenderText = gt;
            uiComp.StatusText = st;
            uiComp.CloseButton = closeBtn.GetComponent<Button>();
            uiComp.ProposeButton = proposeBtn.GetComponent<Button>();
            
            panel.SetActive(false);
        }

        private static void SetupCoupleUI(GameObject canvasGO)
        {
            GameObject panel = new GameObject("CoupleProposalPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.1f, 0.15f, 0.95f);
            
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 250);
            
            CreateText("Proposal", 80, panel.transform, 32);
            
            GameObject msgObj = new GameObject("MessageText"); msgObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI mt = msgObj.AddComponent<TextMeshProUGUI>(); mt.fontSize = 20; mt.color=Color.white; mt.alignment=TextAlignmentOptions.Center;
            ((RectTransform)msgObj.transform).anchoredPosition = new Vector2(0, 20);
            ((RectTransform)msgObj.transform).sizeDelta = new Vector2(350, 80);
            mt.text = "Someone proposed!";

            GameObject acceptBtn = CreateButton("Accept", -50, panel.transform, () => {});
            ((RectTransform)acceptBtn.transform).anchoredPosition = new Vector2(-80, -50);
            
            GameObject rejectBtn = CreateButton("Reject", -50, panel.transform, () => {});
            ((RectTransform)rejectBtn.transform).anchoredPosition = new Vector2(80, -50);
            
            // Notification Text (Overlay on top of screen or inside panel? CoupleUI has it)
            GameObject notifObj = new GameObject("CoupleNotification");
            notifObj.transform.SetParent(canvasGO.transform, false);
            TextMeshProUGUI notifText = notifObj.AddComponent<TextMeshProUGUI>();
            notifText.fontSize = 30; notifText.color = Color.magenta; notifText.alignment = TextAlignmentOptions.Center;
            notifText.raycastTarget = false;
            ((RectTransform)notifObj.transform).anchoredPosition = new Vector2(0, 200);
            ((RectTransform)notifObj.transform).sizeDelta = new Vector2(600, 100);
            
            CoupleUI ui = panel.AddComponent<CoupleUI>();
            ui.ProposalPanel = panel;
            ui.ProposalText = mt;
            ui.AcceptButton = acceptBtn.GetComponent<Button>();
            ui.RejectButton = rejectBtn.GetComponent<Button>();
            ui.NotificationText = notifText;
            
            panel.SetActive(false);
        }

        private static GameObject CreateButton(string label, float y, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnGO = new GameObject(label + "Button");
            btnGO.transform.SetParent(parent, false);
            Image img = btnGO.AddComponent<Image>();
            img.color = Color.white;
            Button btn = btnGO.AddComponent<Button>();
            RectTransform rt = btnGO.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(160, 40);
            
            CreateText(label, 0, btnGO.transform, 20, Color.black);
            
            if (onClick != null) btn.onClick.AddListener(onClick);
            
            return btnGO;
        }
    


        private static void SetupMessengerUI(GameObject canvasGO)
        {
            GameObject panel = new GameObject("MessengerPanel");
            panel.transform.SetParent(canvasGO.transform, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.2f, 0.2f, 0.95f);
            
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 350);
            rt.anchoredPosition = new Vector2(300, 0); 
            
            CreateText("Messenger", 160, panel.transform, 32);
            
            // Log Area
            GameObject logObj = new GameObject("LogText");
            logObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI logText = logObj.AddComponent<TextMeshProUGUI>();
            logText.fontSize = 14;
            logText.alignment = TextAlignmentOptions.TopLeft;
            logText.text = "System: Messenger Ready.\n";
            RectTransform logRT = logObj.GetComponent<RectTransform>();
            logRT.anchorMin = new Vector2(0.1f, 0.3f);
            logRT.anchorMax = new Vector2(0.9f, 0.85f);
            logRT.offsetMin = Vector2.zero;
            logRT.offsetMax = Vector2.zero;
            
            // Input Fields
            GameObject targetObj = CreateInputField("TargetInput", -20, panel.transform);
            GameObject msgObj = CreateInputField("MsgInput", -60, panel.transform);
            
            targetObj.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "Target Name...";
            msgObj.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "Message...";
            
            GameObject sendBtn = CreateButton("Send", -110, panel.transform, () => {});
            GameObject closeBtn = CreateButton("X", 0, panel.transform, () => panel.SetActive(false));
            ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(180, 160);
            ((RectTransform)closeBtn.transform).sizeDelta = new Vector2(30, 30);
             
            MessengerUI ui = panel.AddComponent<MessengerUI>();
            ui.windowPanel = panel;
            ui.targetInput = targetObj.GetComponent<TMP_InputField>();
            ui.messageInput = msgObj.GetComponent<TMP_InputField>();
            ui.sendButton = sendBtn.GetComponent<Button>();
            ui.closeButton = closeBtn.GetComponent<Button>();
            ui.logText = logText;
            
            panel.SetActive(false);
        }

        private static GameObject CreateDropdown(string name, float y, Transform parent)
        {
            GameObject ddGO = new GameObject(name + "_Dropdown");
            ddGO.transform.SetParent(parent, false);
            Image img = ddGO.AddComponent<Image>();
            img.color = Color.white;
            
            TMP_Dropdown dd = ddGO.AddComponent<TMP_Dropdown>();
            
            RectTransform rt = ddGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);
            rt.anchoredPosition = new Vector2(0, y);
            
            // Simplified setup:
            GameObject label = new GameObject("Label");
            label.transform.SetParent(ddGO.transform, false);
            TextMeshProUGUI t = label.AddComponent<TextMeshProUGUI>();
            t.text = name;
            t.color = Color.black;
            t.alignment = TextAlignmentOptions.Center;
            dd.captionText = t;
            
            return ddGO;
        }


    }
}
