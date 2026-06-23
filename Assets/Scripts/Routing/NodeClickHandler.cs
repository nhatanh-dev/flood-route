using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class NodeClickHandler : MonoBehaviour
{
    [SerializeField] private RouteNode routeNode;
    [SerializeField] private BoatRouteMover boatMover;
    [SerializeField] private Round2GameController round2GameController;
    [SerializeField] private Renderer highlightRenderer;
    [SerializeField] private Color validClickColor = new(0.45f, 1f, 0.65f, 1f);
    [SerializeField] private Color invalidClickColor = new(1f, 0.35f, 0.25f, 1f);
    [SerializeField] private float highlightSeconds = 0.25f;

    private Color originalColor;
    private bool hasOriginalColor;
    private float highlightTimer;

    private void Awake()
    {
        routeNode ??= GetComponent<RouteNode>();
        boatMover ??= FindAnyObjectByType<BoatRouteMover>();
        round2GameController ??= FindAnyObjectByType<Round2GameController>();
        highlightRenderer ??= GetComponentInChildren<Renderer>();

        if (highlightRenderer != null && highlightRenderer.material.HasProperty("_Color"))
        {
            originalColor = highlightRenderer.material.color;
            hasOriginalColor = true;
        }
    }

    private void Update()
    {
        if (highlightTimer <= 0f)
        {
            return;
        }

        highlightTimer -= Time.deltaTime;
        if (highlightTimer <= 0f)
        {
            RestoreHighlight();
        }
    }

    private void OnMouseDown()
    {
        routeNode ??= GetComponent<RouteNode>();
        round2GameController ??= FindAnyObjectByType<Round2GameController>();
        boatMover ??= FindAnyObjectByType<BoatRouteMover>();

        if (routeNode == null)
        {
            Debug.LogWarning($"NodeClickHandler on {name} is missing a RouteNode reference.", this);
            Flash(invalidClickColor);
            return;
        }

        bool moved;
        if (round2GameController != null)
        {
            moved = round2GameController.TryMoveTo(routeNode);
        }
        else if (boatMover != null)
        {
            moved = boatMover.TryMoveTo(routeNode);
        }
        else
        {
            Debug.LogWarning($"NodeClickHandler on {name} is missing a BoatRouteMover or Round2GameController reference.", this);
            Flash(invalidClickColor);
            return;
        }

        Flash(moved ? validClickColor : invalidClickColor);
    }

    private void Flash(Color color)
    {
        if (highlightRenderer == null || !highlightRenderer.material.HasProperty("_Color"))
        {
            return;
        }

        highlightRenderer.material.color = color;
        highlightTimer = Mathf.Max(0.01f, highlightSeconds);
    }

    private void RestoreHighlight()
    {
        if (!hasOriginalColor || highlightRenderer == null || !highlightRenderer.material.HasProperty("_Color"))
        {
            return;
        }

        highlightRenderer.material.color = originalColor;
    }
}
