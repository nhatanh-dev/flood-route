using System.Collections;
using System.Collections.Generic;
using Round1;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[UnityEngine.Scripting.Preserve]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string Round1SceneName = "Round1_FirstPersonPrototype";
    private const string Round2SceneName = "Round2_RealtimePrototype";
    // Palette matching MainMenu / Briefing / Endgame screens
    private static readonly Color OverlayColor     = new(0.047f, 0.125f, 0.118f, 0.88f); // dark teal, semi-transparent
    private static readonly Color PanelColor        = new(0.094f, 0.196f, 0.188f, 0.98f); // #183330
    private static readonly Color PanelBorderColor  = new(0.714f, 0.541f, 0.271f, 0.72f); // ochre border
    private static readonly Color OchreColor        = new(0.714f, 0.541f, 0.271f, 1f);    // #B68A45
    private static readonly Color RustColor         = new(0.522f, 0.278f, 0.2f,  1f);    // #854733
    private static readonly Color OffWhiteColor     = new(0.941f, 0.91f, 0.847f, 1f);    // #F0E8D8
    private static readonly Color MutedTextColor    = new(0.65f,  0.72f, 0.68f,  1f);

    [Header("Pause UI")]
    [SerializeField] private GameObject pauseVisualRoot;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform modalPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    [Header("Gameplay State")]
    [SerializeField] private R1RealtimeRoundController round1Controller;
    [SerializeField] private Round1IntroController round1Intro;
    [SerializeField] private Round2RealtimeRoundController round2Controller;
    [SerializeField] private Round2IntroController round2Intro;
    [SerializeField] private Behaviour[] gameplayInputBehaviours;

    [Header("Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField, Range(0.15f, 0.25f)] private float fadeDuration = 0.2f;

    private readonly List<Behaviour> blockedInputs = new();
    private readonly List<bool> blockedInputStates = new();
    private Coroutine transitionRoutine;
    private bool isPaused;
    private float previousTimeScale = 1f;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private Sprite generatedRoundedSprite;
    private Texture2D generatedRoundedTexture;

    public bool IsPaused => isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        if (sceneName == "MainMenu" || sceneName == "Round2_MissionBriefing")
        {
            return;
        }

        // Check if there is a boat controller or round controller in the scene (including inactive objects)
        bool isGameplayScene = FindAnyObjectByType<Round1FirstPersonBoatController>(FindObjectsInactive.Include) != null
            || FindAnyObjectByType<Round2.Round2FirstPersonBoatController>(FindObjectsInactive.Include) != null
            || FindAnyObjectByType<R1RealtimeRoundController>(FindObjectsInactive.Include) != null
            || FindAnyObjectByType<Round2RealtimeRoundController>(FindObjectsInactive.Include) != null;

        if (!isGameplayScene)
        {
            return;
        }

        if (FindAnyObjectByType<PauseMenuController>() != null)
        {
            return;
        }

        CreateRuntimePauseMenu();
    }

    private static void CreateRuntimePauseMenu()
    {
        GameObject system = CreateUiObject("PauseMenuSystem", null);
        system.SetActive(false);
        Stretch(system.GetComponent<RectTransform>());

        Canvas canvas = system.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = system.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        system.AddComponent<GraphicRaycaster>();

        PauseMenuController controller = system.AddComponent<PauseMenuController>();
        controller.generatedRoundedSprite = CreateRoundedSprite(out controller.generatedRoundedTexture);

        TMP_FontAsset barlowBold    = FindLoadedFont("BarlowCondensed_Bold") ?? TMP_Settings.defaultFontAsset;
        TMP_FontAsset beVietnamMed  = FindLoadedFont("BeVietnamPro_Medium")  ?? TMP_Settings.defaultFontAsset;

        // ── Full-screen visual root ────────────────────────────────────────────
        GameObject visualRoot = CreateUiObject("PauseVisualRoot", system.transform);
        Stretch(visualRoot.GetComponent<RectTransform>());
        CanvasGroup group = visualRoot.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // Dark overlay — same darkness as Briefing / Endgame overlay
        Image overlay = AddImage("IMG_PauseDim", visualRoot.transform, OverlayColor, null);
        Stretch(overlay.rectTransform);
        overlay.raycastTarget = true;

        // ── Centered card panel ───────────────────────────────────────────────
        // Size: narrower and taller to match Briefing card proportions
        Image modal = AddImage(
            "PNL_PauseModal", visualRoot.transform, PanelColor, controller.generatedRoundedSprite);
        modal.type = Image.Type.Sliced;
        SetRect(modal.rectTransform, new Vector2(520f, 440f), Vector2.zero);
        AddOutline(modal.gameObject, new Color(PanelBorderColor.r, PanelBorderColor.g, PanelBorderColor.b, 0.7f), new Vector2(1.5f, -1.5f));
        AddShadow(modal.gameObject, new Color(0f, 0f, 0f, 0.65f), new Vector2(8f, -8f));

        // Ochre top accent bar (thin line, same as Briefing header underline)
        GameObject accentGo = CreateUiObject("IMG_PauseAccent", modal.transform);
        RectTransform accentRT = accentGo.GetComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0.1f, 1f);
        accentRT.anchorMax = new Vector2(0.9f, 1f);
        accentRT.pivot     = new Vector2(0.5f, 1f);
        accentRT.offsetMin = new Vector2(0f, -4f);
        accentRT.offsetMax = Vector2.zero;
        Image accentImg = accentGo.AddComponent<Image>();
        accentImg.color  = OchreColor;

        // ── Title — italic-style Barlow, ochre tint, large ───────────────────
        TMP_Text title = AddText("TXT_PauseTitle", modal.transform, "TẠM DỪNG", barlowBold, 56f, OchreColor);
        SetRect(title.rectTransform, new Vector2(480f, 80f), new Vector2(0f, 148f));
        title.alignment       = TextAlignmentOptions.Center;
        title.characterSpacing = 3f;
        title.fontStyle       = FontStyles.Italic;
        AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(3f, -3f));

        // Divider line under title
        GameObject divGo = CreateUiObject("IMG_PauseDivider", modal.transform);
        RectTransform divRT = divGo.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.15f, 0.5f);
        divRT.anchorMax = new Vector2(0.85f, 0.5f);
        divRT.pivot     = new Vector2(0.5f, 0.5f);
        divRT.sizeDelta = new Vector2(0f, 1f);
        divRT.anchoredPosition = new Vector2(0f, 100f);
        Image divImg = divGo.AddComponent<Image>();
        divImg.color = new Color(OchreColor.r, OchreColor.g, OchreColor.b, 0.35f);

        // ── Buttons — outlined style matching MainMenu ────────────────────────
        // Continue: rust/primary fill
        Button continueButton = AddButton(
            "BTN_PauseContinue", modal.transform, "TIẾP TỤC", RustColor, 0.35f,
            barlowBold, controller.generatedRoundedSprite);
        RectTransform contRT = continueButton.GetComponent<RectTransform>();
        SetRect(contRT, new Vector2(380f, 58f), new Vector2(0f, 28f));
        AddOutline(continueButton.gameObject, new Color(RustColor.r, RustColor.g, RustColor.b, 0.6f), new Vector2(1.5f, -1.5f));

        // Restart: dark panel, ochre outline
        Button restartButton = AddButton(
            "BTN_PauseRestart", modal.transform, "CHƠI LẠI", PanelColor, 0.18f,
            barlowBold, controller.generatedRoundedSprite);
        RectTransform restRT = restartButton.GetComponent<RectTransform>();
        SetRect(restRT, new Vector2(380f, 58f), new Vector2(0f, -48f));
        AddOutline(restartButton.gameObject, new Color(OchreColor.r, OchreColor.g, OchreColor.b, 0.55f), new Vector2(1.5f, -1.5f));
        // Change restart button text color to ochre
        var restartLabel = restartButton.GetComponentInChildren<TMP_Text>();
        if (restartLabel != null) restartLabel.color = OchreColor;

        // Menu: same style as restart
        Button menuButton = AddButton(
            "BTN_PauseMenu", modal.transform, "THOÁT VỀ MENU", PanelColor, 0.18f,
            barlowBold, controller.generatedRoundedSprite);
        RectTransform menuRT = menuButton.GetComponent<RectTransform>();
        SetRect(menuRT, new Vector2(380f, 58f), new Vector2(0f, -124f));
        AddOutline(menuButton.gameObject, new Color(OchreColor.r, OchreColor.g, OchreColor.b, 0.35f), new Vector2(1.5f, -1.5f));
        var menuLabel = menuButton.GetComponentInChildren<TMP_Text>();
        if (menuLabel != null) menuLabel.color = new Color(OffWhiteColor.r, OffWhiteColor.g, OffWhiteColor.b, 0.7f);

        // ── Footer hint ───────────────────────────────────────────────────────
        TMP_Text footer = AddText(
            "TXT_PauseFooter", modal.transform, "[P] — tiếp tục chơi",
            beVietnamMed, 13f, MutedTextColor);
        SetRect(footer.rectTransform, new Vector2(480f, 28f), new Vector2(0f, -192f));
        footer.alignment       = TextAlignmentOptions.Center;
        footer.characterSpacing = 0.8f;
        footer.fontStyle       = FontStyles.Italic;

        controller.pauseVisualRoot  = visualRoot;
        controller.pauseCanvasGroup = group;
        controller.modalPanel       = modal.rectTransform;
        controller.continueButton   = continueButton;
        controller.restartButton    = restartButton;
        controller.menuButton       = menuButton;
        visualRoot.SetActive(false);
        system.SetActive(true);
    }

    private static TMP_FontAsset FindLoadedFont(string namePart)
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font != null && font.name.Contains(namePart))
            {
                return font;
            }
        }

        return null;
    }

    private static Sprite CreateRoundedSprite(out Texture2D texture)
    {
        const int size = 64;
        const float radius = 14f;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "PauseMenuRoundedRect",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0f, x - (size - 1f - radius));
                float dy = Mathf.Max(radius - y, 0f, y - (size - 1f - radius));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(16f, 16f, 16f, 16f));
        sprite.name = "PauseMenuRoundedRect";
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private static Button AddButton(
        string name,
        Transform parent,
        string label,
        Color normalColor,
        float borderAlpha,
        TMP_FontAsset font,
        Sprite roundedSprite)
    {
        Image image = AddImage(name, parent, Color.white, roundedSprite);
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.55f);
        colors.fadeDuration = 0.15f;
        button.colors = colors;

        AddOutline(button.gameObject, new Color(OchreColor.r, OchreColor.g, OchreColor.b, borderAlpha), new Vector2(1f, -1f));
        AddShadow(button.gameObject, new Color(0f, 0f, 0f, 0.32f), new Vector2(3f, -3f));

        TMP_Text text = AddText(
            "TXT_ButtonLabel", image.transform, label, font, 26f, OffWhiteColor);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 1.1f;
        text.raycastTarget = false;
        return button;
    }

    private static Image AddImage(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    private static TMP_Text AddText(
        string name,
        Transform parent,
        string value,
        TMP_FontAsset font,
        float fontSize,
        Color color)
    {
        GameObject go = CreateUiObject(name, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        SetVisualState(false);
    }

    private void Update()
    {
        if (isPaused && !CanPause())
        {
            ResumeImmediately();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.pKey.wasPressedThisFrame || IsTypingInInputField())
        {
            return;
        }

        // Dismiss any active intro first (same press), then immediately pause
        TryDismissIntro();

        if (isPaused)
        {
            ResumeGame();
        }
        else if (CanPause())
        {
            PauseGame();
        }
    }

    /// <summary>Dismisses any active intro panel. Returns true if an intro was dismissed.</summary>
    private bool TryDismissIntro()
    {
        if (round1Intro != null && round1Intro.IsIntroActive)
        {
            round1Intro.CompleteIntro();
            return true;
        }

        if (round2Intro != null && round2Intro.IsIntroActive)
        {
            round2Intro.HideIntro();
            return true;
        }

        return false;
    }

    public void PauseGame()
    {
        if (isPaused || !CanPause())
        {
            return;
        }

        isPaused = true;
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        BlockGameplayInput();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartTransition(show: true);
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        RestoreGameplayState();
        StartTransition(show: false);
    }

    public void RestartCurrentScene()
    {
        PrepareForSceneLoad(restoreGameplayCursor: true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("[PauseMenu] Main Menu scene name is not configured.");
            return;
        }

        PrepareForSceneLoad(restoreGameplayCursor: false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private bool CanPause()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            return false;
        }

        if (round1Controller != null)
        {
            return !round1Controller.IsGameOver
                && (round1Intro == null || !round1Intro.IsIntroActive);
        }

        if (round2Controller != null)
        {
            return round2Controller.IsPlaying()
                && (round2Intro == null || !round2Intro.IsIntroActive);
        }

        // If no round controllers are present (e.g. custom sandbox or testing scene), allow pausing by default
        return true;
    }

    private void ResolveReferences()
    {
        round1Controller ??= FindAnyObjectByType<R1RealtimeRoundController>(
            FindObjectsInactive.Include);
        round1Intro ??= FindAnyObjectByType<Round1IntroController>(
            FindObjectsInactive.Include);
        round2Controller ??= FindAnyObjectByType<Round2RealtimeRoundController>(
            FindObjectsInactive.Include);
        round2Intro ??= FindAnyObjectByType<Round2IntroController>(
            FindObjectsInactive.Include);

        if (gameplayInputBehaviours == null || gameplayInputBehaviours.Length == 0)
        {
            gameplayInputBehaviours = FindGameplayInputBehaviours();
        }
    }

    private static Behaviour[] FindGameplayInputBehaviours()
    {
        var inputTypeNames = new HashSet<string>
        {
            "Round1FirstPersonBoatController",
            "Round1FirstPersonInteraction",
            "R1PlanningMapController",
            "R1OrientationMapView",
            "Round2FirstPersonBoatController",
            "Round2BoatInteraction",
            "Round2PlanningMapController"
        };

        var results = new List<Behaviour>();
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && inputTypeNames.Contains(behaviour.GetType().Name))
            {
                results.Add(behaviour);
            }
        }

        return results.ToArray();
    }

    private void BindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
            continueButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartCurrentScene);
            restartButton.onClick.AddListener(RestartCurrentScene);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(ReturnToMainMenu);
            menuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void BlockGameplayInput()
    {
        blockedInputs.Clear();
        blockedInputStates.Clear();

        if (gameplayInputBehaviours == null)
        {
            return;
        }

        foreach (Behaviour behaviour in gameplayInputBehaviours)
        {
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            blockedInputs.Add(behaviour);
            blockedInputStates.Add(behaviour.enabled);
            behaviour.enabled = false;
        }
    }

    private void RestoreGameplayState()
    {
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;

        for (int i = 0; i < blockedInputs.Count; i++)
        {
            if (blockedInputs[i] != null)
            {
                blockedInputs[i].enabled = blockedInputStates[i];
            }
        }

        blockedInputs.Clear();
        blockedInputStates.Clear();
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void PrepareForSceneLoad(bool restoreGameplayCursor)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (isPaused)
        {
            isPaused = false;
            // Clear blocked inputs reference lists without re-enabling the components.
            // This prevents them from running Update() on the frame the scene unloads.
            blockedInputs.Clear();
            blockedInputStates.Clear();
        }

        Time.timeScale = 1f;
        SetVisualState(false);

        if (!restoreGameplayCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ResumeImmediately()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        RestoreGameplayState();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        SetVisualState(false);
    }

    private void StartTransition(bool show)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(AnimatePauseVisual(show));
    }

    private IEnumerator AnimatePauseVisual(bool show)
    {
        if (pauseVisualRoot == null || pauseCanvasGroup == null)
        {
            transitionRoutine = null;
            yield break;
        }

        pauseVisualRoot.SetActive(true);
        pauseCanvasGroup.interactable = show;
        pauseCanvasGroup.blocksRaycasts = show;

        float startAlpha = pauseCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;
        Vector3 startScale = modalPanel != null ? modalPanel.localScale : Vector3.one;
        Vector3 targetScale = show ? Vector3.one : Vector3.one * 0.96f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
            pauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            if (modalPanel != null)
            {
                modalPanel.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            yield return null;
        }

        pauseCanvasGroup.alpha = targetAlpha;
        if (modalPanel != null)
        {
            modalPanel.localScale = targetScale;
        }

        if (!show)
        {
            pauseVisualRoot.SetActive(false);
        }

        transitionRoutine = null;
    }

    private void SetVisualState(bool visible)
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = visible ? 1f : 0f;
            pauseCanvasGroup.interactable = visible;
            pauseCanvasGroup.blocksRaycasts = visible;
        }

        if (modalPanel != null)
        {
            modalPanel.localScale = visible ? Vector3.one : Vector3.one * 0.96f;
        }

        if (pauseVisualRoot != null)
        {
            pauseVisualRoot.SetActive(visible);
        }
    }

    private static bool IsTypingInInputField()
    {
        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;

        return selected != null
            && (selected.GetComponent<InputField>() != null
                || selected.GetComponent<TMP_InputField>() != null);
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            ResumeImmediately();
        }
        else if (Time.timeScale == 0f && SceneManager.GetActiveScene().isLoaded)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartCurrentScene);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(ReturnToMainMenu);
        }

        if (isPaused)
        {
            RestoreGameplayState();
            isPaused = false;
        }

        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        if (generatedRoundedSprite != null)
        {
            Destroy(generatedRoundedSprite);
        }

        if (generatedRoundedTexture != null)
        {
            Destroy(generatedRoundedTexture);
        }
    }
}
