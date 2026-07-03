using UnityEngine;

/// <summary>
/// Visual-only component that applies a seated/crouched pose to a humanoid
/// character by directly rotating its mapped humanoid bones every LateUpdate.
///
/// Attach to a passenger clone AFTER it has been placed on its seat anchor.
/// Does NOT affect any gameplay logic.
/// </summary>
[DefaultExecutionOrder(200)]   // run late so Animator has already written its pose
public class PassengerSeatedPoseController : MonoBehaviour
{
    // ── General ──────────────────────────────────────────────────────────────
    [Header("General")]
    public bool applyOnEnable           = true;
    public bool keepApplyingInLateUpdate = true;
    public bool disableAnimatorAfterPose = true;   // freeze animation
    public bool debugLogs               = false;

    // ── Root Transform Adjustments ────────────────────────────────────────────
    [Header("Root Transform Adjustments")]
    public Vector3 rootLocalPositionOffset  = new Vector3(0f, -0.1f, -0.02f);
    public Vector3 rootLocalEulerOffset     = Vector3.zero;
    public Vector3 rootLocalScaleMultiplier = new Vector3(0.78f, 0.78f, 0.78f);

    // ── Bone Rotation Offsets (local Euler angles) ───────────────────────────
    // These are ADDITIVE offsets on top of the rest pose.
    // Positive X on leg = kick forward; negative X = push back.

    [Header("Hips / Spine")]
    public Vector3 hipsEuler       = new Vector3(-5f,   0f,   0f);
    public Vector3 spineEuler      = new Vector3(8f,  0f,   0f);
    public Vector3 chestEuler      = new Vector3(5f,  0f,   0f);
    public Vector3 headEuler       = new Vector3(-3f,   0f,   0f);

    [Header("Legs")]
    public Vector3 leftUpperLegEuler  = new Vector3(55f,   4f, -6f);
    public Vector3 rightUpperLegEuler = new Vector3(55f,  -4f,  6f);
    public Vector3 leftLowerLegEuler  = new Vector3(-60f,   0f,   0f);
    public Vector3 rightLowerLegEuler = new Vector3(-60f,   0f,   0f);

    [Header("Arms")]
    public Vector3 leftUpperArmEuler  = new Vector3(0f,    0f, -20f);
    public Vector3 rightUpperArmEuler = new Vector3(0f,    0f,  20f);
    public Vector3 leftLowerArmEuler  = new Vector3(-20f,   0f,   0f);
    public Vector3 rightLowerArmEuler = new Vector3(-20f,   0f,   0f);

    // ── Internal ──────────────────────────────────────────────────────────────
    private Animator  _animator;
    private Transform _hips, _spine, _chest, _head;
    private Transform _leftUpperLeg,  _rightUpperLeg;
    private Transform _leftLowerLeg,  _rightLowerLeg;
    private Transform _leftUpperArm,  _rightUpperArm;
    private Transform _leftLowerArm,  _rightLowerArm;

    // Rest-pose local rotations (captured before first pose application)
    private Quaternion _restHips, _restSpine, _restChest, _restHead;
    private Quaternion _restLUL, _restRUL, _restLLL, _restRLL;
    private Quaternion _restLUA, _restRUA, _restLLA, _restRLA;

