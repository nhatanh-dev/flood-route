using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class Round2PlanningMapController : MonoBehaviour
{
    [Header("References")]
    public Camera firstPersonCamera;
    public Camera planningCamera;
    public Round2.Round2FirstPersonBoatController boatController;
    public Round2BoatInteraction boatInteraction;
    public Round2RealtimeRoundController roundController;
    public Round2RescueZone rescueCocTieu;
    public Round2RescueZone rescueNhaSong;
    public Round2RescueZone dropoffDiemTru;

    [Header("Map Camera")]
    public Vector3 mapCameraPosition = new Vector3(56.8f, 48f, -3.6f);
    public float mapOrthographicSize = 18.8f;
    public Color mapBackgroundColor = new Color(0.085f, 0.115f, 0.105f, 1f);

    [Header("Marker Tuning")]
    public Vector2 playerIconSize = new Vector2(46f, 52f);
    public Vector2 rescueIconSize = new Vector2(48f, 58f);
    public Vector2 shelterIconSize = new Vector2(54f, 58f);
    public Vector2 labelOffset = new Vector2(0f, -38f);
    public Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);
    public float shelterPulseSpeed = 2.2f;
    public float shelterCargoPulseScale = 0.08f;

    [Header("Map Readability")]
    [Range(0f, 1f)] public float mapRainOverlayAlpha = 0.012f;

    private readonly List<GameObject> gameplayHudElements = new List<GameObject>();
    private readonly Dictionary<GameObject, bool> gameplayHudOriginalStates = new Dictionary<GameObject, bool>();
    private readonly List<Behaviour> rainOverlayBehaviours = new List<Behaviour>();
    private readonly Dictionary<Behaviour, float> rainOverlayOriginalAlphas = new Dictionary<Behaviour, float>();

    private GameObject canvasRoot;
    private GameObject markerRoot;
    private RectTransform canvasRect;
    private Transform markerCocTieu;
    private Transform markerNhaSong;
    private Transform markerDiemTru;
    private Transform markerPlayer;
    // removed player marker
    // removed player icon
    private TMP_Text labelPlayer;
    private Image fullScreenDimImage;
    private Sprite rescueSprite;
    private Sprite shelterSprite;
    private Sprite playerSprite;
    private Sprite shadowSprite;
    private bool isMapOpen;

    private static readonly Color RescueColor = new Color(0.95f, 0.36f, 0.10f, 1f);
    private static readonly Color ShelterColor = new Color(0.20f, 0.72f, 0.36f, 1f);
    private static readonly Color PlayerColor = new Color(1f, 0.86f, 0.20f, 1f);
    private static readonly Color LabelColor = new Color(0.95f, 0.98f, 0.96f, 1f);
    private static readonly Color PanelColor = new Color(0.02f, 0.06f, 0.08f, 0.42f);
    private bool originalFogState;

    private void Awake()
    {
        EnsureReferences();
        EnsurePlanningCamera();
        EnsureSprites();
        EnsureCanvas();
        EnsureMarkers();
        CollectGameplayHudElements();
        CollectRainOverlays();
        SetMapVisible(false);
    }

    private void OnDisable()
    {
        if (isMapOpen) { RenderSettings.fog = originalFogState; }
    }

    private void OnDestroy()
    {
        if (isMapOpen) { RenderSettings.fog = originalFogState; }
    }

    private void Update()
    {
        if (roundController != null && !roundController.IsPlaying())
        {
            if (isMapOpen)
            {
                CloseMap();
            }
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        if (kb.tabKey.wasPressedThisFrame)
        {
            if (isMapOpen) CloseMap();
            else OpenMap();
        }
        else if (isMapOpen && kb.escapeKey.wasPressedThisFrame)
        {
            CloseMap();
        }

        if (isMapOpen)
        {
            RefreshMarkers();
        }
    }

    private void EnsureReferences()
    {
        if (roundController == null)
        {
            roundController = FindAnyObjectByType<Round2RealtimeRoundController>(FindObjectsInactive.Include);
        }

        if (boatController == null)
        {
            boatController = FindAnyObjectByType<Round2.Round2FirstPersonBoatController>(FindObjectsInactive.Include);
        }

        if (boatInteraction == null)
        {
            boatInteraction = FindAnyObjectByType<Round2BoatInteraction>(FindObjectsInactive.Include);
        }

        if (firstPersonCamera == null)
        {
            GameObject camGo = GameObject.Find("R2_FP_Camera");
            if (camGo != null)
            {
                firstPersonCamera = camGo.GetComponent<Camera>();
            }
        }

        if (firstPersonCamera == null && boatController != null)
        {
            firstPersonCamera = boatController.playerCamera;
        }

        if (firstPersonCamera == null)
        {
            firstPersonCamera = Camera.main;
        }

        rescueCocTieu = rescueCocTieu != null ? rescueCocTieu : FindZone("R2_RescueZone_CocTieu");
        rescueNhaSong = rescueNhaSong != null ? rescueNhaSong : FindZone("R2_RescueZone_NhaSong");
        dropoffDiemTru = dropoffDiemTru != null ? dropoffDiemTru : FindZone("R2_DropoffZone_DiemTru");
    }

    private Round2RescueZone FindZone(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName && all[i].scene.isLoaded)
                {
                    go = all[i];
                    break;
                }
            }
        }

        return go != null ? go.GetComponent<Round2RescueZone>() : null;
    }

    private void EnsurePlanningCamera()
    {
        if (planningCamera == null)
        {
            GameObject existing = GameObject.Find("R2_PlanningMapCamera");
            if (existing != null)
            {
                planningCamera = existing.GetComponent<Camera>();
            }
        }

        if (planningCamera == null)
        {
            GameObject go = new GameObject("R2_PlanningMapCamera");
            planningCamera = go.AddComponent<Camera>();
        }

        planningCamera.gameObject.name = "R2_PlanningMapCamera";
        planningCamera.transform.position = mapCameraPosition;
        planningCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        planningCamera.orthographic = true;
        planningCamera.orthographicSize = mapOrthographicSize;
        planningCamera.nearClipPlane = 0.1f;
        planningCamera.farClipPlane = 90f;
        planningCamera.depth = 210;
        planningCamera.clearFlags = CameraClearFlags.SolidColor;
        planningCamera.backgroundColor = mapBackgroundColor;
        planningCamera.enabled = false;
        planningCamera.gameObject.SetActive(false);
    }

    private void EnsureSprites()
    {
        if (rescueSprite == null) rescueSprite = CreateRescuePinSprite("R2_Map_Rescue_Pin_Icon", RescueColor);
        if (shelterSprite == null) shelterSprite = CreateShelterSprite("R2_Map_Shelter_Icon", ShelterColor);
        if (playerSprite == null) playerSprite = CreatePlayerArrowSprite("R2_Map_Player_Arrow_Icon", PlayerColor);
        if (shadowSprite == null) shadowSprite = CreateSoftDotSprite("R2_Map_Icon_Shadow");
    }

    private void EnsureCanvas()
    {
        if (canvasRoot != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("R2_PlanningMapCanvas");
        if (existing != null)
        {
            Destroy(existing);
        }

        canvasRoot = new GameObject("R2_PlanningMapCanvas");
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasRoot.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        canvasRect = canvasRoot.GetComponent<RectTransform>();

        GameObject panel = CreatePanel(canvasRoot.transform, "R2_PlanningUI_Group", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.018f));
        fullScreenDimImage = panel.GetComponent<Image>();
        CreatePanel(panel.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), Vector2.zero, new Color(0.02f, 0.06f, 0.08f, 0.38f));
        CreateTMP(panel.transform, "TitleText", "BẢN ĐỒ CỨU HỘ", 24f, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), Vector2.zero);

        GameObject legend = CreatePanel(panel.transform, "LegendPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -154f), new Vector2(212f, -62f), PanelColor);
        VerticalLayoutGroup layout = legend.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 12, 10, 10);
        layout.spacing = 3f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateTMPInLayout(legend.transform, "LegendTitle", "CHÚ THÍCH", 16f, TextAlignmentOptions.Left, Color.white);
        CreateTMPInLayout(legend.transform, "LegendBoat", "<color=#00FFFF>▲</color> Thuyền", 14f, TextAlignmentOptions.Left, Color.white);
        CreateTMPInLayout(legend.transform, "LegendRescue", "<color=#FF8800>●</color> Cần cứu", 14f, TextAlignmentOptions.Left, Color.white);
        CreateTMPInLayout(legend.transform, "LegendShelter", "<color=#44FF88>♦</color> Điểm trú", 14f, TextAlignmentOptions.Left, Color.white);

        GameObject footer = CreatePanel(panel.transform, "FooterBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 16f), new Vector2(210f, 46f), new Color(0.02f, 0.06f, 0.08f, 0.42f));
        CreateTMP(footer.transform, "FooterText", "Tab/Esc: Đóng bản đồ", 15f, TextAlignmentOptions.Center, new Color(0.86f, 0.95f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        markerRoot = new GameObject("R2_MapObjectiveIcons");
    }

    private void EnsureMarkers()
    {
        if (markerCocTieu != null)
        {
            return;
        }
        
        Transform cocTieuT = rescueCocTieu != null ? rescueCocTieu.transform : null;
        Transform nhaSongT = rescueNhaSong != null ? rescueNhaSong.transform : null;
        Transform diemTruT = dropoffDiemTru != null ? dropoffDiemTru.transform : null;

        markerCocTieu = CreateWorldMarker("R2_MapMarker_CocTieu", cocTieuT != null ? cocTieuT.position : Vector3.zero, false);
        markerNhaSong = CreateWorldMarker("R2_MapMarker_NhaSong", nhaSongT != null ? nhaSongT.position : Vector3.zero, false);
        markerDiemTru = CreateWorldMarker("R2_MapMarker_DiemTru", diemTruT != null ? diemTruT.position : Vector3.zero, true);
        
        if (boatController != null)
        {
            markerPlayer = CreatePlayerWorldMarker("R2_MapMarker_PlayerBoat");
        }
    }

    private void CreateMapIcon_Unused() {}

    private void OpenMap()
    {
        isMapOpen = true;
        originalFogState = RenderSettings.fog;
        RenderSettings.fog = false;
        if (firstPersonCamera != null) firstPersonCamera.enabled = false;
        if (planningCamera != null)
        {
            planningCamera.gameObject.SetActive(true);
            planningCamera.enabled = true;
        }

        if (boatController != null) boatController.enabled = false;
        if (boatInteraction != null) boatInteraction.enabled = false;
        HideGameplayHudForMap();
        SetMapRainReadability(true);
        SetMapVisible(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMap()
    {
        isMapOpen = false;
        RenderSettings.fog = originalFogState;
        if (planningCamera != null)
        {
            planningCamera.enabled = false;
            planningCamera.gameObject.SetActive(false);
        }
        if (firstPersonCamera != null) firstPersonCamera.enabled = true;

        if (boatController != null) boatController.enabled = true;
        if (boatInteraction != null) boatInteraction.enabled = true;
        RestoreGameplayHudAfterMap();
        SetMapRainReadability(false);
        SetMapVisible(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetMapVisible(bool visible)
    {
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(visible);
        }

        if (visible)
        {
            RefreshMarkers();
        }
    }

    private void RefreshMarkers()
    {
        if (markerCocTieu != null) markerCocTieu.gameObject.SetActive(rescueCocTieu != null && rescueCocTieu.civiliansAvailable > 0);
        if (markerNhaSong != null) markerNhaSong.gameObject.SetActive(rescueNhaSong != null && rescueNhaSong.civiliansAvailable > 0);
        if (markerDiemTru != null) 
        {
            markerDiemTru.gameObject.SetActive(true);
            bool hasCargo = roundController != null && roundController.currentCargo > 0;
            float pulse = hasCargo ? 1f + Mathf.Sin(Time.time * shelterPulseSpeed) * shelterCargoPulseScale : 1f;
            markerDiemTru.localScale = new Vector3(pulse, pulse, pulse);
        }
        if (markerPlayer != null && boatController != null)
        {
            markerPlayer.gameObject.SetActive(true);
            markerPlayer.position = boatController.transform.position + Vector3.up * 25f;
            float heading = boatController.transform.eulerAngles.y;
            markerPlayer.rotation = Quaternion.Euler(0f, heading + 180f, 0f) * Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void UpdateMarkerPosition_Unused() {}

    private void UpdatePlayerMarkerRotation_Unused() {}

    private void CollectGameplayHudElements()
    {
        gameplayHudElements.Clear();
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in allTransforms)
        {
            if (t == null || t.gameObject == gameObject)
            {
                continue;
            }

            string objectName = t.name;
            if (objectName.StartsWith("R2_Planning") || objectName.StartsWith("R2_Map"))
            {
                continue;
            }

            bool isGameplayHud =
                objectName == "R2_RealtimeGameplayHUD_Canvas" ||
                objectName.Contains("RealtimeGameplayHUD") ||
                objectName.Contains("GameplayHUD") ||
                objectName.Contains("Gameplay_HUD") ||
                objectName.Contains("HUD_Group") ||
                objectName.Contains("TXT_R2") ||
                objectName.Contains("Objective") ||
                objectName.Contains("InteractionPrompt") ||
                objectName.Contains("ControlsPrompt") ||
                objectName.Contains("BottomPrompt");

            if (isGameplayHud && !gameplayHudElements.Contains(t.gameObject))
            {
                gameplayHudElements.Add(t.gameObject);
            }
        }
    }

    private void HideGameplayHudForMap()
    {
        gameplayHudOriginalStates.Clear();
        for (int i = 0; i < gameplayHudElements.Count; i++)
        {
            if (gameplayHudElements[i] != null)
            {
                gameplayHudOriginalStates[gameplayHudElements[i]] = gameplayHudElements[i].activeSelf;
                gameplayHudElements[i].SetActive(false);
            }
        }
    }

    private void RestoreGameplayHudAfterMap()
    {
        for (int i = 0; i < gameplayHudElements.Count; i++)
        {
            GameObject go = gameplayHudElements[i];
            if (go != null && gameplayHudOriginalStates.TryGetValue(go, out bool wasActive))
            {
                go.SetActive(wasActive);
            }
        }
    }

    private void CollectRainOverlays()
    {
        rainOverlayBehaviours.Clear();
        rainOverlayOriginalAlphas.Clear();
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (!typeName.Contains("RainOverlay"))
            {
                continue;
            }

            var field = behaviour.GetType().GetField("overlayAlpha");
            if (field == null || field.FieldType != typeof(float))
            {
                continue;
            }

            rainOverlayBehaviours.Add(behaviour);
            rainOverlayOriginalAlphas[behaviour] = (float)field.GetValue(behaviour);
        }
    }

    private void SetMapRainReadability(bool mapOpen)
    {
        foreach (Behaviour behaviour in rainOverlayBehaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            var type = behaviour.GetType();
            var field = type.GetField("overlayAlpha");
            if (field == null)
            {
                continue;
            }

            if (mapOpen)
            {
                if (!rainOverlayOriginalAlphas.ContainsKey(behaviour))
                {
                    rainOverlayOriginalAlphas[behaviour] = (float)field.GetValue(behaviour);
                }
                float current = (float)field.GetValue(behaviour);
                field.SetValue(behaviour, Mathf.Min(mapRainOverlayAlpha, current));
            }
            else if (rainOverlayOriginalAlphas.TryGetValue(behaviour, out float originalAlpha))
            {
                field.SetValue(behaviour, originalAlpha);
            }

            var method = type.GetMethod("ApplySettings");
            if (method != null)
            {
                method.Invoke(behaviour, null);
            }
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return go;
    }

    private static TMP_Text CreateTMP(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return tmp;
    }

    private static TMP_Text CreateTMPInLayout(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = fontSize * 1.55f;
        layout.flexibleWidth = 1f;
        return tmp;
    }

    private static Sprite CreateRescuePinSprite(string name, Color fill)
    {
        const int width = 96;
        const int height = 112;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f - width * 0.5f) / width;
                float ny = (y + 0.5f - height * 0.56f) / height;

                bool outerCircle = Sqr(nx / 0.27f) + Sqr((ny - 0.10f) / 0.24f) <= 1f;
                bool outerTip = PointInTriangle(nx, ny, -0.15f, -0.02f, 0.15f, -0.02f, 0f, -0.36f);
                bool outer = outerCircle || outerTip;

                bool innerCircle = Sqr(nx / 0.21f) + Sqr((ny - 0.10f) / 0.19f) <= 1f;
                bool innerTip = PointInTriangle(nx, ny, -0.105f, -0.03f, 0.105f, -0.03f, 0f, -0.27f);
                bool inner = innerCircle || innerTip;

                Color pixel = clear;
                if (outer)
                {
                    pixel = inner ? fill : Color.white;
                }
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateShelterSprite(string name, Color fill)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size * 0.5f) / size;
                float ny = (y + 0.5f - size * 0.5f) / size;

                bool outer = PointInPolygon(nx, ny, new[]
                {
                    new Vector2(0f, 0.40f),
                    new Vector2(0.34f, 0.13f),
                    new Vector2(0.27f, -0.31f),
                    new Vector2(0f, -0.43f),
                    new Vector2(-0.27f, -0.31f),
                    new Vector2(-0.34f, 0.13f)
                });

                bool inner = PointInPolygon(nx, ny, new[]
                {
                    new Vector2(0f, 0.30f),
                    new Vector2(0.25f, 0.09f),
                    new Vector2(0.20f, -0.24f),
                    new Vector2(0f, -0.33f),
                    new Vector2(-0.20f, -0.24f),
                    new Vector2(-0.25f, 0.09f)
                });

                bool houseRoof = PointInTriangle(nx, ny, -0.16f, 0.02f, 0.16f, 0.02f, 0f, 0.18f);
                bool houseBody = Mathf.Abs(nx) < 0.13f && ny > -0.17f && ny < 0.03f;
                bool houseCut = houseRoof || houseBody;

                Color pixel = clear;
                if (outer)
                {
                    pixel = inner ? fill : Color.white;
                    if (houseCut && inner)
                    {
                        pixel = new Color(0.94f, 0.98f, 0.94f, 1f);
                    }
                }
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreatePlayerArrowSprite(string name, Color fill)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size * 0.5f) / size;
                float ny = (y + 0.5f - size * 0.5f) / size;

                bool outer = PointInTriangle(nx, ny, 0f, 0.40f, -0.28f, -0.29f, 0.28f, -0.29f);
                bool inner = PointInTriangle(nx, ny, 0f, 0.29f, -0.19f, -0.20f, 0.19f, -0.20f);
                bool notch = PointInTriangle(nx, ny, 0f, -0.05f, -0.08f, -0.22f, 0.08f, -0.22f);

                Color pixel = clear;
                if (outer)
                {
                    pixel = inner ? fill : Color.white;
                    if (notch && inner)
                    {
                        pixel = new Color(0.08f, 0.10f, 0.09f, 0.55f);
                    }
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateSoftDotSprite(string name)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - d);
                alpha *= alpha * 0.75f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Sqr(float value)
    {
        return value * value;
    }

    private static bool PointInTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = Sign(px, py, ax, ay, bx, by);
        float d2 = Sign(px, py, bx, by, cx, cy);
        float d3 = Sign(px, py, cx, cy, ax, ay);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    private static float Sign(float px, float py, float ax, float ay, float bx, float by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    private static bool PointInPolygon(float x, float y, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            bool intersect = ((polygon[i].y > y) != (polygon[j].y > y)) &&
                (x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x);
            if (intersect)
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    private Transform CreateWorldMarker(string name, Vector3 position, bool shelter)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(markerRoot.transform, false);
        root.transform.position = position + Vector3.up * 25f; // high enough to not clip
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // flat

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = Vector3.zero;
        labelGo.transform.localRotation = Quaternion.identity;
        var label = labelGo.AddComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 7f; // significantly reduced size
        label.lineSpacing = -8f;
        label.enableWordWrapping = false;
        
        if (shelter) {
            label.text = "<size=350%><color=#44FF88>♦</color></size>\n<size=120%><color=#FFFFFF>Điểm trú</color></size>";
        } else {
            label.text = "<size=350%><color=#FF8800>●</color></size>\n<size=120%><color=#FFFFFF>Cần cứu</color></size>";
        }
        label.outlineWidth = 0.18f;
        label.outlineColor = new Color(0.02f, 0.025f, 0.02f, 0.95f);
        
        return root.transform;
    }


    private Transform CreatePlayerWorldMarker(string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(markerRoot.transform, false);
        root.transform.position = Vector3.up * 25f;
        root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(root.transform, false);
        labelGo.transform.localPosition = Vector3.zero;
        labelGo.transform.localRotation = Quaternion.identity;
        var label = labelGo.AddComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 7f;
        label.lineSpacing = -8f;
        label.enableWordWrapping = false;
        label.text = "<size=300%><color=#00FFFF>▲</color></size>\n<size=90%><color=#FFFFFF>Thuyền</color></size>";
        label.outlineWidth = 0.18f;
        label.outlineColor = new Color(0.02f, 0.025f, 0.02f, 0.95f);
        
        return root.transform;
    }

}