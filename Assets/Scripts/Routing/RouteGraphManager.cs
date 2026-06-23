using System.Collections.Generic;
using UnityEngine;

public sealed class RouteGraphManager : MonoBehaviour
{
    public static RouteGraphManager Instance { get; private set; }

    [SerializeField] private List<RouteNode> routeNodes = new();
    [SerializeField] private List<RouteEdge> routeEdges = new();
    [SerializeField] private bool autoFindOnAwake = true;
    [SerializeField] private bool autoAddEdgeNeighbors = true;

    private readonly Dictionary<string, RouteNode> nodesById = new();

    public IReadOnlyList<RouteNode> RouteNodes => routeNodes;
    public IReadOnlyList<RouteEdge> RouteEdges => routeEdges;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate RouteGraphManager found on {name}. Keeping the first instance.", this);
            return;
        }

        Instance = this;
        InitializeGraph();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("Initialize Graph")]
    public void InitializeGraph()
    {
        if (autoFindOnAwake)
        {
            FindSceneRoutingObjects();
        }

        RemoveNullReferences();
        BuildNodeIndex();

        if (autoAddEdgeNeighbors)
        {
            AddNeighborsFromEdges();
        }

        ValidateGraph();
    }

    public RouteNode GetNodeById(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        if (nodesById.Count == 0)
        {
            InitializeGraph();
        }

        return nodesById.TryGetValue(nodeId, out RouteNode node) ? node : null;
    }

    public RouteEdge GetEdgeBetween(RouteNode a, RouteNode b)
    {
        if (a == null || b == null)
        {
            return null;
        }

        for (int i = 0; i < routeEdges.Count; i++)
        {
            RouteEdge edge = routeEdges[i];
            if (edge != null && edge.Connects(a, b))
            {
                return edge;
            }
        }

        return null;
    }

    public bool CanMove(RouteNode currentNode, RouteNode targetNode)
    {
        return CanMove(currentNode, targetNode, out _);
    }

    public bool CanMove(RouteNode currentNode, RouteNode targetNode, out string message)
    {
        message = string.Empty;

        if (currentNode == null)
        {
            message = "Chưa xác định được vị trí thuyền.";
            return false;
        }

        if (targetNode == null)
        {
            message = "Chưa chọn điểm đến.";
            return false;
        }

        if (currentNode == targetNode)
        {
            message = "Thuyền đang ở điểm này.";
            return false;
        }

        bool isNeighbor = currentNode.HasNeighbor(targetNode);
        RouteEdge edge = GetEdgeBetween(currentNode, targetNode);
        bool hasEdge = edge != null;

        if (!isNeighbor && !hasEdge)
        {
            message = "Chỉ có thể đi tới điểm kế bên.";
            return false;
        }

        if (edge != null && edge.IsBlocked)
        {
            message = "Đường này đang bị chặn.";
            return false;
        }

        message = "Thuyền đang di chuyển.";
        return true;
    }

    private void FindSceneRoutingObjects()
    {
        routeNodes.Clear();
        routeEdges.Clear();
        routeNodes.AddRange(FindObjectsByType<RouteNode>(FindObjectsInactive.Exclude));
        routeEdges.AddRange(FindObjectsByType<RouteEdge>(FindObjectsInactive.Exclude));
    }

    private void RemoveNullReferences()
    {
        routeNodes.RemoveAll(node => node == null);
        routeEdges.RemoveAll(edge => edge == null);
    }

    private void BuildNodeIndex()
    {
        nodesById.Clear();

        for (int i = 0; i < routeNodes.Count; i++)
        {
            RouteNode node = routeNodes[i];
            if (node == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(node.NodeId))
            {
                Debug.LogWarning($"RouteNode on {node.name} has an empty nodeId.", node);
                continue;
            }

            if (nodesById.ContainsKey(node.NodeId))
            {
                Debug.LogWarning($"Duplicate RouteNode nodeId '{node.NodeId}' found on {node.name}.", node);
                continue;
            }

            nodesById.Add(node.NodeId, node);
        }
    }

    private void AddNeighborsFromEdges()
    {
        for (int i = 0; i < routeEdges.Count; i++)
        {
            RouteEdge edge = routeEdges[i];
            if (edge == null || edge.FromNode == null || edge.ToNode == null)
            {
                continue;
            }

            edge.FromNode.AddNeighbor(edge.ToNode);
            edge.ToNode.AddNeighbor(edge.FromNode);
        }
    }

    private void ValidateGraph()
    {
        for (int i = 0; i < routeNodes.Count; i++)
        {
            RouteNode node = routeNodes[i];
            if (node == null)
            {
                continue;
            }

            if (node.Neighbors.Count == 0)
            {
                Debug.LogWarning($"RouteNode '{node.NodeId}' has no neighbors.", node);
            }

            for (int j = 0; j < node.Neighbors.Count; j++)
            {
                RouteNode neighbor = node.Neighbors[j];
                if (neighbor == null)
                {
                    Debug.LogWarning($"RouteNode '{node.NodeId}' has a missing neighbor reference.", node);
                    continue;
                }

                if (GetEdgeBetween(node, neighbor) == null)
                {
                    Debug.LogWarning($"No RouteEdge connects '{node.NodeId}' and '{neighbor.NodeId}'. Movement can still work through the neighbor list, but the visual edge is not linked.", node);
                }
            }
        }

        for (int i = 0; i < routeEdges.Count; i++)
        {
            RouteEdge edge = routeEdges[i];
            if (edge == null)
            {
                continue;
            }

            if (edge.FromNode == null || edge.ToNode == null)
            {
                Debug.LogWarning($"RouteEdge '{edge.name}' is missing fromNode or toNode.", edge);
            }
        }

        WarnIfDisconnected();
    }

    private void WarnIfDisconnected()
    {
        if (routeNodes.Count <= 1)
        {
            return;
        }

        RouteNode start = routeNodes.Find(node => node != null);
        if (start == null)
        {
            return;
        }

        HashSet<RouteNode> visited = new();
        Queue<RouteNode> queue = new();
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            RouteNode current = queue.Dequeue();
            for (int i = 0; i < current.Neighbors.Count; i++)
            {
                RouteNode neighbor = current.Neighbors[i];
                if (neighbor == null || visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        for (int i = 0; i < routeNodes.Count; i++)
        {
            RouteNode node = routeNodes[i];
            if (node != null && !visited.Contains(node))
            {
                Debug.LogWarning($"RouteNode '{node.NodeId}' is disconnected from the route graph.", node);
            }
        }
    }
}
