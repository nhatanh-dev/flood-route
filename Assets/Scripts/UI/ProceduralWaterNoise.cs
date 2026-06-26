using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ProceduralWaterNoise : MonoBehaviour
{
    [Header("Stripe Colors")]
    public Color darkColor = new Color(0.173f, 0.298f, 0.369f, 1f);
    public Color lightColor = new Color(0.373f, 0.557f, 0.627f, 1f);

    [Header("Texture Size")]
    public int stripeCount = 8;
    public int texWidth = 4;
    public int texHeight = 64;

    [Header("Tiling")]
    public float tilingX = 12f;
    public float tilingY = 1f;

    void Awake()
    {
        var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;

        var pixels = new Color[texWidth * texHeight];
        int rowsPerStripe = Mathf.Max(1, texHeight / stripeCount);

        for (int y = 0; y < texHeight; y++)
        {
            float sineNudge = Mathf.Sin(y * 0.45f) * 0.12f;
            float t = ((y / rowsPerStripe) % 2 == 0) ? 0.0f + sineNudge : 1.0f + sineNudge;
            t = Mathf.Clamp01(t);
            Color rowColor = Color.Lerp(darkColor, lightColor, t);

            for (int x = 0; x < texWidth; x++)
                pixels[y * texWidth + x] = rowColor;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        var img = GetComponent<RawImage>();
        img.texture = tex;
        img.color = Color.white;
        img.material = null;
        img.uvRect = new Rect(0f, 0f, tilingX, tilingY);
        img.SetAllDirty();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(img);
#endif
    }
}
