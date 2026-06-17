using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WaterlinePulse : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 1.2f;
    [SerializeField] private float scaleAmount = 0.12f;

    [Header("Opacity")]
    [SerializeField] private float minAlpha = 0.06f;
    [SerializeField] private float maxAlpha = 0.18f;

    private Renderer targetRenderer;
    private Material runtimeMaterial;
    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        runtimeMaterial = targetRenderer.material;

        baseScale = transform.localScale;
        baseColor = runtimeMaterial.color;
    }

    private void Update()
    {
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float scaleMultiplier = 1f + wave * scaleAmount;
        transform.localScale = new Vector3(
            baseScale.x * scaleMultiplier,
            baseScale.y,
            baseScale.z * scaleMultiplier
        );

        float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
        Color color = baseColor;
        color.a = alpha;
        runtimeMaterial.color = color;
    }
}