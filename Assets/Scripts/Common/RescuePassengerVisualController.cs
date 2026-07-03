using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual-only controller that animates rescued civilians from a rescue source
/// position into the boat's passenger seat anchors.
/// Attach to Player_Boat_Root in both Round 1 and Round 2 scenes.
/// Does NOT affect any gameplay logic.
/// </summary>
public class RescuePassengerVisualController : MonoBehaviour
{
    // ── Seat Anchors ─────────────────────────────────────────────────────────
    [Header("Seat Anchors")]
    public Transform[] passengerSeats;

    // ── Fallback ──────────────────────────────────────────────────────────────
    [Header("Fallback Prefab")]
    public GameObject fallbackPassengerPrefab;

    // ── Animation Settings ────────────────────────────────────────────────────
    [Header("Animation Settings")]
    public float pickupDuration    = 0.70f;
    public float pickupArcHeight   = 0.28f;
    public float staggerDelay      = 0.15f;

    [Tooltip("Fallback uniform scale applied at the seat if normalizePassengerHeight is false.")]
    public float seatedScale       = 0.10f;

    public Vector3 seatedLocalPositionOffset = Vector3.zero;
    public Vector3 seatedLocalEulerOffset    = new Vector3(0f, 90f, 0f);

    // ── Height-Based Scale Normalisation ─────────────────────────────────────
    [Header("Passenger Height Normalisation")]
    [Tooltip("Automatically shrink passenger clones to targetPassengerHeight.")]
    public bool  normalizePassengerHeight = true;

    [Tooltip("Desired world-space height of the seated passenger in metres.")]
    public float targetPassengerHeight = 0.24f;

    [Tooltip("Safety clamp – never scale smaller than this.")]
    public float minPassengerScale     = 0.04f;

    [Tooltip("Safety clamp – never scale larger than this.")]
    public float maxPassengerScale     = 0.16f;

    // ── Seated Pose ───────────────────────────────────────────────────────────
    [Header("Seated Pose")]
    [Tooltip("Attach PassengerSeatedPoseController to the clone after it reaches its seat.")]
    public bool applySeatedPose = true;

    [Tooltip("Template whose inspector values are copied onto every clone's PassengerSeatedPoseController. Leave null to use script defaults.")]
    public PassengerSeatedPoseController seatedPoseTemplate;

    // ── Clone Cleanup ─────────────────────────────────────────────────────────
    [Header("Clone Cleanup")]
    [Tooltip("Deprecated — PassengerSeatedPoseController now handles Animator disabling via disableAnimatorAfterPose.")]
    public bool disableAnimatorAfterSeated  = false;
    public bool removeAudioSourcesFromClone = true;
    public bool removeCollidersFromClone    = true;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    public bool debugLogs = false;

    // ── Internal ──────────────────────────────────────────────────────────────
    private readonly List<GameObject> seatedPassengers = new List<GameObject>();
    private int activePickupCoroutines = 0;

    public bool IsAnimating => activePickupCoroutines > 0;

