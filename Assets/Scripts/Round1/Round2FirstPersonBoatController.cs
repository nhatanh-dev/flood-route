using UnityEngine;
using UnityEngine.InputSystem;

namespace Round2
{
    public enum BoatForwardAxis
    {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX
    }

    public class Round2FirstPersonBoatController : MonoBehaviour
    {
        [Header("References")]
        public Transform boatRoot;
        public Transform cameraRig;      // FP_CameraRig: yaw pivot
        public Transform pitchPivot;     // FP_PitchPivot: pitch pivot
        public Camera playerCamera;

        [Header("Boat Movement")]
        public float moveSpeed = 1.0f;
        public float reverseSpeed = 0.45f;
        public float turnSpeed = 50f;
        public float acceleration = 1.8f;
        public float waterY = 0.88f;

        [Tooltip("Natural slowdown when no input is pressed")]
        public float waterDrag = 1.2f;
        [Tooltip("Stronger deceleration when braking (pressing S while moving forward)")]
        public float brakeDeceleration = 2.2f;
        [Tooltip("Minimum turn factor when at zero speed")]
        public float minimumTurnFactor = 0.25f;
        [Tooltip("Maximum turn factor at full speed")]
        public float maxTurnFactor = 1.0f;

        [Header("Passenger Load Effect")]
        [Tooltip("Enable physics penalty when carrying passengers.")]
        public bool enablePassengerLoadEffect = true;
        [Tooltip("Max speed reduction when boat is fully loaded (0.2 = 20% slower).")]
        [Range(0f, 0.5f)] public float maxSpeedPenalty = 0.20f;
        [Tooltip("Max acceleration reduction when fully loaded (0.25 = 25% slower to accelerate).")]
        [Range(0f, 0.6f)] public float maxAccelPenalty = 0.25f;
        [Tooltip("Max turn speed reduction when fully loaded (0.25 = 25% harder to turn).")]
        [Range(0f, 0.5f)] public float maxTurnPenalty = 0.25f;
        [Tooltip("Water drag increase when fully loaded (0.15 = 15% more inertia).")]
        [Range(0f, 0.4f)] public float maxDragIncrease = 0.15f;

        [Header("Boat Direction Calibration")]
        public BoatForwardAxis boatNoseAxis = BoatForwardAxis.NegativeX;
        public bool invertForward = true;
        public bool invertTurn = false;

        [Header("Mouse Look")]
        public float mouseSensitivity = 0.06f;
        public float pitchMin = -25f;
        public float pitchMax = 25f;

        [Tooltip("False = chuột chỉ nhìn xung quanh, A/D lái thuyền.")]
        public bool mouseSteersBoat = false;

        [Header("Optional")]
        public bool lockCursorOnStart = true;
        public bool showDebugForward = true;

        [Header("Collision")]
        public Transform collisionProbe;
        public Vector3 collisionBoxHalfExtents = new Vector3(0.45f, 0.22f, 0.22f);
        public Vector3 collisionBoxCenterOffset = new Vector3(0f, -0.1f, 0f);
        public float collisionCheckDistancePadding = 0.05f;
        public LayerMask obstacleMask;
        public bool enableBoatCollision = true;
        public bool drawCollisionGizmos = true;

        [Header("New Collision Settings")]
        public bool enableRotationCollisionCheck = true;
        public bool enableDepenetration = true;
        public float collisionSkinWidth = 0.05f;
        public int maxDepenetrationIterations = 3;

        [Header("Durability / Damage")]
        public bool enableCollisionDamage = true;
        public float minDamageSpeed = 0.50f;  // ~50% of moveSpeed (1.0)
        public int collisionDamageAmount = 1;
        public float damageCooldown = 1.0f;
        public float damageSpeedReduction = 0.25f;
        
        private float lastDamageTime = -9999f;
        public global::Round2RealtimeRoundController roundController;
        public Round2CollisionWarningUI collisionWarningUI;

        private BoxCollider internalCollider;

        // ── Passenger load state ───────────────────────────────────────────────
        /// <summary>Current 0..1 load ratio based on cargo/capacity.</summary>
        public float currentLoadFactor { get; private set; }

        public float currentSpeed;
        public float CurrentSpeedAbs => Mathf.Abs(currentSpeed);
        private float pitch;
        private float cameraYaw;
        private float softImpactTimer;
        private Vector3 softImpactVelocity;

