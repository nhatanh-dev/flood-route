using System.Collections.Generic;
using UnityEngine;

public sealed class Round2RouteVisualHighlighter : MonoBehaviour
{
    [SerializeField] private BoatRouteMover boatMover;
    [SerializeField] private RouteGraphManager graphManager;
    [SerializeField] private Transform nodesRoot;
    [SerializeField] private string markerChildName = "R2_Node_Visual_Marker";
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material currentMaterial;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private Material waitMaterial;
    [SerializeField] private Material objectiveMaterial;
    [SerializeField] private Material shelterMaterial;
    [SerializeField] private Material mutedMaterial;
    [SerializeField] private float refreshSeconds = 0.12f;

    private readonly List<RouteNode> nodes = new();
    private readonly Dictionary<RouteNode, Renderer[]> markerRenderers = new();
    private float refreshTimer;

    private void Awake()
    {
        EnsureReferences();
        RebuildCache();
        RefreshMarkers();
    }

    private void OnEnable()
    {
        EnsureReferences();
        RebuildCache();
        RefreshMarkers();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = Mathf.Max(0.02f, refreshSeconds);
        RefreshMarkers();
    }

    [ContextMenu("Rebuild Route Marker Cache")]
    public void RebuildCache()
    {
        nodes.Clear();
        markerRenderers.Clear();

        if (nodesRoot == null)
        {
            return;
        }

        RouteNode[] foundNodes = nodesRoot.GetComponentsInChildren<RouteNode>(true);
        for (int i = 0; i < foundNodes.Length; i++)
        {
            RouteNode node = foundNodes[i];
            if (node == null)
            {
                continue;
            }

            nodes.Add(node);

            Transform marker = node.transform.Find(markerChildName);
            if (marker != null)
            {
                markerRenderers[node] = marker.GetComponentsInChildren<Renderer>(true);
            }
        }
    }

    public void RefreshMarkers()
    {
        EnsureReferences();

        RouteNode currentNode = boatMover != null ? boatMover.CurrentNode : null;

        for (int i = 0; i < nodes.Count; i++)
        {
            RouteNode node = nodes[i];
            if (node == null || !markerRenderers.TryGetValue(node, out Renderer[] renderers))
            {
                continue;
            }

            Material material = ChooseMaterial(node, currentNode);
            ApplyMaterial(renderers, material);
        }
    }

    private void EnsureReferences()
    {
        graphManager ??= RouteGraphManager.Instance != null
            ? RouteGraphManager.Instance
            : FindAnyObjectByType<RouteGraphManager>();
        boatMover ??= FindAnyObjectByType<BoatRouteMover>();

        if (nodesRoot == null)
        {
            GameObject nodesObject = GameObject.Find("R2_Gameplay_Nodes");
            nodesRoot = nodesObject != null ? nodesObject.transform : null;
        }
    }

    private Material ChooseMaterial(RouteNode node, RouteNode currentNode)
    {
        if (node == currentNode)
        {
            return currentMaterial != null ? currentMaterial : validMaterial;
        }

        if (currentNode != null
            && graphManager != null
            && graphManager.CanMove(currentNode, node))
        {
            return validMaterial != null ? validMaterial : defaultMaterial;
        }

        if (node.NodeId == "R2_NGA_RE")
        {
            return waitMaterial != null ? waitMaterial : defaultMaterial;
        }

        if (node.NodeType == NodeType.Base)
        {
            return baseMaterial != null ? baseMaterial : defaultMaterial;
        }

        if (node.NodeType == NodeType.Objective)
        {
            return objectiveMaterial != null ? objectiveMaterial : defaultMaterial;
        }

        if (node.NodeType == NodeType.Shelter)
        {
            return shelterMaterial != null ? shelterMaterial : defaultMaterial;
        }

        return mutedMaterial != null ? mutedMaterial : defaultMaterial;
    }

    private static void ApplyMaterial(Renderer[] renderers, Material material)
    {
        if (renderers == null || material == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }
}
