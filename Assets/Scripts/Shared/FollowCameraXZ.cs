using UnityEngine;

public class FollowCameraXZ : MonoBehaviour
{
    [Tooltip("The camera to follow. If null, uses Camera.main")]
    public Camera targetCamera;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera != null)
        {
            Vector3 camPos = targetCamera.transform.position;
            Vector3 myPos = transform.position;
            myPos.x = camPos.x;
            myPos.z = camPos.z;
            transform.position = myPos;
        }
    }
}
