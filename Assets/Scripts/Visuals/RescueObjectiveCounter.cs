using System;
using TMPro;
using UnityEngine;

public class RescueObjectiveCounter : MonoBehaviour
{
    [Header("Counter")]
    [SerializeField] private int totalPeople = 3;
    [SerializeField] private int rescuedPeople;
    [SerializeField] private int waitingPeople = 3;
    [SerializeField] private int onboardPeople;
    [SerializeField] private bool syncFromRound2Controller = true;

    [Header("Optional Source")]
    [SerializeField] private Round2GameController round2GameController;

    [Header("Views")]
    [SerializeField] private RescueCountWorldBadge worldCountBadge;
    [SerializeField] private RescueProgressHUD hudProgress;
    [SerializeField] private TMP_Text worldCountBadgeText;
    [SerializeField] private TMP_Text hudProgressText;
    [SerializeField] private TMP_Text passengerCountText;
    [SerializeField] private GameObject rescueTargetMarker;
    [SerializeField] private GameObject shelterTargetMarker;
    [SerializeField] private string remainingPeopleFormat = "{0} người";
    [SerializeField] private string pickedUpBadgeText = "Đã đón";
    [SerializeField] private string completeBadgeText = "Đã cứu";
    [SerializeField] private float rescueMarkerActiveAlpha = 1f;
    [SerializeField] private float rescueMarkerPickedUpAlpha = 0.62f;
    [SerializeField] private float shelterMarkerIdleScale = 1f;
    [SerializeField] private float shelterMarkerActiveScale = 1.18f;

    public int TotalPeople => totalPeople;
    public int RescuedPeople => rescuedPeople;
    public int RemainingPeople => waitingPeople;
    public int OnboardPeople => onboardPeople;
    public event Action<int, int> CounterChanged;

    private void Awake()
    {
        if (round2GameController == null)
        {
            round2GameController = FindAnyObjectByType<Round2GameController>();
        }

        if (worldCountBadge == null)
        {
            worldCountBadge = GetComponentInChildren<RescueCountWorldBadge>(true);
        }

        if (hudProgress == null)
        {
            hudProgress = FindAnyObjectByType<RescueProgressHUD>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable()
    {
        if (round2GameController != null)
        {
            round2GameController.RescueCountsChanged -= HandleRound2CountsChanged;
            round2GameController.RescueCountsChanged += HandleRound2CountsChanged;
        }

        SyncFromRound2Controller();
        RefreshViews();
    }

    private void OnDisable()
    {
        if (round2GameController != null)
        {
            round2GameController.RescueCountsChanged -= HandleRound2CountsChanged;
        }
    }

    public void SetTotalPeople(int total)
    {
        totalPeople = Mathf.Max(0, total);
        rescuedPeople = Mathf.Clamp(rescuedPeople, 0, totalPeople);
        waitingPeople = Mathf.Clamp(totalPeople - rescuedPeople - onboardPeople, 0, totalPeople);
        RefreshViews();
    }

    public void SetRescuedPeople(int count)
    {
        rescuedPeople = Mathf.Clamp(count, 0, totalPeople);
        waitingPeople = Mathf.Clamp(totalPeople - rescuedPeople - onboardPeople, 0, totalPeople);
        RefreshViews();
    }

    public void SetCounts(int total, int waitingAtTarget, int savedAtShelter)
    {
        totalPeople = Mathf.Max(0, total);
        waitingPeople = Mathf.Clamp(waitingAtTarget, 0, totalPeople);
        rescuedPeople = Mathf.Clamp(savedAtShelter, 0, totalPeople);
        onboardPeople = Mathf.Clamp(totalPeople - waitingPeople - rescuedPeople, 0, totalPeople);
        RefreshViews();
    }

    public void AddRescuedPerson()
    {
        SetRescuedPeople(rescuedPeople + 1);
    }

    public void ResetCounter()
    {
        rescuedPeople = 0;
        onboardPeople = 0;
        waitingPeople = totalPeople;
        RefreshViews();
    }

    public void UpdateVisuals()
    {
        RefreshViews();
    }

    public void SyncFromRound2Controller()
    {
        if (!syncFromRound2Controller || round2GameController == null)
        {
            return;
        }

        int sourceTotal = Mathf.Max(1, round2GameController.TotalCivilians);
        ApplyCounts(sourceTotal, round2GameController.CiviliansAtHouse, round2GameController.SavedCivilians);
    }

    private void HandleRound2CountsChanged(int total, int waitingAtHouse, int saved)
    {
        if (!syncFromRound2Controller)
        {
            return;
        }

        ApplyCounts(total, waitingAtHouse, saved);
        RefreshViews();
    }

    private void ApplyCounts(int total, int waitingAtHouse, int saved)
    {
        totalPeople = Mathf.Max(1, total);
        waitingPeople = Mathf.Clamp(waitingAtHouse, 0, totalPeople);
        rescuedPeople = Mathf.Clamp(saved, 0, totalPeople);
        onboardPeople = Mathf.Clamp(totalPeople - waitingPeople - rescuedPeople, 0, totalPeople);
    }

    private void RefreshViews()
    {
        if (worldCountBadge != null)
        {
            worldCountBadge.SetWaitingCount(waitingPeople, totalPeople);
        }

        if (hudProgress != null)
        {
            hudProgress.SetProgress(rescuedPeople, totalPeople);
        }

        if (worldCountBadgeText != null)
        {
            worldCountBadgeText.text = waitingPeople > 0
                ? string.Format(remainingPeopleFormat, waitingPeople)
                : rescuedPeople >= totalPeople
                    ? completeBadgeText
                    : pickedUpBadgeText;
        }

        if (hudProgressText != null)
        {
            hudProgressText.text = $"Đã cứu: {rescuedPeople}/{totalPeople}";
        }

        if (passengerCountText != null)
        {
            passengerCountText.text = $"Trên thuyền: {onboardPeople} người";
        }

        ApplyMarkerState();

        CounterChanged?.Invoke(rescuedPeople, totalPeople);
    }

    private void ApplyMarkerState()
    {
        float rescueAlpha = waitingPeople > 0 ? rescueMarkerActiveAlpha : rescueMarkerPickedUpAlpha;
        ApplyCanvasAlpha(rescueTargetMarker, rescueAlpha);

        bool carryingPassengers = onboardPeople > 0 && rescuedPeople < totalPeople;
        if (shelterTargetMarker != null)
        {
            shelterTargetMarker.SetActive(true);
            shelterTargetMarker.transform.localScale = Vector3.one * (carryingPassengers ? shelterMarkerActiveScale : shelterMarkerIdleScale);
        }
    }

    private static void ApplyCanvasAlpha(GameObject target, float alpha)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = Mathf.Clamp01(alpha);
    }
}
