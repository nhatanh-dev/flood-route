using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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

    [Header("Campaign Ending Timing")]
    [SerializeField, Min(0.1f)] private float victoryHudFadeDuration = 0.4f;
    [SerializeField, Min(0.1f)] private float resultCardFadeDuration = 0.45f;
    [SerializeField, Min(0.1f)] private float transitionDuration = 0.55f;
    [SerializeField, Min(0.1f)] private float titleRevealDuration = 0.55f;
    [SerializeField, Min(0.1f)] private float resultRevealDuration = 0.45f;
    [SerializeField, Min(0f)] private float resultRevealGap = 0.2f;
    [SerializeField, Min(0.1f)] private float closingRevealDuration = 0.55f;
    [SerializeField, Min(0.1f)] private float buttonRevealDuration = 0.5f;

    [Header("Victory Ambience")]
    [SerializeField] private AudioSource rainAmbienceSource;
    [SerializeField] private AudioSource thunderAmbienceSource;
    [SerializeField] private RandomAmbientAudio thunderAmbientPlayer;
    [SerializeField, Range(0.8f, 1.2f)] private float victoryAmbienceFadeDuration = 1f;

    [Header("Optional Campaign Summary Music")]
    [SerializeField] private AudioSource summaryMusicSource;
    [SerializeField] private AudioClip summaryMusicClip;
    [SerializeField, Range(1f, 1.5f)] private float summaryMusicFadeDuration = 1.25f;
    [SerializeField, Range(0f, 1f)] private float summaryMusicTargetVolume = 0.7f;

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

        yield return FadeCanvasGroup(resultCardGroup, 1f, 0f, resultCardFadeDuration);
        if (resultCard != null)
            resultCard.SetActive(false);

        yield return FadeSummaryTransition(transitionDuration);

        if (campaignEndingPanel != null)
            campaignEndingPanel.SetActive(true);

        StartSummaryMusicFadeIn();
        yield return FadeCampaignPanelIn(titleRevealDuration);
        yield return RevealGroups(titleGroups, titleRevealDuration, false, campaignTitleRect);
        yield return new WaitForSecondsRealtime(0.35f);

        for (int i = 0; i < campaignStatGroups.Count; i++)
        {
            yield return RevealGroups(campaignStatGroups[i], resultRevealDuration, true, null);
            if (i < campaignStatGroups.Count - 1 && resultRevealGap > 0f)
                yield return new WaitForSecondsRealtime(resultRevealGap);
        }

        yield return RevealGroups(new[] { closingGroup }, closingRevealDuration, true, null);
        yield return new WaitForSecondsRealtime(0.35f);
        yield return RevealGroups(buttonGroups, buttonRevealDuration, false, null);

        SetCanvasGroupState(campaignPanelGroup, 1f, true);
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
            campaignTitleRect.localScale = campaignTitleBaseScale * 0.96f;
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

    private IEnumerator FadeSummaryTransition(float duration)
    {
        float startOverlayAlpha = overlayImage != null ? overlayImage.color.a : OverlayColor.a;
        float targetOverlayAlpha = Mathf.Min(0.78f, OverlayColor.a + 0.12f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            if (overlayImage != null)
            {
                Color color = OverlayColor;
                color.a = Mathf.Lerp(startOverlayAlpha, targetOverlayAlpha, eased);
                overlayImage.color = color;
            }

            yield return null;
        }
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

        victoryAmbienceFadeRoutine = StartCoroutine(FadeVictoryAmbienceToSilence());
    }

    private IEnumerator FadeVictoryAmbienceToSilence()
    {
        float rainStartVolume = rainAmbienceSource != null ? rainAmbienceSource.volume : 0f;
        float thunderStartVolume =
            thunderAmbienceSource != null ? thunderAmbienceSource.volume : 0f;
        float thunderPlayerStartVolume =
            thunderAmbientPlayer != null ? thunderAmbientPlayer.volume : 0f;
        float elapsed = 0f;

        while (elapsed < victoryAmbienceFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / victoryAmbienceFadeDuration));

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
        summaryMusicSource.volume = 0f;
    }

    private void StartSummaryMusicFadeIn()
    {
        if (summaryMusicSource == null || summaryMusicStarted ||
            summaryMusicFadeRoutine != null)
        {
            return;
        }

        CacheSummaryMusicState();
        AudioClip clip = summaryMusicClip != null ? summaryMusicClip : summaryMusicSource.clip;
        if (clip == null)
            return;

        summaryMusicSource.clip = clip;
        summaryMusicSource.volume = 0f;
        summaryMusicSource.Play();
        summaryMusicStarted = true;
        summaryMusicFadeRoutine = StartCoroutine(FadeSummaryMusicIn());
    }

    private IEnumerator FadeSummaryMusicIn()
    {
        float elapsed = 0f;
        while (elapsed < summaryMusicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / summaryMusicFadeDuration));
            if (summaryMusicSource != null)
                summaryMusicSource.volume = Mathf.Lerp(0f, summaryMusicTargetVolume, t);
            yield return null;
        }

        if (summaryMusicSource != null)
            summaryMusicSource.volume = summaryMusicTargetVolume;
        summaryMusicFadeRoutine = null;
    }

    private void StopSummaryMusic()
    {
        if (summaryMusicFadeRoutine != null)
        {
            StopCoroutine(summaryMusicFadeRoutine);
            summaryMusicFadeRoutine = null;
        }

        if (summaryMusicSource != null && summaryMusicStarted)
            summaryMusicSource.Stop();

        if (summaryMusicSource != null && summaryMusicStateCached)
        {
            summaryMusicSource.clip = summaryMusicOriginalClip;
            summaryMusicSource.volume = summaryMusicOriginalVolume;
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

    private IEnumerator FadeCampaignPanelIn(float duration)
    {
        if (campaignPanelGroup == null)
            yield break;

        float startOverlayAlpha = overlayImage != null ? overlayImage.color.a : OverlayColor.a;
        campaignPanelGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            campaignPanelGroup.alpha = t;
            if (overlayImage != null)
            {
                Color color = OverlayColor;
                color.a = Mathf.Lerp(startOverlayAlpha, OverlayColor.a, t);
                overlayImage.color = color;
            }
            yield return null;
        }

        campaignPanelGroup.alpha = 1f;
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

    private IEnumerator RevealGroups(
        CanvasGroup[] groups,
        float duration,
        bool slideUp,
        RectTransform scaleTarget)
    {
        if (groups == null || groups.Length == 0)
            yield break;

        List<RectTransform> rects = new List<RectTransform>();
        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];
            if (group == null)
                continue;

            group.alpha = 0f;
            RectTransform rect = group.transform as RectTransform;
            if (rect != null)
            {
                if (!revealBasePositions.ContainsKey(rect))
                    revealBasePositions[rect] = rect.anchoredPosition;
                if (slideUp)
                    rect.anchoredPosition = revealBasePositions[rect] + Vector2.down * 8f;
                rects.Add(rect);
            }
        }

        Vector3 baseScale = scaleTarget != null ? campaignTitleBaseScale : Vector3.one;
        if (scaleTarget != null)
            scaleTarget.localScale = baseScale * 0.96f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null) groups[i].alpha = t;

            if (slideUp)
            {
                for (int i = 0; i < rects.Count; i++)
                    rects[i].anchoredPosition =
                        Vector2.Lerp(revealBasePositions[rects[i]] + Vector2.down * 8f,
                            revealBasePositions[rects[i]], t);
            }

            if (scaleTarget != null)
                scaleTarget.localScale = Vector3.Lerp(baseScale * 0.96f, baseScale, t);

            yield return null;
        }

        for (int i = 0; i < groups.Length; i++)
            if (groups[i] != null) groups[i].alpha = 1f;
        if (scaleTarget != null)
            scaleTarget.localScale = baseScale;
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
