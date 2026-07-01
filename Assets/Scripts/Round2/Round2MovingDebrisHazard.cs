using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Round2;

public class Round2MovingDebrisHazard : MonoBehaviour
{
    public enum DebrisMoveMode
    {
        PingPong,
        CurrentDriftLoop
    }

    [Header("Movement Settings")]
    public DebrisMoveMode moveMode = DebrisMoveMode.CurrentDriftLoop;
    public Transform pointA;
    public Transform pointB;
    [FormerlySerializedAs("moveSpeed")] public float driftSpeed = 0.8f;
    public float respawnDelay = 2.0f;
    public float initialDelay = 0.0f;
    public float randomSpeedRange = 0.15f;
    public bool startVisible = true;

    [Header("Legacy Ping-Pong Settings")]
    public float pauseAtEnds = 0.25f;
    public bool startAtPointA = true;

    [Header("Presentation")]
    public bool useCachedWorldWaypoints = true;
    public bool rotateTowardMovement = true;
    public float yawOffset = 0f;
    public bool enableBobbing = true;
    public float bobAmplitude = 0.04f;
    public float bobSpeed = 1.8f;
    public bool enableRollWobble = true;
    public float rollWobbleAmount = 4f;
    public float rollWobbleSpeed = 2f;

    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 1.0f;
    [FormerlySerializedAs("damageOncePerContact")] public bool damageOncePerPass = true;

    [Header("Soft Impact")]
    public bool applySoftImpact = true;
    public float impactSpeedMultiplier = 0.25f;
    public float impactLockDuration = 0.3f;

    [Header("References")]
    public Round2RealtimeRoundController roundController;
    public Round2CollisionWarningUI collisionWarningUI;

    private Vector3 cachedPointA;
    private Vector3 cachedPointB;
    private Vector3 currentBasePosition;
    private Vector3 lastMoveDirection;
    private float lastDamageTime = -999f;
    private float pauseTimer;
    private float driftTimer;
    private float currentDriftSpeed;
    private bool waitingToRespawn;
    private bool driftVisible;
    private bool hasValidWaypoints;
    private bool hasLoggedMissingWaypoints;
    private bool movingTowardB;
    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;
    private readonly HashSet<Round2FirstPersonBoatController> damagedBoatsThisPass = new HashSet<Round2FirstPersonBoatController>();

    private void Start()
    {
        if (roundController == null)
        {
            roundController = FindObjectOfType<Round2RealtimeRoundController>();
        }

        if (collisionWarningUI == null)
        {
            collisionWarningUI = FindObjectOfType<Round2CollisionWarningUI>();
        }

        CacheWaypoints();
        ConfigureRigidbody();
        CacheVisualAndColliderReferences();

        if (hasValidWaypoints)
        {
            movingTowardB = startAtPointA;
            currentBasePosition = startAtPointA ? GetPointA() : GetPointB();
            currentDriftSpeed = GetRandomizedDriftSpeed();
            driftTimer = Mathf.Max(0f, initialDelay);
            transform.position = ApplyPresentationMotion(currentBasePosition);
        }
        else
        {
            currentBasePosition = transform.position;
        }

        SetDebrisVisible(startVisible);
    }

    private void Update()
    {
        if (roundController == null || !roundController.IsPlaying())
        {
            return;
        }

        if (!hasValidWaypoints)
        {
            if (pointA == null || pointB == null)
            {
                LogMissingWaypointsOnce();
                return;
            }

            CacheWaypoints();
        }

        if (moveMode == DebrisMoveMode.CurrentDriftLoop)
        {
            MoveCurrentDriftLoop();
        }
        else
        {
            MovePingPong();
        }
    }

    private void CacheWaypoints()
    {
        if (pointA == null || pointB == null)
        {
            hasValidWaypoints = false;
            LogMissingWaypointsOnce();
            return;
        }

        cachedPointA = pointA.position;
        cachedPointB = pointB.position;
        hasValidWaypoints = true;
    }

