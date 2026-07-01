using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Round1
{
    public sealed class R1OrientationMapView : MonoBehaviour
    {
        [Header("Runtime References")]
        public Camera planningCamera;
        public Round1FirstPersonBoatController boatController;
        public Round1SceneReferences sceneReferences;
        public Round1RescueController rescueController;
        public R1RealtimeRoundController realtimeController;

        [Header("Marker Tuning")]
        public float playerMarkerSize = 0.9f;
        public float playerMarkerHeight = 2.9f;
        public float objectiveMarkerHeight = 2.55f;
        public float labelHeight = 0.62f;
        public float rescueMarkerDiameter = 0.82f;
        public float shelterMarkerDiameter = 0.98f;
        public float shelterPulseSpeed = 2.2f;
        public float shelterCargoPulseScale = 0.12f;

        private GameObject canvasRoot;
        private GameObject markerRoot;
        private Transform playerMarker;
        private Renderer markerNhaBaRenderer;
        private Renderer markerNhaTuRenderer;
        private Renderer shelterRenderer;
        private Transform markerNhaBaTransform;
        private Transform markerNhaTuTransform;
        private Transform shelterTransform;
        private TMP_Text labelNhaBa;
        private TMP_Text labelNhaTu;
        private TMP_Text labelShelter;
        private TMP_Text txtMapCurrent;
        private TMP_Text txtMapTime;
        private TMP_Text txtMapCargo;
        private TMP_Text txtMapSaved;
        private TMP_Text txtMapObjective;
        private Material playerMaterial;
        private Material rescueMaterial;
        private Material rescuedMaterial;
        private Material shelterMaterial;
        private Material outlineMaterial;
        private bool isVisible;

        private static readonly Color PlayerColor = new Color(1f, 0.86f, 0.18f, 1f);
        private static readonly Color RescueColor = new Color(1f, 0.48f, 0.12f, 1f);
        private static readonly Color RescuedColor = new Color(0.42f, 0.44f, 0.43f, 1f);
        private static readonly Color ShelterColor = new Color(0.22f, 0.9f, 0.38f, 1f);

        public GameObject CanvasRoot => canvasRoot;
        public GameObject MarkerRoot => markerRoot;

        public void Initialize(Camera mapCamera, Round1FirstPersonBoatController boat)
        {
            planningCamera = mapCamera;
            boatController = boat;

            if (sceneReferences == null)
            {
                sceneReferences = FindAnyObjectByType<Round1SceneReferences>(FindObjectsInactive.Include);
            }

            if (rescueController == null)
            {
                rescueController = FindAnyObjectByType<Round1RescueController>(FindObjectsInactive.Include);
            }

            if (realtimeController == null)
            {
                realtimeController = FindAnyObjectByType<R1RealtimeRoundController>(FindObjectsInactive.Include);
            }

            EnsureMaterials();
            EnsureCanvas();
            EnsureMarkers();
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;

            if (canvasRoot != null)
            {
                canvasRoot.SetActive(visible);
            }

            if (markerRoot != null)
            {
                markerRoot.SetActive(visible);
            }

            if (visible)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (!isVisible)
            {
                return;
            }

            UpdatePlayerMarker();
            UpdateObjectiveMarkers();
            UpdateMapHud();
        }

        private void EnsureMaterials()
        {
            if (playerMaterial == null) playerMaterial = CreateUnlitMaterial("R1_Map_PlayerMarker_Mat", PlayerColor);
            if (rescueMaterial == null) rescueMaterial = CreateUnlitMaterial("R1_Map_RescueMarker_Mat", RescueColor);
            if (rescuedMaterial == null) rescuedMaterial = CreateUnlitMaterial("R1_Map_RescuedMarker_Mat", RescuedColor);
            if (shelterMaterial == null) shelterMaterial = CreateUnlitMaterial("R1_Map_ShelterMarker_Mat", ShelterColor);
            if (outlineMaterial == null) outlineMaterial = CreateUnlitMaterial("R1_Map_IconOutline_Mat", new Color(0.015f, 0.02f, 0.018f, 0.9f));
        }

        private static Material CreateUnlitMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            return mat;
        }

        private void EnsureCanvas()
        {
            if (canvasRoot != null)
            {
                return;
            }

            canvasRoot = new GameObject("R1_PlanningMapCanvas");
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = canvasRoot.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            CanvasGroup canvasGroup = canvasRoot.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject panel = new GameObject("R1_PlanningUI_Group");
            panel.transform.SetParent(canvasRoot.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            CreatePanel(panel.transform, "DarkOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.05f, 0.05f, 0.10f));

            GameObject topBar = CreatePanel(panel.transform, "TopBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -76f), Vector2.zero,
                new Color(0.035f, 0.10f, 0.10f, 0.92f));
            CreateTMP(topBar.transform, "TitleText", "BẢN ĐỒ CỨU HỘ  •  ROUND 1", 34f,
                TextAlignmentOptions.Center, new Color(0.94f, 0.91f, 0.84f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject compactCard = CreatePanel(panel.transform, "CompactMapCard",
                Vector2.zero, Vector2.zero,
                new Vector2(36f, 78f), new Vector2(536f, 324f),
                new Color(0.035f, 0.10f, 0.10f, 0.90f));
            VerticalLayoutGroup cardLayout = compactCard.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(16, 16, 12, 12);
            cardLayout.spacing = 4f;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;

            txtMapCurrent = CreateTMPInLayout(compactCard.transform, "CurrentLocation",
                "VỊ TRÍ: THEO DẤU ▲", 18f, TextAlignmentOptions.Left,
                new Color(0.94f, 0.91f, 0.84f));
            LayoutElement currentElement = txtMapCurrent.GetComponent<LayoutElement>();
            currentElement.preferredHeight = 28f;
            currentElement.flexibleWidth = 1f;

            CreateDivider(compactCard.transform);

            GameObject statsGrid = new GameObject("StatsGrid");
            statsGrid.transform.SetParent(compactCard.transform, false);
            VerticalLayoutGroup statsLayout = statsGrid.AddComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 3f;
            statsLayout.childAlignment = TextAnchor.UpperLeft;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = true;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            LayoutElement statsElement = statsGrid.AddComponent<LayoutElement>();
            statsElement.preferredHeight = 66f;
            statsElement.flexibleWidth = 1f;

            txtMapTime = CreateStatRow(statsGrid.transform, "TimeRow", "THỜI GIAN", "--:--");
            txtMapCargo = CreateStatRow(statsGrid.transform, "CargoRow", "TRÊN THUYỀN", "0/3");
            txtMapSaved = CreateStatRow(statsGrid.transform, "SavedRow", "ĐÃ CỨU", "0/3");

            CreateDivider(compactCard.transform);

            TMP_Text objLabel = CreateTMPInLayout(compactCard.transform, "LblObjective", "MỤC TIÊU", 16f,
                TextAlignmentOptions.Left, new Color(0.80f, 0.71f, 0.47f));
            LayoutElement objLabelElement = objLabel.GetComponent<LayoutElement>();
            objLabelElement.preferredHeight = 20f;
            objLabelElement.flexibleWidth = 1f;

            txtMapObjective = CreateTMPInLayout(compactCard.transform, "TxtObjective",
                "Tìm nhà có tín hiệu cầu cứu.", 16f,
                TextAlignmentOptions.Left, new Color(0.93f, 0.91f, 0.86f));
            LayoutElement objValueElement = txtMapObjective.GetComponent<LayoutElement>();
            objValueElement.preferredHeight = 30f;
            objValueElement.flexibleWidth = 1f;
            txtMapObjective.textWrappingMode = TextWrappingModes.Normal;
            txtMapObjective.overflowMode = TextOverflowModes.Overflow;

            CreateDivider(compactCard.transform);
            CreateLegendGrid(compactCard.transform);

            GameObject footer = CreatePanel(panel.transform, "FooterBar",
                Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 58f),
                new Color(0.035f, 0.10f, 0.10f, 0.92f));
            CreateTMP(footer.transform, "TxtFooter", "TAB / ESC  •  ĐÓNG BẢN ĐỒ", 18f,
                TextAlignmentOptions.Center, new Color(0.82f, 0.84f, 0.80f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            ApplyPlanningTypography(panel.transform);
        }

        private void EnsureMarkers()
        {
            if (markerRoot != null)
            {
                return;
            }

            markerRoot = new GameObject("R1_OrientationMarkers");
            SetIgnoreRaycastRecursive(markerRoot);

            playerMarker = CreatePlayerArrow("R1_MapMarker_PlayerBoat").transform;

            Transform nhaBa = FindAnchor("R1_NhaBa_RescueAnchor");
            Transform nhaTu = FindAnchor("R1_NhaTu_RescueAnchor");
            Transform shelter = FindAnchor("R1_R2Style_Shelter_BaiDinh");

            markerNhaBaRenderer = CreateObjectiveMarker("R1_MapMarker_NhaBa", GetMarkerPosition(nhaBa), rescueMaterial, false, out markerNhaBaTransform, out labelNhaBa);
            markerNhaTuRenderer = CreateObjectiveMarker("R1_MapMarker_NhaTu", GetMarkerPosition(nhaTu), rescueMaterial, false, out markerNhaTuTransform, out labelNhaTu);
            shelterRenderer = CreateObjectiveMarker("R1_MapMarker_Shelter", GetMarkerPosition(shelter), shelterMaterial, true, out shelterTransform, out labelShelter);

            if (labelShelter != null)
            {
                labelShelter.text = "Điểm trú";
                labelShelter.color = ShelterColor;
            }
        }

        private GameObject CreatePlayerArrow(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(markerRoot.transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = playerMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Mesh mesh = new Mesh();
            float size = playerMarkerSize;
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, size),
                new Vector3(-size * 0.45f, 0f, -size * 0.45f),
                new Vector3(size * 0.45f, 0f, -size * 0.45f),
                new Vector3(0f, 0f, size),
                new Vector3(size * 0.45f, 0f, -size * 0.45f),
                new Vector3(-size * 0.45f, 0f, -size * 0.45f)
            };
            mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;

            SetIgnoreRaycastRecursive(go);
            return go;
        }

        private static Transform FindAnchor(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go != null)
            {
                return go.transform;
            }

            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == objectName && all[i].scene.isLoaded)
                {
                    return all[i].transform;
                }
            }

            return null;
        }

        private Renderer CreateObjectiveMarker(string name, Vector3 position, Material material, bool shelter, out Transform markerTransform, out TMP_Text label)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(markerRoot.transform, false);
            root.transform.position = position + Vector3.up * objectiveMarkerHeight;
            markerTransform = root.transform;

            GameObject icon = new GameObject(shelter ? "ShelterIcon" : "RescuePinIcon");
            icon.transform.SetParent(root.transform, false);
            MeshFilter filter = icon.AddComponent<MeshFilter>();
            MeshRenderer renderer = icon.AddComponent<MeshRenderer>();
            Mesh iconMesh = shelter ? CreateDiamondMesh(shelterMarkerDiameter) : CreatePinMesh(rescueMarkerDiameter);
            filter.sharedMesh = iconMesh;
            icon.transform.localRotation = Quaternion.identity;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            GameObject outline = new GameObject("IconOutline");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            outline.transform.localScale = Vector3.one * 1.18f;
            MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
            MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
            outlineFilter.sharedMesh = iconMesh;
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outline.transform.SetAsFirstSibling();

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.04f, labelHeight);
            labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 0.5f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = "Cần cứu";
            label.color = RescueColor;
            label.outlineWidth = 0.18f;
            label.outlineColor = new Color(0.02f, 0.025f, 0.02f, 0.95f);

            SetIgnoreRaycastRecursive(root);
            return renderer;
        }

        private static Mesh CreatePinMesh(float diameter)
        {
            float r = diameter * 0.42f;
            float tail = diameter * 0.72f;
            Mesh mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-r, 0f, r * 0.45f),
                new Vector3(0f, 0f, r),
                new Vector3(r, 0f, r * 0.45f),
                new Vector3(r * 0.75f, 0f, -r * 0.25f),
                new Vector3(0f, 0f, -tail),
                new Vector3(-r * 0.75f, 0f, -r * 0.25f),
                new Vector3(0f, 0f, 0f),
                new Vector3(-r * 0.44f, 0f, r * 0.35f),
                new Vector3(0f, 0f, r * 0.58f),
                new Vector3(r * 0.44f, 0f, r * 0.35f),
                new Vector3(r * 0.36f, 0f, -r * 0.05f),
                new Vector3(0f, 0f, -r * 0.36f),
                new Vector3(-r * 0.36f, 0f, -r * 0.05f)
            };
            mesh.triangles = new[]
            {
                0, 1, 6,
                1, 2, 6,
                2, 3, 6,
                3, 4, 6,
                4, 5, 6,
                5, 0, 6
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateDiamondMesh(float diameter)
        {
            float r = diameter * 0.5f;
            Mesh mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, r),
                new Vector3(r, 0f, 0f),
                new Vector3(0f, 0f, -r),
                new Vector3(-r, 0f, 0f),
                new Vector3(0f, 0f, 0f)
            };
            mesh.triangles = new[]
            {
                0, 1, 4,
                1, 2, 4,
                2, 3, 4,
                3, 0, 4
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private Vector3 GetMarkerPosition(Transform target)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            Vector3 pos = target.position;
            pos.y = 0f;
            return pos;
        }

        private void UpdatePlayerMarker()
        {
            if (playerMarker == null || boatController == null)
            {
                return;
            }

            Vector3 pos = boatController.transform.position;
            pos.y += playerMarkerHeight;
            playerMarker.position = pos;

            Vector3 forward = boatController.CurrentBoatForwardWorld;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = boatController.transform.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude > 0.001f)
            {
                playerMarker.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        private void UpdateMapHud()
        {
            if (realtimeController == null) return;

            int minutes = Mathf.FloorToInt(realtimeController.currentTimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(realtimeController.currentTimeRemaining % 60f);
            if (txtMapCurrent != null) txtMapCurrent.text = "VỊ TRÍ: THEO DẤU ▲";
            if (txtMapTime != null) txtMapTime.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            if (txtMapCargo != null) txtMapCargo.text = string.Format("{0}/{1}", realtimeController.currentCargo, realtimeController.boatCapacity);
            if (txtMapSaved != null) txtMapSaved.text = string.Format("{0}/{1}", realtimeController.civiliansSafe, realtimeController.totalCivilians);
            if (txtMapObjective != null)
            {
                string objective = realtimeController.currentObjectiveText ?? "";
                const string prefix = "Mục tiêu:";
                if (objective.StartsWith(prefix)) objective = objective.Substring(prefix.Length).Trim();
                txtMapObjective.text = objective;
            }
        }

        private void UpdateObjectiveMarkers()
        {
            bool nhaBaCleared = realtimeController != null
                ? realtimeController.rescuedA
                : rescueController != null && rescueController.RemainingAtNhaBa <= 0;

            bool nhaTuCleared = realtimeController != null
                ? realtimeController.rescuedB
                : rescueController != null && rescueController.RemainingAtNhaTu <= 0;

            ApplyRescueState(markerNhaBaRenderer, labelNhaBa, nhaBaCleared);
            ApplyRescueState(markerNhaTuRenderer, labelNhaTu, nhaTuCleared);

            if (shelterTransform != null && rescueController != null && rescueController.Cargo > 0)
            {
                float pulse = 1f + Mathf.Sin(Time.time * shelterPulseSpeed) * shelterCargoPulseScale;
                shelterTransform.localScale = new Vector3(pulse, pulse, pulse);
            }
            else if (shelterTransform != null)
            {
                shelterTransform.localScale = Vector3.one;
            }
        }

        private void ApplyRescueState(Renderer markerRenderer, TMP_Text label, bool cleared)
        {
            if (markerRenderer != null)
            {
                markerRenderer.sharedMaterial = cleared ? rescuedMaterial : rescueMaterial;
            }

            if (label != null)
            {
                label.text = cleared ? "Đã đón" : "Cần cứu";
                label.color = cleared ? RescuedColor : RescueColor;
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return go;
        }

        private static TMP_Text CreateTMP(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return tmp;
        }

        private static TMP_Text CreateTMPInLayout(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = fontSize * 1.55f;
            layout.flexibleWidth = 1f;
            return tmp;
        }

        private static void SetIgnoreRaycastRecursive(GameObject go)
        {
            int layer = LayerMask.NameToLayer("Ignore Raycast");
            if (layer < 0) layer = 2;

            Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.layer = layer;
            }
        }

        private static TMP_Text CreateStatRow(Transform parent, string name, string label, string value)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = 20f;
            rowElement.flexibleWidth = 1f;

            TMP_Text labelText = CreateTMPInLayout(row.transform, "Lbl" + name, label, 16f,
                TextAlignmentOptions.Left, new Color(0.80f, 0.71f, 0.47f));
            LayoutElement labelElement = labelText.GetComponent<LayoutElement>();
            labelElement.preferredHeight = 20f;
            labelElement.flexibleWidth = 1f;

            TMP_Text valueText = CreateTMPInLayout(row.transform, "Val" + name, value, 17f,
                TextAlignmentOptions.Right, new Color(0.94f, 0.91f, 0.84f));
            LayoutElement valueElement = valueText.GetComponent<LayoutElement>();
            valueElement.preferredHeight = 20f;
            valueElement.preferredWidth = 64f;
            valueElement.flexibleWidth = 0f;
            return valueText;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.67f, 0.52f, 0.24f, 0.45f);
            image.raycastTarget = false;
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 1f;
            layout.flexibleWidth = 1f;
        }

        private static void CreateLegendGrid(Transform parent)
        {
            GameObject grid = new GameObject("LegendGrid");
            grid.transform.SetParent(parent, false);

            GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(230f, 20f);
            layout.spacing = new Vector2(8f, 4f);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            LayoutElement gridElement = grid.AddComponent<LayoutElement>();
            gridElement.preferredHeight = 44f;
            gridElement.flexibleWidth = 1f;

            CreateLegendItem(grid.transform, "LegendBoat", "<color=#FFDB2E>▲</color> Thuyền");
            CreateLegendItem(grid.transform, "LegendRescue", "<color=#FF7A1F>●</color> Cần cứu");
            CreateLegendItem(grid.transform, "LegendShelter", "<color=#38E65F>□</color> Điểm trú");
            CreateLegendItem(grid.transform, "LegendCleared", "<color=#707371>●</color> Đã cứu");
        }

        private static void CreateLegendItem(Transform parent, string name, string text)
        {
            TMP_Text item = CreateTMPInLayout(parent, name, text, 15f,
                TextAlignmentOptions.Left, new Color(0.82f, 0.84f, 0.80f));
            item.overflowMode = TextOverflowModes.Overflow;
            LayoutElement element = item.GetComponent<LayoutElement>();
            element.preferredHeight = 20f;
        }

        private static void ApplyPlanningTypography(Transform root)
        {
            TMP_FontAsset heading = FindLoadedFont("BarlowCondensed_Bold SDF SDF");
            TMP_FontAsset label = FindLoadedFont("BarlowCondensed_SemiBold SDF SDF");
            TMP_FontAsset body = FindLoadedFont("BeVietnamPro_Medium SDF SDF");
            TMP_FontAsset regular = FindLoadedFont("BeVietnamPro_Regular SDF SDF");

            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == "TitleText")
                    text.font = heading != null ? heading : text.font;
                else if (text.name == "CurrentLocation" || text.name == "LblObjective" ||
                         text.name.StartsWith("Lbl"))
                    text.font = label != null ? label : text.font;
                else if (text.name == "TxtFooter" || text.name.StartsWith("Legend"))
                    text.font = regular != null ? regular : text.font;
                else
                    text.font = body != null ? body : text.font;

                text.raycastTarget = false;
                text.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private static TMP_FontAsset FindLoadedFont(string fontName)
        {
            foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (font.name == fontName) return font;
            return null;
        }
    }
}
