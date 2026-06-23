using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class Round2GameController : MonoBehaviour
{
    private enum MessagePriority
    {
        None = 0,
        General = 1,
        StartHint = 2,
        NgaReHint = 3,
        PickupDropoff = 4,
        WrongWait = 5,
        BlockedEdge = 6,
        CorrectWait = 7,
        Result = 8
    }

    [Header("Routing")]
    [SerializeField] private RouteGraphManager graphManager;
    [SerializeField] private BoatRouteMover boatMover;
    [SerializeField] private RouteEdge debrisEdge;
    [SerializeField] private GameObject debrisBlockerObject;
    [SerializeField] private GameObject blockedVisual;
    [SerializeField] private GameObject openedVisual;
    [SerializeField] private DebrisClearInteraction debrisInteraction;
    [SerializeField] private Round2IntroController introController;

    [Header("Rules")]
    [SerializeField] private int currentTurn = 1;
    [SerializeField] private int maxTurn = 7;
    [SerializeField] private int boatCapacity = 3;
    [SerializeField] private int civiliansAtHouse = 3;
    [SerializeField] private int cargo;
    [SerializeField] private int savedCivilians;

    [Header("Node Ids")]
    [SerializeField] private string debrisWaitNodeId = "R2_NGA_RE";
    [SerializeField] private string debrisTargetNodeId = "R2_TUYEN_CHINH";
    [SerializeField] private string pickupNodeId = "R2_NHA_SONG";
    [SerializeField] private string shelterNodeId = "R2_DIEM_TRU";

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text cargoText;
    [SerializeField] private TMP_Text savedText;
    [SerializeField] private GameObject resultPanelRoot;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultBodyText;
    [SerializeField] private TMP_Text resultTurnsText;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Text legacyStatusText;
    [SerializeField] private Text legacyMessageText;
    [SerializeField] private Text legacyTurnText;
    [SerializeField] private Text legacyCargoText;

    [Header("Hint Timing")]
    [SerializeField] private float shortMessageSeconds = 2.5f;
    [SerializeField] private float importantMessageSeconds = 4f;
    [SerializeField] private Color normalMessageColor = Color.white;
    [SerializeField] private Color warningMessageColor = new(1f, 0.78f, 0.28f);
    [SerializeField] private Color successMessageColor = new(0.58f, 1f, 0.68f);

    private const string StartMovementHint = "Click điểm kế bên để di chuyển thuyền.";
    private const string GeneralObjectiveHint = "Cứu 3 người ở Nhà ven sông và đưa về Điểm trú.";
    private const string CarryToShelterHint = "Chở người dân tới Điểm trú an toàn.";
    private const string NgaReWaitHint = "Tuyến chính phía trước bị rác chặn. Nhấn Q để mở dòng nước.";

    private bool debrisOpenedByWait;
    private bool roundFinished;
    private bool startHintShown;
    private MessagePriority activeMessagePriority;
    private float messageTimer;
    private Coroutine debrisEdgeFlashRoutine;

    public int CurrentTurn => currentTurn;
    public int MaxTurn => maxTurn;
    public int Cargo => cargo;
    public int SavedCivilians => savedCivilians;
    public int CiviliansAtHouse => civiliansAtHouse;
    public int TotalCivilians => civiliansAtHouse + cargo + savedCivilians;
    public bool RoundFinished => roundFinished;
    public event Action<int, int, int> RescueCountsChanged;

    private bool IntroIsActive => introController != null && introController.IsIntroActive;

    private void Awake()
    {
        EnsureReferences();
        Subscribe();
        WireResultButtons();
        ResetRoundState();
    }

    private void OnEnable()
    {
        EnsureReferences();
        Subscribe();
        WireResultButtons();
        RefreshUi();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (IntroIsActive)
        {
            ClearGameplayMessageForIntro();
            return;
        }

        if (roundFinished || boatMover == null && !EnsureReferences())
        {
            return;
        }

        if (!startHintShown)
        {
            startHintShown = true;
            ShowMessage(StartMovementHint, MessagePriority.StartHint, shortMessageSeconds, normalMessageColor);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
        {
            TryWait();
        }

        UpdateMessageTimer();
    }

    public bool TryMoveTo(RouteNode targetNode)
    {
        EnsureReferences();

        if (IntroIsActive)
        {
            ShowMessage("Nhấn SPACE, ENTER hoặc click để bắt đầu.", MessagePriority.StartHint);
            return false;
        }

        if (roundFinished)
        {
            ShowMessage("Round 2 đã kết thúc.", MessagePriority.Result);
            return false;
        }

        if (boatMover == null || graphManager == null)
        {
            ShowMessage("Thiếu thiết lập thuyền hoặc bản đồ đường đi.", MessagePriority.General);
            return false;
        }

        if (boatMover.IsMoving)
        {
            ShowMessage("Thuyền đang di chuyển.", MessagePriority.General, 1.4f);
            return false;
        }

        RouteNode currentNode = boatMover.CurrentNode;
        if (currentNode == null || targetNode == null)
        {
            ShowMessage("Chưa xác định được vị trí thuyền hoặc điểm đến.", MessagePriority.General);
            return false;
        }

        RouteEdge edge = graphManager.GetEdgeBetween(currentNode, targetNode);
        if (edge != null && edge.IsBlocked)
        {
            if (IsDebrisEdge(currentNode, targetNode))
            {
                ShowMessage(
                    "Bị rác chặn - nhấn Q tại Ngã rẽ để mở dòng nước.",
                    MessagePriority.BlockedEdge,
                    importantMessageSeconds,
                    warningMessageColor);
                FlashDebrisEdgeWarning();
                debrisInteraction?.ShowBlockedFeedback();
            }
            else
            {
                ShowMessage("Đường này đang bị chặn.", MessagePriority.BlockedEdge, shortMessageSeconds, warningMessageColor);
            }

            return false;
        }

        if (!graphManager.CanMove(currentNode, targetNode, out _))
        {
            ShowMessage("Chỉ có thể đi tới điểm kế bên.",
                MessagePriority.General,
                shortMessageSeconds,
                warningMessageColor);
            return false;
        }

        bool accepted = boatMover.TryMoveTo(targetNode);
        if (accepted)
        {
            ShowMessage("Thuyền đang di chuyển.", MessagePriority.General, 1.2f);
        }

        return accepted;
    }

    public void TryWait()
    {
        EnsureReferences();

        if (IntroIsActive)
        {
            ShowMessage("Nhấn SPACE, ENTER hoặc click để bắt đầu.", MessagePriority.StartHint);
            return;
        }

        if (roundFinished)
        {
            ShowMessage("Round 2 đã kết thúc.", MessagePriority.Result);
            return;
        }

        if (boatMover != null && boatMover.IsMoving)
        {
            ShowMessage("Không thể chờ khi thuyền đang di chuyển.", MessagePriority.General);
            return;
        }

        RouteNode currentNode = boatMover != null ? boatMover.CurrentNode : null;
        bool correctDebrisWait = currentNode != null
            && currentNode.NodeId == debrisWaitNodeId
            && currentTurn == 3;

        if (correctDebrisWait)
        {
            debrisOpenedByWait = true;
            RefreshDebrisState();
            debrisInteraction?.PlayClearFeedback();
            ShowMessage(
                "Rác đã trôi! Đường đã thông.",
                MessagePriority.CorrectWait,
                importantMessageSeconds,
                successMessageColor);
        }
        else
        {
            ShowMessage(
                "Bạn đã chờ 1 lượt, nhưng không có gì thay đổi.",
                MessagePriority.WrongWait,
                importantMessageSeconds,
                warningMessageColor);
        }

        ConsumeTurn();
        CheckLoseCondition();
    }

    private void ClearGameplayMessageForIntro()
    {
        if (activeMessagePriority != MessagePriority.None || messageTimer > 0f)
        {
            activeMessagePriority = MessagePriority.None;
            messageTimer = 0f;
            SetMessageText(string.Empty, normalMessageColor);
            SetMessagePanelVisible(false);
        }
    }

    [ContextMenu("Reset Round 2 State")]
    public void ResetRoundState()
    {
        currentTurn = 1;
        cargo = 0;
        savedCivilians = 0;
        civiliansAtHouse = 3;
        debrisOpenedByWait = false;
        roundFinished = false;
        startHintShown = false;
        activeMessagePriority = MessagePriority.None;
        messageTimer = 0f;
        RefreshDebrisState();
        RefreshUi();

        if (resultPanelRoot != null)
        {
            resultPanelRoot.SetActive(false);
        }

        ShowMessage(GeneralObjectiveHint, MessagePriority.General, shortMessageSeconds);
    }

    private bool EnsureReferences()
    {
        graphManager ??= RouteGraphManager.Instance != null
            ? RouteGraphManager.Instance
            : FindAnyObjectByType<RouteGraphManager>();
        boatMover ??= FindAnyObjectByType<BoatRouteMover>();
        introController ??= FindAnyObjectByType<Round2IntroController>(FindObjectsInactive.Include);
        debrisInteraction ??= FindAnyObjectByType<DebrisClearInteraction>(FindObjectsInactive.Include);

        if (debrisEdge == null && graphManager != null)
        {
            RouteNode from = graphManager.GetNodeById(debrisWaitNodeId);
            RouteNode to = graphManager.GetNodeById(debrisTargetNodeId);
            debrisEdge = graphManager.GetEdgeBetween(from, to);
        }

        if (debrisBlockerObject == null)
        {
            debrisBlockerObject = GameObject.Find("R2_Debris_Main_Blocker");
        }

        if (debrisBlockerObject != null)
        {
            blockedVisual ??= debrisBlockerObject.transform.Find("Blocked_Visual")?.gameObject;
            openedVisual ??= debrisBlockerObject.transform.Find("Opened_Visual")?.gameObject;
        }

        return graphManager != null && boatMover != null;
    }

    private void Subscribe()
    {
        if (boatMover == null)
        {
            return;
        }

        boatMover.ArrivedAtNode -= HandleBoatArrived;
        boatMover.ArrivedAtNode += HandleBoatArrived;
    }

    private void Unsubscribe()
    {
        if (boatMover != null)
        {
            boatMover.ArrivedAtNode -= HandleBoatArrived;
        }
    }

    private void HandleBoatArrived(RouteNode node)
    {
        if (roundFinished || node == null)
        {
            return;
        }

        ConsumeTurn();
        HandleAutomaticPickupDropoff(node);
        RefreshDebrisState();
        CheckLoseCondition();

        if (!roundFinished)
        {
            ShowContextualHint(node);
        }
    }

    private void HandleAutomaticPickupDropoff(RouteNode node)
    {
        if (node.NodeId == pickupNodeId && civiliansAtHouse > 0)
        {
            int pickupAmount = Mathf.Min(boatCapacity - cargo, civiliansAtHouse);
            if (pickupAmount > 0)
            {
                cargo += pickupAmount;
                civiliansAtHouse -= pickupAmount;
                ShowMessage(
                    $"Đã đón {pickupAmount} người lên thuyền. Chở {cargo} người về Điểm trú.",
                    MessagePriority.PickupDropoff,
                    importantMessageSeconds,
                    successMessageColor);
            }
        }

        if (node.NodeId == shelterNodeId && cargo > 0)
        {
            savedCivilians += cargo;
            cargo = 0;

            if (savedCivilians >= 3)
            {
                roundFinished = true;
                ShowMessage(
                    "Hoàn thành! Đã đưa người dân tới Điểm trú.",
                    MessagePriority.Result,
                    importantMessageSeconds,
                    successMessageColor);
                ShowResultPanel(true);
            }
            else
            {
                ShowMessage("Đã đưa người dân tới Điểm trú.", MessagePriority.PickupDropoff, importantMessageSeconds, successMessageColor);
            }
        }

        RefreshUi();
    }

    private void ConsumeTurn()
    {
        currentTurn++;
        RefreshDebrisState();
        RefreshUi();
    }

    private void CheckLoseCondition()
    {
        if (!roundFinished && currentTurn > maxTurn)
        {
            roundFinished = true;
            ShowMessage(
                "Bạn đã hết lượt trước khi đưa người dân tới Điểm trú.",
                MessagePriority.Result,
                importantMessageSeconds,
                warningMessageColor);
            ShowResultPanel(false);
            RefreshUi();
        }
    }

    private void RefreshDebrisState()
    {
        bool shouldOpen = debrisOpenedByWait && currentTurn >= 4 && currentTurn <= 5;
        bool shouldBlock = !shouldOpen;

        if (debrisEdge != null)
        {
            debrisEdge.SetBlocked(shouldBlock);
        }

        if (debrisBlockerObject != null)
        {
            debrisBlockerObject.SetActive(true);
        }

        if (blockedVisual != null)
        {
            blockedVisual.SetActive(shouldBlock);
        }

        if (openedVisual != null)
        {
            openedVisual.SetActive(shouldOpen);
        }

        if (debrisInteraction != null)
        {
            debrisInteraction.SetDebrisBlocked(shouldBlock);
            debrisInteraction.SetNearFlowControl(shouldBlock && IsAtDebrisWaitNode());
        }
    }

    private bool IsDebrisEdge(RouteNode a, RouteNode b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        return a.NodeId == debrisWaitNodeId && b.NodeId == debrisTargetNodeId
            || a.NodeId == debrisTargetNodeId && b.NodeId == debrisWaitNodeId;
    }

    private void RefreshUi()
    {
        int turnsLeft = Mathf.Max(0, maxTurn - currentTurn + 1);

        SetText(turnText, legacyTurnText, $"Lượt còn lại: {turnsLeft}");
        SetText(cargoText, legacyCargoText, $"Trên thuyền: {cargo} người");
        SetText(savedText, null, $"Đã cứu: {savedCivilians} / 3");

        RescueCountsChanged?.Invoke(TotalCivilians, civiliansAtHouse, savedCivilians);

        string status;
        if (roundFinished && savedCivilians >= 3)
        {
            status = $"Hoàn thành Round 2\nĐã cứu: {savedCivilians}/3    Lượt còn lại: {turnsLeft}";
        }
        else if (roundFinished)
        {
            status = $"Thất bại\nNgười trên thuyền: {cargo}/{boatCapacity}    Đã cứu: {savedCivilians}/3";
        }
        else
        {
            status = "Click điểm kế bên để di chuyển\nNhấn Q để chờ";
        }

        SetText(statusText, legacyStatusText, status);
    }

    private void UpdateMessageTimer()
    {
        if (messageTimer <= 0f)
        {
            ShowContextualHint(boatMover != null ? boatMover.CurrentNode : null);
            return;
        }

        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f)
        {
            activeMessagePriority = MessagePriority.None;
            ShowContextualHint(boatMover != null ? boatMover.CurrentNode : null);
        }
    }

    private void ShowContextualHint(RouteNode currentNode)
    {
        if (roundFinished || IntroIsActive)
        {
            return;
        }

        if (debrisInteraction != null)
        {
            debrisInteraction.SetNearFlowControl(
                currentNode != null
                && currentNode.NodeId == debrisWaitNodeId
                && debrisEdge != null
                && debrisEdge.IsBlocked);
        }

        if (currentNode != null
            && currentNode.NodeId == debrisWaitNodeId
            && debrisEdge != null
            && debrisEdge.IsBlocked)
        {
            SetMessageText(NgaReWaitHint, warningMessageColor);
            activeMessagePriority = MessagePriority.NgaReHint;
            return;
        }

        if (cargo > 0 && currentNode != null && currentNode.NodeId != shelterNodeId)
        {
            SetMessageText($"Chở {cargo} người về Điểm trú.", normalMessageColor);
            activeMessagePriority = MessagePriority.General;
            return;
        }

        SetMessageText(startHintShown ? GeneralObjectiveHint : StartMovementHint, normalMessageColor);
        activeMessagePriority = MessagePriority.General;
    }

    private bool IsAtDebrisWaitNode()
    {
        RouteNode currentNode = boatMover != null ? boatMover.CurrentNode : null;
        return currentNode != null && currentNode.NodeId == debrisWaitNodeId;
    }

    private void WireResultButtons()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(RestartRound);
            replayButton.onClick.AddListener(RestartRound);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(ShowMenuPlaceholder);
            menuButton.onClick.AddListener(ShowMenuPlaceholder);
        }
    }

    private void RestartRound()
    {
        if (boatMover != null && graphManager != null)
        {
            RouteNode baseNode = graphManager.GetNodeById("R2_BASE");
            if (baseNode != null)
            {
                boatMover.WarpToNode(baseNode);
            }
        }

        ResetRoundState();
    }

    private void ShowMenuPlaceholder()
    {
        ShowMessage("Menu chính sẽ được kết nối sau.", MessagePriority.General);
    }

    private void ShowResultPanel(bool won)
    {
        if (resultPanelRoot == null)
        {
            return;
        }

        resultPanelRoot.SetActive(true);
        int usedTurns = Mathf.Clamp(currentTurn - 1, 0, maxTurn);

        if (won)
        {
            SetText(resultTitleText, null, "HOÀN THÀNH ROUND 2");
            SetText(resultBodyText, null, "Bạn đã cứu 3 người và đưa về Điểm trú an toàn.");
        }
        else
        {
            SetText(resultTitleText, null, "THẤT BẠI");
            SetText(resultBodyText, null, "Bạn đã hết lượt trước khi đưa người dân tới Điểm trú.");
        }

        SetText(resultTurnsText, null, $"Số lượt đã dùng: {usedTurns} / {maxTurn}");
    }

    private void ShowMessage(
        string message,
        MessagePriority priority = MessagePriority.General,
        float duration = -1f,
        Color? color = null)
    {
        if (messageTimer > 0f && priority < activeMessagePriority)
        {
            return;
        }

        activeMessagePriority = priority;
        messageTimer = duration >= 0f ? duration : shortMessageSeconds;
        SetMessagePanelVisible(true);
        SetMessageText(message, color ?? normalMessageColor);
        RouteDebugUI.Instance?.ShowMessage(message);
    }

    private void SetMessagePanelVisible(bool visible)
    {
        Transform panel = messageText != null ? messageText.transform.parent : null;
        if (panel == null || panel == transform)
        {
            return;
        }

        string panelName = panel.name.ToLowerInvariant();
        if (panelName.Contains("message") || panelName.Contains("hint"))
        {
            panel.gameObject.SetActive(visible);
        }
    }

    private void SetMessageText(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }

        if (legacyMessageText != null)
        {
            legacyMessageText.text = message;
            legacyMessageText.color = color;
        }
    }

    private void FlashDebrisEdgeWarning()
    {
        if (debrisEdge == null)
        {
            return;
        }

        LineRenderer lineRenderer = debrisEdge.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }

        if (debrisEdgeFlashRoutine != null)
        {
            StopCoroutine(debrisEdgeFlashRoutine);
        }

        debrisEdgeFlashRoutine = StartCoroutine(FlashLineRoutine(lineRenderer));
    }

    private IEnumerator FlashLineRoutine(LineRenderer lineRenderer)
    {
        Color originalStart = lineRenderer.startColor;
        Color originalEnd = lineRenderer.endColor;
        float originalWidth = lineRenderer.widthMultiplier;

        Color warning = new(1f, 0.35f, 0.08f, 1f);
        float elapsed = 0f;
        const float flashSeconds = 0.75f;

        while (elapsed < flashSeconds && lineRenderer != null)
        {
            elapsed += Time.deltaTime;
            float pulse = (Mathf.Sin(elapsed * 28f) + 1f) * 0.5f;
            Color color = Color.Lerp(originalStart, warning, pulse);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.widthMultiplier = originalWidth * Mathf.Lerp(1f, 1.45f, pulse);
            yield return null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.startColor = originalStart;
            lineRenderer.endColor = originalEnd;
            lineRenderer.widthMultiplier = originalWidth;
        }

        debrisEdgeFlashRoutine = null;
    }

    private static void SetText(TMP_Text tmpText, Text legacyText, string value)
    {
        if (tmpText != null)
        {
            tmpText.text = value;
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }
}
