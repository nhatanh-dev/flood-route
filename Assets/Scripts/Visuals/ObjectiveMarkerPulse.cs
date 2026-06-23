using UnityEngine;

public class ObjectiveMarkerPulse : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private Color markerColor = Color.white;
    [SerializeField] private float markerScale = 1f;
    [SerializeField] private float verticalOffset = 1.6f;
    [SerializeField] private float pulseSpeed = 1.6f;
    [SerializeField] private float pulseAmount = 0.08f;

    [Header("Optional")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer[] tintedRenderers;

    private Vector3 baseLocalPosition;
    private Vector3 baseScale;
    private MaterialPropertyBlock propertyBlock;

    public Color MarkerColor
    {
        get { return markerColor; }
        set
        {
            markerColor = value;
            ApplyColor();
        }
    }

    public float MarkerScale
    {
        get { return markerScale; }
        set
        {
            markerScale = Mathf.Max(0.01f, value);
            CacheBaseTransform();
        }
    }

    public float VerticalOffset
    {
        get { return verticalOffset; }
        set
        {
            verticalOffset = value;
            transform.localPosition = new Vector3(baseLocalPosition.x, verticalOffset, baseLocalPosition.z);
        }
    }

    public float PulseSpeed
    {
        get { return pulseSpeed; }
        set { pulseSpeed = Mathf.Max(0f, value); }
    }

    private void Awake()
    {
        CacheBaseTransform();
        ApplyColor();
    }

    private void OnValidate()
    {
        markerScale = Mathf.Max(0.01f, markerScale);
        pulseSpeed = Mathf.Max(0f, pulseSpeed);
        pulseAmount = Mathf.Max(0f, pulseAmount);

        if (!Application.isPlaying)
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            transform.localPosition = new Vector3(transform.localPosition.x, verticalOffset, transform.localPosition.z);
            visualRoot.localScale = Vector3.one * markerScale;
            ApplyColor();
        }
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        visualRoot.localScale = baseScale * markerScale * pulse;
    }

    private void CacheBaseTransform()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        baseLocalPosition = transform.localPosition;
        transform.localPosition = new Vector3(baseLocalPosition.x, verticalOffset, baseLocalPosition.z);
        baseScale = Vector3.one;
        visualRoot.localScale = baseScale * markerScale;
    }

    private void ApplyColor()
    {
        if (tintedRenderers == null || tintedRenderers.Length == 0)
        {
            tintedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        foreach (Renderer renderer in tintedRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", markerColor);
            propertyBlock.SetColor("_Color", markerColor);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
