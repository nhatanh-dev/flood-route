using UnityEngine;
using UnityEngine.UI;

namespace Round1
{
    [RequireComponent(typeof(RawImage))]
    public class R1ScreenRainOverlayController : MonoBehaviour
    {
        [Header("Overlay Settings")]
        [Range(0f, 1f)] public float overlayAlpha = 0.15f;
        public Vector2 scrollSpeed = new Vector2(0.5f, 3f);
        public Vector2 tiling = new Vector2(4f, 4f);

        [Header("Appearance")]
        public Color rainColor = new Color(0.8f, 0.85f, 0.9f, 0.15f);
        public int textureResolution = 512;
        public int dropCount = 300;
        public float dropLength = 10f;
        public float dropWidth = 1f;

        private RawImage rawImage;
        private Texture2D proceduralTexture;
        private Vector2 currentOffset = Vector2.zero;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            GenerateRainTexture();
        }

        private void Start()
        {
            ApplySettings();
        }

        private void Update()
        {
            if (rawImage != null)
            {
                currentOffset += scrollSpeed * Time.deltaTime;
                currentOffset.x %= 1f;
                currentOffset.y %= 1f;
                
                Rect uvRect = rawImage.uvRect;
                uvRect.position = currentOffset;
                rawImage.uvRect = uvRect;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }

        public void ApplySettings()
        {
            if (rawImage != null)
            {
                Color c = rainColor;
                c.a = overlayAlpha;
                rawImage.color = c;
                
                Rect uvRect = rawImage.uvRect;
                uvRect.size = tiling;
                rawImage.uvRect = uvRect;
            }
        }

        private void GenerateRainTexture()
        {
            if (rawImage.texture != null) return; // Use existing texture if assigned

            proceduralTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            proceduralTexture.wrapMode = TextureWrapMode.Repeat;
            proceduralTexture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[textureResolution * textureResolution];
            Color clear = new Color(1, 1, 1, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Draw simple vertical lines for rain
            for (int i = 0; i < dropCount; i++)
            {
                int startX = Random.Range(0, textureResolution);
                int startY = Random.Range(0, textureResolution);
                int len = Random.Range((int)(dropLength * 0.5f), (int)(dropLength * 1.5f));
                
                float alpha = Random.Range(0.2f, 1f);
                Color dropColor = new Color(1, 1, 1, alpha);

                for (int y = 0; y < len; y++)
                {
                    int py = (startY + y) % textureResolution;
                    for (int x = 0; x < dropWidth; x++)
                    {
                        int px = (startX + x) % textureResolution;
                        pixels[py * textureResolution + px] = dropColor;
                    }
                }
            }

            proceduralTexture.SetPixels(pixels);
            proceduralTexture.Apply();

            rawImage.texture = proceduralTexture;
        }

        private void OnDestroy()
        {
            if (proceduralTexture != null)
            {
                Destroy(proceduralTexture);
            }
        }
    }
}
