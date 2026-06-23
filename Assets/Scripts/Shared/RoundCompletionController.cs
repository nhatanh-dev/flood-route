using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class RoundCompletionController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text turnsText;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;

    public UnityEvent ReplayRequested;
    public UnityEvent MenuRequested;

    private void Awake()
    {
        if (replayButton != null)
        {
            replayButton.onClick.AddListener(() => ReplayRequested?.Invoke());
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(() => MenuRequested?.Invoke());
        }
    }

    public void ShowWin(string body, int usedTurns, int maxTurns)
    {
        Show("HOÀN THÀNH", body, usedTurns, maxTurns);
    }

    public void ShowLose(string body, int usedTurns, int maxTurns)
    {
        Show("THẤT BẠI", body, usedTurns, maxTurns);
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Show(string title, string body, int usedTurns, int maxTurns)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        SetText(titleText, title);
        SetText(bodyText, body);
        SetText(turnsText, $"Số lượt đã dùng: {usedTurns} / {maxTurns}");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
