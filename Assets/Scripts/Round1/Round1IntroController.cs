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
                    "ROUND 1 — RESCUE MISSION",
                    "RESCUE 3 CIVILIANS BEFORE THE 9 TURNS RUN OUT.\n\n• NHA BA: 2 CIVILIANS\n• NHA TU: 1 CIVILIAN\n\nBRING ALL 3 TO BAI DINH TO WIN.",
                    "[SPACE / ENTER] NEXT",
                    "1 / 2");
                return;
            }

            SetText(
                "CONTROLS & ROUTE TIPS",
                "• MOVE: WASD / ARROW KEYS\n• WAIT: PRESS Q AT BEN PHU\n\nTHE BEN PHU → CAU TRE ROUTE MAY BE BLOCKED.\nWAIT AT BEN PHU UNTIL IT OPENS.\n\n• BAI DINH CAPACITY: 3\n• GO CAO CAPACITY: 2",
                "[SPACE / ENTER] START",
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