    private void MoveCurrentDriftLoop()
    {
        if (driftTimer > 0f)
        {
            driftTimer -= Time.deltaTime;
            transform.position = ApplyPresentationMotion(currentBasePosition);
            return;
        }

        if (waitingToRespawn)
        {
            ResetCurrentDriftPass();
            return;
        }

        Vector3 target = GetPointB();
        Vector3 previousPosition = currentBasePosition;
        currentBasePosition = Vector3.MoveTowards(currentBasePosition, target, currentDriftSpeed * Time.deltaTime);
        UpdateMovementPresentation(currentBasePosition - previousPosition);
        transform.position = ApplyPresentationMotion(currentBasePosition);

        if ((currentBasePosition - target).sqrMagnitude <= 0.0001f)
        {
            BeginRespawnDelay();
        }
    }

    private void MovePingPong()
    {
        Vector3 target = movingTowardB ? GetPointB() : GetPointA();

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            transform.position = ApplyPresentationMotion(currentBasePosition);
            return;
        }

        Vector3 previousPosition = currentBasePosition;
        currentBasePosition = Vector3.MoveTowards(currentBasePosition, target, driftSpeed * Time.deltaTime);
        UpdateMovementPresentation(currentBasePosition - previousPosition);
        transform.position = ApplyPresentationMotion(currentBasePosition);

