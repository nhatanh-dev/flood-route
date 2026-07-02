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

    private TMP_Text txtMapCurrent;
    private TMP_Text txtMapTime;
    private TMP_Text txtMapCargo;
    private TMP_Text txtMapSaved;
    private TMP_Text txtMapObjective;
    private string lastKnownObjectiveText;
    private RectTransform compactCardRect;
    private GameObject mapObjectiveLabelObject;
    private GameObject mapObjectiveTextObject;
    private GameObject mapObjectiveDividerObject;

    private TMP_Text labelCocTieu;
    private TMP_Text labelNhaSong;
    private TMP_Text labelDiemTru;

    private Vector3 cocTieuBaseScale = Vector3.one;
    private Vector3 nhaSongBaseScale = Vector3.one;
    private Vector3 diemTruBaseScale = Vector3.one;
    private Vector3 playerBaseScale = Vector3.one;

    private static readonly Color BoatColorMap = new Color(1f, 0.86f, 0.18f, 1f);
    private static readonly Color RescueColorMap = new Color(1f, 0.48f, 0.12f, 1f);
    private static readonly Color ShelterColorMap = new Color(0.22f, 0.90f, 0.37f, 1f);
    private static readonly Color ClearedColorMap = new Color(0.57f, 0.59f, 0.58f, 1f);

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
            UpdateMapHud();
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

        canvasRoot = GameObject.Find("R2_PlanningMapCanvas");
        if (canvasRoot == null)
        {
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

            CanvasGroup canvasGroup = canvasRoot.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        canvasRect = canvasRoot.GetComponent<RectTransform>();

        Transform panelT = canvasRoot.transform.Find("R2_PlanningUI_Group");
        GameObject panel;
        if (panelT != null)
        {
            panel = panelT.gameObject;
        }
        else
        {
            panel = new GameObject("R2_PlanningUI_Group");
            panel.transform.SetParent(canvasRoot.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        fullScreenDimImage = panel.GetComponent<Image>();
        if (fullScreenDimImage == null)
        {
            fullScreenDimImage = panel.AddComponent<Image>();
            fullScreenDimImage.color = new Color(0f, 0f, 0f, 0.018f);
            fullScreenDimImage.raycastTarget = false;
        }

        Transform darkOverlayT = panel.transform.Find("DarkOverlay");
        if (darkOverlayT == null)
        {
            CreatePanel(panel.transform, "DarkOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.05f, 0.05f, 0.10f));
        }

        Transform topBarT = panel.transform.Find("TopBar");
        if (topBarT == null)
        {
            GameObject topBar = CreatePanel(panel.transform, "TopBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -76f), Vector2.zero,
                new Color(0.035f, 0.10f, 0.10f, 0.92f));
            CreateTMP(topBar.transform, "TitleText", "BẢN ĐỒ CỨU HỘ  •  ROUND 2", 34f,
                TextAlignmentOptions.Center, new Color(0.94f, 0.91f, 0.84f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        Transform cardT = panel.transform.Find("CompactMapCard");
        GameObject compactCard;
        if (cardT != null)
        {
            compactCard = cardT.gameObject;
        }
        else
        {
            compactCard = CreatePanel(panel.transform, "CompactMapCard",
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color(0.035f, 0.10f, 0.10f, 0.90f));

            VerticalLayoutGroup cardLayout = compactCard.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(18, 18, 14, 14);
            cardLayout.spacing = 5f;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
        }

        RectTransform cardRect = compactCard.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(0f, 1f);
        cardRect.pivot = new Vector2(0f, 1f);
        cardRect.sizeDelta = new Vector2(408f, 302f);
        cardRect.anchoredPosition = new Vector2(30f, -82f);

        Transform currentLocationT = compactCard.transform.Find("CurrentLocation");
        if (currentLocationT != null)
        {
            txtMapCurrent = currentLocationT.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            txtMapCurrent = CreateTMPInLayout(compactCard.transform, "CurrentLocation",
                "VỊ TRÍ: THEO DẤU ▲", 22f, TextAlignmentOptions.Left,
                new Color(0.94f, 0.91f, 0.84f));
            LayoutElement currentElement = txtMapCurrent.GetComponent<LayoutElement>();
            currentElement.preferredHeight = 36f;
            currentElement.flexibleWidth = 1f;
        }

        Transform divider1T = compactCard.transform.Find("Divider1");
        if (divider1T == null)
        {
            CreateDividerWithName(compactCard.transform, "Divider1");
        }

        Transform statsGridT = compactCard.transform.Find("StatsGrid");
        GameObject statsGrid;
        if (statsGridT != null)
        {
            statsGrid = statsGridT.gameObject;
        }
        else
        {
            statsGrid = new GameObject("StatsGrid");
            statsGrid.transform.SetParent(compactCard.transform, false);
            VerticalLayoutGroup statsLayout = statsGrid.AddComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 3f;
            statsLayout.childAlignment = TextAnchor.UpperLeft;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = true;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            LayoutElement statsElement = statsGrid.AddComponent<LayoutElement>();
            statsElement.preferredHeight = 84f;
            statsElement.flexibleWidth = 1f;
        }

        Transform timeRowT = statsGrid.transform.Find("TimeRow");
        if (timeRowT != null)
        {
            Transform valTimeRow = timeRowT.Find("ValTimeRow");
            if (valTimeRow != null) txtMapTime = valTimeRow.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            txtMapTime = CreateStatRow(statsGrid.transform, "TimeRow", "THỜI GIAN", "--:--");
        }

        Transform cargoRowT = statsGrid.transform.Find("CargoRow");
        if (cargoRowT != null)
        {
            Transform valCargoRow = cargoRowT.Find("ValCargoRow");
            if (valCargoRow != null) txtMapCargo = valCargoRow.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            txtMapCargo = CreateStatRow(statsGrid.transform, "CargoRow", "TRÊN THUYỀN", "0/4");
        }

        Transform savedRowT = statsGrid.transform.Find("SavedRow");
        if (savedRowT != null)
        {
            Transform valSavedRow = savedRowT.Find("ValSavedRow");
            if (valSavedRow != null) txtMapSaved = valSavedRow.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            txtMapSaved = CreateStatRow(statsGrid.transform, "SavedRow", "ĐÃ CỨU", "0/4");
        }

        Transform divider2T = compactCard.transform.Find("Divider2");
        if (divider2T == null)
        {
            CreateDividerWithName(compactCard.transform, "Divider2");
        }

        Transform lblObjectiveT = compactCard.transform.Find("LblObjective");
        if (lblObjectiveT == null)
        {
            TMP_Text objLabel = CreateTMPInLayout(compactCard.transform, "LblObjective", "MỤC TIÊU", 19f,
                TextAlignmentOptions.Left, new Color(0.80f, 0.71f, 0.47f));
            LayoutElement objLabelElement = objLabel.GetComponent<LayoutElement>();
            objLabelElement.preferredHeight = 26f;
            objLabelElement.flexibleWidth = 1f;
        }

        Transform txtObjectiveT = compactCard.transform.Find("TxtObjective");
        if (txtObjectiveT != null)
        {
            txtMapObjective = txtObjectiveT.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            txtMapObjective = CreateTMPInLayout(compactCard.transform, "TxtObjective",
                "Tìm nhà có tín hiệu cầu cứu.", 19f,
                TextAlignmentOptions.Left, new Color(0.93f, 0.91f, 0.86f));
            LayoutElement objValueElement = txtMapObjective.GetComponent<LayoutElement>();
            objValueElement.preferredHeight = 42f;
            objValueElement.flexibleWidth = 1f;
            txtMapObjective.textWrappingMode = TextWrappingModes.Normal;
            txtMapObjective.overflowMode = TextOverflowModes.Overflow;
            txtMapObjective.lineSpacing = 1.5f;
        }

        Transform divider3T = compactCard.transform.Find("Divider3");
        if (divider3T == null)
        {
            CreateDividerWithName(compactCard.transform, "Divider3");
        }

        Transform legendGridT = compactCard.transform.Find("LegendGrid");
        if (legendGridT == null)
        {
            CreateLegendGrid(compactCard.transform);
        }

        compactCardRect = compactCard.GetComponent<RectTransform>();
        mapObjectiveLabelObject = compactCard.transform.Find("LblObjective")?.gameObject;
        mapObjectiveTextObject = compactCard.transform.Find("TxtObjective")?.gameObject;
        mapObjectiveDividerObject = compactCard.transform.Find("Divider3")?.gameObject;

        Transform footerT = panel.transform.Find("FooterBar");
        if (footerT == null)
        {
            GameObject footer = CreatePanel(panel.transform, "FooterBar",
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 58f),
                new Color(0.035f, 0.10f, 0.10f, 0.92f));
            CreateTMP(footer.transform, "TxtFooter", "TAB / ESC  •  ĐÓNG BẢN ĐỒ", 18f,
                TextAlignmentOptions.Center, new Color(0.82f, 0.84f, 0.80f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        ApplyPlanningTypography(panel.transform);

        markerRoot = GameObject.Find("R2_MapObjectiveIcons");
        if (markerRoot == null)
        {
            markerRoot = new GameObject("R2_MapObjectiveIcons");
        }
    }

    private void EnsureMarkers()
    {
        if (markerRoot == null)
        {
            markerRoot = GameObject.Find("R2_MapObjectiveIcons");
            if (markerRoot == null)
            {
                markerRoot = new GameObject("R2_MapObjectiveIcons");
            }
        }
        
        Transform cocTieuT = rescueCocTieu != null ? rescueCocTieu.transform : null;
        Transform nhaSongT = rescueNhaSong != null ? rescueNhaSong.transform : null;
        Transform diemTruT = dropoffDiemTru != null ? dropoffDiemTru.transform : null;

        if (markerCocTieu == null)
        {
            Transform existing = markerRoot.transform.Find("R2_MapMarker_CocTieu");
            if (existing != null) markerCocTieu = existing;
            else markerCocTieu = CreateWorldMarker("R2_MapMarker_CocTieu", cocTieuT != null ? cocTieuT.position : Vector3.zero, false);

            labelCocTieu = markerCocTieu.GetComponentInChildren<TextMeshPro>();
            cocTieuBaseScale = markerCocTieu.localScale;
        }

        if (markerNhaSong == null)
        {
            Transform existing = markerRoot.transform.Find("R2_MapMarker_NhaSong");
            if (existing != null) markerNhaSong = existing;
            else markerNhaSong = CreateWorldMarker("R2_MapMarker_NhaSong", nhaSongT != null ? nhaSongT.position : Vector3.zero, false);

            labelNhaSong = markerNhaSong.GetComponentInChildren<TextMeshPro>();
            nhaSongBaseScale = markerNhaSong.localScale;
        }

        if (markerDiemTru == null)
        {
            Transform existing = markerRoot.transform.Find("R2_MapMarker_DiemTru");
            if (existing != null) markerDiemTru = existing;
            else markerDiemTru = CreateWorldMarker("R2_MapMarker_DiemTru", diemTruT != null ? diemTruT.position : Vector3.zero, true);

            labelDiemTru = markerDiemTru.GetComponentInChildren<TextMeshPro>();
            diemTruBaseScale = markerDiemTru.localScale;
        }

        if (markerPlayer == null && boatController != null)
        {
            Transform existing = markerRoot.transform.Find("R2_MapMarker_PlayerBoat");
            if (existing != null) markerPlayer = existing;
            else markerPlayer = CreatePlayerWorldMarker("R2_MapMarker_PlayerBoat");

            playerBaseScale = markerPlayer.localScale;
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
            UpdateMapHud();
        }
    }

    private void RefreshMarkers()
    {
        float rescuePulse = 1f + (Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f) * 0.08f;

        if (markerCocTieu != null) {
            bool cleared = rescueCocTieu == null || rescueCocTieu.civiliansAvailable <= 0;
            markerCocTieu.gameObject.SetActive(true);
            if (!cleared) {
                if (labelCocTieu != null) labelCocTieu.color = RescueColorMap;
                markerCocTieu.localScale = cocTieuBaseScale * rescuePulse;
            } else {
                if (labelCocTieu != null) labelCocTieu.color = ClearedColorMap;
                markerCocTieu.localScale = cocTieuBaseScale;
            }
        }

        if (markerNhaSong != null) {
            bool cleared = rescueNhaSong == null || rescueNhaSong.civiliansAvailable <= 0;
            markerNhaSong.gameObject.SetActive(true);
            if (!cleared) {
                if (labelNhaSong != null) labelNhaSong.color = RescueColorMap;
                markerNhaSong.localScale = nhaSongBaseScale * rescuePulse;
            } else {
                if (labelNhaSong != null) labelNhaSong.color = ClearedColorMap;
                markerNhaSong.localScale = nhaSongBaseScale;
            }
        }

        if (markerDiemTru != null) 
        {
            markerDiemTru.gameObject.SetActive(true);
            bool hasCargo = roundController != null && roundController.currentCargo > 0;
            float pulse = hasCargo ? 1f + Mathf.Sin(Time.unscaledTime * shelterPulseSpeed) * shelterCargoPulseScale : 1f;
            markerDiemTru.localScale = diemTruBaseScale * pulse;
        }

        if (markerPlayer != null && boatController != null)
        {
            markerPlayer.gameObject.SetActive(true);
            markerPlayer.position = boatController.transform.position + Vector3.up * 25f;
            float heading = boatController.transform.eulerAngles.y;
            markerPlayer.rotation = Quaternion.Euler(0f, heading + 180f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            markerPlayer.localScale = playerBaseScale;
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
        label.fontSize = shelter ? 13.4f : 15.5f;
        label.enableWordWrapping = false;
        
        if (shelter) {
            label.text = "♦";
            label.color = ShelterColorMap;
        } else {
            label.text = "●";
            label.color = RescueColorMap;
        }
        label.outlineWidth = 0.16f;
        label.outlineColor = new Color(0.02f, 0.025f, 0.02f, 0.88f);
        
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
        label.fontSize = 13.4f;
        label.enableWordWrapping = false;
        label.text = "▲";
        label.color = BoatColorMap;
        label.outlineWidth = 0.16f;
        label.outlineColor = new Color(0.02f, 0.025f, 0.02f, 0.88f);
        
        return root.transform;
    }

    private void UpdateMapHud()
    {
        if (roundController == null) return;

        int minutes = Mathf.FloorToInt(roundController.CurrentTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(roundController.CurrentTimeRemaining % 60f);
        if (txtMapCurrent != null) txtMapCurrent.text = "VỊ TRÍ: THEO DẤU ▲";
        if (txtMapTime != null) txtMapTime.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        if (txtMapCargo != null) txtMapCargo.text = string.Format("{0}/{1}", roundController.currentCargo, roundController.boatCapacity);
        if (txtMapSaved != null) txtMapSaved.text = string.Format("{0}/{1}", roundController.civiliansSafe, roundController.totalCivilians);
        if (txtMapObjective != null)
        {
            string objective = roundController.currentObjectiveText ?? "";
            if (string.IsNullOrWhiteSpace(objective) && roundController.txtObjective != null)
                objective = roundController.txtObjective.text;

            if (!string.IsNullOrWhiteSpace(objective))
                lastKnownObjectiveText = objective;
            else
                objective = lastKnownObjectiveText;

            const string prefix = "Mục tiêu:";
            if (!string.IsNullOrEmpty(objective) && objective.StartsWith(prefix))
                objective = objective.Substring(prefix.Length).Trim();
            bool hasObjective = !string.IsNullOrWhiteSpace(objective);
            if (hasObjective)
                txtMapObjective.text = objective;

            SetObjectiveSectionVisible(hasObjective);
        }
    }

    private void SetObjectiveSectionVisible(bool visible)
    {
        if (mapObjectiveLabelObject != null)
            mapObjectiveLabelObject.SetActive(visible);
        if (mapObjectiveTextObject != null)
            mapObjectiveTextObject.SetActive(visible);
        if (mapObjectiveDividerObject != null)
            mapObjectiveDividerObject.SetActive(visible);

        if (compactCardRect != null)
        {
            compactCardRect.sizeDelta = new Vector2(408f, visible ? 302f : 232f);
            LayoutRebuilder.MarkLayoutForRebuild(compactCardRect);
        }
    }

    private static TMP_Text CreateStatRow(Transform parent, string name, string label, string value)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.preferredHeight = 26f;
        rowElement.flexibleWidth = 1f;

        TMP_Text labelText = CreateTMPInLayout(row.transform, "Lbl" + name, label, 19f,
            TextAlignmentOptions.Left, new Color(0.80f, 0.71f, 0.47f));
        LayoutElement labelElement = labelText.GetComponent<LayoutElement>();
        labelElement.preferredHeight = 26f;
        labelElement.flexibleWidth = 1f;

        TMP_Text valueText = CreateTMPInLayout(row.transform, "Val" + name, value, 20f,
            TextAlignmentOptions.Right, new Color(0.94f, 0.91f, 0.84f));
        LayoutElement valueElement = valueText.GetComponent<LayoutElement>();
        valueElement.preferredHeight = 26f;
        valueElement.preferredWidth = 78f;
        valueElement.flexibleWidth = 0f;
        return valueText;
    }

    private static void CreateDivider(Transform parent)
    {
        CreateDividerWithName(parent, "Divider");
    }

    private static void CreateDividerWithName(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.67f, 0.52f, 0.24f, 0.45f);
        image.raycastTarget = false;
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 1f;
        layout.flexibleWidth = 1f;
    }

    private static void CreateLegendGrid(Transform parent)
    {
        GameObject grid = new GameObject("LegendGrid");
        grid.transform.SetParent(parent, false);

        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(182f, 24f);
        layout.spacing = new Vector2(8f, 4f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;

        LayoutElement gridElement = grid.AddComponent<LayoutElement>();
        gridElement.preferredHeight = 56f;
        gridElement.flexibleWidth = 1f;

        CreateLegendItem(grid.transform, "LegendBoat", "<color=#FFDB2E>▲</color> Thuyền");
        CreateLegendItem(grid.transform, "LegendRescue", "<color=#FF7A1F>●</color> Cần cứu");
        CreateLegendItem(grid.transform, "LegendShelter", "<color=#38E65F>♦</color> Điểm trú");
        CreateLegendItem(grid.transform, "LegendCleared", "<color=#929794>●</color> Đã cứu");
    }

    private static void CreateLegendItem(Transform parent, string name, string text)
    {
        TMP_Text item = CreateTMPInLayout(parent, name, text, 17.5f,
            TextAlignmentOptions.Left, new Color(0.82f, 0.84f, 0.80f));
        item.overflowMode = TextOverflowModes.Overflow;
        LayoutElement element = item.GetComponent<LayoutElement>();
        element.preferredHeight = 24f;
    }

    private static void ApplyPlanningTypography(Transform root)
    {
        TMP_FontAsset heading = FindLoadedFont("BarlowCondensed_Bold SDF SDF");
        TMP_FontAsset label = FindLoadedFont("BarlowCondensed_SemiBold SDF SDF");
        TMP_FontAsset body = FindLoadedFont("BeVietnamPro_Medium SDF SDF");
        TMP_FontAsset regular = FindLoadedFont("BeVietnamPro_Regular SDF SDF");

        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name == "TitleText")
                text.font = heading != null ? heading : text.font;
            else if (text.name == "CurrentLocation" || text.name == "LblObjective" ||
                     text.name.StartsWith("Lbl"))
                text.font = label != null ? label : text.font;
            else if (text.name == "TxtFooter" || text.name.StartsWith("Legend"))
                text.font = regular != null ? regular : text.font;
            else
                text.font = body != null ? body : text.font;

            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private static TMP_FontAsset FindLoadedFont(string fontName)
    {
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font.name == fontName) return font;
        return null;
    }

}
