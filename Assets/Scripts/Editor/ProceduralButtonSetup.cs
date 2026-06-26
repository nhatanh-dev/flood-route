#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

public class ProceduralButtonSetup : MonoBehaviour
{
    const string kOutputDir = "Assets/Sprites/Procedural";

    static readonly Color kBtnNormal = new Color(0.239f, 0.169f, 0.102f, 1f);
    static readonly Color kBtnHighlight = new Color(0.353f, 0.243f, 0.133f, 1f);
    static readonly Color kBtnPressed = new Color(0.165f, 0.110f, 0.055f, 1f);
    static readonly Color kBtnSelected = new Color(0.353f, 0.243f, 0.133f, 1f);
    static readonly Color kBtnDisabled = new Color(0.118f, 0.078f, 0.063f, 0.627f);
    static readonly Color kOutlineColor = new Color(0.910f, 0.459f, 0.227f, 1f);

    [MenuItem("FloodRoute/Setup Button Sprites")]
    static void SetupButtons()
    {
        if (!AssetDatabase.IsValidFolder(kOutputDir))
        {
            Directory.CreateDirectory(kOutputDir);
            AssetDatabase.Refresh();
        }

        Sprite btnSprite = CreateAndSaveSprite("BtnBackground", kBtnNormal, kOutputDir);
        Sprite outlineSprite = CreateAndSaveSprite("BtnOutlineFocus", kOutlineColor, kOutputDir);

        string[] btnNames = { "BtnStart", "BtnInstructions", "BtnQuit" };
        foreach (var btnName in btnNames)
        {
            var btnGO = GameObject.Find(btnName);
            if (btnGO == null) { Debug.LogWarning($"[ButtonSetup] Could not find '{btnName}'"); continue; }

            ConfigureButton(btnGO, btnSprite);
            ConfigureOutlineFocus(btnGO, outlineSprite);
            Debug.Log($"[ButtonSetup] Configured '{btnName}'");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ButtonSetup] Done. All buttons configured.");
    }

    static Sprite CreateAndSaveSprite(string spriteName, Color color, string dir)
    {
        string path = $"{dir}/{spriteName}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = Vector4.zero;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void ConfigureButton(GameObject btnGO, Sprite bgSprite)
    {
        var bgTransform = btnGO.transform.Find("Background");
        GameObject bgGO = bgTransform != null ? bgTransform.gameObject : CreateChildUI(btnGO, "Background");

        var img = bgGO.GetComponent<Image>() ?? bgGO.AddComponent<Image>();
        img.sprite = bgSprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var rt = bgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsFirstSibling();

        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(280, 52);

        var btn = btnGO.GetComponent<Button>() ?? btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;

        var colors = btn.colors;
        colors.normalColor = kBtnNormal;
        colors.highlightedColor = kBtnHighlight;
        colors.pressedColor = kBtnPressed;
        colors.selectedColor = kBtnSelected;
        colors.disabledColor = kBtnDisabled;
        colors.colorMultiplier = 1.0f;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
    }

    static void ConfigureOutlineFocus(GameObject btnGO, Sprite outlineSprite)
    {
        var outTransform = btnGO.transform.Find("OutlineFocus");
        GameObject outGO = outTransform != null ? outTransform.gameObject : CreateChildUI(btnGO, "OutlineFocus");

        var img = outGO.GetComponent<Image>() ?? outGO.AddComponent<Image>();
        img.sprite = outlineSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.910f, 0.459f, 0.227f, 0f);

        var rt = outGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-3f, -3f);
        rt.offsetMax = new Vector2(3f, 3f);
        rt.SetAsLastSibling();
    }

    static GameObject CreateChildUI(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
#endif
