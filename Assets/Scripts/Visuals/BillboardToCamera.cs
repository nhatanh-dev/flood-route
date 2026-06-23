using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool matchCameraRotation = true;
    [SerializeField] private bool invertForward;

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (matchCameraRotation)
        {
            transform.rotation = targetCamera.transform.rotation;
            if (invertForward)
            {
                transform.Rotate(0f, 180f, 0f, Space.Self);
            }
            return;
        }

        Vector3 toCamera = transform.position - targetCamera.transform.position;
        if (toCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}
