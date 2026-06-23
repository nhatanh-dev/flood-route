using TMPro;
using UnityEngine;

public sealed class RoundUIController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text cargoText;
    [SerializeField] private TMP_Text savedText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private GameObject messagePanel;

    [Header("Intro")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private TMP_Text introTitleText;
    [SerializeField] private TMP_Text introBodyText;
    [SerializeField] private TMP_Text introHintText;
    [SerializeField] private TMP_Text introControlsText;

    public void SetHud(int turnsLeft, int cargo, int cargoCapacity, int saved, int totalPeople)
    {
        SetText(turnText, $"Lượt còn lại: {turnsLeft}");
        SetText(cargoText, $"Trên thuyền: {cargo} / {cargoCapacity}");
        SetText(savedText, $"Đã cứu: {saved} / {totalPeople}");
    }

    public void SetObjective(string objective)
    {
        SetText(objectiveText, objective);
    }

    public void ShowMessage(string message)
    {
        SetText(messageText, message);
        SetActive(messagePanel, !string.IsNullOrWhiteSpace(message));
    }

    public void SetIntro(string title, string body, string hint, string controls)
    {
        SetText(introTitleText, title);
        SetText(introBodyText, body);
        SetText(introHintText, hint);
        SetText(introControlsText, controls);
    }

    public void ShowIntro(bool visible)
    {
        SetActive(introPanel, visible);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
