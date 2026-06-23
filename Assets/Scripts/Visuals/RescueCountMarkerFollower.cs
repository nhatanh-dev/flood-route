using UnityEngine;

public class RescueCountMarkerFollower : MonoBehaviour
{
    [SerializeField] private Transform worldTarget;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform markerRect;
    [SerializeField] private Vector3 worldOffset = new(0f, 1.6f, 0f);
    [SerializeField] private bool hideWhenBehindCamera = true;

    public Transform WorldTarget
    {
        get { return worldTarget; }
        set { worldTarget = value; }
    }

    public Vector3 WorldOffset
    {
        get { return worldOffset; }
        set { worldOffset = value; }
    }

    private void Awake()
    {
        if (markerRect == null)
        {
            markerRect = GetComponent<RectTransform>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (worldTarget == null || markerRect == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 screenPoint = targetCamera.WorldToScreenPoint(worldTarget.position + worldOffset);
        bool isBehind = screenPoint.z < 0f;

        if (hideWhenBehindCamera)
        {
            markerRect.gameObject.SetActive(!isBehind);
        }

        if (isBehind)
        {
            return;
        }

        RectTransform parentRect = markerRect.parent as RectTransform;
        Canvas canvas = markerRect.GetComponentInParent<Canvas>();
        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera != null ? canvas.worldCamera : targetCamera;
        }

        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
        {
            markerRect.anchoredPosition = localPoint;
            return;
        }

        markerRect.position = screenPoint;
    }
}
