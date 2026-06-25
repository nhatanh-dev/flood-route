using UnityEngine;
using UnityEngine.InputSystem;
using Round1;

public class DynamicIsometricCamera : MonoBehaviour
{
    public Transform target;
    public float followZoom = 2.5f;
    public float actionZoom = 2.0f;
    public float overviewZoom = 4.8f;
    public float moveSpeed = 4.0f;
    public float zoomSpeed = 3.0f;
    
    private Camera cam;
    private Vector3 followOffset;
    private Vector3 originalPosition;
    private float targetZoom;
    private Round1BoatController boat;
    private Round1DebrisController debris;

    void Awake()
    {
        cam = GetComponent<Camera>();
        originalPosition = transform.position;
        targetZoom = followZoom;
        
        boat = FindAnyObjectByType<Round1BoatController>();
        debris = FindAnyObjectByType<Round1DebrisController>();
        
        if (target == null && boat != null)
        {
            target = boat.transform;
        }

        if (target != null)
        {
            followOffset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (cam == null || target == null) return;
        
        Keyboard kb = Keyboard.current;
        bool wantsOverview = kb != null && (kb.tabKey.isPressed || kb.mKey.isPressed);

        Vector3 targetPos;

        if (wantsOverview)
        {
            targetZoom = overviewZoom;
            targetPos = originalPosition;
        }
        else
        {
            targetZoom = followZoom;
            targetPos = target.position + followOffset;
            
            if (boat != null && !boat.IsMoving)
            {
                Round1NodeId n = boat.CurrentNode;
                if (n == Round1NodeId.NhaBa || n == Round1NodeId.NhaTu || 
                    n == Round1NodeId.BaiDinh || n == Round1NodeId.GoCao)
                {
                    targetZoom = actionZoom;
                }
                else if (n == Round1NodeId.BenPhu && debris != null && debris.IsRouteBlocked)
                {
                    targetZoom = actionZoom;
                }
            }
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed);
    }
}