        private void Awake()
        {
            if (enableBoatCollision)
            {
                Transform probe = collisionProbe != null ? collisionProbe : (boatRoot != null ? boatRoot : transform);
                internalCollider = probe.gameObject.AddComponent<BoxCollider>();
                internalCollider.isTrigger = true;
                internalCollider.center = collisionBoxCenterOffset;
                internalCollider.size = collisionBoxHalfExtents * 2f;
            }
            if (boatRoot == null)
                boatRoot = transform;

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (cameraRig == null && playerCamera != null && playerCamera.transform.parent != null)
                cameraRig = playerCamera.transform.parent;

            if (pitchPivot == null && playerCamera != null && playerCamera.transform.parent != null)
                pitchPivot = playerCamera.transform.parent;

            // Initialize camera axes based on Editor setup to avoid looking sideways
            if (cameraRig != null)
            {
                cameraYaw = cameraRig.localEulerAngles.y + 90f; // Add 90 degrees to look straight relative to boat nose
                // Clear any weird roll/pitch on the rig, only keep yaw
                cameraRig.localEulerAngles = new Vector3(0f, cameraYaw, 0f);
            }

            if (pitchPivot != null)
            {
                float currentPitch = pitchPivot.localEulerAngles.x;
                if (currentPitch > 180f) currentPitch -= 360f;
                pitch = currentPitch;
                // Clear any weird roll/yaw on the pitch pivot, only keep pitch
                pitchPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (roundController == null)
            {
                roundController = FindObjectOfType<global::Round2RealtimeRoundController>();
            }
            if (collisionWarningUI == null)
            {
                collisionWarningUI = GetComponent<Round2CollisionWarningUI>();
            }
        }

        /// <summary>Returns effective stat scaled by the passenger load penalty.</summary>
        private float ApplyLoad(float baseStat, float maxPenalty)
        {
            if (!enablePassengerLoadEffect || roundController == null) return baseStat;
            return baseStat * Mathf.Lerp(1f, 1f - maxPenalty, currentLoadFactor);
        }

        private void Update()
        {
            if (boatRoot == null || cameraRig == null || pitchPivot == null)
            {
                Debug.LogWarning("Round2FirstPersonBoatController: Missing boatRoot, cameraRig, or pitchPivot reference.");
                return;
            }

            HandleCursor();
            HandleMouseLook();
            HandleBoatMovement();
        }

        private void HandleCursor()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleMouseLook()
        {
            if (roundController != null && !roundController.IsPlaying())
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            Mouse mouse = Mouse.current;

            if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 delta = mouse.delta.ReadValue();

            float yawDelta = delta.x * mouseSensitivity;
            float pitchDelta = delta.y * mouseSensitivity;

            if (mouseSteersBoat)
            {
                float turnSign = invertTurn ? -1f : 1f;
                TryRotate(yawDelta * turnSign);

                // Camera yaw riêng giữ 0 khi chuột dùng để lái thuyền.
                cameraYaw = 0f;
                cameraRig.localRotation = Quaternion.identity;
            }
            else
            {
                // Chuột trái/phải: xoay yaw pivot theo trục Y.
                cameraYaw += yawDelta;
                cameraRig.localRotation = Quaternion.Euler(0f, cameraYaw, 0f);
            }

            // Chuột lên/xuống: chỉ xoay pitch pivot theo trục X.
            pitch -= pitchDelta;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            // Quan trọng: Z luôn = 0 để không bị nghiêng trái/phải.
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleBoatMovement()
        {
            if (roundController != null && !roundController.IsPlaying())
            {
                currentSpeed = 0f;
                softImpactTimer = 0f;
                softImpactVelocity = Vector3.zero;
                if (collisionWarningUI != null)
                {
                    collisionWarningUI.ClearWarning();
                }
                return;
            }

            if (softImpactTimer > 0f)
            {
                softImpactTimer -= Time.deltaTime;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * Time.deltaTime);

                if (boatRoot != null)
                {
                    boatRoot.position += softImpactVelocity * Time.deltaTime;
                }

                if (softImpactTimer <= 0f)
                {
                    softImpactVelocity = Vector3.zero;
                }

                DoDepenetration();
                Vector3 impactPos = boatRoot.position;
                impactPos.y = waterY;
                boatRoot.position = impactPos;
                return;
            }

            Keyboard kb = Keyboard.current;

            if (kb == null)
                return;

            // ── Passenger load factor (0 = empty, 1 = full) ────────────────────
            if (enablePassengerLoadEffect && roundController != null)
            {
                int cap = Mathf.Max(1, roundController.boatCapacity);
                currentLoadFactor = Mathf.Clamp01((float)roundController.currentCargo / cap);
            }
            else
            {
                currentLoadFactor = 0f;
            }

            // Effective stats adjusted by passenger weight
            float effMoveSpeed    = ApplyLoad(moveSpeed,    maxSpeedPenalty);
            float effAcceleration = ApplyLoad(acceleration, maxAccelPenalty);
            float effTurnSpeed    = ApplyLoad(turnSpeed,    maxTurnPenalty);
            float effWaterDrag    = waterDrag * Mathf.Lerp(1f, 1f + maxDragIncrease, currentLoadFactor);

            float forwardInput = 0f;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                forwardInput += 1f;

            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                forwardInput -= 1f;

            float turnInput = 0f;

            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                turnInput -= 1f;

            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                turnInput += 1f;

            if (invertTurn)
                turnInput *= -1f;

            float accelRate = effWaterDrag;
            float targetSpeed = 0f;

            if (forwardInput > 0f)
            {
                targetSpeed = effMoveSpeed;
                accelRate = (currentSpeed < 0f) ? brakeDeceleration : effAcceleration;
            }
            else if (forwardInput < 0f)
            {
                targetSpeed = -reverseSpeed;
                accelRate = (currentSpeed > 0f) ? brakeDeceleration : effAcceleration;
            }

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                accelRate * Time.deltaTime
            );

            if (Mathf.Abs(turnInput) > 0.01f)
            {
                float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / Mathf.Max(0.01f, effMoveSpeed));
                float currentTurnFactor = Mathf.Lerp(minimumTurnFactor, maxTurnFactor, speedRatio);
                TryRotate(turnInput * effTurnSpeed * currentTurnFactor * Time.deltaTime);
            }

