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
        private static readonly Color VictoryAccent = new Color(0.40f, 0.62f, 0.49f, 1f);
        private static readonly Color DefeatAccent = new Color(0.66f, 0.28f, 0.24f, 1f);
        private static readonly Color OverlayColor = new Color(0.02f, 0.08f, 0.09f, 0.76f);

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
                txtTitle.color = VictoryAccent;
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.text = "Bạn đã đưa tất cả người dân đến nơi an toàn.";
                txtSubtitle.gameObject.SetActive(true);
            }

            if (txtDetail != null)
            {
                int safe = roundController != null ? roundController.civiliansSafe : 3;
                int total = roundController != null ? roundController.totalCivilians : 3;
                txtDetail.text = $"Cứu hộ thành công: {safe}/{total} người";
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
                txtTitle.color = DefeatAccent;
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.text = reason;
                txtSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(reason));
            }

            if (txtDetail != null)
            {
                txtDetail.text = detail;
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
            string boat = $"Độ bền thuyền còn lại: {roundController.currentBoatDurability}/{roundController.maxBoatDurability}";
            string remaining = $"Thời gian còn lại: {time}";

            txtStats.text = includeRescueCount
                ? $"Người dân an toàn: {roundController.civiliansSafe}/{roundController.totalCivilians}\n{boat}\n{remaining}"
                : $"{boat}\n{remaining}";
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
