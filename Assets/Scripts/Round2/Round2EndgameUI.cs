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
    public Image outcomeAccent;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtSubtitle;
    public TextMeshProUGUI txtMessage;
    public TextMeshProUGUI txtStats;
    public Button btnRetry;
    public TextMeshProUGUI txtRetryText;
    public TextMeshProUGUI txtRetryHint;

    [Header("Optional")]
    public GameObject gameplayHUDCanvas;

    private bool overlayActive = false;
    private static readonly Color VictoryAccent = new Color(0.40f, 0.62f, 0.49f, 1f);
    private static readonly Color DefeatAccent = new Color(0.66f, 0.28f, 0.24f, 1f);
    private static readonly Color OverlayColor = new Color(0.02f, 0.08f, 0.09f, 0.76f);

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
            if (bg != null) bg.color = OverlayColor;
            overlayPanel.SetActive(true);
        }

        bool isWin = roundController.currentState == Round2GameState.Win;

        if (isWin)
        {
            if (txtTitle != null) { txtTitle.text = "HOÀN THÀNH"; txtTitle.color = VictoryAccent; }
            if (txtSubtitle != null) txtSubtitle.text = "Bạn đã đưa tất cả người dân đến nơi an toàn.";
            if (txtMessage != null) txtMessage.text = $"Cứu hộ thành công: {roundController.civiliansSafe}/{roundController.totalCivilians} người";
            if (txtRetryText != null) txtRetryText.text = "CHƠI LẠI";
            ApplyOutcomeAccent(VictoryAccent);
        }
        else // Fail
        {
            if (txtTitle != null) { txtTitle.text = "THẤT BẠI"; txtTitle.color = DefeatAccent; }
            if (txtRetryText != null) txtRetryText.text = "CHƠI LẠI";
            ApplyOutcomeAccent(DefeatAccent);

            string code = roundController.LastFailReason;
            if (code == "boat_broken")
            {
                if (txtSubtitle != null) txtSubtitle.text = "Thuyền bị hỏng!";
                if (txtMessage != null) txtMessage.text = "Thuyền đã va chạm quá nhiều trong quá trình cứu hộ.";
            }
            else if (code == "time_out")
            {
                if (txtSubtitle != null) txtSubtitle.text = "Hết thời gian!";
                if (txtMessage != null) txtMessage.text = "Bạn đã không hoàn thành nhiệm vụ trước khi hết thời gian.";
            }
            else
            {
                if (txtSubtitle != null) txtSubtitle.text = "Nhiệm vụ chưa hoàn thành.";
                if (txtMessage != null) txtMessage.text = "Nhiệm vụ thất bại.";
            }
        }

        if (txtRetryHint != null)
            txtRetryHint.text = "Nhấn R để chơi lại";

        if (txtStats != null)
        {
            int m = Mathf.FloorToInt(roundController.CurrentTimeRemaining / 60f);
            int s = Mathf.FloorToInt(roundController.CurrentTimeRemaining % 60f);
            string timeStr = $"{m:00}:{s:00}";

            txtStats.text = $"Người dân an toàn: {roundController.civiliansSafe}/{roundController.totalCivilians}\n" +
                            $"Độ bền thuyền còn lại: {roundController.currentBoatDurability}/{roundController.maxBoatDurability}\n" +
                            $"Thời gian còn lại: {timeStr}";
        }
    }

    private void ApplyOutcomeAccent(Color color)
    {
        if (outcomeAccent != null)
            outcomeAccent.color = color;
    }

    private void OnRetryClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
