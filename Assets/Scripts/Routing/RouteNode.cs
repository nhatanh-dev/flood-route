using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Base,
    Normal,
    Objective,
    Shelter,
    BlockedArea
}

public sealed class RouteNode : MonoBehaviour
{
    [SerializeField] private string nodeId;
    [SerializeField] private NodeType nodeType = NodeType.Normal;
    [SerializeField] private List<RouteNode> neighbors = new();

    public string NodeId => nodeId;
    public NodeType NodeType => nodeType;
    public List<RouteNode> Neighbors => neighbors;

    private void Reset()
    {
        nodeId = gameObject.name;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            nodeId = gameObject.name;
        }

        neighbors.RemoveAll(neighbor => neighbor == null || neighbor == this);
    }

    public bool HasNeighbor(RouteNode node)
    {
        return node != null && neighbors.Contains(node);
    }

    public void AddNeighbor(RouteNode node)
    {
        if (node == null || node == this || neighbors.Contains(node))
        {
            return;
        }

        neighbors.Add(node);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.08f, 0.18f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = GetGizmoColor();
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.45f, string.IsNullOrWhiteSpace(nodeId) ? gameObject.name : nodeId);
    }
#endif

    private Color GetGizmoColor()
    {
        return nodeType switch
        {
            NodeType.Base => new Color(0.25f, 0.55f, 1f),
            NodeType.Objective => new Color(1f, 0.55f, 0.2f),
            NodeType.Shelter => new Color(0.25f, 1f, 0.45f),
            NodeType.BlockedArea => new Color(1f, 0.85f, 0.2f),
            _ => new Color(0.75f, 1f, 0.85f)
        };
    }
}
