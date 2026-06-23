using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class RouteEdge : MonoBehaviour
{
    [SerializeField] private RouteNode fromNode;
    [SerializeField] private RouteNode toNode;
    [SerializeField] private bool isBlocked;
    [SerializeField] private GameObject blockerObject;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material blockedMaterial;

    private LineRenderer lineRenderer;

    public RouteNode FromNode => fromNode;
    public RouteNode ToNode => toNode;
    public bool IsBlocked => isBlocked;
    public GameObject BlockerObject => blockerObject;

    private void Awake()
    {
        CacheRenderer();
        RefreshVisualState();
    }

    private void OnValidate()
    {
        CacheRenderer();
        RefreshVisualState();
    }

    public bool Connects(RouteNode a, RouteNode b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        return fromNode == a && toNode == b || fromNode == b && toNode == a;
    }

    public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
        RefreshVisualState();
    }

    private void CacheRenderer()
    {
        lineRenderer ??= GetComponent<LineRenderer>();
    }

    private void RefreshVisualState()
    {
        if (blockerObject != null)
        {
            blockerObject.SetActive(isBlocked);
        }

        if (lineRenderer == null)
        {
            return;
        }

        Material targetMaterial = isBlocked ? blockedMaterial : normalMaterial;
        if (targetMaterial != null)
        {
            lineRenderer.sharedMaterial = targetMaterial;
        }
    }
}
