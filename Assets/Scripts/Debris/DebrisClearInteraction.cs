using System.Collections;
using TMPro;
using UnityEngine;

public sealed class DebrisClearInteraction : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private RouteNode flowControlNode;
    [SerializeField] private GameObject blockingDebrisRoot;
    [SerializeField] private GameObject openedDebrisRoot;

    [Header("Blocked Route")]
    [SerializeField] private LineRenderer linkedBlockedRouteLine;
    [SerializeField] private Color blockedRouteColor = new(0.9f, 0.28f, 0.12f, 0.82f);
    [SerializeField] private Color normalRouteColor = new(0.45f, 0.92f, 0.96f, 0.7f);
    [SerializeField] private float blockedRouteWidth = 0.14f;
    [SerializeField] private float normalRouteWidth = 0.07f;
    [SerializeField] private Material blockedRouteMaterial;
    [SerializeField] private Material normalRouteMaterial;

    [Header("Warning Visuals")]
    [SerializeField] private GameObject debrisWarningIcon;
    [SerializeField] private GameObject debrisWarningRing;
    [SerializeField] private GameObject affectedDebrisHighlight;

    [Header("Flow Control Visuals")]
    [SerializeField] private GameObject flowControlMarker;
    [SerializeField] private GameObject flowControlActiveHighlight;
    [SerializeField] private GameObject currentPathVisual;

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string readyPrompt = "Press Q";
    [SerializeField] private string blockedFeedback = "Blocked";
    [SerializeField] private string clearedFeedback = "Clear";

    [Header("Clear Feedback")]
    [SerializeField] private GameObject clearFeedbackRoot;
    [SerializeField] private float feedbackSeconds = 1.5f;
    [SerializeField] private Transform linkedDebrisCluster;
    [SerializeField] private float clearAnimationSeconds = 0.85f;
    [SerializeField] private Vector3 clearMoveOffset = new(0.42f, -0.08f, 0.12f);
    [SerializeField] private Vector3 clearScale = new(0.82f, 0.82f, 0.82f);

    [Header("Tuning")]
    [SerializeField, Range(0.05f, 1f)] private float warningRingAlpha = 0.36f;
    [SerializeField] private Color flowArrowColor = new(0.38f, 0.92f, 0.95f, 0.48f);
    [SerializeField] private float flowArrowPulseSpeed = 1.6f;

    private Coroutine feedbackRoutine;
    private Coroutine clearRoutine;
    private float routePulseTimer;
    private bool isBlocked = true;
    private bool isNearFlowControl;
    private bool isPlayingClearAnimation;
    private bool routeDefaultsCached;
    private Color cachedRouteStartColor;
    private Color cachedRouteEndColor;
    private float cachedRouteWidth;
    private Material cachedRouteMaterial;
    private bool debrisDefaultsCached;
    private Vector3 cachedDebrisLocalPosition;
    private Vector3 cachedDebrisLocalScale;

    public RouteNode FlowControlNode => flowControlNode;

    private void Awake()
    {
        CacheRouteDefaults();
        CacheDebrisDefaults();
        ApplyState();
        SetPrompt(false, readyPrompt);
        SetClearFeedback(false);
    }

    private void Update()
    {
        AnimateBlockedRoute();
    }

    public void SetDebrisBlocked(bool blocked)
    {
        isBlocked = blocked;
        ApplyState();
    }

    public void SetBlockedRoute(bool blocked)
    {
        SetDebrisBlocked(blocked);
    }

    public void SetNearFlowControl(bool nearFlowControl)
    {
        isNearFlowControl = nearFlowControl;
        ApplyState();
    }

    public void HighlightFlowControlNode(bool active)
    {
        SetNearFlowControl(active);
    }

    public void ShowFlowPath(bool visible)
    {
        SetActive(currentPathVisual, visible);
    }

    public void RestoreRouteVisual()
    {
        if (linkedBlockedRouteLine == null)
        {
            return;
        }

        CacheRouteDefaults();
        linkedBlockedRouteLine.startColor = normalRouteColor;
        linkedBlockedRouteLine.endColor = normalRouteColor;
        linkedBlockedRouteLine.widthMultiplier = normalRouteWidth > 0f ? normalRouteWidth : cachedRouteWidth;

        if (normalRouteMaterial != null)
        {
            linkedBlockedRouteLine.sharedMaterial = normalRouteMaterial;
        }
        else if (cachedRouteMaterial != null)
        {
            linkedBlockedRouteLine.sharedMaterial = cachedRouteMaterial;
        }
    }

    public void ShowBlockedFeedback()
    {
        if (!isBlocked)
        {
            return;
        }

        SetPrompt(true, blockedFeedback);
        ShowTemporaryFeedback(0.9f);
    }

    public void PlayClearFeedback()
    {
        isBlocked = false;

        if (clearRoutine != null)
        {
            StopCoroutine(clearRoutine);
        }

        clearRoutine = StartCoroutine(ClearDebrisRoutine());
    }

    public void SetDebrisCleared()
    {
        PlayClearFeedback();
    }

    private void ApplyState()
    {
        bool shouldShowBlocked = isBlocked;
        bool shouldShowInteract = isBlocked && isNearFlowControl;

        SetActive(debrisWarningIcon, shouldShowBlocked);
        SetActive(debrisWarningRing, shouldShowBlocked);
        SetActive(affectedDebrisHighlight, shouldShowInteract);
        SetActive(flowControlMarker, shouldShowBlocked);
        SetActive(flowControlActiveHighlight, shouldShowInteract);
        SetActive(currentPathVisual, shouldShowInteract);
        SetPrompt(shouldShowInteract, readyPrompt);
        ApplyRouteVisual(shouldShowBlocked, shouldShowInteract);
        ApplyRendererAlpha(debrisWarningRing, warningRingAlpha);
        ApplyRendererColor(currentPathVisual, flowArrowColor);

        if (blockingDebrisRoot != null)
        {
            blockingDebrisRoot.SetActive(shouldShowBlocked || isPlayingClearAnimation);
            if (shouldShowBlocked && !isPlayingClearAnimation)
            {
                ResetDebrisVisual();
            }
        }

        if (openedDebrisRoot != null)
        {
            openedDebrisRoot.SetActive(!shouldShowBlocked && !isPlayingClearAnimation);
        }
    }

    private void ShowTemporaryFeedback(float seconds)
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FeedbackRoutine(seconds));
    }

    private IEnumerator FeedbackRoutine(float seconds)
    {
        SetClearFeedback(true);
        yield return new WaitForSeconds(seconds);
        SetClearFeedback(false);
        SetPrompt(isBlocked && isNearFlowControl, readyPrompt);
        feedbackRoutine = null;
    }

    private IEnumerator ClearDebrisRoutine()
    {
        isPlayingClearAnimation = true;
        CacheDebrisDefaults();

        Transform debrisTransform = GetDebrisTransform();
        Vector3 startPosition = debrisTransform != null ? debrisTransform.localPosition : Vector3.zero;
        Vector3 startScale = debrisTransform != null ? debrisTransform.localScale : Vector3.one;
        Vector3 targetPosition = startPosition + clearMoveOffset;
        Vector3 targetScale = Vector3.Scale(startScale, clearScale);

        SetActive(debrisWarningIcon, false);
        SetActive(debrisWarningRing, false);
        SetActive(affectedDebrisHighlight, false);
        SetActive(flowControlMarker, false);
        SetActive(flowControlActiveHighlight, false);
        SetActive(currentPathVisual, true);
        SetActive(clearFeedbackRoot, true);
        SetPrompt(true, clearedFeedback);
        ApplyRouteVisual(false, false);

        if (blockingDebrisRoot != null)
        {
            blockingDebrisRoot.SetActive(true);
        }

        if (openedDebrisRoot != null)
        {
            openedDebrisRoot.SetActive(false);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, clearAnimationSeconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (debrisTransform != null)
            {
                debrisTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                debrisTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            yield return null;
        }

        if (blockingDebrisRoot != null)
        {
            blockingDebrisRoot.SetActive(false);
        }

        if (openedDebrisRoot != null)
        {
            openedDebrisRoot.SetActive(true);
        }

        SetActive(currentPathVisual, false);
        yield return new WaitForSeconds(Mathf.Max(0.05f, feedbackSeconds - duration));
        SetActive(clearFeedbackRoot, false);
        SetPrompt(false, clearedFeedback);

        ResetDebrisVisual();
        isPlayingClearAnimation = false;
        clearRoutine = null;
    }

    private void SetPrompt(bool visible, string value)
    {
        SetActive(promptRoot, visible);
        if (promptText != null)
        {
            promptText.text = value;
        }
    }

    private void SetClearFeedback(bool visible)
    {
        SetActive(clearFeedbackRoot, visible);
    }

    private void CacheRouteDefaults()
    {
        if (routeDefaultsCached || linkedBlockedRouteLine == null)
        {
            return;
        }

        cachedRouteStartColor = linkedBlockedRouteLine.startColor;
        cachedRouteEndColor = linkedBlockedRouteLine.endColor;
        cachedRouteWidth = linkedBlockedRouteLine.widthMultiplier;
        cachedRouteMaterial = linkedBlockedRouteLine.sharedMaterial;

        if (normalRouteWidth <= 0f)
        {
            normalRouteWidth = cachedRouteWidth;
        }

        if (normalRouteMaterial == null)
        {
            normalRouteMaterial = cachedRouteMaterial;
        }

        routeDefaultsCached = true;
    }

    private void CacheDebrisDefaults()
    {
        if (debrisDefaultsCached)
        {
            return;
        }

        Transform debrisTransform = GetDebrisTransform();
        if (debrisTransform == null)
        {
            return;
        }

        cachedDebrisLocalPosition = debrisTransform.localPosition;
        cachedDebrisLocalScale = debrisTransform.localScale;
        debrisDefaultsCached = true;
    }

    private Transform GetDebrisTransform()
    {
        if (linkedDebrisCluster != null)
        {
            return linkedDebrisCluster;
        }

        return blockingDebrisRoot != null ? blockingDebrisRoot.transform : null;
    }

    private void ResetDebrisVisual()
    {
        Transform debrisTransform = GetDebrisTransform();
        if (debrisTransform == null || !debrisDefaultsCached)
        {
            return;
        }

        debrisTransform.localPosition = cachedDebrisLocalPosition;
        debrisTransform.localScale = cachedDebrisLocalScale;
    }

    private void ApplyRouteVisual(bool blocked, bool highlighted)
    {
        if (linkedBlockedRouteLine == null)
        {
            return;
        }

        CacheRouteDefaults();

        if (!blocked)
        {
            RestoreRouteVisual();
            return;
        }

        if (blockedRouteMaterial != null)
        {
            linkedBlockedRouteLine.sharedMaterial = blockedRouteMaterial;
        }

        Color color = highlighted
            ? Color.Lerp(blockedRouteColor, new Color(1f, 0.58f, 0.22f, 0.95f), 0.35f)
            : blockedRouteColor;

        linkedBlockedRouteLine.startColor = color;
        linkedBlockedRouteLine.endColor = new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.82f, color.a);
        linkedBlockedRouteLine.widthMultiplier = highlighted ? blockedRouteWidth * 1.15f : blockedRouteWidth;
    }

    private void AnimateBlockedRoute()
    {
        if (!isBlocked || linkedBlockedRouteLine == null)
        {
            return;
        }

        routePulseTimer += Time.deltaTime * flowArrowPulseSpeed;
        float pulse = (Mathf.Sin(routePulseTimer) + 1f) * 0.5f;
        float width = Mathf.Lerp(blockedRouteWidth * 0.9f, blockedRouteWidth * 1.12f, pulse);
        linkedBlockedRouteLine.widthMultiplier = isNearFlowControl ? width * 1.08f : width;
    }

    private static void ApplyRendererAlpha(GameObject root, float alpha)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                continue;
            }

            Material material = targetRenderer.sharedMaterial;
            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = alpha;
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                color.a = alpha;
                material.SetColor("_Color", color);
            }
        }
    }

    private static void ApplyRendererColor(GameObject root, Color color)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                continue;
            }

            Material material = targetRenderer.sharedMaterial;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
