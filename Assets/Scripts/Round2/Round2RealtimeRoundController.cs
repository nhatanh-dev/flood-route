using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public enum Round2GameState
{
    Playing,
    Win,
    Fail
}

public class Round2RealtimeRoundController : MonoBehaviour
{
    [Header("Game State")]
    public Round2GameState currentState = Round2GameState.Playing;

    [Header("Configuration")]
    public float roundDurationSeconds = 210f;
    
    private float roundTimer;
    private int boatDurability;
    private int cargo;
    private int safe;
    private string lastFailReasonCode = "";

    public int currentBoatDurability => boatDurability;
    public int maxBoatDurability => 3;
    public int currentCargo => cargo;
    public int civiliansSafe => safe;
    public int totalCivilians => 4;
    public int boatCapacity => 4;
    public float CurrentTimeRemaining => roundTimer;
    public string LastFailReason => lastFailReasonCode;
    public string currentObjectiveText = "Mục tiêu: Đi cứu người dân.";

    [Header("HUD References")]
    public TextMeshProUGUI txtTimer;
    public TextMeshProUGUI txtDurability;
    public TextMeshProUGUI txtCargo;
    public TextMeshProUGUI txtSafe;
    public TextMeshProUGUI txtRemaining;
    public TextMeshProUGUI txtObjective;
    public TextMeshProUGUI txtFeedback;

    private CanvasGroup feedbackGroup;
    private Image feedbackAccent;
    private Coroutine feedbackRoutine;

    private void Start()
    {
        roundTimer = roundDurationSeconds;
        boatDurability = maxBoatDurability;
        cargo = 0;
        safe = 0;
        currentState = Round2GameState.Playing;

        if (txtRemaining == null) txtRemaining = FindText("TXT_R2_Remaining");
        if (txtFeedback == null) txtFeedback = FindText("TXT_R2_ContextToast");
        if (txtFeedback != null)
        {
            feedbackGroup = txtFeedback.GetComponentInParent<CanvasGroup>();
            feedbackAccent = FindImage("IMG_R2_ToastAccent");
            txtFeedback.transform.parent.gameObject.SetActive(false);
        }

        UpdateHUD();
    }

    private void Update()
    {
        if (currentState == Round2GameState.Playing)
        {
            if (roundTimer > 0)
            {
                roundTimer -= Time.deltaTime;
                if (roundTimer <= 0f)
                {
                    roundTimer = 0f;
                    TriggerFail("Hết thời gian!", "time_out");
                }
                UpdateTimerHUD();
            }
        }
    }

    private void UpdateHUD()
    {
        UpdateTimerHUD();
        if (txtDurability != null) txtDurability.text = $"{boatDurability}/{maxBoatDurability}";
        if (txtCargo != null) txtCargo.text = $"{cargo}/{boatCapacity}";
        if (txtSafe != null) txtSafe.text = $"{safe}/{totalCivilians}";
        if (txtRemaining != null) txtRemaining.text = Mathf.Max(0, totalCivilians - safe).ToString();
        if (txtObjective != null) txtObjective.text = FormatObjective(currentObjectiveText);
    }

    private void UpdateTimerHUD()
    {
        if (txtTimer != null)
        {
            int minutes = Mathf.FloorToInt(roundTimer / 60f);
            int seconds = Mathf.FloorToInt(roundTimer % 60f);
            txtTimer.text = $"{minutes:00}:{seconds:00}";
        }
    }

    // --- PUBLIC API FOR LATER INTEGRATION ---

    public void AddCargo(int amount)
    {
        if (currentState != Round2GameState.Playing) return;
        cargo = Mathf.Clamp(cargo + amount, 0, boatCapacity);
        UpdateHUD();
    }

    public void DeliverCargo(int amount)
    {
        if (currentState != Round2GameState.Playing) return;
        cargo = Mathf.Clamp(cargo - amount, 0, boatCapacity);
        safe += amount;
        UpdateHUD();

        if (safe >= totalCivilians)
        {
            TriggerWin();
        }
    }

    public void ApplyDamage(int amount)
    {
        if (currentState != Round2GameState.Playing) return;
        boatDurability -= amount;
        if (boatDurability <= 0)
        {
            boatDurability = 0;
            TriggerFail("Thuyền đã bị hỏng!", "boat_broken");
        }
        UpdateHUD();
    }

    public void SetObjective(string newObjective)
    {
        currentObjectiveText = newObjective;
        UpdateHUD();
    }

    public void TriggerWin()
    {
        if (currentState != Round2GameState.Playing) return;
        currentState = Round2GameState.Win;
        SetObjective("Chiến thắng! Tất cả đã an toàn.");
        Debug.Log("[R2 State] Player WIN!");
    }

    public void TriggerFail(string reasonMessage, string failCode = "unknown")
    {
        if (currentState != Round2GameState.Playing) return;
        currentState = Round2GameState.Fail;
        lastFailReasonCode = failCode;
        SetObjective(reasonMessage);
        Debug.Log("[R2 State] Player FAIL: " + reasonMessage + " | Code: " + failCode);
    }

    public void ShowFeedback(string message)
    {
        if (currentState != Round2GameState.Playing) return;
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FeedbackCoroutine(message, 3f));
    }

    private System.Collections.IEnumerator FeedbackCoroutine(string msg, float duration)
    {
        if (txtFeedback == null || feedbackGroup == null) yield break;

        var root = txtFeedback.transform.parent.gameObject;
        root.SetActive(true);
        txtFeedback.text = msg;

        bool critical = msg.Contains("hỏng") || msg.Contains("đầy") ||
                        msg.Contains("Dừng") || msg.Contains("Hết");
        if (feedbackAccent != null)
            feedbackAccent.color = critical
                ? new Color(0.52f, 0.28f, 0.20f, 1f)
                : new Color(0.67f, 0.52f, 0.24f, 1f);

        feedbackGroup.alpha = 0f;
        const float fadeIn = 0.16f;
        const float fadeOut = 0.28f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            feedbackGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
            yield return null;
        }

        feedbackGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(duration);

        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            feedbackGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
            yield return null;
        }

        feedbackGroup.alpha = 0f;
        root.SetActive(false);
        feedbackRoutine = null;
    }

    private static string FormatObjective(string value)
    {
        const string prefix = "Mục tiêu:";
        return value != null && value.StartsWith(prefix)
            ? value.Substring(prefix.Length).Trim()
            : value;
    }

    private static TextMeshProUGUI FindText(string objectName)
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.name == objectName && text.gameObject.scene.isLoaded) return text;
        return null;
    }

    private static Image FindImage(string objectName)
    {
        foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (image.name == objectName && image.gameObject.scene.isLoaded) return image;
        return null;
    }

    public bool IsPlaying()
    {
        return currentState == Round2GameState.Playing;
    }
}
