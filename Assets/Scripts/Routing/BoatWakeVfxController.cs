using UnityEngine;

public sealed class BoatWakeVfxController : MonoBehaviour
{
    [SerializeField] private BoatRouteMover boatMover;
    [SerializeField] private ParticleSystem[] wakeSystems;
    [SerializeField] private TrailRenderer[] wakeTrails;
    [SerializeField] private Renderer[] movingOnlyRenderers;
    [SerializeField] private float idleEmissionRate;
    [SerializeField] private float movingEmissionRate = 14f;
    [SerializeField] private float fadeSpeed = 8f;

    private float currentEmissionRate;
    private bool wasMoving;
    private BoatRouteMover subscribedMover;

    private void Awake()
    {
        EnsureReferences();

        currentEmissionRate = idleEmissionRate;
        ApplyEmission(currentEmissionRate);
        SetTrailEmission(false, clearTrails: true);
        SetMovingRenderers(false);
    }

    private void OnEnable()
    {
        EnsureReferences();
        SubscribeToMover();
        PlaySystems();
    }

    private void OnDisable()
    {
        UnsubscribeFromMover();
    }

    private void Update()
    {
        bool isMoving = boatMover != null && boatMover.IsMoving;

        if (isMoving != wasMoving)
        {
            SetTrailEmission(isMoving, clearTrails: false);
            SetMovingRenderers(isMoving);
            wasMoving = isMoving;
        }

        float targetRate = isMoving
            ? movingEmissionRate
            : idleEmissionRate;

        currentEmissionRate = Mathf.MoveTowards(
            currentEmissionRate,
            targetRate,
            fadeSpeed * Time.deltaTime);

        ApplyEmission(currentEmissionRate);
    }

    private void EnsureReferences()
    {
        boatMover ??= GetComponentInParent<BoatRouteMover>();

        if (wakeSystems == null || wakeSystems.Length == 0)
        {
            wakeSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        if (wakeTrails == null || wakeTrails.Length == 0)
        {
            wakeTrails = GetComponentsInChildren<TrailRenderer>(true);
        }
    }

    private void SubscribeToMover()
    {
        if (boatMover == null || subscribedMover == boatMover)
        {
            return;
        }

        UnsubscribeFromMover();
        subscribedMover = boatMover;
        subscribedMover.MoveAccepted += HandleMoveAccepted;
        subscribedMover.ArrivedAtNode += HandleArrivedAtNode;
    }

    private void UnsubscribeFromMover()
    {
        if (subscribedMover == null)
        {
            return;
        }

        subscribedMover.MoveAccepted -= HandleMoveAccepted;
        subscribedMover.ArrivedAtNode -= HandleArrivedAtNode;
        subscribedMover = null;
    }

    private void HandleMoveAccepted(RouteNode fromNode, RouteNode toNode)
    {
        currentEmissionRate = movingEmissionRate;
        ApplyEmission(currentEmissionRate);
        EmitWakeBurst(6);
        SetTrailEmission(true, clearTrails: true);
        SetMovingRenderers(true);
        wasMoving = true;
    }

    private void HandleArrivedAtNode(RouteNode node)
    {
        SetTrailEmission(false, clearTrails: false);
        SetMovingRenderers(false);
        wasMoving = false;
    }

    private void ApplyEmission(float rate)
    {
        if (wakeSystems == null)
        {
            return;
        }

        for (int i = 0; i < wakeSystems.Length; i++)
        {
            ParticleSystem system = wakeSystems[i];
            if (system == null)
            {
                continue;
            }

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;
        }
    }

    private void EmitWakeBurst(int count)
    {
        if (wakeSystems == null)
        {
            return;
        }

        for (int i = 0; i < wakeSystems.Length; i++)
        {
            ParticleSystem system = wakeSystems[i];
            if (system == null)
            {
                continue;
            }

            if (!system.isPlaying)
            {
                system.Play();
            }

            system.Emit(count);
        }
    }

    private void SetTrailEmission(bool emitting, bool clearTrails)
    {
        if (wakeTrails == null)
        {
            return;
        }

        for (int i = 0; i < wakeTrails.Length; i++)
        {
            TrailRenderer trail = wakeTrails[i];
            if (trail == null)
            {
                continue;
            }

            trail.emitting = emitting;

            if (clearTrails)
            {
                trail.Clear();
            }
        }
    }

    private void SetMovingRenderers(bool visible)
    {
        if (movingOnlyRenderers == null)
        {
            return;
        }

        for (int i = 0; i < movingOnlyRenderers.Length; i++)
        {
            Renderer renderer = movingOnlyRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private void PlaySystems()
    {
        if (wakeSystems == null)
        {
            return;
        }

        for (int i = 0; i < wakeSystems.Length; i++)
        {
            ParticleSystem system = wakeSystems[i];
            if (system != null && !system.isPlaying)
            {
                system.Play();
            }
        }
    }
}
