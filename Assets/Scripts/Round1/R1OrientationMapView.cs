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

            GameObject panel = CreatePanel(canvasRoot.transform, "R1_PlanningUI_Group", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.4f));
            CreatePanel(panel.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), Vector2.zero, new Color(0.05f, 0.15f, 0.25f, 0.9f));
            CreateTMP(panel.transform, "TitleText", "BẢN ĐỒ CỨU HỘ", 24f, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), Vector2.zero);

            GameObject legend = CreatePanel(panel.transform, "LegendPanel", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 70f), new Vector2(320f, -80f), new Color(0.05f, 0.1f, 0.15f, 0.85f));
            VerticalLayoutGroup layout = legend.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 20f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateTMPInLayout(legend.transform, "LegendTitle", "CHÚ THÍCH", 16f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendBoat", "<color=#FFD92E>▲</color> Thuyền", 14f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendRescue", "<color=#FF7A1F>●</color> Cần cứu", 14f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendShelter", "<color=#38E65F>◆</color> Điểm trú", 14f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendDone", "<color=#707371>●</color> Đã đón", 14f, TextAlignmentOptions.Left, Color.white);

            GameObject footer = CreatePanel(panel.transform, "FooterBar", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 50f), new Color(0f, 0.1f, 0.25f, 0.95f));
            CreateTMP(footer.transform, "FooterText", "Tab/Esc: Đóng bản đồ", 15f, TextAlignmentOptions.Center, new Color(0.86f, 0.95f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
            label.enableWordWrapping = false;
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

        private void UpdateObjectiveMarkers()
        {
            bool nhaBaCleared = rescueController != null
                ? rescueController.RemainingAtNhaBa <= 0
                : realtimeController != null && realtimeController.rescuedA;

            bool nhaTuCleared = rescueController != null
                ? rescueController.RemainingAtNhaTu <= 0
                : realtimeController != null && realtimeController.rescuedB;

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
    }
}
