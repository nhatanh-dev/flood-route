using UnityEngine;
using UnityEngine.UI;

namespace Round1
{
    public class Round1BoundaryWarningUI : MonoBehaviour
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

        private Image borderImage;
        private LineRenderer boundaryLine;
        private Texture2D borderTexture;

        private float currentObstacleAlpha = 0f;
        private float currentBoundaryAlpha = 0f;
        private Color currentVignetteColor;
        private Vector3 lastHitPoint;
        private Vector3 lastHitNormal;

        private void Start()
        {
            // Create Canvas for UI Screen Vignette
            GameObject canvasGo = new GameObject("BoundaryWarningCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = showOverHUD ? overHudSortingOrder : underHudSortingOrder;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var borderGo = new GameObject("RedBorder");
            borderGo.transform.SetParent(canvasGo.transform, false);
            borderImage = borderGo.AddComponent<Image>();
            borderImage.raycastTarget = false;
            
            // Create a procedural red vignette texture
            borderTexture = CreateBorderTexture();
            borderImage.sprite = Sprite.Create(borderTexture, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
            borderImage.color = new Color(1f, 1f, 1f, 0f); 

            var rect = borderImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Setup LineRenderer for World Boundary Visual
            var lineGo = new GameObject("BoundaryLineVisual");
            boundaryLine = lineGo.AddComponent<LineRenderer>();
            boundaryLine.startWidth = 0.2f;
            boundaryLine.endWidth = 0.2f;
            boundaryLine.positionCount = 2;
            boundaryLine.material = new Material(Shader.Find("Sprites/Default"));
            boundaryLine.startColor = new Color(1f, 0f, 0f, 0f);
            boundaryLine.endColor = new Color(1f, 0f, 0f, 0f);
            boundaryLine.useWorldSpace = true;
        }

        private Texture2D CreateBorderTexture()
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

        /// <summary>
        /// Called directly by the Boat Controller when a physical collision overlap happens.
        /// </summary>
        public bool enableDebugLogs = false;

        [System.Obsolete("Use TriggerBoundaryWarning, TriggerLightContactWarning, or TriggerDamageWarning.")]
        public void TriggerCollisionWarning(bool isBoundary, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (enableDebugLogs) Debug.Log($"[CollisionWarning] Triggered! isBoundary={isBoundary}, hit={hitPoint}");
            if (isBoundary)
            {
                TriggerBoundaryWarning(hitPoint, hitNormal);
            }
            else
            {
                TriggerLightContactWarning();
            }
        }

        public void TriggerBoundaryWarning(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (enableDebugLogs) Debug.Log($"[CollisionWarning] Boundary hit={hitPoint}");
            currentBoundaryAlpha = 0.9f;
            lastHitPoint = hitPoint;
            lastHitNormal = hitNormal;
        }

        public void TriggerLightContactWarning()
        {
            if (enableDebugLogs) Debug.Log("[CollisionWarning] Light contact");
            currentVignetteColor = lightContactColor;
            currentObstacleAlpha = Mathf.Max(currentObstacleAlpha, lightContactAlpha);
        }

        public void TriggerDamageWarning(bool isCritical = false)
        {
            if (enableDebugLogs) Debug.Log($"[CollisionWarning] Damage contact critical={isCritical}");
            currentVignetteColor = damageColor;
            currentObstacleAlpha = Mathf.Max(currentObstacleAlpha, isCritical ? criticalAlpha : damageAlpha);
        }

        public void ClearWarning()
        {
            currentObstacleAlpha = 0f;
            currentBoundaryAlpha = 0f;
        }

        private void Update()
        {
            if (borderImage == null || boundaryLine == null) return;

            // 1. Fade alphas down continuously
            currentObstacleAlpha = Mathf.Max(0f, currentObstacleAlpha - Time.deltaTime * fadeSpeed);
            currentBoundaryAlpha = Mathf.Max(0f, currentBoundaryAlpha - Time.deltaTime * fadeSpeed);

            // 2. Apply UI Vignette alpha
            Color c = currentVignetteColor;
            c.a = currentObstacleAlpha;
            borderImage.color = c;

            // 3. Apply World Line position & alpha
            if (currentBoundaryAlpha > 0.01f)
            {
                Vector3 tangent = Vector3.Cross(lastHitNormal, Vector3.up).normalized;
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.right;
                
                Vector3 hitP = lastHitPoint;
                hitP.y = 1.05f; // slightly above water level
                
                Vector3 p1 = hitP + tangent * 3f;
                Vector3 p2 = hitP - tangent * 3f;
                
                boundaryLine.SetPosition(0, p1);
                boundaryLine.SetPosition(1, p2);
            }

            Color lc = boundaryLine.startColor;
            lc.a = currentBoundaryAlpha;
            boundaryLine.startColor = lc;
            boundaryLine.endColor = lc;
        }

        private void OnDestroy()
        {
            if (borderTexture != null)
            {
                Destroy(borderTexture);
            }

            if (boundaryLine != null && boundaryLine.material != null)
            {
                Destroy(boundaryLine.material);
            }
        }
    }
}
