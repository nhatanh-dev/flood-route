using UnityEngine;
using UnityEngine.InputSystem;

namespace Round1
{
    public enum BoatForwardAxis
    {
        PositiveZ,
        NegativeZ,
        PositiveX,
        NegativeX
    }

    public class Round1FirstPersonBoatController : MonoBehaviour
    {
        [Header("References")]
        public Transform boatRoot;
        public Transform cameraRig;      // FP_CameraRig: yaw pivot
        public Transform pitchPivot;     // FP_PitchPivot: pitch pivot
        public Camera playerCamera;

        [Header("Boat Movement")]
        public float moveSpeed = 5f;
        public float reverseSpeed = 2.5f;
        public float turnSpeed = 90f;
        public float acceleration = 8f;
        public float waterY = 0.88f;

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

        private BoxCollider internalCollider;

        public float currentSpeed;
        public float CurrentSpeedAbs => Mathf.Abs(currentSpeed);
        public Vector3 CurrentBoatForwardWorld => GetBoatForwardDirection();
        private float pitch;
        private float cameraYaw;

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
        }

        private void Update()
        {
            if (boatRoot == null || cameraRig == null || pitchPivot == null)
            {
                Debug.LogWarning("Round1FirstPersonBoatController: Missing boatRoot, cameraRig, or pitchPivot reference.");
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
            Keyboard kb = Keyboard.current;

            if (kb == null)
                return;

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

            if (Mathf.Abs(turnInput) > 0.01f)
            {
                TryRotate(turnInput * turnSpeed * Time.deltaTime);
            }

            float targetSpeed = 0f;

            if (forwardInput > 0f)
                targetSpeed = moveSpeed;
            else if (forwardInput < 0f)
                targetSpeed = -reverseSpeed;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                acceleration * Time.deltaTime
            );

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

                        bool overlapObstacle = false;
                        if (!hitObstacle)
                        {
                            Vector3 proposedCenter = center + movement;
                            overlapObstacle = Physics.CheckBox(
                                proposedCenter,
                                collisionBoxHalfExtents,
                                orientation,
                                obstacleMask,
                                QueryTriggerInteraction.Ignore
                            );
                        }

                        if (hitObstacle)
                        {
                            Debug.Log($"[Boat Collision Blocked] {hitInfo.collider.name} | layer: {LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}");
                            ProcessCollision(hitInfo.collider, hitInfo.point, hitInfo.normal);
                        }
                        else if (overlapObstacle)
                        {
                            Debug.Log("[Boat Collision Blocked] CheckBox overlap detected at proposed position.");
                            // We can't get exactly which collider blocked CheckBox easily here, so we skip damage for CheckBox, 
                            // it will be caught by DoDepenetration anyway.
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
                    var warningUI = GetComponent<Round1BoundaryWarningUI>();
                    if (warningUI != null)
                    {
                        foreach (var col in overlaps)
                        {
                            Vector3 hitPoint = col.ClosestPoint(proposedCenter);
                            Vector3 normal = (proposedCenter - hitPoint).normalized;
                            if (normal.sqrMagnitude < 0.001f) normal = Vector3.right;
                            ProcessCollision(col, hitPoint, normal);
                        }
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
        // ─────────────────────────────────────────────────────────────────────
        //  Collision Handling Helper
        // ─────────────────────────────────────────────────────────────────────
        private void ProcessCollision(Collider col, Vector3 hitPoint, Vector3 normal)
        {
            if (col == internalCollider) return;

            string colName = col.name;
            string parentName = col.transform.parent != null ? col.transform.parent.name : "";

            bool isBoundary = colName.Contains("Boundary") || parentName.Contains("Boundary") ||
                              colName.Contains("InvisibleRouteBoundaries") || parentName.Contains("InvisibleRouteBoundaries") ||
                              colName.Contains("MapBoundary") || parentName.Contains("MapBoundary") ||
                              colName.Contains("RouteBoundary") || parentName.Contains("RouteBoundary") ||
                              colName.Contains("Marker") || parentName.Contains("Marker") ||
                              colName.Contains("Halo") || parentName.Contains("Halo") ||
                              colName.Contains("Trigger") || parentName.Contains("Trigger");

            var warningUI = GetComponent<Round1BoundaryWarningUI>();
            if (isBoundary)
            {
                if (warningUI != null)
                {
                    warningUI.TriggerBoundaryWarning(hitPoint, normal);
                }
                return;
            }

            if (!isBoundary)
            {
                var realtime = FindAnyObjectByType<R1RealtimeRoundController>();
                if (realtime != null && realtime.enableCollisionDamage)
                {
                    bool fastEnough = Mathf.Abs(currentSpeed) >= realtime.minDamageSpeed;
                    bool cooldownReady = Time.time - realtime.LastDamageTime >= realtime.damageCooldown;
                    bool causesDamage = fastEnough && cooldownReady;

                    if (causesDamage)
                    {
                        realtime.ApplyBoatDamage(realtime.collisionDamage, colName);
                        if (warningUI != null)
                        {
                            warningUI.TriggerDamageWarning(realtime.currentBoatDurability <= 0);
                        }
                        // Optional: Reduce speed slightly on hard hit
                        currentSpeed *= 0.3f; 
                    }
                    else if (warningUI != null)
                    {
                        warningUI.TriggerLightContactWarning();
                    }
                }
            }
        }
    }
}
