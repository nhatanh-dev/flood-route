using UnityEngine;
using UnityEngine.Events;

public sealed class ObjectiveStateController : MonoBehaviour
{
    public enum ObjectiveState
    {
        WaitingForPickup,
        CarryingPassengers,
        Delivered,
        Failed
    }

    [SerializeField] private ObjectiveState currentState = ObjectiveState.WaitingForPickup;
    [SerializeField] private GameObject waitingVisualRoot;
    [SerializeField] private GameObject carryingVisualRoot;
    [SerializeField] private GameObject deliveredVisualRoot;
    [SerializeField] private GameObject failedVisualRoot;

    public UnityEvent<ObjectiveState> StateChanged;

    public ObjectiveState CurrentState => currentState;

    public void SetWaitingForPickup()
    {
        SetState(ObjectiveState.WaitingForPickup);
    }

    public void SetCarryingPassengers()
    {
        SetState(ObjectiveState.CarryingPassengers);
    }

    public void SetDelivered()
    {
        SetState(ObjectiveState.Delivered);
    }

    public void SetFailed()
    {
        SetState(ObjectiveState.Failed);
    }

    public void SetState(ObjectiveState state)
    {
        if (currentState == state)
        {
            ApplyState();
            return;
        }

        currentState = state;
        ApplyState();
        StateChanged?.Invoke(currentState);
    }

    private void Awake()
    {
        ApplyState();
    }

    private void OnValidate()
    {
        ApplyState();
    }

    private void ApplyState()
    {
        SetActive(waitingVisualRoot, currentState == ObjectiveState.WaitingForPickup);
        SetActive(carryingVisualRoot, currentState == ObjectiveState.CarryingPassengers);
        SetActive(deliveredVisualRoot, currentState == ObjectiveState.Delivered);
        SetActive(failedVisualRoot, currentState == ObjectiveState.Failed);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
