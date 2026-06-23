using UnityEngine;

public sealed class DebrisVisualPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.4f;
    [SerializeField] private float scaleAmount = 0.08f;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private float alphaAmount = 0.1f;

    private Vector3 baseScale;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        baseScale = transform.localScale;
        propertyBlock = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    private void OnEnable()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }
    }

    private void Update()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        transform.localScale = baseScale * (1f + scaleAmount * pulse);
        ApplyAlphaPulse(pulse);
    }

    private void ApplyAlphaPulse(float pulse)
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        float alphaOffset = Mathf.Lerp(-alphaAmount, alphaAmount, pulse);
        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                continue;
            }

            Color color = Color.white;
            Material sharedMaterial = targetRenderer.sharedMaterial;
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                color = sharedMaterial.GetColor("_BaseColor");
            }
            else if (sharedMaterial.HasProperty("_Color"))
            {
                color = sharedMaterial.GetColor("_Color");
            }

            color.a = Mathf.Clamp01(color.a + alphaOffset);
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
