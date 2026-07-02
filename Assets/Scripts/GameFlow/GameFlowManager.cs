using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Central game-flow controller for the Main Menu scene.
/// Manages:
///   1. Main Menu → Video Intro (on BtnStart)
///   2. Video Intro → Round Selection (on video end or SKIP)
///   3. Round Selection → Gameplay Scene (on Round 1 button)
///   4. Application quit (on BtnQuit)
///
/// All UI references are assigned via Inspector — no runtime Find() calls.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    // ── Main Menu ─────────────────────────────────────────────────────
    [Header("Main Menu")]
    [Tooltip("The ButtonGroup GameObject — hidden when video starts.")]
    [SerializeField] private GameObject buttonGroup;

    [Tooltip("BtnStart Button component — onClick bound in Awake.")]
    [SerializeField] private Button btnStart;

    [Tooltip("BtnInstructions Button component — onClick bound in Awake.")]
    [SerializeField] private Button btnInstructions;

    [Tooltip("BtnQuit Button component — onClick bound in Awake.")]
    [SerializeField] private Button btnQuit;

    // ── Video Panel ───────────────────────────────────────────────────
    [Header("Video Intro Panel")]
    [Tooltip("Full-screen black panel containing the VideoPlayer.")]
    [SerializeField] private GameObject videoPanel;

    [Tooltip("VideoPlayer component on the VideoPanel.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("SKIP button shown during video playback.")]
    [SerializeField] private Button btnSkip;

    [Tooltip("Video clip to play. Assign Assets/VideoPlayer.mp4 here.")]
    [SerializeField] private VideoClip introClip;

    // ── Round Selection Panel ─────────────────────────────────────────
    [Header("Round Selection Panel")]
    [Tooltip("Panel shown after video ends or is skipped.")]
    [SerializeField] private GameObject roundSelectionPanel;

    [Tooltip("'BẮT ĐẦU ROUND 1' button — loads the gameplay scene.")]
    [SerializeField] private Button btnStartRound1;

    // ── Instructions Panel ───────────────────────────────────────────
    [Header("Instructions Panel")]
    [Tooltip("Two-page instructions panel opened from the main menu.")]
    [SerializeField] private GameObject instructionsPanel;

    [Tooltip("Page 0 root containing the controls guide.")]
    [SerializeField] private GameObject controlsPage;

    [Tooltip("Page 1 root containing the rescue-map guide.")]
    [SerializeField] private GameObject mapGuidePage;

    [SerializeField] private Button btnPreviousInstructionPage;
    [SerializeField] private Button btnNextInstructionPage;
    [SerializeField] private Button btnCloseInstructions;
    [SerializeField] private TMP_Text instructionPageIndicator;

    private int currentInstructionPage;
    private bool isSceneLoading;

    // ── Scene Config ──────────────────────────────────────────────────
    [Header("Scene Configuration")]
    [Tooltip("Exact name of the gameplay scene to load.")]
    [SerializeField] private string round1SceneName = "Round1_FirstPersonPrototype";
    [SerializeField] private bool isTransitioning = false;
    [SerializeField] private MenuMusicManager menuMusicManager;
    [SerializeField] private UIAudioPlayer uiAudioPlayer;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        ValidateReferences();
        BindButtons();
        SetupVideoPlayer();
        SetInitialState();
    }

    void Start()
    {
        if (menuMusicManager != null)
        {
            menuMusicManager.PlayFromStartWithFade();
        }
    }

    // ── Validation ────────────────────────────────────────────────────

    void ValidateReferences()
    {
        bool valid = true;

        if (buttonGroup        == null) { LogMissing("buttonGroup");        valid = false; }
        if (btnStart           == null) { LogMissing("btnStart");           valid = false; }
        if (btnQuit            == null) { LogMissing("btnQuit");            valid = false; }
        if (videoPanel         == null) { LogMissing("videoPanel");         valid = false; }
        if (videoPlayer        == null) { LogMissing("videoPlayer");        valid = false; }
        if (btnSkip            == null) { LogMissing("btnSkip");            valid = false; }
        if (roundSelectionPanel== null) { LogMissing("roundSelectionPanel");valid = false; }
        if (btnStartRound1     == null) { LogMissing("btnStartRound1");     valid = false; }
        if (instructionsPanel  == null) { LogMissing("instructionsPanel");  valid = false; }
        if (controlsPage       == null) { LogMissing("controlsPage");       valid = false; }
        if (mapGuidePage       == null) { LogMissing("mapGuidePage");       valid = false; }
        if (btnPreviousInstructionPage == null) { LogMissing("btnPreviousInstructionPage"); valid = false; }
        if (btnNextInstructionPage     == null) { LogMissing("btnNextInstructionPage");     valid = false; }
        if (btnCloseInstructions       == null) { LogMissing("btnCloseInstructions");       valid = false; }
        if (instructionPageIndicator   == null) { LogMissing("instructionPageIndicator");   valid = false; }

        if (!valid)
            Debug.LogError("[GameFlowManager] One or more required references " +
                           "are not assigned. Assign them in the Inspector.");
    }

    static void LogMissing(string field) =>
        Debug.LogError($"[GameFlowManager] Missing Inspector reference: '{field}'");

    // ── Button binding ────────────────────────────────────────────────

    void BindButtons()
    {
        // Remove any previously registered listeners to prevent double-firing
        // on domain reload in the Editor.
        btnStart.onClick.RemoveAllListeners();
        btnQuit .onClick.RemoveAllListeners();
        btnSkip .onClick.RemoveAllListeners();
        btnStartRound1.onClick.RemoveAllListeners();
        btnPreviousInstructionPage.onClick.RemoveAllListeners();
        btnNextInstructionPage.onClick.RemoveAllListeners();
        btnCloseInstructions.onClick.RemoveAllListeners();

        if (btnInstructions != null)
            btnInstructions.onClick.RemoveAllListeners();

        // Bind
        btnStart      .onClick.AddListener(OnBtnStartClicked);
        btnQuit       .onClick.AddListener(OnBtnQuitClicked);
        btnSkip       .onClick.AddListener(OnSkipPressed);
        btnStartRound1.onClick.AddListener(OnStartRound1Clicked);
        btnPreviousInstructionPage.onClick.AddListener(OnBtnPreviousInstructionPageClicked);
        btnNextInstructionPage.onClick.AddListener(OnBtnNextInstructionPageClicked);
        btnCloseInstructions.onClick.AddListener(OnBtnCloseInstructionsClicked);

        // BtnInstructions — placeholder, extend later
        if (btnInstructions != null)
            btnInstructions.onClick.AddListener(OnBtnInstructionsClicked);
    }

    // ── VideoPlayer setup ─────────────────────────────────────────────

    void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;

        videoPlayer.playOnAwake   = false;
        videoPlayer.isLooping     = false;
        videoPlayer.renderMode    = VideoRenderMode.RenderTexture;

        // Assign clip if provided; otherwise rely on URL set on the component
        if (introClip != null)
            videoPlayer.clip = introClip;

        // Subscribe to loop-point event (fires when non-looping video reaches end)
        videoPlayer.loopPointReached -= OnVideoEndReached; 
        videoPlayer.loopPointReached += OnVideoEndReached;
    }

    // ── Initial UI state ──────────────────────────────────────────────

    public void SetInitialState()
    {
        isTransitioning = false;
        isSceneLoading = false;
        currentInstructionPage = 0;
        SetActive(buttonGroup,         true);
        SetActive(videoPanel,          false);
        SetActive(roundSelectionPanel, false);
        SetActive(instructionsPanel,    false);
        ShowInstructionPage(0);
    }

    // ─────────────────────────────────────────────────────────────────
    // Button handlers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// BtnStart clicked: hide menu, show video panel, begin playback.
    /// </summary>
    void OnBtnStartClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        uiAudioPlayer?.PlayClick();

        StartCoroutine(StartVideoTransitionRoutine());
    }

    private IEnumerator StartVideoTransitionRoutine()
    {
        SetActive(buttonGroup,         false);
        SetActive(videoPanel,          true);
        SetActive(roundSelectionPanel, false);
        SetActive(instructionsPanel,    false);

        if (btnSkip != null)
            btnSkip.interactable = false;

        if (videoPlayer != null)
        {
            videoPlayer.enabled = true;
            videoPlayer.Prepare();
        }

        // Preparation continues asynchronously while the menu music fades out.
        if (menuMusicManager != null)
        {
            yield return menuMusicManager.PauseWithFade();
        }

        if (videoPlayer != null)
        {
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }
            videoPlayer.Play();
            if (btnSkip != null)
                btnSkip.interactable = true;
            Debug.Log("[GameFlowManager] Video started.");
        }
        else
        {
            OnSkipOrVideoEnd();
        }

        isTransitioning = false;
    }

    private void OnSkipPressed()
    {
        if (isTransitioning) return;

        uiAudioPlayer?.PlayClick();
        OnSkipOrVideoEnd();
    }

    private void OnVideoEndReached(VideoPlayer vp)
    {
        OnSkipOrVideoEnd();
    }

    void OnSkipOrVideoEnd()
    {
        if (isTransitioning) return; // Nếu đang chuyển cảnh rồi thì block các lệnh gọi trùng lặp
        isTransitioning = true;

        if (videoPlayer != null)
            videoPlayer.Stop();

        SetActive(videoPanel,          false);
        SetActive(roundSelectionPanel, true);
        SetActive(buttonGroup,         false);
        SetActive(instructionsPanel,    false);

        if (menuMusicManager != null)
        {
            StartCoroutine(menuMusicManager.ResumeWithFade());
        }

        Debug.Log("[GameFlowManager] Showing Round Selection panel.");
    }

    /// <summary>
    /// Round 1 button: load the gameplay scene.
    /// </summary>
    void OnStartRound1Clicked()
    {
        if (string.IsNullOrEmpty(round1SceneName))
        {
            Debug.LogError("[GameFlowManager] round1SceneName is empty. " +
                           "Set it in the Inspector.");
            return;
        }

        if (isSceneLoading) return;
        isSceneLoading = true;
        uiAudioPlayer?.PlayMissionStart();

        StartCoroutine(StartRound1TransitionRoutine());
    }

    private IEnumerator StartRound1TransitionRoutine()
    {
        if (menuMusicManager != null)
        {
            yield return menuMusicManager.StopWithFade();
        }

        Debug.Log($"[GameFlowManager] Loading scene: {round1SceneName}");
        SceneManager.LoadScene(round1SceneName);
    }

    void OnBtnInstructionsClicked()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        uiAudioPlayer?.PlayClick();
        SetActive(buttonGroup,         false);
        SetActive(videoPanel,          false);
        SetActive(roundSelectionPanel, false);
        SetActive(instructionsPanel,    true);
        ShowInstructionPage(0);
    }

    void OnBtnCloseInstructionsClicked()
    {
        uiAudioPlayer?.PlayClick();
        SetActive(instructionsPanel,    false);
        SetActive(videoPanel,           false);
        SetActive(roundSelectionPanel,  false);
        SetActive(buttonGroup,          true);

        isTransitioning = false;
        ShowInstructionPage(0);
    }

    void OnBtnNextInstructionPageClicked()
    {
        uiAudioPlayer?.PlayClick();
        ShowInstructionPage(currentInstructionPage + 1);
    }

    void OnBtnPreviousInstructionPageClicked()
    {
        uiAudioPlayer?.PlayClick();
        ShowInstructionPage(currentInstructionPage - 1);
    }

    void ShowInstructionPage(int pageIndex)
    {
        currentInstructionPage = Mathf.Clamp(pageIndex, 0, 1);

        SetActive(controlsPage, currentInstructionPage == 0);
        SetActive(mapGuidePage, currentInstructionPage == 1);

        if (instructionPageIndicator != null)
            instructionPageIndicator.text = $"{currentInstructionPage + 1} / 2";

        if (btnPreviousInstructionPage != null)
            btnPreviousInstructionPage.gameObject.SetActive(currentInstructionPage > 0);

        if (btnNextInstructionPage != null)
            btnNextInstructionPage.gameObject.SetActive(currentInstructionPage < 1);

        if (btnCloseInstructions != null)
            btnCloseInstructions.gameObject.SetActive(true);
    }

    /// <summary>
    /// BtnQuit: works in both built player and Editor Play Mode.
    /// </summary>
    void OnBtnQuitClicked()
    {
        uiAudioPlayer?.PlayClick();
        Debug.Log("[GameFlowManager] Quitting application.");

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────────

    static void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }

    void OnDestroy()
    {
        // Unsubscribe from VideoPlayer event to prevent memory leaks
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEndReached;
    }
}
