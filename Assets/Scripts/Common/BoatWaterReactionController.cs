using System.Reflection;
using UnityEngine;

public sealed class BoatWaterReactionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour boatController;

    [SerializeField] private ParticleSystem bowSprayLeft;
    [SerializeField] private ParticleSystem bowSprayRight;
    [SerializeField] private ParticleSystem bowFoamLeft;
    [SerializeField] private ParticleSystem bowFoamRight;
    [SerializeField] private ParticleSystem sternDisturbance;

    [Header("Speed Response")]
    [SerializeField] private float minEffectSpeed = 0.05f;
    [SerializeField] private float fullEffectSpeed = 1.7f;
    [SerializeField] private float responseSmoothing = 8f;
    [SerializeField] private float maxBowSprayEmission = 90f;
    [SerializeField] private float maxBowFoamEmission = 25f;
    [SerializeField] private float maxSternEmission = 18f;

    [Header("Debug")]
    [SerializeField] private bool debugForceVisible = false;

    private ParticleSystem[] systems;
    private float[] baseStartSizes;
    private PropertyInfo speedProperty;
    private FieldInfo speedField;
    private MonoBehaviour cachedController;
    private float smoothedWakeStrength;

    private void Awake()
    {
        CacheReferences();
        CacheSpeedMember();
        CaptureBaseSizes();
        SetEffect(0f);
    }

    private void OnEnable()
    {
        CacheReferences();
        CacheSpeedMember();
        PlaySystems();
    }

    private void Update()
    {
        float targetStrength = 0f;

        if (debugForceVisible)
        {
            targetStrength = 1f;
        }
        else if (TryGetSpeed(out float speed))
        {
            targetStrength = Mathf.InverseLerp(minEffectSpeed, fullEffectSpeed, speed);
            targetStrength = Mathf.Clamp01(targetStrength);
            targetStrength = targetStrength * targetStrength * (3f - 2f * targetStrength); // SmoothStep
        }

        smoothedWakeStrength = Application.isPlaying
            ? Mathf.Lerp(smoothedWakeStrength, targetStrength, 1f - Mathf.Exp(-responseSmoothing * Time.deltaTime))
            : targetStrength;

        SetEffect(smoothedWakeStrength);
    }

    private void CacheReferences()
    {
        if (bowSprayLeft == null) bowSprayLeft = transform.Find("BowSpray_Left")?.GetComponent<ParticleSystem>();
        if (bowSprayRight == null) bowSprayRight = transform.Find("BowSpray_Right")?.GetComponent<ParticleSystem>();
        if (bowFoamLeft == null) bowFoamLeft = transform.Find("BowFoam_Left")?.GetComponent<ParticleSystem>();
        if (bowFoamRight == null) bowFoamRight = transform.Find("BowFoam_Right")?.GetComponent<ParticleSystem>();
        if (sternDisturbance == null) sternDisturbance = transform.Find("SternDisturbance")?.GetComponent<ParticleSystem>();

        systems = new[] { bowSprayLeft, bowSprayRight, bowFoamLeft, bowFoamRight, sternDisturbance };
    }

    private void CacheSpeedMember()
    {
        if (boatController == cachedController && (speedProperty != null || speedField != null))
            return;

        cachedController = boatController;
        speedProperty = null;
        speedField = null;

        if (boatController == null) return;

        System.Type type = boatController.GetType();
        speedProperty = type.GetProperty("CurrentSpeedAbs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (speedProperty == null || speedProperty.PropertyType != typeof(float))
        {
            speedProperty = null;
            speedField = type.GetField("currentSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    private bool TryGetSpeed(out float speed)
    {
        speed = 0f;
        if (boatController == null) return false;
        CacheSpeedMember();

        if (speedProperty != null)
        {
            speed = Mathf.Abs((float)speedProperty.GetValue(boatController, null));
            return true;
        }
        if (speedField != null)
        {
            speed = Mathf.Abs((float)speedField.GetValue(boatController));
            return true;
        }
        return false;
    }

    private void CaptureBaseSizes()
    {
        if (systems == null) return;
        baseStartSizes = new float[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) { baseStartSizes[i] = 1f; continue; }
            baseStartSizes[i] = systems[i].main.startSizeMultiplier;
        }
    }

    private void SetEffect(float strength)
    {
        float sprayRate = maxBowSprayEmission * strength;
        float foamRate  = maxBowFoamEmission * strength;
        float sternRate = maxSternEmission * strength;

        ApplySystem(bowSprayLeft,  0, sprayRate, strength);
        ApplySystem(bowSprayRight, 1, sprayRate, strength);
        ApplySystem(bowFoamLeft,   2, foamRate,  strength);
        ApplySystem(bowFoamRight,  3, foamRate,  strength);
        ApplySystem(sternDisturbance, 4, sternRate, strength);
    }

    private void ApplySystem(ParticleSystem system, int index, float emissionRate, float strength)
    {
        if (system == null) return;

        var emission = system.emission;
        emission.rateOverTime = emissionRate;

        var main = system.main;
        if (baseStartSizes != null && index >= 0 && index < baseStartSizes.Length)
            main.startSizeMultiplier = baseStartSizes[index] * Mathf.Lerp(0.85f, 1.12f, strength);

        if (strength > 0f)
        {
            if (!system.isPlaying) system.Play(false);
        }
        else if (system.isPlaying)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void PlaySystems()
    {
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
            if (systems[i] != null && !systems[i].isPlaying) systems[i].Play(false);
    }
}
