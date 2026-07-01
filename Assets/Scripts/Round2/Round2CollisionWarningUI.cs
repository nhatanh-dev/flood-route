using UnityEngine;
using UnityEngine.UI;

namespace Round2
{
    public class Round2CollisionWarningUI : MonoBehaviour
    {
        [Header("Vignette")]
        public float lightContactAlpha = 0.12f;
        public float damageAlpha = 0.35f;
        public float criticalAlpha = 0.45f;
        public float fadeSpeed = 4.5f;
        [Range(0.1f, 4f)] public float edgePower = 2.0f;
        public Color lightContactColor = new Color(1f, 0.45f, 0.12f, 1f);
        public Color damageColor = new Color(1f, 0.05f, 0.02f, 1f);

        [Header("Canvas")]
        public bool showOverHUD = false;
        public int underHudSortingOrder = -5;
        public int overHudSortingOrder = 100;

        [Header("Debug")]
        public bool enableDebugLogs = false;

        private Image vignetteImage;
        private Texture2D vignetteTexture;
        private float currentAlpha;
        private Color currentColor;

        private void Awake()
        {
            currentColor = damageColor;
            CreateRuntimeCanvas();
        }

        private void CreateRuntimeCanvas()
        {
            GameObject canvasGo = new GameObject("R2_CollisionWarningCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = showOverHUD ? overHudSortingOrder : underHudSortingOrder;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            GameObject imageGo = new GameObject("R2_RedCollisionVignette");
            imageGo.transform.SetParent(canvasGo.transform, false);

            vignetteImage = imageGo.AddComponent<Image>();
            vignetteImage.raycastTarget = false;

            vignetteTexture = CreateVignetteTexture();
            vignetteImage.sprite = Sprite.Create(vignetteTexture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            vignetteImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform rect = vignetteImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Texture2D CreateVignetteTexture()
        {
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float dx = Mathf.Abs(x - 128f) / 128f;
                    float dy = Mathf.Abs(y - 128f) / 128f;
                    float dist = Mathf.Max(dx, dy);
                    float alpha = 0f;

                    if (dist > 0.6f)
                    {
                        float edgeT = Mathf.SmoothStep(0f, 1f, (dist - 0.6f) / 0.4f);
                        alpha = Mathf.Pow(edgeT, edgePower);
                    }

                    tex.SetPixel(x, y, new Color(1f, 0f, 0f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        public void TriggerLightContactWarning()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Round2CollisionWarningUI] Light contact");
            }

            currentColor = lightContactColor;
            currentAlpha = Mathf.Max(currentAlpha, lightContactAlpha);
        }

        public void TriggerDamageWarning(bool isCritical = false)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Round2CollisionWarningUI] Damage contact critical=" + isCritical);
            }

            currentColor = damageColor;
            currentAlpha = Mathf.Max(currentAlpha, isCritical ? criticalAlpha : damageAlpha);
        }

        public void ClearWarning()
        {
            currentAlpha = 0f;
            ApplyAlpha();
        }

        private void Update()
        {
            if (vignetteImage == null)
            {
                return;
            }

            currentAlpha = Mathf.Max(0f, currentAlpha - Time.deltaTime * fadeSpeed);
            ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            if (vignetteImage == null)
            {
                return;
            }

            Color c = currentColor;
            c.a = currentAlpha;
            vignetteImage.color = c;
        }

        private void OnDestroy()
        {
            if (vignetteTexture != null)
            {
                Destroy(vignetteTexture);
            }
        }
    }
}
