using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    private Canvas canvas;
    private Camera mainCamera;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || canvas == null)
        {
            return;
        }

        transform.rotation = mainCamera.transform.rotation;
    }
}
