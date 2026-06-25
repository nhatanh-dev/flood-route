using UnityEngine;
using TMPro;
using System.Collections;

namespace Round1
{
    /// <summary>
    /// Handles:
    ///  - World-space target marker (visible only when en route)
    ///  - Per-frame arrival detection with arrivalRadius
    ///  - HUD turn / instruction text updates
    ///  - Q-key wait action
    /// Does NOT touch Round1FirstPersonBoatController.
    /// </summary>
    public class R1RouteTargetController : MonoBehaviour
    {
        // ── Public references (wired in Editor or auto-found) ─────────────
        public R1RouteGraph         routeGraph;
        public Transform            boatRoot;
        [Header("Tuning")]
        [Tooltip("How close the boat must be to the marker to trigger arrival")]
        public float                arrivalRadius  = 1.0f;
        public float                nodeActionDelay = 1.5f;

        [Header("UI")]
        public TextMeshProUGUI      instructionText;
        public TextMeshProUGUI      turnText;

        [Header("Target Marker (auto-created if null)")]
        public GameObject           markerInstance;

        // ── Private ───────────────────────────────────────────────────────
        private float               logTimer;
        private const float         LOG_INTERVAL   = 1f;   // log distance every 1s max

        // ── Unity ─────────────────────────────────────────────────────────
        private void Start()
        {
            // Auto-find missing references
            if (routeGraph == null)
                routeGraph = FindAnyObjectByType<R1RouteGraph>();

            if (boatRoot == null)
            {
                var go = GameObject.Find("Player_Boat_Root");
                if (go != null) boatRoot = go.transform;
            }

            if (instructionText == null)
            {
                var go = GameObject.Find("TXT_R1_Shared_Message");
                if (go != null) instructionText = go.GetComponent<TextMeshProUGUI>();
            }

            if (turnText == null)
            {
                var go = GameObject.Find("TXT_R1_Shared_Turn");
                if (go != null) turnText = go.GetComponent<TextMeshProUGUI>();
            }

            // Create or configure world-space marker
            EnsureMarkerExists();

            // Initial HUD sync
            UpdateTurnHUD();
        }

        private void Update()
        {
            if (routeGraph == null || boatRoot == null) return;

            HandleEnRouteMarkerAndArrival();
            HandleQWait();
        }

        // ── En-route: marker + arrival ────────────────────────────────────
        private void HandleEnRouteMarkerAndArrival()
        {
            var target = routeGraph.selectedTargetNode;

            if (target == null || target.worldAnchor == null)
            {
                if (markerInstance != null && markerInstance.activeSelf)
                    markerInstance.SetActive(false);
                return;
            }

            // Show and position marker
            if (!markerInstance.activeSelf)
                markerInstance.SetActive(true);

            // Determine current step anchor
            Transform currentStepAnchor = target.worldAnchor;
            bool isFinalStep = true;

            if (target.pathWaypoints != null && routeGraph.currentRouteStepIndex < target.pathWaypoints.Count)
            {
                var wp = target.pathWaypoints[routeGraph.currentRouteStepIndex];
                if (wp != null)
                {
                    currentStepAnchor = wp;
                    isFinalStep = false;
                }
            }

            // Place exactly at the node's position (which is usually floating just above the water)
            markerInstance.transform.position = currentStepAnchor.position;

            // Distance check
            float dist = Vector3.Distance(boatRoot.position, currentStepAnchor.position);

            // Throttled log to avoid spam
            logTimer += Time.deltaTime;
            if (logTimer >= LOG_INTERVAL)
            {
                logTimer = 0f;
                Debug.Log($"[R1Target] Distance to {(isFinalStep ? "final target" : "waypoint " + routeGraph.currentRouteStepIndex)}: {dist:F2}");
            }

            if (dist <= arrivalRadius)
            {
                if (isFinalStep)
                {
                    ArriveAtTarget(target);
                }
                else
                {
                    routeGraph.currentRouteStepIndex++;
                    Debug.Log($"[R1Target] Reached waypoint, moving to step {routeGraph.currentRouteStepIndex}");
                }
            }
        }

