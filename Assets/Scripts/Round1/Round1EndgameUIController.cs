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

            if (txtTitle != null)
            {
                txtTitle.text = "HOÀN THÀNH";
                ColorUtility.TryParseHtmlString("#FFD700", out Color gold);
                txtTitle.color = gold;
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.text = "Bạn đã đưa tất cả người dân đến nơi an toàn.";
                txtSubtitle.gameObject.SetActive(true);
            }

            if (txtDetail != null)
            {
                txtDetail.text = "Cứu hộ thành công: 3/3 người";
                txtDetail.gameObject.SetActive(true);
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

            if (txtTitle != null)
            {
                txtTitle.text = "THẤT BẠI";
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
                txtDetail.text = detail;
                txtDetail.gameObject.SetActive(!string.IsNullOrEmpty(detail));
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
