using System.Collections.Generic;
using UnityEngine;

public enum NodeVisualState
{
    Normal,
    Current,
    Available,
    RescueTarget,
    Shelter,
    Blocked,
    Completed
}

public enum RouteVisualState
{
    Normal,
    Available,
    Suggested,
    Active,
    Blocked,
    Completed
}

[ExecuteAlways]
public sealed class RouteVisualManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RouteGraphManager graphManager;
    [SerializeField] private BoatRouteMover boatMover;
    [SerializeField] private Round2GameController round2GameController;
    [SerializeField] private Transform nodesRoot;
    [SerializeField] private Transform edgesRoot;
    [SerializeField] private string markerChildName = "R2_Node_Visual_Marker";
    [SerializeField] private string shelterNodeId = "R2_DIEM_TRU";

    [Header("Node Scale")]
    [SerializeField] private float normalNodeScale = 1.28f;
    [SerializeField] private float currentNodeScale = 1.65f;
    [SerializeField] private float availableNodeScale = 1.45f;
    [SerializeField] private float objectiveNodeScale = 1.42f;
    [SerializeField] private float completedNodeScale = 1.12f;
    [SerializeField] private float nodePulseSpeed = 2.2f;
    [SerializeField] private float nodePulseAmount = 0.08f;

    [Header("Route Width")]
    [SerializeField] private float routeLineWidth = 0.052f;
    [SerializeField] private float suggestedRouteLineWidth = 0.086f;
    [SerializeField] private float activeRouteLineWidth = 0.094f;
    [SerializeField] private float blockedRouteLineWidth = 0.075f;

    [Header("Node Colors")]
    [SerializeField] private Color normalNodeColor = new(0.36f, 0.95f, 0.92f, 0.82f);
    [SerializeField] private Color currentNodeColor = new(1f, 0.73f, 0.22f, 1f);
    [SerializeField] private Color availableNodeColor = new(0.58f, 1f, 1f, 1f);
    [SerializeField] private Color rescueTargetNodeColor = new(1f, 0.34f, 0.16f, 1f);
    [SerializeField] private Color shelterNodeColor = new(0.27f, 1f, 0.45f, 1f);
    [SerializeField] private Color blockedNodeColor = new(0.62f, 0.28f, 0.26f, 0.85f);
    [SerializeField] private Color completedNodeColor = new(0.42f, 0.78f, 0.54f, 0.58f);

    [Header("Route Colors")]
    [SerializeField] private Color normalRouteColor = new(0.50f, 0.88f, 0.88f, 0.50f);
    [SerializeField] private Color availableRouteColor = new(0.62f, 1f, 1f, 0.82f);
    [SerializeField] private Color suggestedRouteColor = new(0.86f, 1f, 1f, 0.96f);
    [SerializeField] private Color activeRouteColor = new(1f, 0.77f, 0.28f, 1f);
    [SerializeField] private Color blockedRouteColor = new(0.78f, 0.22f, 0.16f, 0.85f);
    [SerializeField] private Color completedRouteColor = new(0.35f, 0.78f, 0.48f, 0.42f);

    [Header("Halo")]
    [SerializeField] private bool createMissingHalos = true;
    [SerializeField] private float haloDiameter = 0.46f;
    [SerializeField] private float haloThickness = 0.075f;
    [SerializeField] private float haloHeightOffset = 0.012f;
    [SerializeField] private Material haloMaterial;

    [Header("Update")]
    [SerializeField] private float refreshSeconds = 0.08f;
    [SerializeField] private bool autoHighlightPathToShelterWhenCarrying = true;

    private readonly List<RouteNode> nodes = new();
    private readonly List<RouteEdge> edges = new();
    private readonly Dictionary<RouteNode, Transform> nodeMarkers = new();
    private readonly Dictionary<RouteNode, Renderer[]> nodeRenderers = new();
    private readonly Dictionary<RouteNode, Renderer> nodeHalos = new();
    private readonly Dictionary<RouteNode, NodeVisualState> manualNodeStates = new();
    private readonly Dictionary<RouteEdge, RouteVisualState> manualRouteStates = new();
    private readonly HashSet<RouteEdge> completedEdges = new();
    private readonly HashSet<RouteEdge> suggestedEdges = new();
    private readonly HashSet<RouteNode> completedNodes = new();
    private MaterialPropertyBlock propertyBlock;
    private RouteEdge activeEdge;
    private float refreshTimer;

    private void Awake()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        EnsureReferences();
        RebuildCache();
        Subscribe();
        RefreshAllVisuals();
    }

    private void OnEnable()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        EnsureReferences();
        RebuildCache();
        Subscribe();
        RefreshAllVisuals();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            ApplyPulseOnly();
            return;
        }

        refreshTimer = Mathf.Max(0.02f, refreshSeconds);
        RefreshAllVisuals();
    }

    [ContextMenu("Rebuild Route Visual Cache")]
    public void RebuildCache()
    {
        nodes.Clear();
        edges.Clear();
        nodeMarkers.Clear();
        nodeRenderers.Clear();
        nodeHalos.Clear();

        if (graphManager != null)
        {
            nodes.AddRange(graphManager.RouteNodes);
            edges.AddRange(graphManager.RouteEdges);
        }
        else
        {
            if (nodesRoot != null)
            {
                nodes.AddRange(nodesRoot.GetComponentsInChildren<RouteNode>(true));
            }

            if (edgesRoot != null)
            {
                edges.AddRange(edgesRoot.GetComponentsInChildren<RouteEdge>(true));
            }
        }

        nodes.RemoveAll(node => node == null);
        edges.RemoveAll(edge => edge == null);

        for (int i = 0; i < nodes.Count; i++)
        {
            RouteNode node = nodes[i];
            Transform marker = node.transform.Find(markerChildName);
            if (marker == null)
            {
                continue;
            }

            nodeMarkers[node] = marker;
            nodeRenderers[node] = marker.GetComponentsInChildren<Renderer>(true);
            Renderer halo = FindOrCreateHalo(marker);
            if (halo != null)
            {
                nodeHalos[node] = halo;
            }
        }
    }

    public void SetNodeState(RouteNode node, NodeVisualState state)
    {
        if (node == null)
        {
            return;
        }

        manualNodeStates[node] = state;
        ApplyNodeVisual(node, state);
    }

    public void SetRouteState(RouteEdge edge, RouteVisualState state)
    {
        if (edge == null)
        {
            return;
        }

        manualRouteStates[edge] = state;
        ApplyRouteVisual(edge, state);
    }

    public void HighlightSuggestedRoute(List<RouteEdge> route)
    {
        suggestedEdges.Clear();
        if (route != null)
        {
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i] != null)
                {
                    suggestedEdges.Add(route[i]);
                }
            }
        }

        RefreshAllVisuals();
    }

    public void ClearRouteHighlights()
    {
        suggestedEdges.Clear();
        activeEdge = null;
        RefreshAllVisuals();
    }

    private void EnsureReferences()
    {
        graphManager ??= RouteGraphManager.Instance != null ? RouteGraphManager.Instance : FindAnyObjectByType<RouteGraphManager>();
        boatMover ??= FindAnyObjectByType<BoatRouteMover>();
        round2GameController ??= FindAnyObjectByType<Round2GameController>();

        if (nodesRoot == null)
        {
            GameObject nodesObject = GameObject.Find("R2_Gameplay_Nodes");
            nodesRoot = nodesObject != null ? nodesObject.transform : null;
        }

        if (edgesRoot == null)
        {
            GameObject edgesObject = GameObject.Find("R2_Gameplay_Edges");
            edgesRoot = edgesObject != null ? edgesObject.transform : null;
        }
    }

    private void Subscribe()
    {
        if (boatMover == null)
        {
            return;
        }

        boatMover.MoveAccepted -= HandleMoveAccepted;
        boatMover.MoveAccepted += HandleMoveAccepted;
        boatMover.ArrivedAtNode -= HandleArrivedAtNode;
        boatMover.ArrivedAtNode += HandleArrivedAtNode;
    }

    private void Unsubscribe()
    {
        if (boatMover == null)
        {
            return;
        }

        boatMover.MoveAccepted -= HandleMoveAccepted;
        boatMover.ArrivedAtNode -= HandleArrivedAtNode;
    }

    private void HandleMoveAccepted(RouteNode from, RouteNode to)
    {
        activeEdge = graphManager != null ? graphManager.GetEdgeBetween(from, to) : null;
        if (from != null)
        {
            completedNodes.Add(from);
        }

        RefreshAllVisuals();
    }

    private void HandleArrivedAtNode(RouteNode node)
    {
        if (activeEdge != null)
        {
            completedEdges.Add(activeEdge);
            activeEdge = null;
        }

        if (node != null)
        {
            completedNodes.Add(node);
        }

        RefreshAllVisuals();
    }

    private void RefreshAllVisuals()
    {
        EnsureReferences();
        if (nodes.Count == 0 || edges.Count == 0)
        {
            RebuildCache();
        }

        if (autoHighlightPathToShelterWhenCarrying)
        {
            RefreshSuggestedShelterPath();
        }

        for (int i = 0; i < edges.Count; i++)
        {
            RouteEdge edge = edges[i];
            if (edge != null)
            {
                ApplyRouteVisual(edge, ResolveRouteState(edge));
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            RouteNode node = nodes[i];
            if (node != null)
            {
                ApplyNodeVisual(node, ResolveNodeState(node));
            }
        }
    }

    private void RefreshSuggestedShelterPath()
    {
        suggestedEdges.Clear();

        if (round2GameController == null || graphManager == null || boatMover == null)
        {
            return;
        }

        if (round2GameController.Cargo <= 0 || round2GameController.SavedCivilians >= round2GameController.TotalCivilians)
        {
            return;
        }

        RouteNode start = boatMover.CurrentNode;
        RouteNode shelter = graphManager.GetNodeById(shelterNodeId);
        List<RouteEdge> path = FindPath(start, shelter);
        if (path == null)
        {
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            suggestedEdges.Add(path[i]);
        }
    }

    private List<RouteEdge> FindPath(RouteNode start, RouteNode target)
    {
        if (start == null || target == null)
        {
            return null;
        }

        Queue<RouteNode> frontier = new();
        Dictionary<RouteNode, RouteNode> cameFrom = new();
        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            RouteNode current = frontier.Dequeue();
            if (current == target)
            {
                break;
            }

            for (int i = 0; i < current.Neighbors.Count; i++)
            {
                RouteNode next = current.Neighbors[i];
                if (next == null || cameFrom.ContainsKey(next))
                {
                    continue;
                }

                RouteEdge edge = graphManager.GetEdgeBetween(current, next);
                if (edge != null && edge.IsBlocked)
                {
                    continue;
                }

                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(target))
        {
            return null;
        }

        List<RouteEdge> path = new();
        RouteNode node = target;
        while (cameFrom[node] != null)
        {
            RouteNode previous = cameFrom[node];
            RouteEdge edge = graphManager.GetEdgeBetween(previous, node);
            if (edge != null)
            {
                path.Add(edge);
            }

            node = previous;
        }

        path.Reverse();
        return path;
    }

    private NodeVisualState ResolveNodeState(RouteNode node)
    {
        if (manualNodeStates.TryGetValue(node, out NodeVisualState manualState))
        {
            return manualState;
        }

        RouteNode currentNode = boatMover != null ? boatMover.CurrentNode : null;
        if (node == currentNode)
        {
            return NodeVisualState.Current;
        }

        if (currentNode != null && graphManager != null)
        {
            RouteEdge edge = graphManager.GetEdgeBetween(currentNode, node);
            if (edge != null && edge.IsBlocked)
            {
                return NodeVisualState.Blocked;
            }

            if (graphManager.CanMove(currentNode, node))
            {
                return NodeVisualState.Available;
            }
        }

        if (completedNodes.Contains(node))
        {
            return NodeVisualState.Completed;
        }

        if (node.NodeType == NodeType.Objective)
        {
            return NodeVisualState.RescueTarget;
        }

        if (node.NodeType == NodeType.Shelter)
        {
            return NodeVisualState.Shelter;
        }

        return NodeVisualState.Normal;
    }

    private RouteVisualState ResolveRouteState(RouteEdge edge)
    {
        if (manualRouteStates.TryGetValue(edge, out RouteVisualState manualState))
        {
            return manualState;
        }

        if (edge == activeEdge)
        {
            return RouteVisualState.Active;
        }

        if (edge.IsBlocked)
        {
            return RouteVisualState.Blocked;
        }

        if (suggestedEdges.Contains(edge))
        {
            return RouteVisualState.Suggested;
        }

        RouteNode currentNode = boatMover != null ? boatMover.CurrentNode : null;
        if (currentNode != null && (edge.FromNode == currentNode || edge.ToNode == currentNode))
        {
            return RouteVisualState.Available;
        }

        if (completedEdges.Contains(edge))
        {
            return RouteVisualState.Completed;
        }

        return RouteVisualState.Normal;
    }

    private void ApplyPulseOnly()
    {
        RouteNode currentNode = boatMover != null ? boatMover.CurrentNode : null;
        if (currentNode != null)
        {
            ApplyNodeScale(currentNode, NodeVisualState.Current);
        }
    }

    private void ApplyNodeVisual(RouteNode node, NodeVisualState state)
    {
        if (!nodeMarkers.TryGetValue(node, out Transform marker))
        {
            return;
        }

        Color color = GetNodeColor(state);
        if (nodeRenderers.TryGetValue(node, out Renderer[] renderers))
        {
            ApplyColor(renderers, color);
        }

        if (nodeHalos.TryGetValue(node, out Renderer halo))
        {
            ApplyColor(halo, new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * GetHaloAlpha(state))));
            halo.enabled = state != NodeVisualState.Completed || color.a > 0.15f;
        }

        ApplyNodeScale(node, state);
    }

    private void ApplyNodeScale(RouteNode node, NodeVisualState state)
    {
        if (!nodeMarkers.TryGetValue(node, out Transform marker))
        {
            return;
        }

        float targetScale = GetNodeScale(state);
        if (state == NodeVisualState.Current)
        {
            targetScale += Mathf.Sin(Time.time * nodePulseSpeed) * nodePulseAmount;
        }

        marker.localScale = Vector3.one * targetScale;
    }

    private void ApplyRouteVisual(RouteEdge edge, RouteVisualState state)
    {
        LineRenderer lineRenderer = edge.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }

        Color color = GetRouteColor(state);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.widthMultiplier = GetRouteWidth(state);
        lineRenderer.numCornerVertices = Mathf.Max(lineRenderer.numCornerVertices, 4);
        lineRenderer.numCapVertices = Mathf.Max(lineRenderer.numCapVertices, 4);
    }

    private Color GetNodeColor(NodeVisualState state)
    {
        return state switch
        {
            NodeVisualState.Current => currentNodeColor,
            NodeVisualState.Available => availableNodeColor,
            NodeVisualState.RescueTarget => rescueTargetNodeColor,
            NodeVisualState.Shelter => shelterNodeColor,
            NodeVisualState.Blocked => blockedNodeColor,
            NodeVisualState.Completed => completedNodeColor,
            _ => normalNodeColor
        };
    }

    private Color GetRouteColor(RouteVisualState state)
    {
        return state switch
        {
            RouteVisualState.Available => availableRouteColor,
            RouteVisualState.Suggested => suggestedRouteColor,
            RouteVisualState.Active => activeRouteColor,
            RouteVisualState.Blocked => blockedRouteColor,
            RouteVisualState.Completed => completedRouteColor,
            _ => normalRouteColor
        };
    }

    private float GetNodeScale(NodeVisualState state)
    {
        return state switch
        {
            NodeVisualState.Current => currentNodeScale,
            NodeVisualState.Available => availableNodeScale,
            NodeVisualState.RescueTarget => objectiveNodeScale,
            NodeVisualState.Shelter => objectiveNodeScale,
            NodeVisualState.Blocked => availableNodeScale,
            NodeVisualState.Completed => completedNodeScale,
            _ => normalNodeScale
        };
    }

    private float GetRouteWidth(RouteVisualState state)
    {
        return state switch
        {
            RouteVisualState.Suggested => suggestedRouteLineWidth,
            RouteVisualState.Active => activeRouteLineWidth,
            RouteVisualState.Blocked => blockedRouteLineWidth,
            _ => routeLineWidth
        };
    }

    private float GetHaloAlpha(NodeVisualState state)
    {
        return state switch
        {
            NodeVisualState.Current => 0.72f,
            NodeVisualState.Available => 0.58f,
            NodeVisualState.RescueTarget => 0.54f,
            NodeVisualState.Shelter => 0.58f,
            NodeVisualState.Blocked => 0.52f,
            NodeVisualState.Completed => 0.25f,
            _ => 0.35f
        };
    }

    private Renderer FindOrCreateHalo(Transform marker)
    {
        Transform existing = marker.Find("RouteVisual_Halo");
        if (existing != null)
        {
            return existing.GetComponent<Renderer>();
        }

        if (!createMissingHalos)
        {
            return null;
        }

        GameObject halo = new("RouteVisual_Halo");
        halo.transform.SetParent(marker, false);
        halo.transform.localPosition = new Vector3(0f, haloHeightOffset, 0f);
        halo.transform.localRotation = Quaternion.identity;
        halo.transform.localScale = Vector3.one;
        halo.layer = LayerMask.NameToLayer("Ignore Raycast");

        MeshFilter meshFilter = halo.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateRingMesh(haloDiameter, haloThickness, 64);

        MeshRenderer meshRenderer = halo.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = haloMaterial != null ? haloMaterial : GetDefaultHaloMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        return meshRenderer;
    }

    private Material GetDefaultHaloMaterial()
    {
        if (haloMaterial != null)
        {
            return haloMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        haloMaterial = new Material(shader != null ? shader : Shader.Find("Sprites/Default"))
        {
            name = "MAT_RouteVisual_Halo_Runtime"
        };
        haloMaterial.renderQueue = 3100;
        if (haloMaterial.HasProperty("_Surface"))
        {
            haloMaterial.SetFloat("_Surface", 1f);
        }

        if (haloMaterial.HasProperty("_SrcBlend"))
        {
            haloMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (haloMaterial.HasProperty("_DstBlend"))
        {
            haloMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (haloMaterial.HasProperty("_ZWrite"))
        {
            haloMaterial.SetFloat("_ZWrite", 0f);
        }

        if (haloMaterial.HasProperty("_Cull"))
        {
            haloMaterial.SetFloat("_Cull", 0f);
        }

        return haloMaterial;
    }

    private static Mesh CreateRingMesh(float diameter, float thickness, int segments)
    {
        Mesh mesh = new();
        int safeSegments = Mathf.Max(12, segments);
        Vector3[] vertices = new Vector3[safeSegments * 4];
        int[] triangles = new int[safeSegments * 6];
        float outer = Mathf.Max(0.01f, diameter * 0.5f);
        float inner = Mathf.Max(0.005f, outer - thickness);

        for (int i = 0; i < safeSegments; i++)
        {
            float a0 = Mathf.PI * 2f * i / safeSegments;
            float a1 = Mathf.PI * 2f * (i + 1) / safeSegments;
            int v = i * 4;
            vertices[v] = new Vector3(Mathf.Cos(a0) * outer, 0f, Mathf.Sin(a0) * outer);
            vertices[v + 1] = new Vector3(Mathf.Cos(a1) * outer, 0f, Mathf.Sin(a1) * outer);
            vertices[v + 2] = new Vector3(Mathf.Cos(a0) * inner, 0f, Mathf.Sin(a0) * inner);
            vertices[v + 3] = new Vector3(Mathf.Cos(a1) * inner, 0f, Mathf.Sin(a1) * inner);

            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ApplyColor(Renderer[] renderers, Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            ApplyColor(renderers[i], color);
        }
    }

    private void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        renderer.SetPropertyBlock(propertyBlock);
    }
}
