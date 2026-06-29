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
    private static readonly Color VictoryAccent = new Color(0.35f, 0.50f, 0.41f, 1f); // #5A8069 (slightly darker victory accent)
    private static readonly Color DefeatAccent = new Color(0.55f, 0.27f, 0.23f, 1f); // #8C453A (slightly darker defeat accent)
    private static readonly Color VictoryTitle = new Color(0.47f, 0.66f, 0.54f, 1f); // #78A88B (brightened muted sage green)
    private static readonly Color DefeatTitle = new Color(0.72f, 0.36f, 0.30f, 1f); // #B85B4D (brightened muted terracotta red)
    private static readonly Color OverlayColor = new Color(0.015f, 0.045f, 0.045f, 0.64f);

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
            if (txtRetryText != null) txtRetryText.text = "CHƠI LẠI";
            ApplyOutcomeAccent(VictoryAccent);
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

    private void OnRetryClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
