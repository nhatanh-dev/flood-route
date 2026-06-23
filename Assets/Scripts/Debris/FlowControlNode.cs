using UnityEngine;

public sealed class FlowControlNode : MonoBehaviour
{
    [SerializeField] private string nodeId = "R2_NGA_RE";
    [SerializeField] private GameObject markerRoot;
    [SerializeField] private GameObject activeHighlightRoot;

    public string NodeId => nodeId;

    public void SetAvailable(bool available)
    {
        if (markerRoot != null)
        {
            markerRoot.SetActive(available);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (activeHighlightRoot != null)
        {
            activeHighlightRoot.SetActive(highlighted);
        }
    }
}
