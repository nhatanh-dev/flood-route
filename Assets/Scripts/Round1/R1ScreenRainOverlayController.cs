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
        [Range(-24f, 24f)] public float slantPixels = -5f;
        [Range(0f, 1f)] public float minDropAlpha = 0.12f;
        [Range(0f, 1f)] public float maxDropAlpha = 0.55f;
        [Range(0f, 1f)] public float shortDropChance = 0.35f;
        public int randomSeed = 1207;

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

            Random.State previousState = Random.state;
            Random.InitState(randomSeed + gameObject.GetInstanceID());

            proceduralTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            proceduralTexture.wrapMode = TextureWrapMode.Repeat;
            proceduralTexture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[textureResolution * textureResolution];
            Color clear = new Color(1, 1, 1, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Draw soft diagonal streaks with slight variation so the overlay feels like rain,
            // not a flat white screen filter.
            for (int i = 0; i < dropCount; i++)
            {
                int startX = Random.Range(0, textureResolution);
                int startY = Random.Range(0, textureResolution);
                bool shortDrop = Random.value < shortDropChance;
                float lengthMultiplier = shortDrop ? Random.Range(0.25f, 0.7f) : Random.Range(0.75f, 1.45f);
                int len = Mathf.Max(2, Mathf.RoundToInt(dropLength * lengthMultiplier));
                int width = Mathf.Max(1, Mathf.RoundToInt(dropWidth + Random.Range(-0.35f, 0.65f)));
                float alpha = Random.Range(minDropAlpha, maxDropAlpha);

                for (int y = 0; y < len; y++)
                {
                    float t = len <= 1 ? 1f : y / (float)(len - 1);
                    int pxCenter = Mathf.RoundToInt(startX + slantPixels * t);
                    int py = PositiveMod(startY + y, textureResolution);
                    float taperedAlpha = alpha * Mathf.Sin(t * Mathf.PI);

                    for (int x = 0; x < width; x++)
                    {
                        int px = PositiveMod(pxCenter + x, textureResolution);
                        Color existing = pixels[py * textureResolution + px];
                        float blendedAlpha = Mathf.Clamp01(existing.a + taperedAlpha);
                        pixels[py * textureResolution + px] = new Color(1f, 1f, 1f, blendedAlpha);
                    }
                }
            }

            proceduralTexture.SetPixels(pixels);
            proceduralTexture.Apply();

            rawImage.texture = proceduralTexture;
            Random.state = previousState;
        }

        private int PositiveMod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
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
