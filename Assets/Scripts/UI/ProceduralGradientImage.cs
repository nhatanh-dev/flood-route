using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ProceduralGradientImage : MonoBehaviour
{
    [Header("Gradient Colors")]
    public Color topColor = Color.black;
    public Color bottomColor = Color.white;

    [Header("Resolution")]
    public int textureHeight = 64;

    void Awake()
    {
        var tex = new Texture2D(1, textureHeight, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[textureHeight];
        for (int i = 0; i < textureHeight; i++)
        {
            float t = (float)i / (textureHeight - 1);
            pixels[i] = Color.Lerp(bottomColor, topColor, t);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        var img = GetComponent<RawImage>();
        img.texture = tex;
        img.color = Color.white;
        img.material = null;
        img.uvRect = new Rect(0, 0, 1, 1);
        img.SetAllDirty();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(img);
#endif
    }
}
