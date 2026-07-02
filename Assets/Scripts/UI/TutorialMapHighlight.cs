using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialMapHighlight : MaskableGraphic
{
    [SerializeField] private Vector2 markerCenter = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 markerSize = new Vector2(0.08f, 0.12f);
    [SerializeField] private Vector2 connectorEnd = new Vector2(0f, 0.5f);
    [SerializeField, Min(1f)] private float lineThickness = 2.5f;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = rectTransform.rect;
        Vector2 center = NormalizedToLocal(rect, markerCenter);
        Vector2 size = new Vector2(
            markerSize.x * rect.width,
            markerSize.y * rect.height);
        Vector2 halfSize = size * 0.5f;
        Vector2 end = NormalizedToLocal(rect, connectorEnd);
        Vector2 direction = end - center;

        if (direction.sqrMagnitude > 0.001f)
        {
            float xScale = Mathf.Abs(direction.x) > 0.001f
                ? halfSize.x / Mathf.Abs(direction.x)
                : float.PositiveInfinity;
            float yScale = Mathf.Abs(direction.y) > 0.001f
                ? halfSize.y / Mathf.Abs(direction.y)
                : float.PositiveInfinity;
            Vector2 start = center + direction * Mathf.Min(xScale, yScale);
            AddLine(vertexHelper, start, end, lineThickness);
        }

        Vector2 bottomLeft = center - halfSize;
        Vector2 topRight = center + halfSize;
        Vector2 topLeft = new Vector2(bottomLeft.x, topRight.y);
        Vector2 bottomRight = new Vector2(topRight.x, bottomLeft.y);

        AddLine(vertexHelper, topLeft, topRight, lineThickness);
        AddLine(vertexHelper, topRight, bottomRight, lineThickness);
        AddLine(vertexHelper, bottomRight, bottomLeft, lineThickness);
        AddLine(vertexHelper, bottomLeft, topLeft, lineThickness);
    }

    private static Vector2 NormalizedToLocal(Rect rect, Vector2 normalized)
    {
        return new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
            Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
    }

    private void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
        int index = vertexHelper.currentVertCount;

        vertexHelper.AddVert(start - normal, color, Vector2.zero);
        vertexHelper.AddVert(start + normal, color, Vector2.zero);
        vertexHelper.AddVert(end + normal, color, Vector2.zero);
        vertexHelper.AddVert(end - normal, color, Vector2.zero);
        vertexHelper.AddTriangle(index, index + 1, index + 2);
        vertexHelper.AddTriangle(index, index + 2, index + 3);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        markerCenter = Clamp01(markerCenter);
        markerSize = Clamp01(markerSize);
        connectorEnd = Clamp01(connectorEnd);
        lineThickness = Mathf.Max(1f, lineThickness);
        SetVerticesDirty();
    }

    private static Vector2 Clamp01(Vector2 value)
    {
        return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
    }
#endif
}
