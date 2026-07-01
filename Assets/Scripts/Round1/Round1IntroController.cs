using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Round1
{
    public sealed class Round1IntroController : MonoBehaviour
    {
        [SerializeField] private GameObject introPanelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text continueText;
        [SerializeField] private TMP_Text pageIndicatorText;

        private int pageIndex;
        private bool completed;

        public bool IsIntroActive => introPanelRoot != null && introPanelRoot.activeSelf && !completed;

        public event Action IntroCompleted;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Start()
        {
            ShowIntro();
        }

        private void Update()
        {
            if (!IsIntroActive)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                AdvanceIntro();
            }
        }

        public void ShowIntro()
        {
            EnsureReferences();
            completed = false;
            pageIndex = 0;

            if (introPanelRoot != null)
            {
                introPanelRoot.SetActive(true);
            }

            ApplyPage();
        }

        public void SkipIntro()
        {
            CompleteIntro();
        }

        public void CompleteIntro()
        {
            if (completed)
            {
                return;
            }

            completed = true;

            if (introPanelRoot != null)
            {
                introPanelRoot.SetActive(false);
            }

            IntroCompleted?.Invoke();
        }

        private void AdvanceIntro()
        {
            if (pageIndex == 0)
            {
                pageIndex = 1;
                ApplyPage();
                return;
            }

            CompleteIntro();
        }

        private void ApplyPage()
        {
            if (pageIndex == 0)
            {
                SetText(
                    "VÒNG 1 — NHIỆM VỤ CỨU HỘ",
                    "CỨU 3 NGƯỜI DÂN TRƯỚC KHI HẾT THỜI GIAN.\n\n• NHÀ BÀ: 1 NGƯỜI DÂN\n• NHÀ TƯ: 2 NGƯỜI DÂN\n\nĐƯA TẤT CẢ VỀ ĐIỂM TRÚ ĐỂ CHIẾN THẮNG.",
                    "[SPACE / ENTER] TIẾP TỤC",
                    "1 / 2");
                return;
            }

            SetText(
                "ĐIỀU KHIỂN & HƯỚNG DẪN",
                "• DI CHUYỂN: WASD / MŨI TÊN\n• BẢN ĐỒ: TAB\n• CỨU/THẢ NGƯỜI: E\n\nHÃY LÁI THUYỀN CẨN THẬN ĐỂ TRÁNH VA CHẠM VÀ HỎNG THUYỀN.",
                "[SPACE / ENTER] BẮT ĐẦU",
                "2 / 2");
        }

        private void SetText(string title, string body, string continuePrompt, string pageIndicator)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (continueText != null)
            {
                continueText.text = continuePrompt;
            }

            if (pageIndicatorText != null)
            {
                pageIndicatorText.text = pageIndicator;
            }
        }

        private void EnsureReferences()
        {
            introPanelRoot ??= gameObject;
            titleText ??= transform.Find("TXT_IntroTitle")?.GetComponent<TMP_Text>();
            bodyText ??= transform.Find("TXT_IntroBody")?.GetComponent<TMP_Text>();
            continueText ??= transform.Find("TXT_IntroContinue")?.GetComponent<TMP_Text>();
            pageIndicatorText ??= transform.Find("TXT_IntroPageIndicator")?.GetComponent<TMP_Text>();
        }
    }
}
