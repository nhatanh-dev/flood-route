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
        public Vector2 rescueIconSize = new Vector2(48f, 58f);
        public Vector2 shelterIconSize = new Vector2(54f, 58f);
        public Vector2 labelOffset = new Vector2(0f, -38f);
        public Vector3 worldOffset = new Vector3(0f, 1.4f, 0f);
        public float playerMarkerHeight = 2.9f;
        public float objectiveMarkerHeight = 2.55f;
        public float labelHeight = 0.62f;
        public float rescueMarkerDiameter = 0.82f;
        public float shelterMarkerDiameter = 0.98f;
        public float shelterPulseSpeed = 2.2f;
        public float shelterCargoPulseScale = 0.08f;

        private GameObject canvasRoot;
        private GameObject markerRoot;
        
        
        private Transform nhaBaMarker;
        private Transform nhaTuMarker;
        private Transform shelterMarker;
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
        private Transform nhaBaAnchor;
        private Transform nhaTuAnchor;
        private Transform shelterAnchor;
        private Sprite rescueSprite;
        private Sprite shelterSprite;
        private Sprite shadowSprite;
        private bool isVisible;

        private static readonly Color RescueColor = new Color(0.95f, 0.36f, 0.10f, 1f);
        private static readonly Color ShelterColor = new Color(0.20f, 0.72f, 0.36f, 1f);
        private static readonly Color LabelColor = new Color(0.95f, 0.98f, 0.96f, 1f);
        private static readonly Color PanelColor = new Color(0.02f, 0.06f, 0.08f, 0.50f);

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

            EnsureSprites();
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

            UpdateObjectiveMarkers();
            UpdateMapHud();
        }

        private void EnsureSprites()
        {
            if (rescueSprite == null)
            {
                rescueSprite = CreateRescuePinSprite("R1_Map_Rescue_Pin_Icon", RescueColor);
            }

            if (shelterSprite == null)
            {
                shelterSprite = CreateShelterSprite("R1_Map_Shelter_Icon", ShelterColor);
            }

            if (shadowSprite == null)
            {
                shadowSprite = CreateSoftDotSprite("R1_Map_Icon_Shadow");
            }
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
            if (nhaBaMarker != null) return;

            markerRoot = new GameObject("R1_MapObjectiveIcons");
            nhaBaAnchor = FindAnchor("R1_NhaBa_RescueAnchor");
            nhaTuAnchor = FindAnchor("R1_NhaTu_RescueAnchor");
            shelterAnchor = FindAnchor("R1_R2Style_Shelter_BaiDinh");

            nhaBaMarker = CreateWorldMarker("R1_MapMarker_NhaBa", false);
            nhaTuMarker = CreateWorldMarker("R1_MapMarker_NhaTu", false);
            shelterMarker = CreateWorldMarker("R1_MapMarker_Shelter", true);
            
            if (boatController != null)
            {
                playerMarker = CreatePlayerWorldMarker("R1_MapMarker_PlayerBoat");
            }
        }

        private void CreateMapIcon_Unused() {}

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
            bool nhaBaActive = realtimeController != null
                ? !realtimeController.rescuedA
                : rescueController == null || rescueController.RemainingAtNhaBa > 0;
            bool nhaTuActive = realtimeController != null
                ? !realtimeController.rescuedB
                : rescueController == null || rescueController.RemainingAtNhaTu > 0;

            float rescuePulse = 1f + (Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f) * 0.08f;
            
            if (nhaBaMarker != null && nhaBaAnchor != null) {
                nhaBaMarker.gameObject.SetActive(nhaBaActive);
                nhaBaMarker.position = nhaBaAnchor.position + Vector3.up * 10f;
                if (nhaBaActive) nhaBaMarker.localScale = new Vector3(rescuePulse, rescuePulse, rescuePulse);
            }
            
            if (nhaTuMarker != null && nhaTuAnchor != null) {
                nhaTuMarker.gameObject.SetActive(nhaTuActive);
                nhaTuMarker.position = nhaTuAnchor.position + Vector3.up * 10f;
                if (nhaTuActive) nhaTuMarker.localScale = new Vector3(rescuePulse, rescuePulse, rescuePulse);
            }
            
            if (shelterMarker != null && shelterAnchor != null) {
                shelterMarker.gameObject.SetActive(true);
                shelterMarker.position = shelterAnchor.position + Vector3.up * 10f;
                bool hasCargo = realtimeController != null
                    ? realtimeController.currentCargo > 0
                    : rescueController != null && rescueController.Cargo > 0;
                float pulse = hasCargo ? 1f + Mathf.Sin(Time.time * shelterPulseSpeed) * shelterCargoPulseScale : 1f;
                shelterMarker.localScale = new Vector3(pulse, pulse, pulse);
            }
            
            if (playerMarker != null && boatController != null) {
                playerMarker.gameObject.SetActive(true);
                playerMarker.position = boatController.transform.position + Vector3.up * 10f;
                float heading = boatController.transform.eulerAngles.y;
                playerMarker.rotation = Quaternion.Euler(0f, heading + 180f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private void UpdateMarkerPosition_Unused() {}

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

        private static Sprite CreateRescuePinSprite(string name, Color fill)
        {
            const int width = 96;
            const int height = 112;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f - width * 0.5f) / width;
                    float ny = (y + 0.5f - height * 0.56f) / height;

                    bool outerCircle = Sqr(nx / 0.27f) + Sqr((ny - 0.10f) / 0.24f) <= 1f;
                    bool outerTip = PointInTriangle(nx, ny, -0.15f, -0.02f, 0.15f, -0.02f, 0f, -0.36f);
                    bool outer = outerCircle || outerTip;

                    bool innerCircle = Sqr(nx / 0.21f) + Sqr((ny - 0.10f) / 0.19f) <= 1f;
                    bool innerTip = PointInTriangle(nx, ny, -0.105f, -0.03f, 0.105f, -0.03f, 0f, -0.27f);
                    bool inner = innerCircle || innerTip;

                    Color pixel = clear;
                    if (outer)
                    {
                        pixel = inner ? fill : Color.white;
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
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
        private static Sprite CreateShelterSprite(string name, Color fill)
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - size * 0.5f) / size;
                    float ny = (y + 0.5f - size * 0.5f) / size;

                    bool outer = PointInPolygon(nx, ny, new[]
                    {
                        new Vector2(0f, 0.40f),
                        new Vector2(0.34f, 0.13f),
                        new Vector2(0.27f, -0.31f),
                        new Vector2(0f, -0.43f),
                        new Vector2(-0.27f, -0.31f),
                        new Vector2(-0.34f, 0.13f)
                    });

                    bool inner = PointInPolygon(nx, ny, new[]
                    {
                        new Vector2(0f, 0.30f),
                        new Vector2(0.25f, 0.09f),
                        new Vector2(0.20f, -0.24f),
                        new Vector2(0f, -0.33f),
                        new Vector2(-0.20f, -0.24f),
                        new Vector2(-0.25f, 0.09f)
                    });

                    bool houseRoof = PointInTriangle(nx, ny, -0.16f, 0.02f, 0.16f, 0.02f, 0f, 0.18f);
                    bool houseBody = Mathf.Abs(nx) < 0.13f && ny > -0.17f && ny < 0.03f;
                    bool houseCut = houseRoof || houseBody;

                    Color pixel = clear;
                    if (outer)
                    {
                        pixel = inner ? fill : Color.white;
                        if (houseCut && inner)
                        {
                            pixel = new Color(0.94f, 0.98f, 0.94f, 1f);
                        }
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateSoftDotSprite(string name)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                    float ny = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Clamp01(1f - d);
                    alpha *= alpha * 0.75f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float Sqr(float value)
        {
            return value * value;
        }

        private static bool PointInTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = Sign(px, py, ax, ay, bx, by);
            float d2 = Sign(px, py, bx, by, cx, cy);
            float d3 = Sign(px, py, cx, cy, ax, ay);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        private static float Sign(float px, float py, float ax, float ay, float bx, float by)
        {
            return (px - bx) * (ay - by) - (ax - bx) * (py - by);
        }

        private static bool PointInPolygon(float x, float y, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                bool intersect = ((polygon[i].y > y) != (polygon[j].y > y)) &&
                    (x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x);
                if (intersect)
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        private Transform CreateWorldMarker(string name, bool shelter)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(markerRoot.transform, false);
            root.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // flat

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = Vector3.zero;
            labelGo.transform.localRotation = Quaternion.identity;
            var label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 5f; // very compact size
            label.lineSpacing = -6f;
            label.enableWordWrapping = false;
            
            if (shelter) {
                label.text = "<size=300%><color=#6BAE74>♦</color></size>\n<size=100%><color=#E0E0E0>Điểm trú</color></size>";
            } else {
                label.text = "<size=300%><color=#C97A24>●</color></size>\n<size=100%><color=#E0E0E0>Cần cứu</color></size>";
            }
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color(0.02f, 0.02f, 0.02f, 0.90f);
            
            return root.transform;
        }

        private Transform CreatePlayerWorldMarker(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(markerRoot.transform, false);
            root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = Vector3.zero;
            labelGo.transform.localRotation = Quaternion.identity;
            var label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 5f;
            label.lineSpacing = -6f;
            label.enableWordWrapping = false;
            label.text = "<size=300%><color=#6FAFB2>▲</color></size>\n<size=100%><color=#E0E0E0>Thuyền</color></size>";
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color(0.02f, 0.02f, 0.02f, 0.90f);
            
            return root.transform;
        }

    }
}
