using UnityEngine;

public enum DebrisVisualCategory
{
    Decorative,
    Blocking,
    FlowClearable,
    Helper
}

public sealed class DebrisVisualState : MonoBehaviour
{
    [SerializeField] private DebrisVisualCategory category = DebrisVisualCategory.Decorative;
    [SerializeField] private bool visualOnly = true;
    [SerializeField] private string note;

    public DebrisVisualCategory Category => category;
    public bool VisualOnly => visualOnly;
    public string Note => note;
}
