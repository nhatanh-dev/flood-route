using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class DebrisCurrentPathVisual : MonoBehaviour
{
    [SerializeField] private Transform fromTarget;
    [SerializeField] private Transform toTarget;
    [SerializeField] private float yOffset = 0.08f;
    [SerializeField] private float textureScrollSpeed = 0.25f;

    private LineRenderer lineRenderer;
    private Material runtimeMaterial;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer.sharedMaterial != null)
        {
            runtimeMaterial = Instantiate(lineRenderer.sharedMaterial);
            lineRenderer.sharedMaterial = runtimeMaterial;
        }
    }

    private void LateUpdate()
    {
        if (lineRenderer == null || fromTarget == null || toTarget == null)
        {
            return;
        }

        Vector3 from = fromTarget.position + Vector3.up * yOffset;
        Vector3 to = toTarget.position + Vector3.up * yOffset;
        Vector3 middle = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 0.02f;

        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, middle);
        lineRenderer.SetPosition(2, to);

        if (runtimeMaterial != null)
        {
            runtimeMaterial.mainTextureOffset = new Vector2(Time.time * textureScrollSpeed, 0f);
        }
    }

    public void SetTargets(Transform from, Transform to)
    {
        fromTarget = from;
        toTarget = to;
    }
}
