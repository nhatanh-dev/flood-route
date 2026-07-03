using System.Reflection;
using UnityEngine;

public sealed class BoatWakeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour boatController;

    // PRIMARY: small bow splash — main visible effect
    [SerializeField] private ParticleSystem bowSplashLeft;
    [SerializeField] private ParticleSystem bowSplashRight;

    // SECONDARY: side ripple flecks
    [SerializeField] private ParticleSystem sideLeft;
    [SerializeField] private ParticleSystem sideRight;

    // SECONDARY: stern disturbance
    [SerializeField] private ParticleSystem sternFoam;

    [Header("Speed Response")]
    [SerializeField] private float minWakeSpeed = 0.08f;
    [SerializeField] private float fullWakeSpeed = 2.2f;
    [SerializeField] private float responseSmoothing = 7.5f;
    [SerializeField] private float maxBowSplashEmission = 35f;
    [SerializeField] private float maxSideEmission = 8f;
    [SerializeField] private float maxSternEmission = 14f;
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
            float missingTarget = debugForceVisible ? 1f : 0f;
            smoothedWakeStrength = SmoothWake(smoothedWakeStrength, missingTarget);
            lastWakeStrength = smoothedWakeStrength;

            if (debugForceVisible) { SetWake(smoothedWakeStrength); return; }
            if (stopWhenNoController) SetWake(smoothedWakeStrength);
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

    public void Rebind(MonoBehaviour controller) => BoatController = controller;

    private void CacheReferences()
    {
        if (bowSplashLeft == null)
        {
            var c = transform.Find("BowSplash_Left");
            if (c != null) bowSplashLeft = c.GetComponent<ParticleSystem>();
        }
        if (bowSplashRight == null)
        {
            var c = transform.Find("BowSplash_Right");
            if (c != null) bowSplashRight = c.GetComponent<ParticleSystem>();
        }
        if (sideLeft == null)
        {
            var c = transform.Find("SideWake_Left");
            if (c != null) sideLeft = c.GetComponent<ParticleSystem>();
        }
        if (sideRight == null)
        {
            var c = transform.Find("SideWake_Right");
            if (c != null) sideRight = c.GetComponent<ParticleSystem>();
        }
        if (sternFoam == null)
        {
            var c = transform.Find("SternFoam");
            if (c != null) sternFoam = c.GetComponent<ParticleSystem>();
        }

        systems = new[] { bowSplashLeft, bowSplashRight, sideLeft, sideRight, sternFoam };
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
            Debug.LogWarning($"{name}: no CurrentSpeedAbs or currentSpeed found on {type.Name}.", this);
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

    private void SetWake(float strength)
    {
        float splashRate = maxBowSplashEmission * strength;
        float sideRate   = maxSideEmission       * strength;
        float sternRate  = maxSternEmission      * strength;

        ApplySystem(bowSplashLeft,  0, splashRate, strength);
        ApplySystem(bowSplashRight, 1, splashRate, strength);
        ApplySystem(sideLeft,       2, sideRate,   strength);
        ApplySystem(sideRight,      3, sideRate,   strength);
        ApplySystem(sternFoam,      4, sternRate,  strength);
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

    private float SmoothWake(float current, float target)
    {
        if (!Application.isPlaying) return target;
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-responseSmoothing * Time.deltaTime));
    }

    private static float SmoothStep01(float v)
    {
        v = Mathf.Clamp01(v);
        return v * v * (3f - 2f * v);
    }

    private void PlaySystems()
    {
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
            if (systems[i] != null && !systems[i].isPlaying) systems[i].Play(false);
    }
}