        // ── Arrival ───────────────────────────────────────────────────────
        private void ArriveAtTarget(R1RouteNode target)
        {
            Debug.Log($"[R1Target] Arrived at: {target.displayName}");

            // Update graph state (use the proper method so it's atomic)
            routeGraph.ArriveAtSelectedTarget();
            // ArriveAtSelectedTarget sets currentNode = selectedTargetNode then clears it.
            // If currentNode didn't change (e.g. not adjacent), force it:
            if (routeGraph.currentNode != target)
                routeGraph.currentNode = target;

            Debug.Log($"[R1Target] Current node is now: {routeGraph.currentNode.displayName}");

            // Hide marker
            if (markerInstance != null)
                markerInstance.SetActive(false);

            // Arrival message
            ShowInstruction(BuildArrivalMessage(target));

            // Sync HUD
            UpdateTurnHUD();
        }

        private static string BuildArrivalMessage(R1RouteNode target)
        {
            switch (target.nodeType)
            {
                case R1NodeType.Rescue:
                    return target.isCollected
                        ? $"Đã tới {target.displayName}. Nhấn Tab để chọn hướng đi tiếp theo."
                        : $"Đã tới {target.displayName}. Nhấn E để cứu {target.peopleCount} người.";
                case R1NodeType.Shelter:
                    return "Đã tới điểm trú. Nhấn E để đưa người dân vào nơi an toàn.";
                case R1NodeType.Junction:
                    return $"Đã tới {target.displayName}. Nhấn Tab để chọn hướng đi tiếp theo.";
                case R1NodeType.Start:
                    return "Đã tới bến thuyền. Nhấn Tab để chọn hướng đi tiếp theo.";
                default:
                    return $"Đã tới {target.displayName}. Nhấn Tab để chọn hướng đi tiếp theo.";
            }
        }

        // ── Q wait ────────────────────────────────────────────────────────
        private void HandleQWait()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !kb.qKey.wasPressedThisFrame) return;

            if (routeGraph.remainingTurns > 0)
            {
                routeGraph.remainingTurns--;
                UpdateTurnHUD();
                ShowInstruction("Đã chờ 1 lượt.");
            }
        }

        // ── Public API ────────────────────────────────────────────────────
        public void ShowInstruction(string text)
        {
            if (instructionText != null)
                instructionText.text = text;
        }

        public void UpdateTurnHUD()
        {
            if (turnText != null && routeGraph != null)
                turnText.text = $"Lượt còn lại: {routeGraph.remainingTurns}";
        }

        /// Called by R1PlanningMapController after route selected
        public void OnTargetSelected(R1RouteNode target)
        {
            Debug.Log($"[R1Target] Selected target: {target.displayName}");
            logTimer = LOG_INTERVAL; // Force first distance log next Update
            UpdateTurnHUD();

            // Show instruction immediately
            ShowInstruction($"Đi tới {target.displayName}.\nWASD: Lái thuyền | Tab: Bản đồ | E: Cứu/Thả | Q: Chờ");
        }

        // ── Marker ────────────────────────────────────────────────────────
        private void EnsureMarkerExists()
        {
            if (markerInstance != null)
            {
                markerInstance.SetActive(false);
                return;
            }

            // ── Root container ──
            markerInstance = new GameObject("R1_TargetHaloMarker");
            markerInstance.transform.SetParent(null); // World root

            // Instead of dealing with missing textures, we can use a Point Light 
            // placed right at the water surface to create a beautiful, dynamic,
            // soft glowing aura on the water and surrounding objects.
            var lightGo = new GameObject("HaloLight");
            lightGo.transform.SetParent(markerInstance.transform);
            lightGo.transform.localPosition = new Vector3(0f, 0.5f, 0f); // Slightly above water

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.9f, 0.2f); // Warm golden/yellow
            light.intensity = 5f; // Strong enough to glow
            light.range = 8f;     // Soft falloff radius
            // In URP, we might need to set the light layer or render mode, but default point light works well

            // Add a small solid core so the center isn't completely empty
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "HaloCore";
            core.transform.SetParent(markerInstance.transform);
            core.transform.localPosition = new Vector3(0f, -0.3f, 0f); // Slightly below the node center
            core.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f); // Flattened wide disk
            Destroy(core.GetComponent<Collider>());
            
            // Sprites/Default handles transparency out-of-the-box in URP without needing keyword modifications
            var coreMat = new Material(Shader.Find("Sprites/Default"));
            coreMat.color = new Color(1f, 0.9f, 0.2f, 0.4f); // Semi-transparent yellow aura
            core.GetComponent<Renderer>().material = coreMat;

            // Start hidden
            markerInstance.SetActive(false);
        }
    }
}
