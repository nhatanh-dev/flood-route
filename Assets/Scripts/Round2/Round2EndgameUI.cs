using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Round2EndgameUI : MonoBehaviour
{
    [Header("References")]
    public Round2RealtimeRoundController roundController;
    public GameObject overlayPanel;
    public GameObject resultCard;
    public Image outcomeAccent;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtSubtitle;
    public TextMeshProUGUI txtMessage;
    public TextMeshProUGUI txtStats;
    public Button btnRetry;
    public TextMeshProUGUI txtRetryText;
    public TextMeshProUGUI txtRetryHint;

    [Header("Campaign Ending")]
    public Button btnViewCampaignEnding;
    public GameObject campaignEndingPanel;
    public Button btnCampaignMenu;
    public Button btnCampaignRetry;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Summary Panel Transition")]
    [SerializeField, Min(0.1f)] private float victoryHudFadeDuration = 0.4f;
    [SerializeField, Range(0.6f, 0.65f)] private float resultCardFadeDuration = 0.65f;
    [SerializeField, Range(0.65f, 0.75f)] private float summaryPanelFadeDuration = 0.7f;

    [Header("Summary Content Timing")]
    [SerializeField, Min(0f)] private float titleRevealStart = 0.55f;
    [SerializeField, Min(0.1f)] private float titleRevealDuration = 0.55f;
    [SerializeField, Min(0f)] private float statsRevealStart = 1.35f;
    [FormerlySerializedAs("resultRevealDuration")]
    [SerializeField, Min(0.1f)] private float statsRevealDuration = 0.4f;
    [FormerlySerializedAs("resultRevealGap")]
    [Tooltip("Seconds between the start of each statistic reveal.")]
    [SerializeField, Min(0f)] private float statsStagger = 0.35f;
    [SerializeField, Min(0f)] private float messageRevealStart = 2.7f;
    [SerializeField, Min(0.1f)] private float closingRevealDuration = 0.7f;
    [SerializeField, Min(0f)] private float buttonsRevealStart = 3.8f;
    [SerializeField, Min(0.1f)] private float buttonRevealDuration = 0.6f;

    [Header("Summary Motion")]
    [SerializeField, Range(0.9f, 1f)] private float titleStartScale = 0.96f;
    [SerializeField, Range(0f, 24f)] private float contentVerticalOffset = 12f;

    [Header("Victory Audio")]
    [SerializeField] private AudioSource rainAmbienceSource;
    [SerializeField] private AudioSource thunderAmbienceSource;
    [SerializeField] private RandomAmbientAudio thunderAmbientPlayer;
    [Tooltip("Multiplier applied to the cached gameplay rain/storm volume on victory.")]
    [SerializeField, Range(0.2f, 0.25f)] private float victoryAmbienceWaitingVolumeScale = 0.22f;
    [SerializeField, Range(0.7f, 1f)] private float victoryAmbienceFadeDuration = 0.8f;

    [Header("Summary Audio Transition")]
    [SerializeField, Range(1.2f, 1.6f)] private float summaryAmbienceFadeDuration = 1.4f;
    [SerializeField] private AudioSource summaryMusicSource;
    [SerializeField] private AudioClip summaryMusicClip;
    [FormerlySerializedAs("summaryMusicStartDelay")]
    [Tooltip("Delay before rain, music volume, and summary background begin fading.")]
    [SerializeField, Range(0f, 0.3f)] private float summaryCrossfadeDelay = 0.15f;
    [SerializeField, Range(2.8f, 3f)] private float summaryMusicFadeDuration = 2.9f;
    [SerializeField, Range(0f, 1f)] private float summaryMusicTargetVolume = 0.6f;

    [Header("Campaign Totals")]
    [SerializeField, Min(0)] private int round1CivilianTotal = 3;
    [SerializeField, Min(1)] private int campaignStageTotal = 2;

    [Header("Optional")]
    public GameObject gameplayHUDCanvas;
    [SerializeField] private GameObject persistentGameplayHudRoot;
    [SerializeField] private GameObject gameplayRainOverlay;
    [SerializeField] private UIAudioPlayer uiAudioPlayer;

    private bool overlayActive = false;
    private bool victoryPresentationStarted;
    private bool endingSequenceStarted;
    private bool endingSequenceComplete;
    private bool victorySoundPlayed;
    private Coroutine gameplayHudFadeRoutine;
    private Coroutine endingSequenceRoutine;
    private Coroutine victoryAmbienceFadeRoutine;
    private Coroutine summaryMusicFadeRoutine;
    private CanvasGroup gameplayHudGroup;
    private CanvasGroup resultCardGroup;
    private CanvasGroup campaignPanelGroup;
    private CanvasGroup[] titleGroups;
    private CanvasGroup closingGroup;
    private CanvasGroup[] closingGroups;
    private CanvasGroup[] buttonGroups;
    private readonly List<CanvasGroup[]> campaignStatGroups = new List<CanvasGroup[]>();
    private readonly Dictionary<RectTransform, Vector2> revealBasePositions = new Dictionary<RectTransform, Vector2>();
    private Image overlayImage;
    private RectTransform campaignTitleRect;
    private Vector3 campaignTitleBaseScale = Vector3.one;
    private float rainOriginalVolume;
    private float thunderOriginalVolume;
    private float thunderAmbientOriginalVolume;
    private bool rainWasPlaying;
    private bool thunderWasPlaying;
    private bool ambienceStateCached;
    private bool ambienceMutedForVictory;
    private bool suppressAmbienceRestoreOnDestroy;
    private bool gameplayHudInitialActive;
    private bool gameplayRainOverlayInitialActive;
    private AudioClip summaryMusicOriginalClip;
    private float summaryMusicOriginalVolume;
    private bool summaryMusicStateCached;
    private bool summaryMusicStarted;
    private static readonly Color VictoryAccent = new Color(0.35f, 0.50f, 0.41f, 1f); // #5A8069 (slightly darker victory accent)
    private static readonly Color DefeatAccent = new Color(0.55f, 0.27f, 0.23f, 1f); // #8C453A (slightly darker defeat accent)
    private static readonly Color VictoryTitle = new Color(0.47f, 0.66f, 0.54f, 1f); // #78A88B (brightened muted sage green)
    private static readonly Color DefeatTitle = new Color(0.72f, 0.36f, 0.30f, 1f); // #B85B4D (brightened muted terracotta red)
    private static readonly Color OverlayColor = new Color(0.015f, 0.045f, 0.045f, 0.64f);
    private static readonly Color PrimaryNormal = new Color(0.7176471f, 0.4745098f, 0.1529412f, 1f);
    private static readonly Color PrimaryHover = new Color(0.8117647f, 0.5686275f, 0.2196078f, 1f);
    private static readonly Color PrimaryPressed = new Color(0.5725490f, 0.3607843f, 0.1137255f, 1f);
    private static readonly Color SecondaryNormal = new Color(0.1607843f, 0.2784314f, 0.2941177f, 1f);
    private static readonly Color SecondaryHover = new Color(0.2117647f, 0.3568628f, 0.3764706f, 1f);
    private static readonly Color SecondaryPressed = new Color(0.1254902f, 0.2196078f, 0.2352941f, 1f);

    private void Start()
    {
        if (roundController == null)
        {
            roundController = FindObjectOfType<Round2RealtimeRoundController>();
        }

        if (gameplayHUDCanvas == null)
        {
            gameplayHUDCanvas = GameObject.Find("R2_RealtimeGameplayHUD_Canvas");
        }

        if (persistentGameplayHudRoot == null && gameplayHUDCanvas != null)
        {
            Transform persistentHud = gameplayHUDCanvas.transform.Find("PNL_R2_HUD_TopLeft");
            if (persistentHud != null)
                persistentGameplayHudRoot = persistentHud.gameObject;
        }

        if (persistentGameplayHudRoot == null)
            Debug.LogError("[Round2EndgameUI] Persistent HUD root PNL_R2_HUD_TopLeft was not found.");

        if (gameplayRainOverlay == null)
            gameplayRainOverlay = GameObject.Find("Canvas_RainOverlay");

        gameplayHudInitialActive = gameplayHUDCanvas != null && gameplayHUDCanvas.activeSelf;
        gameplayRainOverlayInitialActive =
            gameplayRainOverlay != null && gameplayRainOverlay.activeSelf;

        ResolveAndCacheVictoryAudio();
        CacheSummaryMusicState();

        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }

        if (campaignEndingPanel != null)
        {
            campaignEndingPanel.SetActive(false);
        }

        if (btnRetry != null)
        {
            btnRetry.onClick.RemoveListener(OnRetryButtonClicked);
            btnRetry.onClick.AddListener(OnRetryButtonClicked);
        }

        if (btnViewCampaignEnding != null)
        {
            btnViewCampaignEnding.onClick.RemoveListener(OnViewCampaignEndingClicked);
            btnViewCampaignEnding.onClick.AddListener(OnViewCampaignEndingClicked);
        }

        if (btnCampaignMenu != null)
        {
            btnCampaignMenu.onClick.RemoveListener(OnCampaignMenuClicked);
            btnCampaignMenu.onClick.AddListener(OnCampaignMenuClicked);
        }

        if (btnCampaignRetry != null)
        {
            btnCampaignRetry.onClick.RemoveListener(OnRetryButtonClicked);
            btnCampaignRetry.onClick.AddListener(OnRetryButtonClicked);
        }

        ApplyButtonRole(btnViewCampaignEnding, true);
        ApplyButtonRole(btnCampaignMenu, true);
        ApplyButtonRole(btnCampaignRetry, false);

        CacheCampaignEndingReferences();
        InitializeEndingPresentation();
    }

    private void Update()
    {
        if (roundController == null) return;

        if (!overlayActive && !roundController.IsPlaying())
        {
            ShowOverlay();
        }

        if (overlayActive && (!endingSequenceStarted || endingSequenceComplete))
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartCurrentScene();
            }
        }
    }

    private void ShowOverlay()
    {
        if (overlayActive)
            return;

        bool isWin = roundController.currentState == Round2GameState.Win;
        overlayActive = true;

        if (!isWin && gameplayHUDCanvas != null)
        {
            gameplayHUDCanvas.SetActive(false);
        }

        if (overlayPanel != null)
        {
            Image bg = overlayPanel.GetComponent<Image>();
            if (bg != null) bg.color = OverlayColor;
            overlayPanel.SetActive(true);
        }

        if (resultCard != null)
        {
            resultCard.SetActive(true);
            SetCanvasGroupState(resultCardGroup, 1f, true);
        }

        if (campaignEndingPanel != null)
        {
            campaignEndingPanel.SetActive(false);
        }

        if (isWin && !victorySoundPlayed)
        {
            victorySoundPlayed = true;
            uiAudioPlayer?.PlayVictory();
            StartVictoryAmbienceFade();
        }
        else if (!isWin)
        {
            uiAudioPlayer?.PlayDefeat();
        }

        if (btnViewCampaignEnding != null)
        {
            btnViewCampaignEnding.gameObject.SetActive(isWin);
            btnViewCampaignEnding.interactable = isWin;
            RectTransform viewEndingRect = btnViewCampaignEnding.GetComponent<RectTransform>();
            if (viewEndingRect != null)
                viewEndingRect.anchoredPosition = new Vector2(150f, -190f);
        }
        if (btnRetry != null)
        {
            btnRetry.interactable = true;
            RectTransform retryRect = btnRetry.GetComponent<RectTransform>();
            if (retryRect != null)
                retryRect.anchoredPosition = new Vector2(isWin ? -150f : 0f, -190f);
        }

        if (isWin)
        {
            if (txtTitle != null)
            {
                txtTitle.text = "HOÀN THÀNH";
                txtTitle.color = VictoryTitle;
                if (txtTitle.fontMaterial != null)
                {
                    txtTitle.fontMaterial.SetColor("_GlowColor", new Color(0f, 0f, 0f, 0.6f)); // Dark underlay/shadow instead of glow
                }
            }
            if (txtSubtitle != null)
            {
                txtSubtitle.text = "Bạn đã đưa tất cả người dân đến nơi an toàn.";
                txtSubtitle.color = new Color(0.97f, 0.95f, 0.92f, 1f); // Slightly brightened warm off-white
            }
            if (txtMessage != null)
            {
                txtMessage.text = $"Cứu hộ thành công: {roundController.civiliansSafe}/{roundController.totalCivilians} người";
                txtMessage.fontSize = 21f;
                txtMessage.color = new Color(0.97f, 0.95f, 0.92f, 1f); // Slightly brightened warm off-white
            }
            if (txtRetryText != null) txtRetryText.text = "CHƠI LẠI ROUND 2";
            ApplyOutcomeAccent(VictoryAccent);

            victoryPresentationStarted = true;
            EnsureGameplayHudFadeGroup();
            gameplayHudFadeRoutine = StartCoroutine(FadeGameplayHudForVictory());
        }
        else // Fail
        {
            if (txtTitle != null)
            {
                txtTitle.text = "THẤT BẠI";
                txtTitle.color = DefeatTitle;
                if (txtTitle.fontMaterial != null)
                {
                    txtTitle.fontMaterial.SetColor("_GlowColor", new Color(0f, 0f, 0f, 0.6f)); // Dark underlay/shadow instead of glow
                }
            }
            if (txtRetryText != null) txtRetryText.text = "CHƠI LẠI";
            ApplyOutcomeAccent(DefeatAccent);

            string code = roundController.LastFailReason;
            if (code == "boat_broken")
            {
                if (txtSubtitle != null)
                {
                    txtSubtitle.text = "Thuyền bị hỏng!";
                    txtSubtitle.color = new Color(0.97f, 0.95f, 0.92f, 1f);
                }
                if (txtMessage != null)
                {
                    txtMessage.text = "Thuyền đã va chạm quá nhiều trong quá trình cứu hộ.";
                    txtMessage.fontSize = 18f;
                    txtMessage.color = new Color(0.80f, 0.83f, 0.81f, 1f); // Slightly brightened gray-green
                }
            }
            else if (code == "time_out")
            {
                if (txtSubtitle != null)
                {
                    txtSubtitle.text = "Hết thời gian!";
                    txtSubtitle.color = new Color(0.97f, 0.95f, 0.92f, 1f);
                }
                if (txtMessage != null)
                {
                    txtMessage.text = "Bạn đã không hoàn thành nhiệm vụ trước khi hết thời gian.";
                    txtMessage.fontSize = 18f;
                    txtMessage.color = new Color(0.80f, 0.83f, 0.81f, 1f); // Slightly brightened gray-green
                }
            }
            else
            {
                if (txtSubtitle != null)
                {
                    txtSubtitle.text = "Nhiệm vụ chưa hoàn thành.";
                    txtSubtitle.color = new Color(0.97f, 0.95f, 0.92f, 1f);
                }
                if (txtMessage != null)
                {
                    txtMessage.text = "Nhiệm vụ thất bại.";
                    txtMessage.fontSize = 18f;
                    txtMessage.color = new Color(0.80f, 0.83f, 0.81f, 1f); // Slightly brightened gray-green
                }
            }
        }

        if (txtRetryHint != null)
            txtRetryHint.text = "Nhấn R để chơi lại";

        if (txtStats != null)
        {
            int m = Mathf.FloorToInt(roundController.CurrentTimeRemaining / 60f);
            int s = Mathf.FloorToInt(roundController.CurrentTimeRemaining % 60f);
            string timeStr = $"{m:00}:{s:00}";

            txtStats.text =
                $"{roundController.civiliansSafe}/{roundController.totalCivilians}\n" +
                $"{roundController.currentBoatDurability}/{roundController.maxBoatDurability}\n" +
                timeStr;
        }
    }

    private void ApplyOutcomeAccent(Color color)
    {
        if (outcomeAccent != null)
            outcomeAccent.color = color;
    }

    private void OnRetryButtonClicked()
    {
        uiAudioPlayer?.PlayClick();
        RestartCurrentScene();
    }

    private void RestartCurrentScene()
    {
        ResetEndingPresentation(true);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnViewCampaignEndingClicked()
    {
        if (roundController == null || roundController.currentState != Round2GameState.Win ||
            !victoryPresentationStarted || endingSequenceStarted)
            return;

        if (btnViewCampaignEnding != null)
            btnViewCampaignEnding.interactable = false;

        uiAudioPlayer?.PlayClick();
        StartCampaignEndingSequence();
    }

    private void OnCampaignMenuClicked()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("[Round2EndgameUI] Main Menu scene name is not configured.");
            return;
        }

        uiAudioPlayer?.PlayClick();
        suppressAmbienceRestoreOnDestroy = true;
        ResetEndingPresentation(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        if (btnRetry != null)
            btnRetry.onClick.RemoveListener(OnRetryButtonClicked);

        if (btnViewCampaignEnding != null)
            btnViewCampaignEnding.onClick.RemoveListener(OnViewCampaignEndingClicked);

        if (btnCampaignMenu != null)
            btnCampaignMenu.onClick.RemoveListener(OnCampaignMenuClicked);

        if (btnCampaignRetry != null)
            btnCampaignRetry.onClick.RemoveListener(OnRetryButtonClicked);

        StopSummaryMusic();
        if (!suppressAmbienceRestoreOnDestroy)
            RestoreVictoryAmbience();
    }

    private void StartCampaignEndingSequence()
    {
        if (endingSequenceStarted || roundController == null ||
            roundController.currentState != Round2GameState.Win ||
            !victoryPresentationStarted)
        {
            return;
        }

        endingSequenceStarted = true;
        endingSequenceComplete = false;
        overlayActive = true;
        StartSummaryAmbienceFadeToSilence();
        StartSummaryMusicTransitionFade();
        HideGameplayPresentationForCampaignSummary();

        if (endingSequenceRoutine != null)
            StopCoroutine(endingSequenceRoutine);

        endingSequenceRoutine = StartCoroutine(CampaignEndingSequence());
    }

    private IEnumerator CampaignEndingSequence()
    {
        CacheCampaignEndingReferences();
        PrepareCampaignValues();
        PrepareCampaignRevealState();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (campaignEndingPanel != null)
            campaignEndingPanel.SetActive(true);

        if (resultCardGroup != null)
        {
            resultCardGroup.interactable = false;
            resultCardGroup.blocksRaycasts = false;
        }

        float sequenceDuration = buttonsRevealStart + buttonRevealDuration;
        float elapsed = 0f;
        while (elapsed < sequenceDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float resultProgress = GetRevealProgress(
                elapsed, 0f, resultCardFadeDuration);
            if (resultCardGroup != null)
                resultCardGroup.alpha = 1f - resultProgress;
            if (resultCard != null && resultProgress >= 1f && resultCard.activeSelf)
                resultCard.SetActive(false);

            float panelProgress = GetRevealProgress(
                elapsed, summaryCrossfadeDelay, summaryPanelFadeDuration);
            if (campaignPanelGroup != null)
                campaignPanelGroup.alpha = panelProgress;

            float titleProgress = GetRevealProgress(
                elapsed, titleRevealStart, titleRevealDuration);
            SetRevealProgress(titleGroups, titleProgress, false, 0f);
            if (campaignTitleRect != null)
            {
                campaignTitleRect.localScale = Vector3.Lerp(
                    campaignTitleBaseScale * titleStartScale,
                    campaignTitleBaseScale,
                    titleProgress);
            }

            for (int i = 0; i < campaignStatGroups.Count; i++)
            {
                float statProgress = GetRevealProgress(
                    elapsed,
                    statsRevealStart + statsStagger * i,
                    statsRevealDuration);
                SetRevealProgress(
                    campaignStatGroups[i],
                    statProgress,
                    true,
                    contentVerticalOffset);
            }

            float messageProgress = GetRevealProgress(
                elapsed, messageRevealStart, closingRevealDuration);
            SetRevealProgress(
                closingGroups,
                messageProgress,
                true,
                contentVerticalOffset);

            float buttonsProgress = GetRevealProgress(
                elapsed, buttonsRevealStart, buttonRevealDuration);
            SetRevealProgress(
                buttonGroups,
                buttonsProgress,
                true,
                contentVerticalOffset * 0.5f);

            yield return null;
        }

        SetCanvasGroupState(campaignPanelGroup, 1f, true);
        SetRevealProgress(titleGroups, 1f, false, 0f);
        for (int i = 0; i < campaignStatGroups.Count; i++)
            SetRevealProgress(campaignStatGroups[i], 1f, true, contentVerticalOffset);
        SetRevealProgress(
            closingGroups, 1f, true, contentVerticalOffset);
        SetRevealProgress(
            buttonGroups, 1f, true, contentVerticalOffset * 0.5f);
        if (campaignTitleRect != null)
            campaignTitleRect.localScale = campaignTitleBaseScale;
        SetCampaignButtonsInteractable(true);
        endingSequenceComplete = true;
        endingSequenceRoutine = null;
    }

    private void CacheCampaignEndingReferences()
    {
        if (overlayPanel != null)
            overlayImage = overlayPanel.GetComponent<Image>();

        if (resultCard != null)
            resultCardGroup = GetOrAddCanvasGroup(resultCard);

        if (campaignEndingPanel == null)
            return;

        campaignPanelGroup = GetOrAddCanvasGroup(campaignEndingPanel);

        TMP_Text heading = FindCampaignText("TXT_R2_CampaignHeading");
        TMP_Text title = FindCampaignText("TXT_R2_CampaignTitle");
        TMP_Text message = FindCampaignText("TXT_R2_CampaignMessage");
        TMP_Text closing = FindCampaignText("TXT_R2_CampaignClosing");

        if (heading != null)
            heading.text = "CHIẾN DỊCH CỨU HỘ";

        if (title != null)
        {
            title.text = "NHIỆM VỤ CỨU HỘ ĐÃ HOÀN THÀNH";
            title.enableAutoSizing = true;
            title.fontSizeMin = 42f;
            title.fontSizeMax = 62f;
            campaignTitleRect = title.rectTransform;
            campaignTitleBaseScale = campaignTitleRect.localScale;
        }

        if (message != null)
            message.text = "Tất cả mục tiêu cứu hộ đã được hoàn thành.";

        if (closing != null)
        {
            closing.text = "Giữa dòng nước dữ, mọi người đã được đưa đến nơi an toàn.";
            closingGroup = GetOrAddCanvasGroup(closing.gameObject);
        }
        closingGroups = closingGroup != null
            ? new[] { closingGroup }
            : System.Array.Empty<CanvasGroup>();

        titleGroups = CreateGroups(
            FindCampaignObject("IMG_R2_CampaignTopAccent"),
            heading != null ? heading.gameObject : null,
            FindCampaignObject("IMG_R2_CampaignDivider"),
            title != null ? title.gameObject : null,
            message != null ? message.gameObject : null);

        campaignStatGroups.Clear();
        campaignStatGroups.Add(CreateGroups(
            FindCampaignObject("TXT_R2_SummaryValue_Rescued"),
            FindCampaignObject("TXT_R2_SummaryLabel_Rescued")));
        campaignStatGroups.Add(CreateGroups(
            FindCampaignObject("TXT_R2_SummaryValue_Stages"),
            FindCampaignObject("TXT_R2_SummaryLabel_Stages")));
        campaignStatGroups.Add(CreateGroups(
            FindCampaignObject("TXT_R2_SummaryValue_Status"),
            FindCampaignObject("TXT_R2_SummaryLabel_Status")));

        buttonGroups = CreateGroups(
            btnCampaignMenu != null ? btnCampaignMenu.gameObject : null,
            btnCampaignRetry != null ? btnCampaignRetry.gameObject : null);

        ConfigureCampaignSummaryText();
    }

    private void PrepareCampaignValues()
    {
        int round2Total = roundController != null ? roundController.totalCivilians : 0;
        int campaignCivilianTotal = round1CivilianTotal + round2Total;

        SetCampaignText("TXT_R2_SummaryValue_Rescued",
            $"{campaignCivilianTotal}/{campaignCivilianTotal}");
        SetCampaignText("TXT_R2_SummaryLabel_Rescued", "NGƯỜI DÂN AN TOÀN");
        SetCampaignText("TXT_R2_SummaryValue_Stages",
            $"{campaignStageTotal}/{campaignStageTotal}");
        SetCampaignText("TXT_R2_SummaryLabel_Stages", "GIAI ĐOẠN HOÀN TẤT");
        SetCampaignText("TXT_R2_SummaryValue_Status", "HOÀN TẤT");
        SetCampaignText("TXT_R2_SummaryLabel_Status", "TRẠNG THÁI");
    }

    private void PrepareCampaignRevealState()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(true);
        if (campaignEndingPanel != null)
            campaignEndingPanel.SetActive(false);

        SetCanvasGroupState(campaignPanelGroup, 0f, false);
        SetGroupsState(titleGroups, 0f);
        foreach (CanvasGroup[] groups in campaignStatGroups)
            SetGroupsState(groups, 0f);
        SetCanvasGroupState(closingGroup, 0f, false);
        SetGroupsState(buttonGroups, 0f);
        SetCampaignButtonsInteractable(false);

        if (campaignTitleRect != null)
            campaignTitleRect.localScale = campaignTitleBaseScale * titleStartScale;
    }

    private void InitializeEndingPresentation()
    {
        RestoreRevealTransforms();
        SetCanvasGroupState(resultCardGroup, 1f, true);
        SetCanvasGroupState(campaignPanelGroup, 1f, false);
        SetGroupsState(titleGroups, 1f);
        foreach (CanvasGroup[] groups in campaignStatGroups)
            SetGroupsState(groups, 1f);
        SetCanvasGroupState(closingGroup, 1f, false);
        SetGroupsState(buttonGroups, 1f);
        SetCampaignButtonsInteractable(false);

        if (campaignTitleRect != null)
            campaignTitleRect.localScale = campaignTitleBaseScale;
        if (overlayImage != null)
            overlayImage.color = OverlayColor;
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
        if (resultCard != null)
            resultCard.SetActive(true);
        if (campaignEndingPanel != null)
            campaignEndingPanel.SetActive(false);

        overlayActive = false;
        victoryPresentationStarted = false;
        endingSequenceStarted = false;
        endingSequenceComplete = false;
        victorySoundPlayed = false;
    }

    private void ResetEndingPresentation(bool restoreGameplayAudio)
    {
        if (gameplayHudFadeRoutine != null)
        {
            StopCoroutine(gameplayHudFadeRoutine);
            gameplayHudFadeRoutine = null;
        }

        if (endingSequenceRoutine != null)
        {
            StopCoroutine(endingSequenceRoutine);
            endingSequenceRoutine = null;
        }

        if (victoryAmbienceFadeRoutine != null)
        {
            StopCoroutine(victoryAmbienceFadeRoutine);
            victoryAmbienceFadeRoutine = null;
        }

        StopSummaryMusic();
        if (restoreGameplayAudio)
            RestoreVictoryAmbience();
        RestoreRevealTransforms();

        if (gameplayHUDCanvas != null)
            gameplayHUDCanvas.SetActive(gameplayHudInitialActive);
        if (gameplayRainOverlay != null)
            gameplayRainOverlay.SetActive(gameplayRainOverlayInitialActive);
        if (persistentGameplayHudRoot != null)
            persistentGameplayHudRoot.SetActive(true);
        SetCanvasGroupState(gameplayHudGroup, 1f, false);
        SetCanvasGroupState(resultCardGroup, 1f, true);
        SetCanvasGroupState(campaignPanelGroup, 1f, false);
        SetGroupsState(titleGroups, 1f);
        foreach (CanvasGroup[] groups in campaignStatGroups)
            SetGroupsState(groups, 1f);
        SetCanvasGroupState(closingGroup, 1f, false);
        SetGroupsState(buttonGroups, 1f);
        SetCampaignButtonsInteractable(false);

        if (campaignTitleRect != null)
            campaignTitleRect.localScale = campaignTitleBaseScale;

        if (overlayImage != null)
            overlayImage.color = OverlayColor;
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
        if (resultCard != null)
            resultCard.SetActive(true);
        if (campaignEndingPanel != null)
            campaignEndingPanel.SetActive(false);

        if (btnViewCampaignEnding != null)
            btnViewCampaignEnding.interactable = true;
        if (btnRetry != null)
            btnRetry.interactable = true;

        overlayActive = false;
        victoryPresentationStarted = false;
        endingSequenceStarted = false;
        endingSequenceComplete = false;
        victorySoundPlayed = false;
    }

    private void ResolveAndCacheVictoryAudio()
    {
        if (rainAmbienceSource == null)
        {
            GameObject environmentRoot = GameObject.Find("=== R2_ENVIRONMENT ===");
            Transform rainTransform = environmentRoot != null
                ? environmentRoot.transform.Find("R2_Lighting_And_VFX")
                : null;
            if (rainTransform != null)
                rainAmbienceSource = rainTransform.GetComponent<AudioSource>();
        }

        Transform thunderTransform = rainAmbienceSource != null
            ? rainAmbienceSource.transform.Find("Thunder_Audio")
            : null;
        if (thunderAmbienceSource == null && thunderTransform != null)
            thunderAmbienceSource = thunderTransform.GetComponent<AudioSource>();
        if (thunderAmbientPlayer == null && thunderTransform != null)
            thunderAmbientPlayer = thunderTransform.GetComponent<RandomAmbientAudio>();

        if (ambienceStateCached)
            return;

        if (rainAmbienceSource != null)
        {
            rainOriginalVolume = rainAmbienceSource.volume;
            rainWasPlaying =
                rainAmbienceSource.isPlaying ||
                (rainAmbienceSource.playOnAwake && rainAmbienceSource.clip != null);
            if (rainWasPlaying && !rainAmbienceSource.isPlaying)
                rainAmbienceSource.Play();
        }

        if (thunderAmbienceSource != null)
        {
            thunderOriginalVolume = thunderAmbienceSource.volume;
            thunderWasPlaying = thunderAmbienceSource.isPlaying;
        }

        if (thunderAmbientPlayer != null)
            thunderAmbientOriginalVolume = thunderAmbientPlayer.volume;

        ambienceStateCached =
            rainAmbienceSource != null ||
            thunderAmbienceSource != null ||
            thunderAmbientPlayer != null;
    }

    private void StartVictoryAmbienceFade()
    {
        if (ambienceMutedForVictory || victoryAmbienceFadeRoutine != null)
            return;

        ResolveAndCacheVictoryAudio();
        if (!ambienceStateCached)
            return;

        victoryAmbienceFadeRoutine = StartCoroutine(FadeVictoryAmbienceToWaitingLevel());
    }

    private IEnumerator FadeVictoryAmbienceToWaitingLevel()
    {
        float rainStartVolume = rainAmbienceSource != null ? rainAmbienceSource.volume : 0f;
        float thunderStartVolume =
            thunderAmbienceSource != null ? thunderAmbienceSource.volume : 0f;
        float thunderPlayerStartVolume =
            thunderAmbientPlayer != null ? thunderAmbientPlayer.volume : 0f;
        float rainTargetVolume = rainOriginalVolume * victoryAmbienceWaitingVolumeScale;
        float thunderTargetVolume = thunderOriginalVolume * victoryAmbienceWaitingVolumeScale;
        float thunderPlayerTargetVolume =
            thunderAmbientOriginalVolume * victoryAmbienceWaitingVolumeScale;
        float elapsed = 0f;

        while (elapsed < victoryAmbienceFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / victoryAmbienceFadeDuration));

            if (rainAmbienceSource != null)
                rainAmbienceSource.volume =
                    Mathf.Lerp(rainStartVolume, rainTargetVolume, t);
            if (thunderAmbienceSource != null)
                thunderAmbienceSource.volume =
                    Mathf.Lerp(thunderStartVolume, thunderTargetVolume, t);
            if (thunderAmbientPlayer != null)
                thunderAmbientPlayer.volume =
                    Mathf.Lerp(thunderPlayerStartVolume, thunderPlayerTargetVolume, t);

            yield return null;
        }

        if (rainAmbienceSource != null)
            rainAmbienceSource.volume = rainTargetVolume;
        if (thunderAmbienceSource != null)
            thunderAmbienceSource.volume = thunderTargetVolume;
        if (thunderAmbientPlayer != null)
            thunderAmbientPlayer.volume = thunderPlayerTargetVolume;

        ambienceMutedForVictory = true;
        victoryAmbienceFadeRoutine = null;
    }

    private void StartSummaryAmbienceFadeToSilence()
    {
        ResolveAndCacheVictoryAudio();
        if (!ambienceStateCached)
            return;

        if (victoryAmbienceFadeRoutine != null)
        {
            StopCoroutine(victoryAmbienceFadeRoutine);
            victoryAmbienceFadeRoutine = null;
        }

        victoryAmbienceFadeRoutine = StartCoroutine(FadeSummaryAmbienceToSilence());
    }

    private IEnumerator FadeSummaryAmbienceToSilence()
    {
        float delayElapsed = 0f;
        while (delayElapsed < summaryCrossfadeDelay)
        {
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        float rainStartVolume = rainAmbienceSource != null ? rainAmbienceSource.volume : 0f;
        float thunderStartVolume =
            thunderAmbienceSource != null ? thunderAmbienceSource.volume : 0f;
        float thunderPlayerStartVolume =
            thunderAmbientPlayer != null ? thunderAmbientPlayer.volume : 0f;
        float elapsed = 0f;

        while (elapsed < summaryAmbienceFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / summaryAmbienceFadeDuration));

            if (rainAmbienceSource != null)
                rainAmbienceSource.volume = Mathf.Lerp(rainStartVolume, 0f, t);
            if (thunderAmbienceSource != null)
                thunderAmbienceSource.volume = Mathf.Lerp(thunderStartVolume, 0f, t);
            if (thunderAmbientPlayer != null)
                thunderAmbientPlayer.volume =
                    Mathf.Lerp(thunderPlayerStartVolume, 0f, t);

            yield return null;
        }

        if (rainAmbienceSource != null)
            rainAmbienceSource.volume = 0f;
        if (thunderAmbienceSource != null)
            thunderAmbienceSource.volume = 0f;
        if (thunderAmbientPlayer != null)
            thunderAmbientPlayer.volume = 0f;

        ambienceMutedForVictory = true;
        victoryAmbienceFadeRoutine = null;
    }

    private void RestoreVictoryAmbience()
    {
        if (!ambienceStateCached)
            return;

        if (rainAmbienceSource != null)
        {
            rainAmbienceSource.volume = rainOriginalVolume;
            RestoreAudioPlaybackState(rainAmbienceSource, rainWasPlaying);
        }

        if (thunderAmbienceSource != null)
        {
            thunderAmbienceSource.volume = thunderOriginalVolume;
            RestoreAudioPlaybackState(thunderAmbienceSource, thunderWasPlaying);
        }

        if (thunderAmbientPlayer != null)
            thunderAmbientPlayer.volume = thunderAmbientOriginalVolume;

        ambienceMutedForVictory = false;
    }

    private static void RestoreAudioPlaybackState(AudioSource source, bool shouldBePlaying)
    {
        if (source == null)
            return;

        if (shouldBePlaying && !source.isPlaying)
            source.Play();
        else if (!shouldBePlaying && source.isPlaying)
            source.Stop();
    }

    private void CacheSummaryMusicState()
    {
        if (summaryMusicSource == null || summaryMusicStateCached)
            return;

        summaryMusicOriginalClip = summaryMusicSource.clip;
        summaryMusicOriginalVolume = summaryMusicSource.volume;
        summaryMusicStateCached = true;
        summaryMusicSource.Stop();
        summaryMusicSource.time = 0f;
        summaryMusicSource.volume = 0f;
    }

    private void StartSummaryMusicTransitionFade()
    {
        if (summaryMusicSource == null || summaryMusicStarted ||
            summaryMusicFadeRoutine != null)
            return;

        if (!EnsureSummaryMusicPlaying())
            return;

        summaryMusicFadeRoutine = StartCoroutine(BeginSummaryMusicFadeIn());
    }

    private IEnumerator BeginSummaryMusicFadeIn()
    {
        float elapsed = 0f;
        while (elapsed < summaryCrossfadeDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return FadeSummaryMusicToVolume(
            summaryMusicTargetVolume, summaryMusicFadeDuration);
    }

    private bool EnsureSummaryMusicPlaying()
    {
        if (summaryMusicSource == null)
            return false;

        if (summaryMusicSource.isPlaying)
        {
            summaryMusicStarted = true;
            return true;
        }

        if (summaryMusicStarted)
            return false;

        CacheSummaryMusicState();
        AudioClip clip = summaryMusicClip != null ? summaryMusicClip : summaryMusicSource.clip;
        if (clip == null)
            return false;

        summaryMusicSource.clip = clip;
        summaryMusicSource.time = 0f;
        summaryMusicSource.volume = 0f;
        summaryMusicSource.Play();
        summaryMusicStarted = true;
        return true;
    }

    private IEnumerator FadeSummaryMusicToVolume(float targetVolume, float duration)
    {
        float startVolume = summaryMusicSource != null ? summaryMusicSource.volume : 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (summaryMusicSource != null)
                summaryMusicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (summaryMusicSource != null)
            summaryMusicSource.volume = targetVolume;
        summaryMusicFadeRoutine = null;
    }

    private void StopSummaryMusic()
    {
        if (summaryMusicFadeRoutine != null)
        {
            StopCoroutine(summaryMusicFadeRoutine);
            summaryMusicFadeRoutine = null;
        }

        if (summaryMusicSource != null)
        {
            summaryMusicSource.Stop();
            summaryMusicSource.time = 0f;

            if (summaryMusicStateCached)
            {
                summaryMusicSource.clip = summaryMusicOriginalClip;
                summaryMusicSource.volume = summaryMusicOriginalVolume;
            }
            else
            {
                summaryMusicSource.volume = 0f;
            }
        }

        summaryMusicStarted = false;
    }

    private void HideGameplayPresentationForCampaignSummary()
    {
        if (gameplayHUDCanvas != null)
            gameplayHUDCanvas.SetActive(false);
        if (gameplayRainOverlay != null)
            gameplayRainOverlay.SetActive(false);
    }

    private IEnumerator FadeGameplayHudForVictory()
    {
        if (!victoryPresentationStarted || roundController == null ||
            roundController.currentState != Round2GameState.Win || gameplayHudGroup == null)
        {
            yield break;
        }

        yield return FadeCanvasGroup(gameplayHudGroup, 1f, 0f, victoryHudFadeDuration);
        gameplayHudFadeRoutine = null;
    }

    private void EnsureGameplayHudFadeGroup()
    {
        if (gameplayHudGroup == null && persistentGameplayHudRoot != null)
            gameplayHudGroup = GetOrAddCanvasGroup(persistentGameplayHudRoot);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        group.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
    }

    private static float GetRevealProgress(float elapsed, float start, float duration)
    {
        if (duration <= 0f)
            return elapsed >= start ? 1f : 0f;

        return Mathf.SmoothStep(
            0f, 1f, Mathf.Clamp01((elapsed - start) / duration));
    }

    private void SetRevealProgress(
        CanvasGroup[] groups,
        float progress,
        bool slideUp,
        float verticalOffset)
    {
        if (groups == null)
            return;

        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];
            if (group == null)
                continue;

            group.alpha = progress;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform rect = group.transform as RectTransform;
            if (rect == null || !slideUp)
                continue;

            if (!revealBasePositions.ContainsKey(rect))
                revealBasePositions[rect] = rect.anchoredPosition;

            rect.anchoredPosition = Vector2.Lerp(
                revealBasePositions[rect] + Vector2.down * verticalOffset,
                revealBasePositions[rect],
                progress);
        }
    }

    private void RestoreRevealTransforms()
    {
        foreach (KeyValuePair<RectTransform, Vector2> pair in revealBasePositions)
            if (pair.Key != null) pair.Key.anchoredPosition = pair.Value;
        revealBasePositions.Clear();
    }

    private TMP_Text FindCampaignText(string objectName)
    {
        GameObject target = FindCampaignObject(objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private GameObject FindCampaignObject(string objectName)
    {
        if (campaignEndingPanel == null)
            return null;

        Transform[] children = campaignEndingPanel.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            if (children[i].name == objectName) return children[i].gameObject;
        return null;
    }

    private void SetCampaignText(string objectName, string value)
    {
        TMP_Text text = FindCampaignText(objectName);
        if (text != null)
            text.text = value;
    }

    private void ConfigureCampaignSummaryText()
    {
        string[] names =
        {
            "TXT_R2_SummaryValue_Rescued",
            "TXT_R2_SummaryLabel_Rescued",
            "TXT_R2_SummaryValue_Stages",
            "TXT_R2_SummaryLabel_Stages",
            "TXT_R2_SummaryValue_Status",
            "TXT_R2_SummaryLabel_Status"
        };

        for (int i = 0; i < names.Length; i++)
        {
            TMP_Text text = FindCampaignText(names[i]);
            if (text == null)
                continue;

            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        TMP_Text status = FindCampaignText("TXT_R2_SummaryValue_Status");
        TMP_Text statusReference = FindCampaignText("TXT_R2_SummaryValue_Stages");
        if (status != null && statusReference != null)
        {
            status.alignment = statusReference.alignment;
            status.margin = statusReference.margin;
            status.fontSize = statusReference.fontSize;
            RectTransform rect = status.rectTransform;
            RectTransform referenceRect = statusReference.rectTransform;
            rect.anchorMin = referenceRect.anchorMin;
            rect.anchorMax = referenceRect.anchorMax;
            rect.pivot = referenceRect.pivot;
            rect.sizeDelta = referenceRect.sizeDelta;
            rect.anchoredPosition =
                new Vector2(referenceRect.anchoredPosition.x, rect.anchoredPosition.y);
        }
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static CanvasGroup[] CreateGroups(params GameObject[] targets)
    {
        List<CanvasGroup> groups = new List<CanvasGroup>();
        for (int i = 0; i < targets.Length; i++)
        {
            CanvasGroup group = GetOrAddCanvasGroup(targets[i]);
            if (group != null)
                groups.Add(group);
        }
        return groups.ToArray();
    }

    private static void SetCanvasGroupState(CanvasGroup group, float alpha, bool interactive)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.interactable = interactive;
        group.blocksRaycasts = interactive;
    }

    private static void SetGroupsState(CanvasGroup[] groups, float alpha)
    {
        if (groups == null)
            return;

        for (int i = 0; i < groups.Length; i++)
            SetCanvasGroupState(groups[i], alpha, false);
    }

    private void SetCampaignButtonsInteractable(bool interactable)
    {
        if (btnCampaignMenu != null)
            btnCampaignMenu.interactable = interactable;
        if (btnCampaignRetry != null)
            btnCampaignRetry.interactable = interactable;

        if (buttonGroups == null)
            return;
        for (int i = 0; i < buttonGroups.Length; i++)
        {
            if (buttonGroups[i] == null)
                continue;
            buttonGroups[i].interactable = interactable;
            buttonGroups[i].blocksRaycasts = interactable;
        }
    }

    private void ApplyButtonRole(Button button, bool primary)
    {
        if (button == null) return;

        var graphic = button.targetGraphic as Image;
        if (graphic != null)
        {
            Color oldColor = graphic.color;
            graphic.color = new Color(1f, 1f, 1f, oldColor.a);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = primary ? PrimaryNormal : SecondaryNormal;
        colors.highlightedColor = primary ? PrimaryHover : SecondaryHover;
        colors.pressedColor = primary ? PrimaryPressed : SecondaryPressed;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }
}