            Vector3 moveDirection = GetBoatForwardDirection();
            Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;

            if (Mathf.Abs(currentSpeed) > 0.01f)
            {
                if (enableBoatCollision)
                {
                    Transform probe = collisionProbe != null ? collisionProbe : boatRoot;
                    Vector3 center = probe.position + probe.TransformDirection(collisionBoxCenterOffset);
                    Quaternion orientation = probe.rotation;
                    
                    float distance = movement.magnitude;
                    Vector3 dir = movement.normalized;
                    
                    if (distance > 0.0001f)
                    {
                        bool hitObstacle = Physics.BoxCast(
                            center,
                            collisionBoxHalfExtents,
                            dir,
                            out RaycastHit hitInfo,
                            orientation,
                            distance + collisionCheckDistancePadding,
                            obstacleMask,
                            QueryTriggerInteraction.Ignore
                        );

                        Collider overlapCollider = null;
                        if (!hitObstacle)
                        {
                            Vector3 proposedCenter = center + movement;
                            Collider[] overlaps = Physics.OverlapBox(
                                proposedCenter,
                                collisionBoxHalfExtents,
                                orientation,
                                obstacleMask,
                                QueryTriggerInteraction.Ignore
                            );
                            for (int i = 0; i < overlaps.Length; i++)
                            {
                                if (overlaps[i] != internalCollider)
                                {
                                    overlapCollider = overlaps[i];
                                    break;
                                }
                            }
                        }

                        if (hitObstacle)
                        {
                            Debug.Log($"[Boat Collision Blocked] {hitInfo.collider.name} | layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}");
                            ProcessCollision(hitInfo.collider, hitInfo.point, hitInfo.normal);
                            currentSpeed *= 0.5f; // Dampen speed on collision
                        }
                        else if (overlapCollider != null)
                        {
                            Debug.Log($"[Boat Collision Blocked] Overlap with {overlapCollider.name} | layer: {LayerMask.LayerToName(overlapCollider.gameObject.layer)}");
                            Vector3 hitPoint = overlapCollider.ClosestPoint(center);
                            Vector3 normal = (center - hitPoint).normalized;
                            if (normal.sqrMagnitude < 0.001f)
                            {
                                normal = -dir;
                            }
                            ProcessCollision(overlapCollider, hitPoint, normal);
                            currentSpeed *= 0.5f; // Dampen speed on collision
                        }
                        else
                        {
                            boatRoot.position += movement;
                        }
                    }
                }
                else
                {
                    boatRoot.position += movement;
                }
            }

