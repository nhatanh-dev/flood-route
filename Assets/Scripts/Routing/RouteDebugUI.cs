using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RouteDebugUI : MonoBehaviour
{
    public static RouteDebugUI Instance { get; private set; }

    [SerializeField] private BoatRouteMover boatMover;
    [SerializeField] private TMP_Text currentNodeText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Text legacyCurrentNodeText;
    [SerializeField] private Text legacyMessageText;
    [SerializeField] private float messageDuration = 2.5f;

    private float messageTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate RouteDebugUI found on {name}. Keeping the first instance.", this);
            return;
        }

        Instance = this;
        boatMover ??= FindAnyObjectByType<BoatRouteMover>();
    }

    private void Start()
    {
        if (boatMover != null)
        {
            SetCurrentNode(boatMover.CurrentNode);
        }
    }

    private void Update()
    {
        if (boatMover != null)
        {
            SetCurrentNode(boatMover.CurrentNode);
        }

        if (messageTimer <= 0f)
        {
            return;
        }

        messageTimer -= Time.deltaTime;
        if (messageTimer <= 0f)
        {
            SetMessageText(string.Empty);
        }
    }

    public void SetCurrentNode(RouteNode node)
    {
        string text = node != null ? "Đang ở điểm hiện tại" : string.Empty;

        if (currentNodeText != null)
        {
            currentNodeText.text = text;
        }

        if (legacyCurrentNodeText != null)
        {
            legacyCurrentNodeText.text = text;
        }
    }

    public void ShowMessage(string message)
    {
        SetMessageText(message);
        messageTimer = messageDuration;
    }

    public void ShowArrival(RouteNode node)
    {
        if (node == null)
        {
            return;
        }

        if (node.NodeType == NodeType.Objective || node.NodeType == NodeType.Shelter)
        {
            ShowMessage("Đã tới điểm quan trọng.");
            return;
        }

        ShowMessage("Đã tới điểm kế tiếp.");
    }

    private void SetMessageText(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        if (legacyMessageText != null)
        {
            legacyMessageText.text = message;
        }
    }
}