        if ((currentBasePosition - target).sqrMagnitude <= 0.0001f)
        {
            movingTowardB = !movingTowardB;
            pauseTimer = Mathf.Max(0f, pauseAtEnds);
        }
    }

    private void BeginRespawnDelay()
    {
        waitingToRespawn = true;
        driftTimer = Mathf.Max(0f, respawnDelay);
        SetDebrisVisible(false);
    }

    private void ResetCurrentDriftPass()
    {
        waitingToRespawn = false;
        currentBasePosition = GetPointA();
        currentDriftSpeed = GetRandomizedDriftSpeed();
        damagedBoatsThisPass.Clear();
        lastDamageTime = -999f;
        SetDebrisVisible(true);
        transform.position = ApplyPresentationMotion(currentBasePosition);
        UpdateMovementPresentation(GetPointB() - GetPointA());
    }

    private void UpdateMovementPresentation(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        lastMoveDirection = moveDirection;
        RotateToward(moveDirection);
    }

    private float GetRandomizedDriftSpeed()
    {
        float randomOffset = randomSpeedRange > 0f ? Random.Range(-randomSpeedRange, randomSpeedRange) : 0f;
        return Mathf.Max(0.05f, driftSpeed + randomOffset);
    }

    private Vector3 GetPointA()
    {
        return useCachedWorldWaypoints ? cachedPointA : pointA.position;
    }

    private Vector3 GetPointB()
    {
        return useCachedWorldWaypoints ? cachedPointB : pointB.position;
    }

    private Vector3 ApplyPresentationMotion(Vector3 basePosition)
    {
        ApplyRollWobble();
        return ApplyBobbing(basePosition);
    }

    private Vector3 ApplyBobbing(Vector3 basePosition)
    {
        if (!enableBobbing || bobAmplitude <= 0f)
        {
            return basePosition;
        }

        Vector3 result = basePosition;
        result.y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        return result;
    }

    private void ApplyRollWobble()
    {
        if (!enableRollWobble || rollWobbleAmount <= 0f)
        {
            return;
        }

        Vector3 euler = transform.eulerAngles;
        euler.z = Mathf.Sin(Time.time * rollWobbleSpeed) * rollWobbleAmount;
        transform.eulerAngles = euler;
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (!rotateTowardMovement)
        {
            return;
        }

        moveDirection.y = 0f;
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up) * Quaternion.Euler(0f, yawOffset, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private void ConfigureRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void CacheVisualAndColliderReferences()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private void SetDebrisVisible(bool visible)
    {
        driftVisible = visible;

        if (cachedRenderers != null)
        {
            foreach (Renderer renderer in cachedRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        if (cachedColliders != null)
        {
            foreach (Collider collider in cachedColliders)
            {
                if (collider != null)
                {
                    collider.enabled = visible;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamageBoat(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamageBoat(other);
    }

    private void TryDamageBoat(Collider other)
    {
        if (!driftVisible)
        {
            return;
        }

        if (roundController == null)
        {
            roundController = FindObjectOfType<Round2RealtimeRoundController>();
        }

        if (collisionWarningUI == null)
        {
            collisionWarningUI = FindObjectOfType<Round2CollisionWarningUI>();
        }

        if (roundController == null || !roundController.IsPlaying())
        {
            return;
        }

        if (Time.time - lastDamageTime < damageCooldown)
        {
            return;
        }

        var boatController = other.GetComponentInParent<Round2FirstPersonBoatController>();
        if (boatController == null)
        {
            return;
        }

        if (damageOncePerPass && damagedBoatsThisPass.Contains(boatController))
        {
            return;
        }

        if (damageOncePerPass)
        {
            damagedBoatsThisPass.Add(boatController);
        }

        if (damageAmount > 0)
        {
            int durabilityBefore = roundController.currentBoatDurability;
            roundController.ApplyDamage(damageAmount);
            bool durabilityLost = roundController.currentBoatDurability < durabilityBefore;

            if (durabilityLost && collisionWarningUI != null)
            {
                collisionWarningUI.TriggerDamageWarning(roundController.currentBoatDurability <= 0);
            }

            roundController.ShowFeedback("Va chạm vật trôi! Độ bền -" + damageAmount + ".");
        }
        
        lastDamageTime = Time.time;

        if (applySoftImpact)
        {
            Vector3 impactDirection = boatController.transform.position - transform.position;
            if (lastMoveDirection.sqrMagnitude > 0.0001f)
            {
                impactDirection += lastMoveDirection.normalized * 0.35f;
            }

            boatController.ApplySoftImpact(impactDirection, impactSpeedMultiplier, impactLockDuration);
        }
    }

    private void LogMissingWaypointsOnce()
    {
        if (hasLoggedMissingWaypoints)
        {
            return;
        }

        hasLoggedMissingWaypoints = true;
        Debug.LogWarning("[Round2MovingDebrisHazard] Missing waypoint reference on " + gameObject.name, this);
    }

    private void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null)
        {
            return;
        }

        Gizmos.color = moveMode == DebrisMoveMode.CurrentDriftLoop
            ? new Color(0.2f, 0.85f, 1f, 0.9f)
            : new Color(1f, 0.65f, 0.1f, 0.9f);

        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawSphere(pointA.position, 0.25f);
        Gizmos.DrawSphere(pointB.position, 0.25f);

        Vector3 arrowDirection = pointB.position - pointA.position;
        if (arrowDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 direction = arrowDirection.normalized;
            Vector3 arrowCenter = Vector3.Lerp(pointA.position, pointB.position, 0.78f);
            Vector3 side = Quaternion.Euler(0f, 35f, 0f) * -direction;
            Vector3 otherSide = Quaternion.Euler(0f, -35f, 0f) * -direction;
            Gizmos.DrawLine(arrowCenter, arrowCenter + side * 0.55f);
            Gizmos.DrawLine(arrowCenter, arrowCenter + otherSide * 0.55f);
        }

        if (Application.isPlaying && hasValidWaypoints)
        {
            Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.9f);
            Gizmos.DrawLine(cachedPointA, cachedPointB);
            Gizmos.DrawWireSphere(cachedPointA, 0.35f);
            Gizmos.DrawWireSphere(cachedPointB, 0.35f);
        }

        if (lastMoveDirection.sqrMagnitude > 0.0001f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, lastMoveDirection.normalized * 1.25f);
        }
    }
}
