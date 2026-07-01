using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Round1
{
    /// <summary>
    /// Manages the Tab Planning Mode: switches to a top-down overview camera,
    /// shows a full-screen map UI with route options, node visuals, and state info.
    /// Does NOT touch Round1FirstPersonBoatController.
    /// </summary>
    public class R1PlanningMapController : MonoBehaviour
    {
        // ── References set by Editor or auto-found ─────────────────────────
        public R1RouteGraph routeGraph;
        public bool enableLegacyRouteSelection = false;
        // planningPanel and planningCamera are private (runtime-built) to avoid Unity serializing stale refs
        private GameObject _planningPanel;
        private Camera     _planningCamera;
        public  GameObject planningPanel  { get => _planningPanel;  set => _planningPanel  = value; }
        public  Camera     planningCamera { get => _planningCamera; set => _planningCamera = value; }

        public TextMeshProUGUI mapText;
        public TextMeshProUGUI instructionText;

        [Header("Top-Down Map Camera")]
        public Camera fpCamera;                // Iso Camera (first-person)

        [Header("FP Controller References")]
        public MonoBehaviour fpBoatController; // Round1FirstPersonBoatController – enabled/disabled on toggle
        public MonoBehaviour fpInteraction;    // Round1FirstPersonInteraction
        public R1OrientationMapView orientationMapView;

        [Header("Map Readability")]
        [Range(0f, 1f)] public float mapRainOverlayAlpha = 0.035f;

        [Header("Planning UI – Generated")]
        // Left info panel texts
        private TextMeshProUGUI txtCurrentNode;
        private TextMeshProUGUI txtTurns;
        private TextMeshProUGUI txtCargo;
        private TextMeshProUGUI txtSaved;

        // Route options
        private TextMeshProUGUI txtRouteOptions;
        private TextMeshProUGUI txtFooter;

        [Header("World Node Dot Markers")]
        private GameObject nodeVisualsRoot;
        private readonly List<NodeMarker> nodeMarkers = new List<NodeMarker>();

        // ── State ──────────────────────────────────────────────────────────
        private List<R1RouteNode> currentOptions = new List<R1RouteNode>();
        private bool isPanelOpen = false;

        // ── RescueController for cargo/saved ───────────────────────────────
        private Round1RescueController rescueController;

        // ── Global map-only visuals ──────────────────────────────────────
        private GameObject globalRouteVisuals;

        // ── Gameplay HUD Elements ────────────────────────────────────────
        private List<GameObject> gameplayHudElements = new List<GameObject>();
        private readonly List<R1ScreenRainOverlayController> rainOverlays = new List<R1ScreenRainOverlayController>();
        private readonly Dictionary<R1ScreenRainOverlayController, float> rainOverlayOriginalAlphas = new Dictionary<R1ScreenRainOverlayController, float>();

        // ─────────────────────────────────────────────────────────────────
        private void Awake()
        {
        }

        private void Start()
        {
            // Auto-find references
            if (routeGraph == null) routeGraph = FindAnyObjectByType<R1RouteGraph>();
            if (routeGraph != null && routeGraph.currentNode == null)
                routeGraph.currentNode = routeGraph.startNode;

            rescueController = FindAnyObjectByType<Round1RescueController>(FindObjectsInactive.Include);

            // Auto-find FP camera
            if (fpCamera == null)
            {
                var isoGo = GameObject.Find("Iso Camera");
                if (isoGo != null) fpCamera = isoGo.GetComponent<Camera>();
            }

            // Auto-find FP controllers
            if (fpBoatController == null)
                fpBoatController = FindAnyObjectByType<Round1FirstPersonBoatController>(FindObjectsInactive.Include);
            if (fpInteraction == null)
                fpInteraction = FindAnyObjectByType<Round1FirstPersonInteraction>(FindObjectsInactive.Include);

            // Auto-find existing planning camera
            if (planningCamera == null)
            {
                var existingCamGo = GameObject.Find("R1_PlanningMapCamera");
                if (existingCamGo != null) planningCamera = existingCamGo.GetComponent<Camera>();
            }
            if (planningCamera == null)
                planningCamera = CreatePlanningCamera();
            ConfigurePlanningCamera();

            CollectGameplayHudElements();
            CollectRainOverlays();

            // Global route visuals
            var globalVisuals = GameObject.Find("R1_PlanningVisuals");
            if (globalVisuals != null)
                globalRouteVisuals = globalVisuals;

            if (!enableLegacyRouteSelection)
            {
                EnsureOrientationMapView();
                planningPanel = orientationMapView != null ? orientationMapView.CanvasRoot : null;
            }
            else
            {
                // Generate legacy route planning UI
                var existingUiGo = GameObject.Find("R1_PlanningUI_Group");
                if (existingUiGo != null) Destroy(existingUiGo);
                planningPanel = BuildPlanningUI();
                BuildNodeMarkers();
            }

            // Hide UI initially
            SetPlanningUiVisible(false);
            if (enableLegacyRouteSelection) SetNodeMarkersVisible(false);
            else if (orientationMapView != null) orientationMapView.SetVisible(false);
            if (globalRouteVisuals != null) globalRouteVisuals.SetActive(false);

            UpdateInstructionText();
        }

        // ─────────────────────────────────────────────────────────────────
        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            var realtime = FindAnyObjectByType<R1RealtimeRoundController>();
            if (realtime != null && realtime.IsGameOver) return;

            if (kb.tabKey.wasPressedThisFrame || (isPanelOpen && kb.escapeKey.wasPressedThisFrame))
                TogglePanel();

            if (isPanelOpen)
            {
                if (enableLegacyRouteSelection)
                {
                    HandleNumberInput();
                    UpdateOrientationPlayer();
                }
                else if (orientationMapView != null)
                {
                    orientationMapView.Refresh();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        private void TogglePanel()
        {
            if (routeGraph == null || planningPanel == null)
            {
                Debug.LogWarning("[R1Planning] Cannot toggle – missing references.");
                return;
            }

            isPanelOpen = !isPanelOpen;

            if (isPanelOpen)
            {
                OpenPlanningMode();
            }
            else
            {
                ClosePlanningMode();
            }
        }

        private void CollectGameplayHudElements()
        {
            gameplayHudElements.Clear();
            var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null || t.gameObject == gameObject)
                {
                    continue;
                }

                string objectName = t.name;
                if (objectName.StartsWith("R1_Planning") ||
                    objectName.StartsWith("R1_Orientation") ||
                    objectName == "R1_PlanningMapCamera")
                {
                    continue;
                }

                bool isGameplayHud =
                    objectName.StartsWith("HUD_Group_Gameplay") ||
                    objectName == "TXT_R1_Shared_Objective" ||
                    objectName == "TXT_R1_Shared_Message" ||
                    objectName.Contains("GameplayHUD") ||
                    objectName.Contains("Gameplay_HUD") ||
                    objectName.Contains("HUD_Gameplay") ||
                    objectName.Contains("HUD_Group") ||
                    objectName.Contains("ObjectivePanel") ||
                    objectName.Contains("Objective_Text") ||
                    objectName.Contains("TXT_Objective") ||
                    objectName.Contains("TXT_Timer") ||
                    objectName.Contains("TXT_Durability") ||
                    objectName.Contains("TXT_Cargo") ||
                    objectName.Contains("TXT_Saved");

                if (isGameplayHud && !gameplayHudElements.Contains(t.gameObject))
                {
                    gameplayHudElements.Add(t.gameObject);
                }
            }
        }

        private void CollectRainOverlays()
        {
            rainOverlays.Clear();
            rainOverlayOriginalAlphas.Clear();
            var overlays = FindObjectsByType<R1ScreenRainOverlayController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var overlay in overlays)
            {
                if (overlay == null)
                {
                    continue;
                }

                rainOverlays.Add(overlay);
                rainOverlayOriginalAlphas[overlay] = overlay.overlayAlpha;
            }
        }

        private void SetGameplayHudVisible(bool visible)
        {
            foreach (var go in gameplayHudElements)
            {
                if (go != null) go.SetActive(visible);
            }
        }

        private void SetPlanningUiVisible(bool visible)
        {
            if (planningPanel != null) planningPanel.SetActive(visible);
        }

        private void OpenPlanningMode()
        {
            // Switch cameras
            if (fpCamera != null) fpCamera.enabled = false;
            if (planningCamera != null)
            {
                planningCamera.gameObject.SetActive(true);
                planningCamera.enabled = true;
            }

            // Disable FP input
            if (fpBoatController != null) fpBoatController.enabled = false;
            if (fpInteraction != null) fpInteraction.enabled = false;

            // Show UI
            SetGameplayHudVisible(false);
            SetMapRainReadability(true);
            if (enableLegacyRouteSelection)
            {
                SetPlanningUiVisible(true);
                RefreshPlanningUI();

                // Show node markers
                SetNodeMarkersVisible(true);
                RefreshNodeMarkers();

                if (globalRouteVisuals != null) globalRouteVisuals.SetActive(true);
            }
            else if (orientationMapView != null)
            {
                orientationMapView.SetVisible(true);
            }
        }

        private void ClosePlanningMode()
        {
            // Restore FP camera
            if (planningCamera != null)
            {
                planningCamera.enabled = false;
                planningCamera.gameObject.SetActive(false);
            }
            if (fpCamera != null) fpCamera.enabled = true;

            // Re-enable FP input
            if (fpBoatController != null) fpBoatController.enabled = true;
            if (fpInteraction != null) fpInteraction.enabled = true;

            // Hide UI
            SetGameplayHudVisible(true);
            SetMapRainReadability(false);
            if (enableLegacyRouteSelection)
            {
                SetPlanningUiVisible(false);

                // Hide node markers (keep target marker in R1RouteTargetController)
                SetNodeMarkersVisible(false);

                if (globalRouteVisuals != null) globalRouteVisuals.SetActive(false);
            }
            else if (orientationMapView != null)
            {
                orientationMapView.SetVisible(false);
            }
        }

        private void EnsureOrientationMapView()
        {
            if (orientationMapView == null)
            {
                orientationMapView = GetComponent<R1OrientationMapView>();
                if (orientationMapView == null)
                    orientationMapView = gameObject.AddComponent<R1OrientationMapView>();
            }

            orientationMapView.Initialize(planningCamera, fpBoatController as Round1FirstPersonBoatController);
        }

        private void ConfigurePlanningCamera()
        {
            if (planningCamera == null) return;

            planningCamera.gameObject.name = "R1_PlanningMapCamera";
            planningCamera.transform.position = new Vector3(2.6f, 22f, -0.2f);
            planningCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            planningCamera.orthographic = true;
            planningCamera.orthographicSize = 9.35f;
            planningCamera.nearClipPlane = 0.1f;
            planningCamera.farClipPlane = 60f;
            planningCamera.depth = 200;
            planningCamera.clearFlags = CameraClearFlags.SolidColor;
            planningCamera.backgroundColor = new Color(0.05f, 0.08f, 0.10f, 1f);
            planningCamera.enabled = false;
            planningCamera.gameObject.SetActive(false);
        }

        private void SetMapRainReadability(bool mapOpen)
        {
            foreach (var overlay in rainOverlays)
            {
                if (overlay == null)
                {
                    continue;
                }

                if (mapOpen)
                {
                    if (!rainOverlayOriginalAlphas.ContainsKey(overlay))
                    {
                        rainOverlayOriginalAlphas[overlay] = overlay.overlayAlpha;
                    }

                    overlay.overlayAlpha = Mathf.Min(mapRainOverlayAlpha, overlay.overlayAlpha);
                }
                else if (rainOverlayOriginalAlphas.TryGetValue(overlay, out float originalAlpha))
                {
                    overlay.overlayAlpha = originalAlpha;
                }

                overlay.ApplySettings();
            }
        }

        // ── Property Helpers ─────────────────────────────────────────────
        public bool IsEnRoute => routeGraph != null && routeGraph.selectedTargetNode != null;

        // ─────────────────────────────────────────────────────────────────
        //  Refresh Planning UI content
        // ─────────────────────────────────────────────────────────────────
        private void RefreshPlanningUI()
        {
            var current = routeGraph.currentNode;
            if (current == null) return;

            // ── En-route lock ─────────────────────────────────────────────
            if (IsEnRoute)
            {
                var target = routeGraph.selectedTargetNode;
                if (txtCurrentNode != null) txtCurrentNode.text = $"Vị trí hiện tại: <b>{current.displayName}</b>";
                if (txtTurns != null)       txtTurns.text = $"Lượt còn lại: <b>{routeGraph.remainingTurns}</b>";

                int cargo_e = rescueController != null ? rescueController.Cargo : 0;
                int saved_e = rescueController != null ? rescueController.Saved : 0;
                int total_e = rescueController != null ? rescueController.TotalCivilians : 3;
                int cap_e   = rescueController != null ? rescueController.CargoCapacity : 3;
                if (txtCargo != null) txtCargo.text = $"Trên thuyền: {cargo_e} / {cap_e}";
                if (txtSaved != null) txtSaved.text = $"Đã cứu: {saved_e} / {total_e}";

                if (txtRouteOptions != null)
                    txtRouteOptions.text = $"<color=#FFB300>Đang đi tới {target.displayName}.</color>\n\n<color=#AAAAAA>Hãy tới điểm đã chọn\ntrước khi chọn tuyến mới.</color>";
                return;
            }
            // ─────────────────────────────────────────────────────────────

            currentOptions = routeGraph.GetAdjacentNodes(current);

            // Left info panel
            if (txtCurrentNode != null) txtCurrentNode.text = $"Vị trí hiện tại: {current.displayName}";
            if (txtTurns != null) txtTurns.text = $"Lượt còn lại: {routeGraph.remainingTurns}";

            int cargo = rescueController != null ? rescueController.Cargo : 0;
            int saved = rescueController != null ? rescueController.Saved : 0;
            int total = rescueController != null ? rescueController.TotalCivilians : 3;
            int cap   = rescueController != null ? rescueController.CargoCapacity : 3;

            if (txtCargo != null) txtCargo.text = $"Trên thuyền: {cargo} / {cap}";
            if (txtSaved != null) txtSaved.text = $"Đã cứu: {saved} / {total}";

            // Route options panel
            string options = "Chọn tuyến tiếp theo:\n\n";
            if (currentOptions == null || currentOptions.Count == 0)
            {
                options += "<color=#FF6666>Không có đường đi kề bên.</color>";
            }
            else
            {
                for (int i = 0; i < currentOptions.Count; i++)
                {
                    var node = currentOptions[i];
                    if (node == null) continue;
                    string color  = GetNodeColor(node);
                    string optionText = $"[{i + 1}] <color={color}>{node.displayName}</color>";

                    if (node.nodeType == R1NodeType.Rescue)
                    {
                        string status = node.isCollected ? "<color=#AAAAAA>Đã đón</color>" : $"<color=#FF8800>Cứu {node.peopleCount} người</color>";
                        optionText += $" - {status}";
                    }

                    options += optionText + "\n";
                }
            }
            if (txtRouteOptions != null) txtRouteOptions.text = options;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Node status helpers
        // ─────────────────────────────────────────────────────────────────
        private string GetNodeStatusString(R1RouteNode node)
        {
            switch (node.nodeType)
            {
                case R1NodeType.Rescue:
                    return node.isCollected ? "<color=#AAAAAA>Đã đón</color>" : $"<color=#FF8800>Cứu {node.peopleCount} người</color>";
                case R1NodeType.Shelter:
                    return "<color=#44FF88>Điểm trú an toàn</color>";
                case R1NodeType.Junction:
                    return "<color=#44CCFF>Ngã rẽ</color>";
                case R1NodeType.Start:
                    return "<color=#FFFF44>Bến thuyền</color>";
                default:
                    return "Không rõ";
            }
        }

        private string GetNodeColor(R1RouteNode node)
        {
            switch (node.nodeType)
            {
                case R1NodeType.Rescue:  return node.isCollected ? "#888888" : "#FF8800";
                case R1NodeType.Shelter: return "#44FF88";
                case R1NodeType.Junction: return "#44CCFF";
                default: return "#FFFF44";
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Number key input
        // ─────────────────────────────────────────────────────────────────
        private void HandleNumberInput()
        {
            if (!enableLegacyRouteSelection) return;
            if (IsEnRoute) return; // Cannot pick a new target while en route
            if (currentOptions == null || currentOptions.Count == 0) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            for (int i = 0; i < currentOptions.Count; i++)
            {
                bool pressed = false;
                switch (i)
                {
                    case 0: pressed = kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame; break;
                    case 1: pressed = kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame; break;
                    case 2: pressed = kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame; break;
                    case 3: pressed = kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame; break;
                    case 4: pressed = kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame; break;
                }

                if (pressed)
                {
                    SelectTarget(currentOptions[i]);
                    break;
                }
            }
        }

        private void SelectTarget(R1RouteNode targetNode)
        {
            Debug.Log($"[R1Planning] Selected option: {targetNode.displayName}");
            if (routeGraph.SetSelectedTarget(targetNode))
            {
                TogglePanel(); // Close & restore FP view

                var targetCtrl = FindAnyObjectByType<R1RouteTargetController>();
                if (targetCtrl != null)
                    targetCtrl.OnTargetSelected(targetNode);
                else
                    UpdateInstructionText();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Instruction text (bottom bar in FP mode)
        // ─────────────────────────────────────────────────────────────────
        private void UpdateInstructionText()
        {
            if (instructionText == null) return;
            const string base_ = "WASD: Lái thuyền | Tab: Bản đồ | E: Cứu/Thả người";
            if (routeGraph != null && routeGraph.selectedTargetNode != null)
                instructionText.text = $"Đi tới {routeGraph.selectedTargetNode.displayName}.\n{base_}";
            else
                instructionText.text = base_;
        }


        // ─────────────────────────────────────────────────────────────────
        //  World-space node markers (colored spheres + labels)
        // ─────────────────────────────────────────────────────────────────
        private struct NodeMarker
        {
            public R1RouteNode node;
            public GameObject go;
            public Renderer rend;
            public TextMeshPro label;
        }

        private GameObject orientationMarkersRoot;
        private Transform playerMarker;

        
        private void BuildOrientationMarkers()
        {
            if (orientationMarkersRoot != null) Destroy(orientationMarkersRoot);
            orientationMarkersRoot = new GameObject("R1_OrientationMarkers");

            // Player Marker
            var pMarkerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pMarkerGo.name = "PlayerMarker";
            pMarkerGo.transform.SetParent(orientationMarkersRoot.transform);
            pMarkerGo.transform.localScale = new Vector3(2f, 1f, 6f);
            Destroy(pMarkerGo.GetComponent<Collider>());
            var pRend = pMarkerGo.GetComponent<Renderer>();
            pRend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            pRend.material.color = Color.yellow;
            playerMarker = pMarkerGo.transform;

            // Find all Rescue Zones and Shelter
            var realtime = FindAnyObjectByType<R1RealtimeRoundController>();
            
            // Build Rescue A
            CreateOrientationLabel("Nhà A", new Vector3(-65, 2, 70), () => {
                if (realtime != null) return realtime.rescuedA ? "Đã cứu" : "Cần cứu";
                return "Cần cứu";
            });

            // Build Rescue B
            CreateOrientationLabel("Nhà B", new Vector3(80, 2, 85), () => {
                if (realtime != null) return realtime.rescuedB ? "Đã cứu" : "Cần cứu";
                return "Cần cứu";
            });

            // Build Shelter
            CreateOrientationLabel("Điểm trú", new Vector3(20, 2, -15), () => "Điểm trú");
        }

        private System.Collections.Generic.List<System.Action> markerUpdaters = new System.Collections.Generic.List<System.Action>();

        private void CreateOrientationLabel(string name, Vector3 pos, System.Func<string> getText)
        {
            var go = new GameObject("Marker_" + name);
            go.transform.SetParent(orientationMarkersRoot.transform);
            go.transform.position = pos + Vector3.up * 5f;
            
            var bg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bg.transform.SetParent(go.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = Vector3.one * 4f;
            Destroy(bg.GetComponent<Collider>());
            var rend = bg.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = Vector3.up * 4f;
            var tmp = labelGo.AddComponent<TMPro.TextMeshPro>();
            tmp.fontSize = 20f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            markerUpdaters.Add(() => {
                string text = getText();
                tmp.text = text;
                if (text == "Cần cứu") {
                    rend.material.color = new Color(1f, 0.5f, 0f);
                    tmp.color = new Color(1f, 0.5f, 0f);
                } else if (text == "Đã cứu") {
                    rend.material.color = new Color(0.4f, 0.4f, 0.4f);
                    tmp.color = new Color(0.4f, 0.4f, 0.4f);
                } else if (text == "Điểm trú") {
                    rend.material.color = new Color(0.2f, 1f, 0.4f);
                    tmp.color = new Color(0.2f, 1f, 0.4f);
                }
            });
        }

        private void RefreshOrientationMarkers()
        {
            if (orientationMarkersRoot != null) orientationMarkersRoot.SetActive(true);
            foreach (var update in markerUpdaters) update();
        }

        private void UpdateOrientationPlayer()
        {
            if (playerMarker != null && fpBoatController != null)
            {
                playerMarker.position = fpBoatController.transform.position + Vector3.up * 10f;
                playerMarker.rotation = Quaternion.Euler(90, fpBoatController.transform.eulerAngles.y, 0);
            }
        }



        private void BuildNodeMarkers()
        {
            if (routeGraph == null) return;
            if (!enableLegacyRouteSelection)
            {
                BuildOrientationMarkers();
                return;
            }
            nodeVisualsRoot = new GameObject("R1_PlanningNodeVisuals");

            foreach (var node in routeGraph.allNodes)
            {
                if (node == null || node.worldAnchor == null) continue;

                // Sphere
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "PlanningDot_" + node.nodeId;
                go.transform.SetParent(nodeVisualsRoot.transform);
                go.transform.position = node.worldAnchor.position + Vector3.up * 0.3f;
                go.transform.localScale = Vector3.one * 0.6f;
                Destroy(go.GetComponent<Collider>()); // no physics needed

                var rend = go.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                // World-space label
                var labelGo = new GameObject("Label_" + node.nodeId);
                labelGo.transform.SetParent(go.transform);
                labelGo.transform.localPosition = Vector3.up * 1.5f;
                var tmp = labelGo.AddComponent<TextMeshPro>();
                tmp.fontSize = 2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                nodeMarkers.Add(new NodeMarker { node = node, go = go, rend = rend, label = tmp });
            }
        }

        private void SetNodeMarkersVisible(bool visible)
        {
            if (nodeVisualsRoot != null) nodeVisualsRoot.SetActive(visible);
            if (orientationMarkersRoot != null) orientationMarkersRoot.SetActive(visible);
        }

        private void RefreshNodeMarkers()
        {
            if (!enableLegacyRouteSelection)
            {
                RefreshOrientationMarkers();
                return;
            }
            if (routeGraph == null) return;
            var current  = routeGraph.currentNode;
            var adjacent = current != null ? routeGraph.GetAdjacentNodes(current) : new List<R1RouteNode>();

            int adjIdx = 1;
            foreach (var m in nodeMarkers)
            {
                if (m.node == null || m.go == null) continue;
                Color c;
                string labelText = m.node.displayName;

                if (m.node == current)
                {
                    c = new Color(1f, 0.95f, 0f);   // Yellow – current
                    labelText = "★ " + m.node.displayName;
                }
                else if (adjacent.Contains(m.node))
                {
                    // Cyan selectable with number
                    c = new Color(0f, 0.9f, 1f);
                    string status = "";
                    if (m.node.nodeType == R1NodeType.Rescue && !m.node.isCollected)
                        status = $"\n{m.node.peopleCount} người";
                    else if (m.node.nodeType == R1NodeType.Shelter)
                        status = "\nĐiểm trú";
                    labelText = $"[{adjIdx}] {m.node.displayName}{status}";
                    adjIdx++;
                }
                else if (m.node.nodeType == R1NodeType.Rescue)
                    c = new Color(1f, 0.5f, 0f);    // Orange – rescue
                else if (m.node.nodeType == R1NodeType.Shelter)
                    c = new Color(0.2f, 1f, 0.4f);  // Green – shelter
                else
                    c = new Color(0.4f, 0.4f, 0.4f); // Grey – unreachable

                m.rend.material.color = c;
                m.label.text = labelText;
                m.label.color = c;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Create Planning Camera
        // ─────────────────────────────────────────────────────────────────
        private Camera CreatePlanningCamera()
        {
            var go = new GameObject("R1_PlanningMapCamera");
            var cam = go.AddComponent<Camera>();

            // Top-down view over Round 1 map centre
            // Nodes span roughly X:-6 to +5, Z:-2 to +4 → centre ≈ (-0.5, y, 1)
            go.transform.position = new Vector3(-0.5f, 18f, 1f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            cam.orthographic     = true;
            cam.orthographicSize = 8f;
            cam.nearClipPlane    = 0.1f;
            cam.farClipPlane     = 50f;
            cam.depth            = 200;  // Render above FP cam
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.05f, 0.08f, 0.12f, 1f);

            go.SetActive(false);
            return cam;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Build full-screen Planning UI
        // ─────────────────────────────────────────────────────────────────
        private GameObject BuildPlanningUI()
        {
            // Find or use R1_Shared_RoundGameUI_Canvas
            Canvas parentCanvas = null;
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.name.Contains("R1_Shared") || c.name.Contains("R1_HUD"))
                {
                    parentCanvas = c;
                    break;
                }
            }
            if (parentCanvas == null && allCanvases.Length > 0)
                parentCanvas = allCanvases[0];
            if (parentCanvas == null)
            {
                Debug.LogError("[R1Planning] No Canvas found!");
                return new GameObject("R1_PlanningUI_Group_ERROR");
            }

            // Root panel – full screen container
            var panel = new GameObject("R1_PlanningUI_Group");
            panel.transform.SetParent(parentCanvas.transform, false);

            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            BuildUIElements(panel.transform);
            return panel;
        }

        private void BuildUIElements(Transform root)
        {
            // ── DarkOverlay ──────────────────────────────────────────────
            var darkOverlay = CreatePanel(root, "DarkOverlay",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0, 0), new Vector2(0, 0),
                new Color(0f, 0f, 0f, 0.4f)); // Light translucent overlay

            // ── TopBar ──────────────────────────────────────────────────
            var topBar = CreatePanel(root, "TopBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0, -60f), new Vector2(0, 0),
                new Color(0.05f, 0.15f, 0.25f, 0.9f));
            CreateTMP(topBar.transform, "TitleText", "BẢN ĐỒ CỨU HỘ - ROUND 1",
                      28, TextAlignmentOptions.Center, Color.white,
                      Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── LeftInfoPanel ──────────────────────────────────────────
            var leftPanel = CreatePanel(root, "LeftInfoPanel",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(20f, 70f), new Vector2(320f, -80f),
                new Color(0.05f, 0.1f, 0.15f, 0.85f));

            var leftLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(20, 20, 20, 20);
            leftLayout.spacing = 15;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;

            txtCurrentNode = CreateTMPInLayout(leftPanel.transform, "TxtCurrentNode",
                                               "Vị trí hiện tại: –", 18,
                                               TextAlignmentOptions.Left, Color.white);
            txtTurns = CreateTMPInLayout(leftPanel.transform, "TxtTurns",
                                          "Lượt còn lại: –", 18,
                                          TextAlignmentOptions.Left, new Color(1f, 0.9f, 0.3f));
            txtCargo = CreateTMPInLayout(leftPanel.transform, "TxtCargo",
                                          "Trên thuyền: – / 3", 18,
                                          TextAlignmentOptions.Left, new Color(0.4f, 0.9f, 1f));
            txtSaved = CreateTMPInLayout(leftPanel.transform, "TxtSaved",
                                          "Đã cứu: – / 3", 18,
                                          TextAlignmentOptions.Left, new Color(0.3f, 1f, 0.5f));

            CreateDivider(leftPanel.transform);
            CreateTMPInLayout(leftPanel.transform, "TxtObjective",
                              "Mục tiêu:\nCứu 3 người và đưa về Nhà văn hóa.", 16,
                              TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));

            // ── RouteOptionsPanel ────────────────────────────────────────
            var rightPanel = CreatePanel(root, "RouteOptionsPanel",
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-380f, 70f), new Vector2(-20f, -80f),
                new Color(0.05f, 0.1f, 0.15f, 0.85f));

            var rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
            
            if (!enableLegacyRouteSelection) {
                // Wipe left panel UI entirely
                foreach (Transform child in leftPanel.transform) Destroy(child.gameObject);
                var legLayout = leftPanel.GetComponent<VerticalLayoutGroup>();
                legLayout.padding = new RectOffset(20,20,20,20);
                legLayout.spacing = 20;
                
                CreateTMPInLayout(leftPanel.transform, "L_Title", "CHÚ THÍCH", 24, TextAlignmentOptions.Center, Color.white);
                CreateTMPInLayout(leftPanel.transform, "L_1", "<color=yellow>■</color> Vị trí thuyền", 20, TextAlignmentOptions.Left, Color.white);
                CreateTMPInLayout(leftPanel.transform, "L_2", "<color=#FF8800>●</color> Nhà cần cứu", 20, TextAlignmentOptions.Left, Color.white);
                CreateTMPInLayout(leftPanel.transform, "L_3", "<color=#33FF66>●</color> Điểm trú", 20, TextAlignmentOptions.Left, Color.white);
                CreateTMPInLayout(leftPanel.transform, "L_4", "<color=#666666>●</color> Đã cứu", 20, TextAlignmentOptions.Left, Color.white);

                // Right panel
                rightPanel.SetActive(false);
                return;
            }

            rightLayout.padding = new RectOffset(20, 20, 20, 20);
            rightLayout.spacing = 15;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;

            txtRouteOptions = CreateTMPInLayout(rightPanel.transform, "TxtRoutes",
                                                "Đang tải...", 20,
                                                TextAlignmentOptions.Left, Color.white);

            // ── Footer ───────────────────────────────────────────────────
            var footerBar = CreatePanel(root, "FooterBar",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0, 0), new Vector2(0, 50f),
                new Color(0f, 0.1f, 0.25f, 0.95f));
            txtFooter = CreateTMP(footerBar.transform, "TxtFooter",
                                  "Tab/Esc: Đóng bản đồ",
                                  15, TextAlignmentOptions.Center, new Color(0.7f, 0.9f, 1f),
                                  Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        // ─────────────────────────────────────────────────────────────────
        //  UI helpers
        // ─────────────────────────────────────────────────────────────────
        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return go;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions align, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = align;
            tmp.color     = color;
            tmp.enableWordWrapping = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return tmp;
        }

        private static TextMeshProUGUI CreateTMPInLayout(Transform parent, string name, string text,
            float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = align;
            tmp.color     = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize * 2.2f;
            le.flexibleWidth   = 1;
            return tmp;
        }

        private static void CreateDivider(Transform parent)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.5f, 0.7f, 0.5f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1f;
            le.flexibleWidth = 1;
        }
    }
}
