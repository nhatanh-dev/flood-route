using TMPro;
using UnityEngine;

public class RescueCountWorldBadge : MonoBehaviour
{
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private Renderer backgroundRenderer;
    [SerializeField] private Color waitingColor = new(0.86f, 0.27f, 0.14f, 0.95f);
    [SerializeField] private Color completeColor = new(0.24f, 0.70f, 0.36f, 0.95f);
    [SerializeField] private string waitingPrefix = "x";
    [SerializeField] private string completeText = "Done";

    private MaterialPropertyBlock propertyBlock;

    public void SetWaitingCount(int waitingPeople, int totalPeople)
    {
        waitingPeople = Mathf.Clamp(waitingPeople, 0, Mathf.Max(0, totalPeople));

        if (badgeText != null)
        {
            badgeText.text = waitingPeople > 0 ? $"{waitingPrefix}{waitingPeople}" : completeText;
        }

        ApplyBackground(waitingPeople > 0 ? waitingColor : completeColor);
    }

    private void Awake()
    {
        if (badgeText == null)
        {
            badgeText = GetComponentInChildren<TMP_Text>(true);
        }

        if (backgroundRenderer == null)
        {
            backgroundRenderer = GetComponentInChildren<Renderer>(true);
        }
    }

    private void ApplyBackground(Color color)
    {
        if (backgroundRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        backgroundRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        backgroundRenderer.SetPropertyBlock(propertyBlock);
    }
}
