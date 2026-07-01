using System.Collections.Generic;
using System.Linq;
using Round1;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CampaignFlowSetup
{
    private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
    private const string Round1Scene = "Assets/Scenes/Round1_FirstPersonPrototype.unity";
    private const string Round2BriefingScene = "Assets/Scenes/Round2_MissionBriefing.unity";
    private const string Round2GameplayScene = "Assets/Scenes/Round2_RealtimePrototype.unity";

    private static readonly Color Background = Hex("#13262D");
    private static readonly Color MainPanel = Hex("#183330");
    private static readonly Color InfoPanel = Hex("#243F3B");
    private static readonly Color Thumbnail = Hex("#435B5F");
    private static readonly Color OffWhite = Hex("#F0E8D8");
    private static readonly Color SecondaryText = Hex("#B8C2BA");
    private static readonly Color Ochre = Hex("#B68A45");
    private static readonly Color Rust = Hex("#854733");
    private static readonly Color DarkButton = Hex("#293B38");

    private static TMP_FontAsset roadRage;
    private static TMP_FontAsset barlowBold;
    private static TMP_FontAsset barlowSemiBold;
    private static TMP_FontAsset beVietnamRegular;
    private static TMP_FontAsset beVietnamMedium;

    [MenuItem("Tools/Campaign/Build Linear Campaign Flow")]
    public static void BuildLinearCampaignFlow()
    {
        LoadFonts();
        AddRound1CampaignIndicator();
        UpdateRound1VictoryActions();
        CreateRound2Briefing();
        UpdateBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CampaignFlowSetup] Linear campaign flow created successfully.");
    }

    [MenuItem("Tools/Campaign/Apply Briefing Visual Consistency")]
    public static void ApplyBriefingVisualConsistency()
    {
        LoadFonts();
        AddRound1CampaignIndicator();
        CreateRound2Briefing();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CampaignFlowSetup] Mission Briefing visual consistency applied successfully.");
    }

    private static void LoadFonts()
    {
        roadRage = LoadFont("Assets/Fonts/SVN-Road Rage SDF.asset");
        barlowBold = LoadFont("Assets/Fonts/BarlowCondensed/BarlowCondensed_Bold SDF SDF.asset");
        barlowSemiBold = LoadFont("Assets/Fonts/BarlowCondensed/BarlowCondensed_SemiBold SDF SDF.asset");
        beVietnamRegular = LoadFont("Assets/Fonts/BeVietnamPro/BeVietnamPro_Regular SDF SDF.asset");
        beVietnamMedium = LoadFont("Assets/Fonts/BeVietnamPro/BeVietnamPro_Medium SDF SDF.asset");
    }

    private static TMP_FontAsset LoadFont(string path)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null)
            throw new MissingReferenceException("Missing approved TMP font asset: " + path);
        return font;
    }

    private static void AddRound1CampaignIndicator()
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
        var panelContent = GameObject.Find("MainMenu/Canvas/RoundSelection_Panel/PanelContent");
        if (panelContent == null)
            throw new MissingReferenceException("Round 1 briefing PanelContent was not found.");

        DestroyChild(panelContent.transform, "CampaignProgress");

        var panelImage = panelContent.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;
        AddOutline(panelContent, new Color(Ochre.r, Ochre.g, Ochre.b, 0.32f), new Vector2(1f, -1f));

        var panelRect = panelContent.GetComponent<RectTransform>();
        SetRect(panelRect, new Vector2(0.075f, 0.12f), new Vector2(0.925f, 0.88f), Vector2.zero, Vector2.zero);

        var missionHeader = FindDescendant(panelContent.transform, "MissionHeader") as RectTransform;
        var thumbnailColumn = FindDescendant(panelContent.transform, "ThumbnailColumn") as RectTransform;
        var missionTextColumn = FindDescendant(panelContent.transform, "MissionTextColumn") as RectTransform;
        var backButtonRect = FindDescendant(panelContent.transform, "Btn_BackToMenu") as RectTransform;
        var startButtonRect = FindDescendant(panelContent.transform, "Btn_Round1") as RectTransform;
        var headerDivider = FindDescendant(panelContent.transform, "HeaderAccentLine") as RectTransform;

        SetRect(missionHeader, new Vector2(0.045f, 0.82f), new Vector2(0.955f, 0.965f), Vector2.zero, Vector2.zero);
        SetRect(thumbnailColumn, new Vector2(0.045f, 0.18f), new Vector2(0.545f, 0.79f), Vector2.zero, Vector2.zero);
        SetRect(missionTextColumn, new Vector2(0.565f, 0.18f), new Vector2(0.955f, 0.79f), Vector2.zero, Vector2.zero);
        SetRect(backButtonRect, new Vector2(0.045f, 0.055f), new Vector2(0.205f, 0.13f), Vector2.zero, Vector2.zero);
        SetRect(startButtonRect, new Vector2(0.70f, 0.055f), new Vector2(0.955f, 0.13f), Vector2.zero, Vector2.zero);
        SetRect(headerDivider, new Vector2(0.045f, 0.80f), new Vector2(0.955f, 0.803f), Vector2.zero, Vector2.zero);

        var missionTitle = FindText(panelContent.transform, "MissionTitle");
        missionTitle.fontSize = 46f;
        missionTitle.color = OffWhite;

        var missionSubtitle = FindText(panelContent.transform, "MissionSubtitle");
        missionSubtitle.fontSize = 24f;
        missionSubtitle.color = Ochre;

        var objectiveHeading = FindText(panelContent.transform, "ObjectiveHeading");
        objectiveHeading.fontSize = 28f;
        objectiveHeading.color = Ochre;

        var objectiveText = FindText(panelContent.transform, "ObjectiveText");
        objectiveText.fontSize = 19.5f;
        objectiveText.color = OffWhite;
        objectiveText.lineSpacing = 6f;
        objectiveText.paragraphSpacing = 7f;
        SetRect(objectiveText.rectTransform, new Vector2(0.06f, 0.47f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);

        var missionInfo = FindText(panelContent.transform, "MissionInfo");
        missionInfo.fontSize = 18f;
        missionInfo.color = new Color(0.88f, 0.87f, 0.82f, 1f);
        missionInfo.lineSpacing = 4f;
        SetRect(missionInfo.rectTransform, new Vector2(0.06f, 0.055f), new Vector2(0.95f, 0.44f), Vector2.zero, Vector2.zero);

        var backLabel = FindDescendant(panelContent.transform, "Btn_BackToMenu").GetComponentInChildren<TMP_Text>(true);
        var startLabel = FindDescendant(panelContent.transform, "Btn_Round1").GetComponentInChildren<TMP_Text>(true);
        backLabel.fontSize = 26f;
        startLabel.fontSize = 26f;

        var round1Thumbnail = FindDescendant(panelContent.transform, "Round1Thumbnail");
        PrepareThumbnail(round1Thumbnail, "KHU DÂN CƯ NGẬP LỤT");

        var root = CreatePanel("CampaignProgress", panelContent.transform, new Color(0.11f, 0.20f, 0.19f, 0.68f));
        SetRect(root.rectTransform, new Vector2(0.69f, 0.835f), new Vector2(0.955f, 0.955f), Vector2.zero, Vector2.zero);

        var heading = CreateText("TXT_CampaignHeading", root.transform,
            "CHIẾN DỊCH 1 / 2", barlowSemiBold, 18f, new Color(0.80f, 0.79f, 0.74f, 1f),
            TextAlignmentOptions.TopRight);
        SetRect(heading.rectTransform, new Vector2(0.035f, 0.53f), new Vector2(0.965f, 0.93f), Vector2.zero, Vector2.zero);

        var stage1 = CreateText("TXT_Stage1", root.transform,
            "GIAI ĐOẠN 1 — TIẾP CẬN KHU DÂN CƯ", barlowSemiBold, 19f, Ochre,
            TextAlignmentOptions.BottomRight);
        SetRect(stage1.rectTransform, new Vector2(0.035f, 0.08f), new Vector2(0.965f, 0.55f), Vector2.zero, Vector2.zero);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void UpdateRound1VictoryActions()
    {
        var scene = EditorSceneManager.OpenScene(Round1Scene, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<Round1EndgameUIController>(FindObjectsInactive.Include);
        if (controller == null || controller.btnRetry == null)
            throw new MissingReferenceException("Authoritative Round 1 endgame controller or retry button was not found.");

        var retry = controller.btnRetry;
        var card = retry.transform.parent;
        DestroyChild(card, "BTN_ContinueRound2");

        var retryRect = retry.GetComponent<RectTransform>();
        retryRect.anchoredPosition = new Vector2(-155f, -190f);
        retryRect.sizeDelta = new Vector2(280f, 60f);
        StyleButton(retry, DarkButton, new Color(Ochre.r, Ochre.g, Ochre.b, 0.42f));
        var retryLabel = retry.GetComponentInChildren<TMP_Text>(true);
        retryLabel.text = "CHƠI LẠI ROUND 1";
        retryLabel.font = barlowBold;
        retryLabel.color = OffWhite;
        retryLabel.fontSize = 22f;

        var continueObject = Object.Instantiate(retry.gameObject, card);
        continueObject.name = "BTN_ContinueRound2";
        var continueRect = continueObject.GetComponent<RectTransform>();
        continueRect.anchoredPosition = new Vector2(155f, -190f);
        continueRect.sizeDelta = new Vector2(280f, 60f);
        var continueButton = continueObject.GetComponent<Button>();
        StyleButton(continueButton, Rust, new Color(Ochre.r, Ochre.g, Ochre.b, 0.8f));
        var continueLabel = continueObject.GetComponentInChildren<TMP_Text>(true);
        continueLabel.text = "SANG ROUND 2";
        continueLabel.font = barlowBold;
        continueLabel.color = OffWhite;
        continueLabel.fontSize = 24f;
        continueObject.SetActive(false);

        controller.btnContinueRound2 = continueButton;
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateRound2Briefing()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Round2_MissionBriefing";

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;

        var lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        lightObject.GetComponent<Light>().type = LightType.Directional;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var background = CreatePanel("Background", canvasObject.transform, Background);
        SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateBackdropBands(background.transform);

        var panel = CreatePanel("Round2BriefingPanel", background.transform, MainPanel);
        SetRect(panel.rectTransform, new Vector2(0.065f, 0.055f), new Vector2(0.935f, 0.945f), Vector2.zero, Vector2.zero);
        AddOutline(panel.gameObject, new Color(Ochre.r, Ochre.g, Ochre.b, 0.32f), new Vector2(1f, -1f));

        var title = CreateText("TXT_Title", panel.transform, "NHIỆM VỤ CỨU TRỢ", roadRage, 46f, OffWhite, TextAlignmentOptions.Left);
        SetRect(title.rectTransform, new Vector2(0.045f, 0.845f), new Vector2(0.62f, 0.965f), Vector2.zero, Vector2.zero);

        var subtitle = CreateText("TXT_Subtitle", panel.transform, "ROUND 2 — GẤP RÚT SƠ TÁN KHU DÂN CƯ",
            barlowSemiBold, 24f, Ochre, TextAlignmentOptions.Left);
        SetRect(subtitle.rectTransform, new Vector2(0.047f, 0.78f), new Vector2(0.68f, 0.855f), Vector2.zero, Vector2.zero);

        var campaign = CreatePanel("CampaignProgress", panel.transform, new Color(0.11f, 0.20f, 0.19f, 0.92f));
        SetRect(campaign.rectTransform, new Vector2(0.70f, 0.805f), new Vector2(0.955f, 0.955f), Vector2.zero, Vector2.zero);
        var campaignText = CreateText("TXT_CampaignProgress", campaign.transform,
            "CHIẾN DỊCH 2 GIAI ĐOẠN\n<color=#B8C2BA>GIAI ĐOẠN 1 — HOÀN THÀNH</color>\n<color=#B68A45>GIAI ĐOẠN 2 — HIỆN TẠI</color>",
            barlowSemiBold, 18f, OffWhite, TextAlignmentOptions.Center);
        campaignText.richText = true;
        campaignText.lineSpacing = 5f;
        SetRect(campaignText.rectTransform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);

        var divider = CreatePanel("HeaderDivider", panel.transform, new Color(Ochre.r, Ochre.g, Ochre.b, 0.6f));
        SetRect(divider.rectTransform, new Vector2(0.045f, 0.755f), new Vector2(0.955f, 0.758f), Vector2.zero, Vector2.zero);

        var thumbnailFrame = CreatePanel("ThumbnailFrame", panel.transform, new Color(0.12f, 0.23f, 0.22f, 1f));
        SetRect(thumbnailFrame.rectTransform, new Vector2(0.045f, 0.20f), new Vector2(0.53f, 0.72f), Vector2.zero, Vector2.zero);
        AddOutline(thumbnailFrame.gameObject, new Color(Ochre.r, Ochre.g, Ochre.b, 0.35f), new Vector2(1f, -1f));
        var thumbnail = CreatePanel("Round2Thumbnail", thumbnailFrame.transform, Thumbnail);
        SetRect(thumbnail.rectTransform, new Vector2(0.025f, 0.04f), new Vector2(0.975f, 0.96f), Vector2.zero, Vector2.zero);
        thumbnail.preserveAspect = true;
        var thumbnailAspect = thumbnail.gameObject.AddComponent<AspectRatioFitter>();
        thumbnailAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        thumbnailAspect.aspectRatio = 16f / 9f;
        var placeholder = CreateText("TXT_ThumbnailPlaceholder", thumbnail.transform,
            "ẢNH KHU DÂN CƯ NGẬP LỤT NGHIÊM TRỌNG\n<color=#B8C2BA>(THAY SPRITE TRONG INSPECTOR)</color>",
            beVietnamMedium, 18f, OffWhite, TextAlignmentOptions.Center);
        placeholder.richText = true;
        SetRect(placeholder.rectTransform, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);
        PrepareThumbnail(thumbnail.transform, "KHU SƠ TÁN KHẨN CẤP");

        var info = CreatePanel("MissionInformation", panel.transform, InfoPanel);
        SetRect(info.rectTransform, new Vector2(0.55f, 0.12f), new Vector2(0.955f, 0.72f), Vector2.zero, Vector2.zero);

        var objectiveHeading = CreateText("TXT_ObjectiveHeading", info.transform, "MỤC TIÊU NHIỆM VỤ",
            barlowBold, 27f, Ochre, TextAlignmentOptions.Left);
        SetRect(objectiveHeading.rectTransform, new Vector2(0.065f, 0.83f), new Vector2(0.94f, 0.96f), Vector2.zero, Vector2.zero);

        var objective = CreateText("TXT_Objective", info.transform,
            "Tình hình ngập lụt đang trở nên nghiêm trọng hơn và nhiều người dân vẫn đang chờ được cứu.\n\n" +
            "Hãy điều khiển thuyền cẩn thận, tính toán đường đi và thời điểm di chuyển để tiếp cận các điểm cứu hộ kịp thời.\n\n" +
            "Đưa đủ 4 người dân đến nơi an toàn trước khi hết thời gian, đồng thời bảo toàn độ bền của thuyền.",
            beVietnamRegular, 19f, OffWhite, TextAlignmentOptions.TopLeft);
        objective.enableWordWrapping = true;
        objective.lineSpacing = 5f;
        objective.paragraphSpacing = 6f;
        SetRect(objective.rectTransform, new Vector2(0.065f, 0.43f), new Vector2(0.94f, 0.85f), Vector2.zero, Vector2.zero);

        var infoDivider = CreatePanel("InfoDivider", info.transform, new Color(Ochre.r, Ochre.g, Ochre.b, 0.42f));
        SetRect(infoDivider.rectTransform, new Vector2(0.065f, 0.235f), new Vector2(0.94f, 0.24f), Vector2.zero, Vector2.zero);

        var noteSection = CreatePanel("MissionNote", info.transform, new Color(0.10f, 0.19f, 0.18f, 0.66f));
        SetRect(noteSection.rectTransform, new Vector2(0.065f, 0.255f), new Vector2(0.94f, 0.415f), Vector2.zero, Vector2.zero);

        var noteHeading = CreateText("TXT_NoteHeading", noteSection.transform,
            "LƯU Ý NHIỆM VỤ", barlowSemiBold, 19f, Ochre, TextAlignmentOptions.Left);
        SetRect(noteHeading.rectTransform, new Vector2(0.035f, 0.62f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);

        var noteBody = CreateText("TXT_NoteBody", noteSection.transform,
            "Round 2 không có giao diện bản đồ cứu hộ. Hãy quan sát môi trường và lập kế hoạch di chuyển cẩn thận.",
            beVietnamMedium, 17f, SecondaryText, TextAlignmentOptions.Left);
        noteBody.lineSpacing = 3f;
        SetRect(noteBody.rectTransform, new Vector2(0.035f, 0.06f), new Vector2(0.97f, 0.64f), Vector2.zero, Vector2.zero);

        var stats = CreateText("TXT_MissionStats", info.transform,
            "THỜI GIAN: 03:30\nMỤC TIÊU CỨU HỘ: 4 NGƯỜI\nĐỘ BỀN THUYỀN: 3\nĐỘ KHÓ: CAO HƠN ROUND 1",
            barlowSemiBold, 18.5f, OffWhite, TextAlignmentOptions.TopLeft);
        stats.lineSpacing = 5f;
        SetRect(stats.rectTransform, new Vector2(0.065f, 0.02f), new Vector2(0.48f, 0.205f), Vector2.zero, Vector2.zero);

        var strategyHeading = CreateText("TXT_StrategyHeading", info.transform,
            "CHIẾN THUẬT\nTÍNH TOÁN ĐƯỜNG ĐI\nVÀ THỜI ĐIỂM DI CHUYỂN",
            barlowSemiBold, 20.5f, Ochre, TextAlignmentOptions.TopLeft);
        strategyHeading.lineSpacing = 3f;
        SetRect(strategyHeading.rectTransform, new Vector2(0.50f, 0.085f), new Vector2(0.94f, 0.23f), Vector2.zero, Vector2.zero);

        var strategyBody = CreateText("TXT_StrategyBody", info.transform,
            "Có lúc bạn cần chờ và chọn đúng\nthời điểm để di chuyển.",
            beVietnamMedium, 17f, SecondaryText, TextAlignmentOptions.TopLeft);
        strategyBody.lineSpacing = 0f;
        SetRect(strategyBody.rectTransform, new Vector2(0.50f, 0f), new Vector2(0.94f, 0.085f), Vector2.zero, Vector2.zero);

        var startButton = CreateButton("BTN_StartRound2", panel.transform, "BẮT ĐẦU ROUND 2");
        SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.045f), new Vector2(0.955f, 0.11f), Vector2.zero, Vector2.zero);
        StyleButton(startButton, Rust, new Color(Ochre.r, Ochre.g, Ochre.b, 0.82f));

        var controllerObject = new GameObject("Round2MissionBriefingController", typeof(Round2MissionBriefingController));
        var controller = controllerObject.GetComponent<Round2MissionBriefingController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("startRound2Button").objectReferenceValue = startButton;
        serialized.FindProperty("round2GameplaySceneName").stringValue = "Round2_RealtimePrototype";
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, Round2BriefingScene);
    }

    private static void CreateBackdropBands(Transform parent)
    {
        var top = CreatePanel("BackdropTopBand", parent, new Color(0.16f, 0.28f, 0.29f, 0.22f));
        SetRect(top.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var bottom = CreatePanel("BackdropBottomBand", parent, new Color(0.07f, 0.17f, 0.20f, 0.65f));
        SetRect(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.14f), Vector2.zero, Vector2.zero);
    }

    private static void UpdateBuildScenes()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        AddUnique(scenes, Round2BriefingScene);
        AddUnique(scenes, Round2GameplayScene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddUnique(ICollection<EditorBuildSettingsScene> scenes, string path)
    {
        if (scenes.All(scene => scene.path != path))
            scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    private static Image CreatePanel(string name, Transform parent, Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        var image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, TMP_FontAsset font,
        float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        var text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableWordWrapping = true;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        var image = CreatePanel(name, parent, Rust);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Ochre.r, Ochre.g, Ochre.b, 0.82f);
        outline.effectDistance = new Vector2(1f, -1f);

        var text = CreateText("TXT_ButtonLabel", image.transform, label, barlowBold, 25f, OffWhite, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.characterSpacing = 1.1f;
        return button;
    }

    private static void StyleButton(Button button, Color normal, Color border)
    {
        var image = button.GetComponent<Image>();
        image.color = Color.white;
        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.55f);
        colors.fadeDuration = 0.15f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        var outline = button.GetComponent<Outline>();
        if (outline == null)
            outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        var outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            throw new MissingReferenceException("Required RectTransform was not found while building Mission Briefing UI.");

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        throw new MissingReferenceException($"Required Mission Briefing object '{name}' was not found.");
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var text = FindDescendant(root, name).GetComponent<TMP_Text>();
        if (text == null)
            throw new MissingReferenceException($"Mission Briefing object '{name}' does not contain TMP text.");
        return text;
    }

    private static void PrepareThumbnail(Transform thumbnailRoot, string label)
    {
        DestroyChild(thumbnailRoot, "ThumbnailDarkOverlay");
        DestroyChild(thumbnailRoot, "ThumbnailLabelBar");

        var overlay = CreatePanel("ThumbnailDarkOverlay", thumbnailRoot, new Color(0f, 0f, 0f, 0.14f));
        SetRect(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var labelBar = CreatePanel("ThumbnailLabelBar", thumbnailRoot, new Color(0.05f, 0.10f, 0.10f, 0.72f));
        SetRect(labelBar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);

        var labelText = CreateText("TXT_ThumbnailLabel", labelBar.transform, label,
            barlowSemiBold, 18f, OffWhite, TextAlignmentOptions.Center);
        labelText.characterSpacing = 0.8f;
        SetRect(labelText.rectTransform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero);
    }

    private static void DestroyChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }

    private static Color Hex(string value)
    {
        if (!ColorUtility.TryParseHtmlString(value, out var color))
            throw new System.ArgumentException("Invalid HTML color: " + value);
        return color;
    }
}
