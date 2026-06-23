using UnityEngine;

public sealed class RescuePeopleVisualController : MonoBehaviour
{
    public enum RescuePeopleState
    {
        WaitingForPickup,
        PickedUp,
        Delivered
    }

    [Header("State Source")]
    [SerializeField] private Round2GameController round2GameController;
    [SerializeField] private bool syncFromRound2Controller = true;

    [Header("Visuals")]
    [SerializeField] private GameObject peopleRoot;
    [SerializeField] private GameObject[] rescuePeople = new GameObject[0];
    [SerializeField] private float hiddenScale = 0.01f;
    [SerializeField] private float stateBlendSpeed = 8f;

    [Header("Idle Motion")]
    [SerializeField] private bool animateWaitingPeople = true;
    [SerializeField] private float bobHeight = 0.055f;
    [SerializeField] private float bobSpeed = 1.5f;
    [SerializeField] private float waveAngle = 12f;
    [SerializeField] private float waveSpeed = 2.4f;
    [SerializeField] private Transform[] wavingArms = new Transform[0];

    [Header("Debug")]
    [SerializeField] private RescuePeopleState currentState = RescuePeopleState.WaitingForPickup;

    private Vector3[] baseLocalPositions;
    private Quaternion[] baseArmRotations;
    private float visibleAmount = 1f;

    public RescuePeopleState CurrentState => currentState;

    private void Awake()
    {
        CacheBaseTransforms();

        if (peopleRoot == null)
        {
            peopleRoot = gameObject;
        }

        if (round2GameController == null)
        {
            round2GameController = FindAnyObjectByType<Round2GameController>();
        }
    }

    private void OnEnable()
    {
        if (round2GameController != null)
        {
            round2GameController.RescueCountsChanged -= HandleRescueCountsChanged;
            round2GameController.RescueCountsChanged += HandleRescueCountsChanged;
            HandleRescueCountsChanged(
                round2GameController.TotalCivilians,
                round2GameController.CiviliansAtHouse,
                round2GameController.SavedCivilians);
        }
    }

    private void OnDisable()
    {
        if (round2GameController != null)
        {
            round2GameController.RescueCountsChanged -= HandleRescueCountsChanged;
        }
    }

    private void Update()
    {
        float targetVisible = currentState == RescuePeopleState.WaitingForPickup ? 1f : 0f;
        visibleAmount = Mathf.MoveTowards(visibleAmount, targetVisible, Time.deltaTime * stateBlendSpeed);
        ApplyVisibility();

        if (animateWaitingPeople && currentState == RescuePeopleState.WaitingForPickup)
        {
            AnimatePeople();
        }
    }

    public void ShowWaitingPeople()
    {
        SetState(RescuePeopleState.WaitingForPickup);
    }

    public void HideWaitingPeople()
    {
        SetState(RescuePeopleState.PickedUp);
    }

    public void OnPeoplePickedUp()
    {
        SetState(RescuePeopleState.PickedUp);
    }

    public void OnPeopleDelivered()
    {
        SetState(RescuePeopleState.Delivered);
    }

    public void SetState(RescuePeopleState state)
    {
        currentState = state;
        if (peopleRoot != null && state == RescuePeopleState.WaitingForPickup)
        {
            peopleRoot.SetActive(true);
        }
    }

    private void HandleRescueCountsChanged(int total, int waitingAtHouse, int saved)
    {
        if (!syncFromRound2Controller)
        {
            return;
        }

        if (saved >= total && total > 0)
        {
            OnPeopleDelivered();
        }
        else if (waitingAtHouse <= 0)
        {
            OnPeoplePickedUp();
        }
        else
        {
            ShowWaitingPeople();
        }
    }

    private void CacheBaseTransforms()
    {
        if (rescuePeople == null)
        {
            rescuePeople = new GameObject[0];
        }

        baseLocalPositions = new Vector3[rescuePeople.Length];
        for (int i = 0; i < rescuePeople.Length; i++)
        {
            if (rescuePeople[i] != null)
            {
                baseLocalPositions[i] = rescuePeople[i].transform.localPosition;
            }
        }

        if (wavingArms == null)
        {
            wavingArms = new Transform[0];
        }

        baseArmRotations = new Quaternion[wavingArms.Length];
        for (int i = 0; i < wavingArms.Length; i++)
        {
            if (wavingArms[i] != null)
            {
                baseArmRotations[i] = wavingArms[i].localRotation;
            }
        }
    }

    private void ApplyVisibility()
    {
        if (peopleRoot == null)
        {
            return;
        }

        bool shouldBeActive = visibleAmount > 0.01f || currentState == RescuePeopleState.WaitingForPickup;
        if (peopleRoot.activeSelf != shouldBeActive)
        {
            peopleRoot.SetActive(shouldBeActive);
        }

        if (!shouldBeActive)
        {
            return;
        }

        float scale = Mathf.Lerp(hiddenScale, 1f, visibleAmount);
        peopleRoot.transform.localScale = Vector3.one * scale;
    }

    private void AnimatePeople()
    {
        if (rescuePeople == null)
        {
            return;
        }

        for (int i = 0; i < rescuePeople.Length; i++)
        {
            GameObject person = rescuePeople[i];
            if (person == null || i >= baseLocalPositions.Length)
            {
                continue;
            }

            float phase = Time.time * bobSpeed + i * 1.4f;
            Vector3 position = baseLocalPositions[i];
            position.y += Mathf.Sin(phase) * bobHeight;
            person.transform.localPosition = position;
        }

        for (int i = 0; i < wavingArms.Length; i++)
        {
            Transform arm = wavingArms[i];
            if (arm == null || i >= baseArmRotations.Length)
            {
                continue;
            }

            float phase = Time.time * waveSpeed + i * 1.7f;
            arm.localRotation = baseArmRotations[i] * Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * waveAngle);
        }
    }
}