    // ── Types of components to strip from clones ──────────────────────────────
    private static readonly System.Type[] s_StripTypes = new System.Type[]
    {
        typeof(Collider),
        typeof(Rigidbody),
        typeof(AudioSource),
        typeof(RescueVoiceCue),
        typeof(Round2RescueZone),
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Animate up to <paramref name="count"/> civilians from
    /// <paramref name="sourcePeople"/> into the next available seats.
    /// </summary>
    public void PlayPickupVisual(List<Transform> sourcePeople, int count)
    {
        if (count <= 0) return;
        if (passengerSeats == null || passengerSeats.Length == 0)
        {
            if (debugLogs) Debug.Log("[RescuePassengerVC] No passenger seats assigned.");
            return;
        }

        int available = GetAvailableSeatCount();
        if (available <= 0)
        {
            if (debugLogs) Debug.Log("[RescuePassengerVC] All seats occupied.");
            return;
        }

        int toAnimate = Mathf.Min(count, available);

        for (int i = 0; i < toAnimate; i++)
        {
            Transform source = (sourcePeople != null && i < sourcePeople.Count)
                ? sourcePeople[i]
                : null;

            Transform targetSeat = GetNextAvailableSeat();
            if (targetSeat == null) break;

            // Reserve the seat immediately by adding a placeholder entry
            seatedPassengers.Add(null);
            int placeholderIndex = seatedPassengers.Count - 1;

            StartCoroutine(AnimatePassenger(source, targetSeat, placeholderIndex, i * staggerDelay));
        }
    }

    /// <summary>
    /// Remove up to <paramref name="count"/> seated passengers (fade-out then destroy).
    /// </summary>
    public void ClearPassengers(int count)
    {
        if (count <= 0) return;
        int toClear = Mathf.Min(count, seatedPassengers.Count);

        for (int i = toClear - 1; i >= 0; i--)
        {
            var go = seatedPassengers[i];
            seatedPassengers.RemoveAt(i);
            if (go != null) StartCoroutine(FadeAndDestroy(go, 0.4f));
        }
    }

    /// <summary>Remove all seated passenger visuals.</summary>
    public void ClearAllPassengers()
    {
        foreach (var go in seatedPassengers)
        {
            if (go != null) StartCoroutine(FadeAndDestroy(go, 0.4f));
        }
        seatedPassengers.Clear();
    }

    /// <summary>Returns the current number of seated passengers.</summary>
    public int GetPassengerCount() => seatedPassengers.Count;

    /// <summary>Returns how many more passengers can be seated.</summary>
    public int GetAvailableSeatCount()
    {
        if (passengerSeats == null) return 0;
        return Mathf.Max(0, passengerSeats.Length - seatedPassengers.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Context Menu Debug Helpers
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Debug Clear Passengers")]
    private void DebugClearPassengers()
    {
        if (debugLogs) Debug.Log("[RescuePassengerVC] DebugClearPassengers called.");
        ClearAllPassengers();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private Transform GetNextAvailableSeat()
    {
        int occupied = seatedPassengers.Count;
        if (passengerSeats == null || occupied >= passengerSeats.Length) return null;
        return passengerSeats[occupied];
    }

    /// <summary>
    /// Calculate a world-space uniform scale so the clone's renderer height matches
    /// <see cref="targetPassengerHeight"/>. Returns <see cref="seatedScale"/> as
    /// fallback if bounds cannot be computed.
    /// </summary>
    private float CalcNormalisedScale(GameObject clone)
    {
        if (!normalizePassengerHeight) return seatedScale;

        var renderers = clone.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return seatedScale;

        // Compute combined bounds at current clone scale (localScale = source scale)
        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        float currentHeight = combined.size.y;
        if (currentHeight < 1e-4f) return seatedScale;

        // Scale factor so final world height == targetPassengerHeight
        // Final world height = currentHeight * (desiredScale / clone.localScale.y)
        // We want desiredScale such that: currentHeight * desiredScale / clone.localScale.y == target
        // => desiredScale = target * clone.localScale.y / currentHeight
        float currentUniformScale = clone.transform.localScale.y; // lossyScale already baked in
        float desiredUniformScale = targetPassengerHeight * currentUniformScale / currentHeight;

        float clamped = Mathf.Clamp(desiredUniformScale, minPassengerScale, maxPassengerScale);

        if (debugLogs)
            Debug.Log($"[RescuePassengerVC] NormaliseScale: srcH={currentHeight:F3} " +
                      $"srcLocalScale={currentUniformScale:F3} " +
                      $"desired={desiredUniformScale:F4} clamped={clamped:F4}");

        return clamped;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator AnimatePassenger(Transform source, Transform targetSeat,
                                         int placeholderIndex, float initialDelay)
    {
        activePickupCoroutines++;

        // Stagger delay
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        // ── Build clone ───────────────────────────────────────────────────────
        GameObject clone = null;

        if (source != null && source.gameObject != null)
        {
            clone = Instantiate(source.gameObject);
            CleanupClone(clone);

            // Place at source world position BEFORE computing bounds
            clone.transform.position = source.position;
            clone.transform.rotation = source.rotation;
            clone.SetActive(true);
        }
        else if (fallbackPassengerPrefab != null)
        {
            clone = Instantiate(fallbackPassengerPrefab);
            CleanupClone(clone);
            clone.transform.position = targetSeat.position + Vector3.up * 0.5f;
            clone.transform.rotation = Quaternion.identity;
            clone.SetActive(true);
        }
        else
        {
            if (debugLogs) Debug.Log("[RescuePassengerVC] No source or fallback — skipping seat.");
            seatedPassengers[placeholderIndex] = null;
            activePickupCoroutines--;
            yield break;
        }

        // ── Apply normalised scale IMMEDIATELY so clone is small during arc ───
        float finalUniformScale = CalcNormalisedScale(clone);
        clone.transform.localScale = Vector3.one * finalUniformScale;

        // ── Arc animation ─────────────────────────────────────────────────────
        Vector3 startPos = clone.transform.position;
        float elapsed    = 0f;

        while (elapsed < pickupDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / pickupDuration);
            float st = Mathf.SmoothStep(0f, 1f, t);

            // Re-evaluate target every frame so it follows a moving boat
            Vector3 endPos = targetSeat.position;
            Vector3 mid    = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * pickupArcHeight;

            // Quadratic Bezier: B(t) = (1-t)^2 * P0 + 2t(1-t) * P1 + t^2 * P2
            float inv = 1f - st;
            Vector3 pos = inv * inv * startPos
                        + 2f * st * inv * mid
                        + st * st * endPos;

            clone.transform.position = pos;

            // Gradually rotate to face the same direction as the boat root
            Quaternion targetRot = Quaternion.Euler(seatedLocalEulerOffset) * transform.rotation;
            clone.transform.rotation = Quaternion.Slerp(
                clone.transform.rotation, targetRot, st);

            yield return null;
        }

        // ── Seat the passenger ────────────────────────────────────────────────
        clone.transform.SetParent(targetSeat, worldPositionStays: false);
        clone.transform.localPosition    = seatedLocalPositionOffset;
        clone.transform.localEulerAngles = seatedLocalEulerOffset;
        // Preserve the already-computed uniform scale in local space under the seat
        clone.transform.localScale = Vector3.one * finalUniformScale;

        // ── Apply seated bone pose ─────────────────────────────────────────────
        if (applySeatedPose)
        {
            var poseCtrl = clone.GetComponent<PassengerSeatedPoseController>();
            if (poseCtrl == null)
                poseCtrl = clone.AddComponent<PassengerSeatedPoseController>();

            // Copy tuning values from template if one is assigned
            if (seatedPoseTemplate != null)
            {
                poseCtrl.disableAnimatorAfterPose  = seatedPoseTemplate.disableAnimatorAfterPose;
                poseCtrl.keepApplyingInLateUpdate  = seatedPoseTemplate.keepApplyingInLateUpdate;
                poseCtrl.rootLocalPositionOffset   = seatedPoseTemplate.rootLocalPositionOffset;
                poseCtrl.rootLocalEulerOffset      = seatedPoseTemplate.rootLocalEulerOffset;
                poseCtrl.rootLocalScaleMultiplier  = seatedPoseTemplate.rootLocalScaleMultiplier;
                poseCtrl.hipsEuler       = seatedPoseTemplate.hipsEuler;
                poseCtrl.spineEuler      = seatedPoseTemplate.spineEuler;
                poseCtrl.chestEuler      = seatedPoseTemplate.chestEuler;
                poseCtrl.headEuler       = seatedPoseTemplate.headEuler;
                poseCtrl.leftUpperLegEuler  = seatedPoseTemplate.leftUpperLegEuler;
                poseCtrl.rightUpperLegEuler = seatedPoseTemplate.rightUpperLegEuler;
                poseCtrl.leftLowerLegEuler  = seatedPoseTemplate.leftLowerLegEuler;
                poseCtrl.rightLowerLegEuler = seatedPoseTemplate.rightLowerLegEuler;
                poseCtrl.leftUpperArmEuler  = seatedPoseTemplate.leftUpperArmEuler;
                poseCtrl.rightUpperArmEuler = seatedPoseTemplate.rightUpperArmEuler;
                poseCtrl.leftLowerArmEuler  = seatedPoseTemplate.leftLowerArmEuler;
                poseCtrl.rightLowerArmEuler = seatedPoseTemplate.rightLowerArmEuler;
                poseCtrl.debugLogs          = seatedPoseTemplate.debugLogs;
            }

            // applyOnEnable fired automatically on AddComponent, so Initialise() has already run.
            // We just need to force the new template values to apply now.
            poseCtrl.ForceApplyPose();

            if (debugLogs) Debug.Log($"[RescuePassengerVC] SeatedPose applied to {clone.name}.");
        }
        else if (disableAnimatorAfterSeated)
        {
            // Legacy path
            var anim = clone.GetComponent<Animator>();
            if (anim != null) anim.enabled = false;
        }

        // Replace placeholder in list
        seatedPassengers[placeholderIndex] = clone;

        if (debugLogs) Debug.Log($"[RescuePassengerVC] Passenger seated at {targetSeat.name}.");
        activePickupCoroutines--;
    }

    private IEnumerator FadeAndDestroy(GameObject go, float duration)
    {
        // Collect all renderers and their original colours
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var origColors = new List<(Renderer r, Color[] cols)>();

        foreach (var r in renderers)
        {
            var mats = r.materials;
            var cols = new Color[mats.Length];
            for (int m = 0; m < mats.Length; m++)
                cols[m] = mats[m].HasProperty("_Color") ? mats[m].color : Color.white;
            origColors.Add((r, cols));
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);

            foreach (var (r, cols) in origColors)
            {
                var mats = r.materials;
                for (int m = 0; m < mats.Length && m < cols.Length; m++)
                {
                    if (mats[m].HasProperty("_Color"))
                        mats[m].color = new Color(cols[m].r, cols[m].g, cols[m].b, alpha);
                }
            }
            yield return null;
        }

        Destroy(go);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Clone Cleanup
    // ─────────────────────────────────────────────────────────────────────────

    private void CleanupClone(GameObject clone)
    {
        // Set to Ignore Raycast layer
        SetLayerRecursively(clone, LayerMask.NameToLayer("Ignore Raycast"));

        // Remove unwanted component types
        foreach (var type in s_StripTypes)
        {
            var components = clone.GetComponentsInChildren(type, includeInactive: true);
            foreach (var comp in components)
            {
                if (comp is Behaviour b) b.enabled = false;
                Destroy(comp);
            }
        }

        // Strip all MonoBehaviours except Animator
        var monos = clone.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mono in monos)
        {
            if (mono == null) continue;
            if (mono.GetType() == typeof(Animator)) continue;
            Destroy(mono);
        }
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