            DoDepenetration();

            Vector3 pos = boatRoot.position;
            pos.y = waterY;
            boatRoot.position = pos;

            if (showDebugForward)
            {
                Debug.DrawRay(
                    boatRoot.position + Vector3.up * 0.35f,
                    boatRoot.forward * 3f,
                    Color.red
                );

                Debug.DrawRay(
                    boatRoot.position + Vector3.up * 0.55f,
                    moveDirection * 3f,
                    Color.green
                );
            }
        }

        private void TryRotate(float angle)
        {
            if (Mathf.Abs(angle) < 0.0001f) return;

            if (enableRotationCollisionCheck && enableBoatCollision)
            {
                Transform probe = collisionProbe != null ? collisionProbe : boatRoot;
                
                Quaternion proposedRootRot = boatRoot.rotation * Quaternion.Euler(0f, angle, 0f);
                
                Vector3 localProbePos = boatRoot.InverseTransformPoint(probe.position);
                Vector3 proposedProbePos = boatRoot.position + proposedRootRot * localProbePos;
                
                Quaternion localProbeRot = Quaternion.Inverse(boatRoot.rotation) * probe.rotation;
                Quaternion proposedProbeRot = proposedRootRot * localProbeRot;
                
                Vector3 proposedCenter = proposedProbePos + proposedProbeRot * collisionBoxCenterOffset;

                Collider[] overlaps = Physics.OverlapBox(
                    proposedCenter,
                    collisionBoxHalfExtents,
                    proposedProbeRot,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                if (overlaps.Length > 0)
                {
                    foreach (var col in overlaps)
                    {
                        Vector3 hitPoint = col.ClosestPoint(proposedCenter);
                        Vector3 normal = (proposedCenter - hitPoint).normalized;
                        if (normal.sqrMagnitude < 0.001f) normal = Vector3.right;
                        ProcessCollision(col, hitPoint, normal);
                    }
                    return;
                }
            }

            boatRoot.Rotate(Vector3.up, angle, Space.World);
        }

        private void DoDepenetration()
        {
            if (!enableDepenetration || !enableBoatCollision || internalCollider == null) return;

            Transform probe = collisionProbe != null ? collisionProbe : boatRoot;
            
            internalCollider.center = collisionBoxCenterOffset;
            internalCollider.size = collisionBoxHalfExtents * 2f;

            for (int i = 0; i < maxDepenetrationIterations; i++)
            {
                Vector3 center = probe.position + probe.TransformDirection(collisionBoxCenterOffset);
                Quaternion orientation = probe.rotation;

                Collider[] overlaps = Physics.OverlapBox(
                    center,
                    collisionBoxHalfExtents,
                    orientation,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                if (overlaps.Length == 0) break;

                foreach (var col in overlaps)
                {
                    if (col == internalCollider) continue;
                    Vector3 hitPoint = col.ClosestPoint(probe.position);
                    Vector3 normal = (probe.position - hitPoint).normalized;
                    if (normal.sqrMagnitude < 0.001f) normal = Vector3.right;
                    ProcessCollision(col, hitPoint, normal);
                }

                bool resolvedAny = false;
                foreach (var col in overlaps)
                {
                    if (col == internalCollider) continue;

                    Vector3 direction;
                    float distance;

                    if (Physics.ComputePenetration(
                        internalCollider, probe.position, probe.rotation,
                        col, col.transform.position, col.transform.rotation,
                        out direction, out distance))
                    {
                        boatRoot.position += direction * (distance + collisionSkinWidth);
                        resolvedAny = true;
                    }
                }

                if (!resolvedAny) break;
            }
        }

        private Vector3 GetBoatForwardDirection()
        {
            Vector3 localForward = GetLocalForwardAxis();
            Vector3 worldForward = boatRoot.TransformDirection(localForward);

            worldForward.y = 0f;

            if (worldForward.sqrMagnitude < 0.001f)
                worldForward = boatRoot.forward;

            worldForward.Normalize();

            if (invertForward)
                worldForward *= -1f;

            return worldForward;
        }

        private Vector3 GetLocalForwardAxis()
        {
            switch (boatNoseAxis)
            {
                case BoatForwardAxis.PositiveZ:
                    return Vector3.forward;

                case BoatForwardAxis.NegativeZ:
                    return Vector3.back;

                case BoatForwardAxis.PositiveX:
                    return Vector3.right;

                case BoatForwardAxis.NegativeX:
                    return Vector3.left;

                default:
                    return Vector3.forward;
            }
        }

        private void OnValidate()
        {
            if (moveSpeed < 0f) moveSpeed = 0f;
            if (reverseSpeed < 0f) reverseSpeed = 0f;
            if (turnSpeed < 0f) turnSpeed = 0f;
            if (acceleration < 0.1f) acceleration = 0.1f;

            if (pitchMin > pitchMax)
            {
                float temp = pitchMin;
                pitchMin = pitchMax;
                pitchMax = temp;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCollisionGizmos) return;

            Transform probe = collisionProbe != null ? collisionProbe : (boatRoot != null ? boatRoot : transform);
            if (probe == null) return;

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            
            // Apply the local offset oriented to the probe
            Vector3 center = probe.position + probe.TransformDirection(collisionBoxCenterOffset);
            
            // Draw box using matrix to rotate it
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, probe.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, collisionBoxHalfExtents * 2f);
            Gizmos.matrix = oldMatrix;

            // Draw proposed movement direction
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Vector3 moveDir = GetBoatForwardDirection();
                Gizmos.DrawRay(center, moveDir * 2f);
            }
        }

