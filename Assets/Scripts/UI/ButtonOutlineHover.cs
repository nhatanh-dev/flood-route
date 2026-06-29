using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonOutlineHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("The OutlineFocus child Image component")]
    public Image outlineImage;

    [Range(0f, 1f)] public float hoverAlpha = 0.784f;
    [Range(0f, 1f)] public float pressedAlpha = 0.392f;
    public float fadeDuration = 0.08f;

    float _targetAlpha = 0f;
    float _currentAlpha = 0f;

    void Awake()
    {
        if (outlineImage == null)
        {
            var child = transform.Find("OutlineFocus");
            if (child != null) outlineImage = child.GetComponent<Image>();
        }
        if (outlineImage != null)
            SetAlpha(0f);
    }

    void Update()
    {
        if (outlineImage == null) return;
        _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha,
                                          Time.deltaTime / Mathf.Max(fadeDuration, 0.001f));
        SetAlpha(_currentAlpha);
    }

    public void OnPointerEnter(PointerEventData e) => _targetAlpha = hoverAlpha;
    public void OnPointerExit(PointerEventData e) => _targetAlpha = 0f;
    public void OnPointerDown(PointerEventData e) => _targetAlpha = pressedAlpha;
    public void OnPointerUp(PointerEventData e) => _targetAlpha = hoverAlpha;

    void SetAlpha(float a)
    {
        if (outlineImage == null) return;
        var c = outlineImage.color;
        c.a = a;
        outlineImage.color = c;
    }
}
