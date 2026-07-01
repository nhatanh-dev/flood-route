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

        [Header("Button")]
        public Button btnRetry;
        public Button btnContinueRound2;
        public TMP_Text txtRetryHint;

        private void Awake()
        {
            if (endgameRoot != null)
                endgameRoot.SetActive(false);

            if (btnRetry != null)
            {
                btnRetry.onClick.RemoveAllListeners();
                btnRetry.onClick.AddListener(RetryCurrentScene);
            }
            if (btnContinueRound2 != null)
            {
                btnContinueRound2.onClick.RemoveAllListeners();
                btnContinueRound2.onClick.AddListener(() => SceneManager.LoadScene("Round2_MissionBriefing"));
                btnContinueRound2.gameObject.SetActive(false);
            }
        }

        public void ShowWin()
        {
            HideGameplayHUD();
            
            if (endgameRoot != null)
                endgameRoot.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(true);

            if (dimOverlay != null)
                dimOverlay.color = new Color(0, 0, 0, 0.45f);

            var rainAudio = FindObjectOfType<R1RainAmbienceController>();
            if (rainAudio != null) rainAudio.FadeToEndgameVolume();

            var rc = FindObjectOfType<R1RealtimeRoundController>();
            string statsStr = "";
            if (rc != null)
            {
                int min = Mathf.FloorToInt(rc.currentTimeRemaining / 60);
                int sec = Mathf.FloorToInt(rc.currentTimeRemaining % 60);
                statsStr = $"Người dân an toàn: {rc.civiliansSafe}/{rc.totalCivilians}\n" +
                           $"Độ bền còn lại: {rc.currentBoatDurability}/{rc.maxBoatDurability}\n" +
                           $"Thời gian còn lại: {min:00}:{sec:00}";
            }

            if (txtTitle != null)
            {
                txtTitle.text = "HOÀN THÀNH CỨU HỘ!";
                ColorUtility.TryParseHtmlString("#FFD700", out Color gold);
                txtTitle.color = gold;
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.text = "Bạn đã đưa toàn bộ người dân đến nơi an toàn.";
                txtSubtitle.gameObject.SetActive(true);
            }

            if (txtDetail != null)
            {
                txtDetail.text = statsStr;
                txtDetail.gameObject.SetActive(true);
            }

            var tmps = endgameRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.text.Contains("Nhấn R") || t.text.Contains("R để"))
                {
                    t.text = "Nhấn R để chơi lại";
                }
            }

            if (btnRetry != null)
            {
                var btnText = btnRetry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Chơi lại";
            }
            if (btnContinueRound2 != null)
            {
                btnContinueRound2.gameObject.SetActive(true);
            }
        }

        public void ShowLose(string reason, string detail)
        {
            HideGameplayHUD();
            
            if (endgameRoot != null)
                endgameRoot.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(false);

            if (dimOverlay != null)
                dimOverlay.color = new Color(0, 0, 0, 0.65f);

            var rainAudio = FindObjectOfType<R1RainAmbienceController>();
            if (rainAudio != null) rainAudio.FadeToEndgameVolume();

            var rc = FindObjectOfType<R1RealtimeRoundController>();
            string statsStr = detail;
            if (rc != null)
            {
                int min = Mathf.FloorToInt(rc.currentTimeRemaining / 60);
                int sec = Mathf.FloorToInt(rc.currentTimeRemaining % 60);
                statsStr = $"Người dân an toàn: {rc.civiliansSafe}/{rc.totalCivilians}\n" +
                           $"Độ bền còn lại: {rc.currentBoatDurability}/{rc.maxBoatDurability}\n" +
                           $"Thời gian còn lại: {min:00}:{sec:00}";
            }

            if (txtTitle != null)
            {
                txtTitle.text = "NHIỆM VỤ THẤT BẠI";
                ColorUtility.TryParseHtmlString("#FF3333", out Color red);
                txtTitle.color = red;
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.text = reason;
                txtSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(reason));
            }

            if (txtDetail != null)
            {
                txtDetail.text = statsStr;
                txtDetail.gameObject.SetActive(!string.IsNullOrEmpty(statsStr));
            }

            var tmps = endgameRoot.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.text.Contains("Nhấn R") || t.text.Contains("R để"))
                {
                    t.text = "Nhấn R để thử lại";
                }
            }

            if (btnRetry != null)
            {
                var btnText = btnRetry.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Thử lại";
            }
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
