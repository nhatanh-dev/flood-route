using System;
using System.Collections;
using UnityEngine;

public sealed class BoatRouteMover : MonoBehaviour
{
    [SerializeField] private RouteGraphManager graphManager;
    [SerializeField] private RouteNode currentNode;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private Vector3 boatOffset = new(0f, 0.25f, 0f);
    [SerializeField] private bool faceMoveDirection = true;

    private Coroutine movementCoroutine;

    public RouteNode CurrentNode => currentNode;
    public float MoveSpeed => moveSpeed;
    public bool IsMoving => movementCoroutine != null;

    public event Action<RouteNode, RouteNode> MoveAccepted;
    public event Action<RouteNode> ArrivedAtNode;

    private void Start()
    {
        graphManager ??= RouteGraphManager.Instance != null
            ? RouteGraphManager.Instance
            : FindAnyObjectByType<RouteGraphManager>();

        if (currentNode != null)
        {
            transform.position = GetBoatPosition(currentNode);
        }
        else
        {
            Debug.LogWarning("BoatRouteMover has no currentNode assigned.", this);
        }
    }

    private void OnDisable()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }

    public bool TryMoveTo(RouteNode targetNode)
    {
        if (IsMoving)
        {
            RouteDebugUI.Instance?.ShowMessage("Boat is already moving.");
            return false;
        }

        graphManager ??= RouteGraphManager.Instance;
        if (graphManager == null)
        {
            RouteDebugUI.Instance?.ShowMessage("Route graph manager is missing.");
            Debug.LogWarning("BoatRouteMover cannot move because no RouteGraphManager was found.", this);
            return false;
        }

        if (!graphManager.CanMove(currentNode, targetNode, out string message))
        {
            RouteDebugUI.Instance?.ShowMessage(message);
            Debug.Log(message, this);
            return false;
        }

        RouteNode fromNode = currentNode;
        movementCoroutine = StartCoroutine(MoveToNodeRoutine(targetNode));
        MoveAccepted?.Invoke(fromNode, targetNode);
        RouteDebugUI.Instance?.ShowMessage(message);
        return true;
    }

    public void WarpToNode(RouteNode node)
    {
        if (node == null)
        {
            return;
        }

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        currentNode = node;
        transform.position = GetBoatPosition(node);
        RouteDebugUI.Instance?.SetCurrentNode(currentNode);
    }

    private IEnumerator MoveToNodeRoutine(RouteNode targetNode)
    {
        Vector3 start = transform.position;
        Vector3 end = GetBoatPosition(targetNode);
        float distance = Vector3.Distance(start, end);
        float duration = distance / Mathf.Max(0.01f, moveSpeed);
        float elapsed = 0f;

        if (faceMoveDirection)
        {
            FaceDirection(end - start);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
        currentNode = targetNode;
        movementCoroutine = null;

        RouteDebugUI.Instance?.SetCurrentNode(currentNode);
        RouteDebugUI.Instance?.ShowArrival(currentNode);
        ArrivedAtNode?.Invoke(currentNode);
    }

    private Vector3 GetBoatPosition(RouteNode node)
    {
        return node.transform.position + boatOffset;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
