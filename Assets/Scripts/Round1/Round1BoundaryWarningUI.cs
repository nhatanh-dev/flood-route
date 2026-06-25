using UnityEngine;
using UnityEngine.UI;

namespace Round1
{
    public class Round1BoundaryWarningUI : MonoBehaviour
    {
        public float maxAlpha = 0.7f;
        public float fadeSpeed = 3f;

        private Image borderImage;
        private LineRenderer boundaryLine;

        private float currentObstacleAlpha = 0f;
        private float currentBoundaryAlpha = 0f;
        private Vector3 lastHitPoint;
        private Vector3 lastHitNormal;

        private void Start()
        {
            // Create Canvas for UI Screen Vignette
            GameObject canvasGo = new GameObject("BoundaryWarningCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var borderGo = new GameObject("RedBorder");
            borderGo.transform.SetParent(canvasGo.transform, false);
            borderImage = borderGo.AddComponent<Image>();
            
            // Create a procedural red vignette texture
            borderImage.sprite = Sprite.Create(CreateBorderTexture(), new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
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
                        alpha = Mathf.SmoothStep(0f, 1f, (dist - 0.6f) / 0.4f);
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
        public void TriggerCollisionWarning(bool isBoundary, Vector3 hitPoint, Vector3 hitNormal)
        {
            Debug.Log($"[CollisionWarning] Triggered! isBoundary={isBoundary}, hit={hitPoint}");
            if (isBoundary)
            {
                currentBoundaryAlpha = 1f; // Full opacity
                lastHitPoint = hitPoint;
                lastHitNormal = hitNormal;
            }
            else
            {
                currentObstacleAlpha = maxAlpha; // UI vignette
            }
        }

        private void Update()
        {
            if (borderImage == null || boundaryLine == null) return;

            // 1. Fade alphas down continuously
            currentObstacleAlpha = Mathf.Max(0f, currentObstacleAlpha - Time.deltaTime * fadeSpeed);
            currentBoundaryAlpha = Mathf.Max(0f, currentBoundaryAlpha - Time.deltaTime * fadeSpeed);

            // 2. Apply UI Vignette alpha
            Color c = borderImage.color;
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
    }
}
