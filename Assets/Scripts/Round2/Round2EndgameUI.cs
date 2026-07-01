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
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtMessage;
    public TextMeshProUGUI txtStats;
    public Button btnRetry;
    public TextMeshProUGUI txtRetryText;

    [Header("Optional")]
    public GameObject gameplayHUDCanvas;

    private bool overlayActive = false;

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

        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }

        if (btnRetry != null)
        {
            btnRetry.onClick.AddListener(OnRetryClicked);
        }
    }

    private void Update()
    {
        if (roundController == null) return;

        if (!overlayActive && !roundController.IsPlaying())
        {
            ShowOverlay();
        }

        if (overlayActive)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                OnRetryClicked();
            }
        }
    }

    private void ShowOverlay()
    {
        overlayActive = true;

        if (gameplayHUDCanvas != null)
        {
            gameplayHUDCanvas.SetActive(false);
        }

        if (overlayPanel != null)
        {
            Image bg = overlayPanel.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0, 0, 0, 0.80f);
            overlayPanel.SetActive(true);
        }

        bool isWin = roundController.currentState == Round2GameState.Win;

        if (isWin)
        {
            if (txtTitle != null) { txtTitle.text = "HOÀN THÀNH CỨU HỘ!"; txtTitle.color = Color.green; }
            if (txtMessage != null) txtMessage.text = "Bạn đã đưa toàn bộ người dân đến nơi an toàn.";
            if (txtRetryText != null) txtRetryText.text = "Chơi lại";
        }
        else // Fail
        {
            if (txtTitle != null) { txtTitle.text = "NHIỆM VỤ THẤT BẠI"; txtTitle.color = Color.red; }
            if (txtRetryText != null) txtRetryText.text = "Thử lại";

            string code = roundController.LastFailReason;
            if (code == "boat_broken")
            {
                if (txtMessage != null) txtMessage.text = "Thuyền đã bị hỏng trong quá trình cứu hộ.";
            }
            else if (code == "time_out")
            {
                if (txtMessage != null) txtMessage.text = "Hết thời gian cứu hộ.";
            }
            else
            {
                if (txtMessage != null) txtMessage.text = "Nhiệm vụ thất bại.";
            }
        }

        if (txtStats != null)
        {
            int m = Mathf.FloorToInt(roundController.CurrentTimeRemaining / 60f);
            int s = Mathf.FloorToInt(roundController.CurrentTimeRemaining % 60f);
            string timeStr = $"{m:00}:{s:00}";

            txtStats.text = $"Người dân an toàn: {roundController.civiliansSafe}/{roundController.totalCivilians}\n" +
                            $"Độ bền còn lại: {roundController.currentBoatDurability}/{roundController.maxBoatDurability}\n" +
                            $"Thời gian còn lại: {timeStr}";
        }

        var tmps = overlayPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps)
        {
            if (t.text.Contains("Nhấn R") || t.text.Contains("R để"))
            {
                t.text = isWin ? "Nhấn R để chơi lại" : "Nhấn R để thử lại";
            }
        }
    }

    private void OnRetryClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