        public void ApplySoftImpact(Vector3 impactDirection, float speedMultiplier, float lockDuration)
        {
            if (boatRoot == null)
            {
                return;
            }

            impactDirection.y = 0f;
            if (impactDirection.sqrMagnitude < 0.0001f)
            {
                impactDirection = -GetBoatForwardDirection();
            }

            currentSpeed *= Mathf.Clamp01(speedMultiplier);
            softImpactTimer = Mathf.Max(0f, lockDuration);
            softImpactVelocity = impactDirection.normalized * 0.55f;
        }
        
        private void ProcessCollision(Collider col, Vector3 hitPoint, Vector3 normal)
        {
            if (col == internalCollider) return;

            if (!enableCollisionDamage) return;
            if (roundController != null && roundController.currentState != global::Round2GameState.Playing) return;

            string colName = col.gameObject.name.ToLower();
            string parentName = col.transform.parent != null ? col.transform.parent.name.ToLower() : "";

            bool isBoundary = colName.Contains("boundary") || parentName.Contains("boundary") ||
                              colName.Contains("bnd_") || parentName.Contains("bnd_") ||
                              colName.Contains("realtimeboundaries") || parentName.Contains("realtimeboundaries") ||
                              colName.Contains("invisible") || parentName.Contains("invisible") ||
                              colName.Contains("mapboundary") || parentName.Contains("mapboundary") ||
                              colName.Contains("blocker") || parentName.Contains("blocker") ||
                              colName.Contains("wall") || parentName.Contains("wall") ||
                              colName.Contains("trigger") || parentName.Contains("trigger") ||
                              col.isTrigger;

            if (isBoundary)
            {
                if (collisionWarningUI == null)
                {
                    collisionWarningUI = GetComponent<Round2CollisionWarningUI>();
                }

                if (collisionWarningUI != null)
                {
                    collisionWarningUI.TriggerBoundaryWarning(hitPoint, normal);
                }
                return;
            }

            if (collisionWarningUI == null)
            {
                collisionWarningUI = GetComponent<Round2CollisionWarningUI>();
            }

            if (Mathf.Abs(currentSpeed) < minDamageSpeed)
            {
                if (collisionWarningUI != null)
                {
                    collisionWarningUI.TriggerLightContactWarning();
                }
                return;
            }

            if (Time.time - lastDamageTime < damageCooldown) return;

            lastDamageTime = Time.time;
            currentSpeed *= damageSpeedReduction;

            if (roundController != null)
            {
                int durabilityBefore = roundController.currentBoatDurability;
                roundController.ApplyDamage(collisionDamageAmount);
                bool durabilityLost = roundController.currentBoatDurability < durabilityBefore;
                if (durabilityLost && collisionWarningUI != null)
                {
                    collisionWarningUI.TriggerDamageWarning(roundController.currentBoatDurability <= 0);
                }

                if (roundController.currentState == global::Round2GameState.Playing)
                {
                    roundController.ShowFeedback("Va chạm mạnh! Độ bền -1.");
                }
            }
        }
    }
}
