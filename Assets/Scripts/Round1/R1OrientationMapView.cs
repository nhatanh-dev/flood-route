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
        public float shelterPulseSpeed = 2.2f;
        public float shelterCargoPulseScale = 0.08f;

        private GameObject canvasRoot;
        private GameObject markerRoot;
        
        
        private Transform nhaBaMarker;
        private Transform nhaTuMarker;
        private Transform shelterMarker;
        private Transform playerMarker;
        
        
        
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

            GameObject panel = CreatePanel(canvasRoot.transform, "R1_PlanningUI_Group", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.018f));
            CreatePanel(panel.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), Vector2.zero, new Color(0.02f, 0.06f, 0.08f, 0.50f));
            CreateTMP(panel.transform, "TitleText", "BẢN ĐỒ CỨU HỘ", 24f, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), Vector2.zero);

            GameObject legend = CreatePanel(panel.transform, "LegendPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -154f), new Vector2(212f, -62f), PanelColor);
            VerticalLayoutGroup layout = legend.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 12, 10, 10);
            layout.spacing = 3f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateTMPInLayout(legend.transform, "LegendTitle", "CHÚ THÍCH", 16f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendBoat", "<color=#6FAFB2>▲</color> Thuyền", 14f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendRescue", "<color=#C97A24>●</color> Cần cứu", 14f, TextAlignmentOptions.Left, Color.white);
            CreateTMPInLayout(legend.transform, "LegendShelter", "<color=#6BAE74>♦</color> Điểm trú", 14f, TextAlignmentOptions.Left, Color.white);

            GameObject footer = CreatePanel(panel.transform, "FooterBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 16f), new Vector2(210f, 46f), new Color(0.02f, 0.06f, 0.08f, 0.52f));
            CreateTMP(footer.transform, "FooterText", "Tab/Esc: Đóng bản đồ", 15f, TextAlignmentOptions.Center, new Color(0.86f, 0.95f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            markerRoot = new GameObject("R1_MapObjectiveIcons");
        }

        private void EnsureMarkers()
        {
            if (nhaBaMarker != null) return;

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

        private void UpdateObjectiveMarkers()
        {
            bool nhaBaActive = !(rescueController != null ? rescueController.RemainingAtNhaBa <= 0 : realtimeController != null && realtimeController.rescuedA);
            bool nhaTuActive = !(rescueController != null ? rescueController.RemainingAtNhaTu <= 0 : realtimeController != null && realtimeController.rescuedB);

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
                bool hasCargo = rescueController != null && rescueController.Cargo > 0;
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