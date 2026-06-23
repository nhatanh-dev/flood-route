using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class Round2IntroController : MonoBehaviour
{
    [SerializeField] private GameObject introPanelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text continueText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Text legacyTitleText;
    [SerializeField] private Text legacyBodyText;
    [SerializeField] private Text legacyContinueText;
    [SerializeField] private Text legacyPageText;

    private bool introCompleted;

    public bool IsIntroActive => !introCompleted && introPanelRoot != null && introPanelRoot.activeSelf;

    private void Awake()
    {
        EnsureReferences();
        ApplyDefaultText();
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
        Mouse mouse = Mouse.current;
        bool keyboardDismiss = keyboard != null
            && (keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame);
        bool mouseDismiss = mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (keyboardDismiss || mouseDismiss)
        {
            HideIntro();
        }
    }

    [ContextMenu("Show Intro")]
    public void ShowIntro()
    {
        EnsureReferences();
        introCompleted = false;

        if (introPanelRoot != null)
        {
            introPanelRoot.SetActive(true);
        }
    }

    [ContextMenu("Hide Intro")]
    public void HideIntro()
    {
        introCompleted = true;

        if (introPanelRoot != null)
        {
            introPanelRoot.SetActive(false);
        }
    }

    private void EnsureReferences()
    {
        introPanelRoot ??= gameObject;

        titleText ??= FindTmpText("TXT_IntroTitle") ?? FindTmpText("R2_Intro_Title");
        bodyText ??= FindTmpText("TXT_IntroBody") ?? FindTmpText("R2_Intro_Goal");
        continueText ??= FindTmpText("TXT_IntroContinue") ?? FindTmpText("R2_Intro_Controls");
        pageText ??= FindTmpText("TXT_IntroPage") ?? FindTmpText("R2_Intro_Hint");
    }

    private void ApplyDefaultText()
    {
        SetText(titleText, legacyTitleText, "ROUND 2 - THÔN TRUNG");
        SetText(bodyText, legacyBodyText,
            "Cứu 3 người ở Nhà ven sông.\n"
            + "Đưa họ về Điểm trú trước khi hết lượt.");
        SetText(pageText, legacyPageText, "Rác chặn tuyến chính. Hãy quan sát và dùng Q đúng lúc.");
        SetText(continueText, legacyContinueText,
            "Click điểm kế bên để di chuyển.\n"
            + "Nhấn Q để chờ.\n"
            + "SPACE / ENTER / CLICK để bắt đầu");
    }

    private TMP_Text FindTmpText(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static void SetText(TMP_Text tmpText, Text legacyText, string value)
    {
        if (tmpText != null)
        {
            tmpText.text = value;
        }

        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }
}
