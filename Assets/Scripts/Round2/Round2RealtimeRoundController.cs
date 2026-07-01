using UnityEngine;
using TMPro;

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
    public AudioClip collisionSound;
    public int currentCargo => cargo;
    public int civiliansSafe => safe;
    public int totalCivilians => 4;
    public int boatCapacity => 4;
    public float CurrentTimeRemaining => roundTimer;
    public string LastFailReason => lastFailReasonCode;
    public string currentObjectiveText = "Mục tiêu: Tìm nhà có tín hiệu cầu cứu.";

    [Header("HUD References")]
    public TextMeshProUGUI txtTimer;
    public TextMeshProUGUI txtDurability;
    public TextMeshProUGUI txtCargo;
    public TextMeshProUGUI txtSafe;
    public TextMeshProUGUI txtObjective;

    private void Start()
    {
        roundTimer = roundDurationSeconds;
        boatDurability = maxBoatDurability;
        cargo = 0;
        safe = 0;
        currentState = Round2GameState.Playing;
        currentObjectiveText = "Mục tiêu: Tìm nhà có tín hiệu cầu cứu.";

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
        if (txtDurability != null) txtDurability.text = $"Độ bền: {boatDurability}/{maxBoatDurability}";
        if (txtCargo != null) txtCargo.text = $"Trên thuyền: {cargo}/{boatCapacity}";
        if (txtSafe != null) txtSafe.text = $"An toàn: {safe}/{totalCivilians}";
        if (txtObjective != null) txtObjective.text = currentObjectiveText;
    }

    private void UpdateTimerHUD()
    {
        if (txtTimer != null)
        {
            int minutes = Mathf.FloorToInt(roundTimer / 60f);
            int seconds = Mathf.FloorToInt(roundTimer % 60f);
            txtTimer.text = $"Thời gian: {minutes:00}:{seconds:00}";
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
        
        ShowFeedback($"Va chạm mạnh! Độ bền -{amount}.");

        if (collisionSound != null)
        {
            var cam = UnityEngine.Camera.main;
            UnityEngine.AudioSource.PlayClipAtPoint(collisionSound, cam != null ? cam.transform.position : transform.position, 1.0f);
        }
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
        SetObjective("Mục tiêu: Hoàn thành cứu hộ!");
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
        StartCoroutine(FeedbackCoroutine(message, 2f));
    }

    private System.Collections.IEnumerator FeedbackCoroutine(string msg, float duration)
    {
        string oldText = currentObjectiveText;
        if (txtObjective != null)
        {
            txtObjective.text = msg;
            txtObjective.color = UnityEngine.Color.red;
        }
        
        yield return new WaitForSeconds(duration);
        
        if (currentState == Round2GameState.Playing && txtObjective != null)
        {
            txtObjective.text = oldText;
            txtObjective.color = UnityEngine.Color.yellow;
        }
    }

    public bool IsPlaying()
    {
        return currentState == Round2GameState.Playing;
    }
}