    private bool _isInitialised = false;
    private bool _bonesReady = false;
    private bool _animatorDisabled = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (applyOnEnable && !_isInitialised) Initialise();
    }

    /// <summary>Call this explicitly right after attaching to the seated clone and configuring properties.</summary>
    public void Initialise()
    {
        if (_isInitialised) return;
        _isInitialised = true;

        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            if (debugLogs) Debug.Log($"[SeatedPose] No Animator on {name} — skipping.");
            return;
        }

        if (!_animator.isHuman)
        {
            if (debugLogs) Debug.Log($"[SeatedPose] {name} is NOT Humanoid — skipping.");
            return;
        }

        // Cache bones
        _hips      = _animator.GetBoneTransform(HumanBodyBones.Hips);
        _spine     = _animator.GetBoneTransform(HumanBodyBones.Spine);
        _chest     = _animator.GetBoneTransform(HumanBodyBones.Chest);
        _head      = _animator.GetBoneTransform(HumanBodyBones.Head);

        _leftUpperLeg  = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        _rightUpperLeg = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        _leftLowerLeg  = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        _rightLowerLeg = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        _leftUpperArm  = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _leftLowerArm  = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _rightLowerArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

        bool ok = _hips != null && _spine != null && _leftUpperLeg != null && _rightUpperLeg != null;
        if (!ok)
        {
            if (debugLogs) Debug.LogWarning($"[SeatedPose] Some critical bones missing on {name}.");
            return;
        }

        // Capture rest-pose rotations BEFORE we first override
        // Rebind the animator to force the character back to default T-Pose.
        // This prevents capturing the bones while they are mid-animation (e.g. Waving).
        _animator.Rebind();
        _animator.Update(0f);

        CaptureRestPose();

        // Optionally pause Animator so it no longer advances (avoids Unity Editor Graph bug)
        if (disableAnimatorAfterPose)
        {
            _animator.speed = 0f;
            _animatorDisabled = true;
        }

        // Apply root offset
        _originalLocalPos = transform.localPosition;
        _originalLocalEuler = transform.localEulerAngles;
        _originalLocalScale = transform.localScale;

        _bonesReady = true;

        // Immediate first application
        ForceApplyPose();

        if (debugLogs) Debug.Log($"[SeatedPose] Initialised on {name}. animDisabled={_animatorDisabled}");
    }

    private Vector3 _originalLocalPos;
    private Vector3 _originalLocalEuler;
    private Vector3 _originalLocalScale;

    private void LateUpdate()
    {
        if (!keepApplyingInLateUpdate || !_bonesReady) return;
        ForceApplyPose();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Pose application
    // ─────────────────────────────────────────────────────────────────────────

    public void ForceApplyPose()
    {
        if (!_bonesReady) return;
        
        SetBone(_hips,          _restHips,  hipsEuler);
        SetBone(_spine,         _restSpine, spineEuler);
        SetBone(_chest,         _restChest, chestEuler);
        SetBone(_head,          _restHead,  headEuler);

        SetBone(_leftUpperLeg,  _restLUL, leftUpperLegEuler);
        SetBone(_rightUpperLeg, _restRUL, rightUpperLegEuler);
        SetBone(_leftLowerLeg,  _restLLL, leftLowerLegEuler);
        SetBone(_rightLowerLeg, _restRLL, rightLowerLegEuler);

        SetBone(_leftUpperArm,  _restLUA, leftUpperArmEuler);
        SetBone(_rightUpperArm, _restRUA, rightUpperArmEuler);
        SetBone(_leftLowerArm,  _restLLA, leftLowerArmEuler);
        SetBone(_rightLowerArm, _restRLA, rightLowerArmEuler);

        // Re-apply root offsets in case they changed
        transform.localPosition = _originalLocalPos + rootLocalPositionOffset;
        transform.localEulerAngles = _originalLocalEuler + rootLocalEulerOffset;
        transform.localScale = Vector3.Scale(_originalLocalScale, rootLocalScaleMultiplier);
    }

    private static void SetBone(Transform bone, Quaternion rest, Vector3 offsetEuler)
    {
        if (bone == null) return;
        bone.localRotation = rest * Quaternion.Euler(offsetEuler);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Rest-pose capture
    // ─────────────────────────────────────────────────────────────────────────

    private void CaptureRestPose()
    {
        _restHips  = SafeRot(_hips);
        _restSpine = SafeRot(_spine);
        _restChest = SafeRot(_chest);
        _restHead  = SafeRot(_head);

        _restLUL = SafeRot(_leftUpperLeg);
        _restRUL = SafeRot(_rightUpperLeg);
        _restLLL = SafeRot(_leftLowerLeg);
        _restRLL = SafeRot(_rightLowerLeg);

        _restLUA = SafeRot(_leftUpperArm);
        _restRUA = SafeRot(_rightUpperArm);
        _restLLA = SafeRot(_leftLowerArm);
        _restRLA = SafeRot(_rightLowerArm);
    }

    private static Quaternion SafeRot(Transform t) =>
        t != null ? t.localRotation : Quaternion.identity;
}
