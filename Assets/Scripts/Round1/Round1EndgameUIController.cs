using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Round1
{
    public class Round1EndgameUIController : MonoBehaviour
    {
        [Header("Roots")]
        public GameObject endgameRoot;
        public GameObject[] hudRootsToHide;

        [Header("Background & Dim")]
        public Image backgroundImage;
        public Image dimOverlay;

        [Header("Result Card Texts")]
        public TMP_Text txtTitle;
        public TMP_Text txtSubtitle;
        public TMP_Text txtDetail;
        public TMP_Text txtStats;

        [Header("Outcome Accent")]
        public Image outcomeAccent;

        [Header("Button")]
        public Button btnRetry;
        public TMP_Text txtRetryHint;

        private R1RealtimeRoundController roundController;
        private static readonly Color VictoryAccent = new Color(0.35f, 0.50f, 0.41f, 1f); // #5A8069 (slightly darker victory accent)
        private static readonly Color DefeatAccent = new Color(0.55f, 0.27f, 0.23f, 1f); // #8C453A (slightly darker defeat accent)
        private static readonly Color VictoryTitle = new Color(0.47f, 0.66f, 0.54f, 1f); // #78A88B (brightened muted sage green)
        private static readonly Color DefeatTitle = new Color(0.72f, 0.36f, 0.30f, 1f); // #B85B4D (brightened muted terracotta red)
        private static readonly Color OverlayColor = new Color(0.015f, 0.045f, 0.045f, 0.64f);

        private void Awake()
        {
            roundController = FindAnyObjectByType<R1RealtimeRoundController>();

            if (endgameRoot != null)
                endgameRoot.SetActive(false);

            if (btnRetry != null)
            {
                btnRetry.onClick.RemoveAllListeners();
                btnRetry.onClick.AddListener(RetryCurrentScene);
            }
        }

        public void ShowWin()
        {
            HideGameplayHUD();
            
            if (endgameRoot != null)
                endgameRoot.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(false);

            if (dimOverlay != null)
                dimOverlay.color = OverlayColor;

            var rainAudio = FindObjectOfType<R1RainAmbienceController>();
            if (rainAudio != null) rainAudio.FadeToEndgameVolume();

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
                txtSubtitle.gameObject.SetActive(true);
            }

            if (txtDetail != null)
            {
                int safe = roundController != null ? roundController.civiliansSafe : 3;
                int total = roundController != null ? roundController.totalCivilians : 3;
                txtDetail.text = $"Cứu hộ thành công: {safe}/{total} người";
                txtDetail.fontSize = 21f; // Emphasized
                txtDetail.color = new Color(0.97f, 0.95f, 0.92f, 1f); // Slightly brightened warm off-white
                txtDetail.gameObject.SetActive(true);
            }

            ApplyOutcomeAccent(VictoryAccent);
            RefreshStats(includeRescueCount: false);
        }

        public void ShowLose(string reason, string detail)
        {
            HideGameplayHUD();
            
            if (endgameRoot != null)
                endgameRoot.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(false);

            if (dimOverlay != null)
                dimOverlay.color = OverlayColor;

            var rainAudio = FindObjectOfType<R1RainAmbienceController>();
            if (rainAudio != null) rainAudio.FadeToEndgameVolume();

            if (txtTitle != null)
            {
                txtTitle.text = "THẤT BẠI";
                txtTitle.color = DefeatTitle;
                if (txtTitle.fontMaterial != null)
                {
                    txtTitle.fontMaterial.SetColor("_GlowColor", new Color(0f, 0f, 0f, 0.6f)); // Dark underlay/shadow instead of glow
                }
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.text = reason;
                txtSubtitle.color = new Color(0.97f, 0.95f, 0.92f, 1f); // Slightly brightened warm off-white
                txtSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(reason));
            }

            if (txtDetail != null)
            {
                txtDetail.text = detail;
                txtDetail.fontSize = 18f; // Secondary explanatory text
                txtDetail.color = new Color(0.80f, 0.83f, 0.81f, 1f); // Slightly brightened gray-green
                txtDetail.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            }

            ApplyOutcomeAccent(DefeatAccent);
            RefreshStats(includeRescueCount: true);
        }

        private void ApplyOutcomeAccent(Color color)
        {
            if (outcomeAccent != null)
                outcomeAccent.color = color;
        }

        private void RefreshStats(bool includeRescueCount)
        {
            if (txtStats == null || roundController == null)
                return;

            int minutes = Mathf.FloorToInt(roundController.currentTimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(roundController.currentTimeRemaining % 60f);
            string time = $"{minutes:00}:{seconds:00}";
            txtStats.text =
                $"{roundController.civiliansSafe}/{roundController.totalCivilians}\n" +
                $"{roundController.currentBoatDurability}/{roundController.maxBoatDurability}\n" +
                time;
            txtStats.gameObject.SetActive(true);
        }

        private void HideGameplayHUD()
        {
            if (hudRootsToHide != null)
            {
                foreach (var root in hudRootsToHide)
                {
                    if (root != null) root.SetActive(false);
                }
            }
        }

        public void RetryCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
