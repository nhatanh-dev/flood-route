using System.Reflection;
using UnityEngine;

public sealed class BoatWakeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour boatController;
    [SerializeField] private ParticleSystem bowLeft;
    [SerializeField] private ParticleSystem bowRight;
    [SerializeField] private ParticleSystem bowPuffLeft;
    [SerializeField] private ParticleSystem bowPuffRight;
    [SerializeField] private ParticleSystem sideLeft;
    [SerializeField] private ParticleSystem sideRight;
    [SerializeField] private ParticleSystem sternFoam;

    [Header("Speed Response")]
    [SerializeField] private float minWakeSpeed = 0.15f;
    [SerializeField] private float fullWakeSpeed = 2.2f;
    [SerializeField] private float responseSmoothing = 7.5f;
    [SerializeField] private float maxBowEmission = 40f;
    [SerializeField] private float maxBowPuffEmission = 14f;
    [SerializeField] private float maxSideEmission = 14f;
    [SerializeField] private float maxSternEmission = 22f;
    [SerializeField] private bool stopWhenNoController = true;
    [SerializeField] private bool debugForceVisible;
    [SerializeField] private bool enableDebugLogs;

    private ParticleSystem[] systems;
    private float[] baseStartSizes;
    private PropertyInfo speedProperty;
    private FieldInfo speedField;
    private MonoBehaviour cachedController;
    private float lastSpeed;
    private float lastWakeStrength;
    private float smoothedWakeStrength;

    public MonoBehaviour BoatController
    {
        get => boatController;
        set
        {
            boatController = value;
            CacheSpeedMember();
        }
    }

    public float LastSpeed => lastSpeed;
    public float LastWakeStrength => lastWakeStrength;

    private void Awake()
    {
        CacheReferences();
        CacheSpeedMember();
        CaptureBaseSizes();
        SetWake(0f);
    }

    private void OnEnable()
    {
        CacheReferences();
        CacheSpeedMember();
        PlaySystems();
    }

    private void Update()
    {
        float speed;
        if (!TryGetSpeed(out speed))
        {
            lastSpeed = 0f;
            float missingControllerTarget = debugForceVisible ? 1f : 0f;
            smoothedWakeStrength = SmoothWake(smoothedWakeStrength, missingControllerTarget);
            lastWakeStrength = smoothedWakeStrength;

            if (debugForceVisible)
            {
                SetWake(smoothedWakeStrength);
                return;
            }

            if (stopWhenNoController)
            {
                SetWake(smoothedWakeStrength);
            }

            return;
        }

        float targetStrength = debugForceVisible
            ? 1f
            : Mathf.InverseLerp(minWakeSpeed, fullWakeSpeed, speed);
        targetStrength = SmoothStep01(targetStrength);
        smoothedWakeStrength = SmoothWake(smoothedWakeStrength, targetStrength);

        lastSpeed = speed;
        lastWakeStrength = smoothedWakeStrength;
        SetWake(smoothedWakeStrength);
    }

    public void Rebind(MonoBehaviour controller)
    {
        BoatController = controller;
    }

    private void CacheReferences()
    {
        if (bowLeft == null)
        {
            Transform child = transform.Find("BowWake_Left");
            if (child != null) bowLeft = child.GetComponent<ParticleSystem>();
        }

        if (bowRight == null)
        {
            Transform child = transform.Find("BowWake_Right");
            if (child != null) bowRight = child.GetComponent<ParticleSystem>();
        }

        if (bowPuffLeft == null)
        {
            Transform child = transform.Find("BowFoamPuff_Left");
            if (child != null) bowPuffLeft = child.GetComponent<ParticleSystem>();
        }

        if (bowPuffRight == null)
        {
            Transform child = transform.Find("BowFoamPuff_Right");
            if (child != null) bowPuffRight = child.GetComponent<ParticleSystem>();
        }

        if (sideLeft == null)
        {
            Transform child = transform.Find("SideWake_Left");
            if (child != null) sideLeft = child.GetComponent<ParticleSystem>();
        }

        if (sideRight == null)
        {
            Transform child = transform.Find("SideWake_Right");
            if (child != null) sideRight = child.GetComponent<ParticleSystem>();
        }

        if (sternFoam == null)
        {
            Transform child = transform.Find("SternFoam");
            if (child != null) sternFoam = child.GetComponent<ParticleSystem>();
        }

        systems = new[] { bowLeft, bowRight, bowPuffLeft, bowPuffRight, sideLeft, sideRight, sternFoam };
    }

    private void CacheSpeedMember()
    {
        if (boatController == cachedController && (speedProperty != null || speedField != null))
        {
            return;
        }

        cachedController = boatController;
        speedProperty = null;
        speedField = null;

        if (boatController == null)
        {
            return;
        }

        System.Type type = boatController.GetType();
        speedProperty = type.GetProperty(
            "CurrentSpeedAbs",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (speedProperty == null || speedProperty.PropertyType != typeof(float))
        {
            speedProperty = null;
            speedField = type.GetField(
                "currentSpeed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (enableDebugLogs && speedProperty == null && speedField == null)
        {
            Debug.LogWarning($"{name}: no CurrentSpeedAbs property or currentSpeed field found on {type.Name}.", this);
        }
    }

    private bool TryGetSpeed(out float speed)
    {
        speed = 0f;

        if (boatController == null)
        {
            return false;
        }

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
        if (systems == null)
        {
            return;
        }

        baseStartSizes = new float[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
            {
                baseStartSizes[i] = 1f;
                continue;
            }

            var main = systems[i].main;
            baseStartSizes[i] = main.startSizeMultiplier;
        }
    }

    private void SetWake(float strength)
    {
        float bowRate = maxBowEmission * strength;
        float bowPuffRate = maxBowPuffEmission * strength;
        float sideRate = maxSideEmission * strength;
        float sternRate = maxSternEmission * strength;

        ApplySystem(bowLeft, 0, bowRate, strength);
        ApplySystem(bowRight, 1, bowRate, strength);
        ApplySystem(bowPuffLeft, 2, bowPuffRate, strength);
        ApplySystem(bowPuffRight, 3, bowPuffRate, strength);
        ApplySystem(sideLeft, 4, sideRate, strength);
        ApplySystem(sideRight, 5, sideRate, strength);
        ApplySystem(sternFoam, 6, sternRate, strength);
    }

    private void ApplySystem(ParticleSystem system, int index, float emissionRate, float strength)
    {
        if (system == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = emissionRate;

        ParticleSystem.MainModule main = system.main;
        if (baseStartSizes != null && index >= 0 && index < baseStartSizes.Length)
        {
            main.startSizeMultiplier = baseStartSizes[index] * Mathf.Lerp(0.85f, 1.12f, strength);
        }

        if (strength > 0f)
        {
            if (!system.isPlaying)
            {
                system.Play(false);
            }
        }
        else if (system.isPlaying)
        {
            system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private float SmoothWake(float current, float target)
    {
        if (Application.isPlaying)
        {
            float factor = 1f - Mathf.Exp(-responseSmoothing * Time.deltaTime);
            return Mathf.Lerp(current, target, factor);
        }

        return target;
    }

    private static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private void PlaySystems()
    {
        if (systems == null)
        {
            return;
        }

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && !systems[i].isPlaying)
            {
                systems[i].Play(false);
            }
        }
    }
}
