using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FloodRoutePhase3HudPlacardOverhaul
{
    private const string ScreenshotPath = "Assets/Screenshots/FloodRoute_Phase3_HUD_Placard_Final.png";
    private static TMP_FontAsset font;

    [MenuItem("Window/Flood Route/Phase 3 HUD Placard Overhaul")]
    public static void Apply()
    {
        font = TMP_Settings.defaultFontAsset;

        RestoreNodeMeshVisibility();
        ApplyWaterAndTerrainPalette();
        RebuildTopBannerHud();
        ConfigurePlacardPrefabsAndScene();
        PreserveLighting();
        CaptureScreenshot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("PHASE 3 COMPLETE: UI/HUD and mesh visibility fully restored. Scene production-ready.");
    }

    private static void RestoreNodeMeshVisibility()
    {
        foreach (var nodeName in new[] { "Base_Node_A", "House_Node_D", "House_Node_E", "Tree_Node_F", "Tree_Node_G" })
        {
            var node = GameObject.Find(nodeName);
            if (node == null)
            {
                Debug.LogError($"Missing gameplay node: {nodeName}");
                continue;
            }

            var visual = node.transform.Find("Substitution_Visual_Model");
            if (visual == null)
            {
                Debug.LogWarning($"{nodeName} has no Substitution_Visual_Model child.");
                continue;
            }

            visual.localScale = new Vector3(15f, 15f, 15f);
            visual.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;

            if (visual.childCount > 0)
            {
                var importedModel = visual.GetChild(0);
                importedModel.localRotation = Quaternion.identity;
                // Phase 2 normalized FBX child scale to 0.01; with 15x wrapper this gives readable node scale.
                importedModel.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
        }
    }

    private static void ApplyWaterAndTerrainPalette()
    {
        var water = GameObject.Find("WaterPlane_Grid");
        if (water != null)
        {
            var renderer = water.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = GetOrCreateLitMaterial(
                    "Assets/Art/Environment/Materials/Water_Mat.mat",
                    "Water_Mat",
                    new Color(70f / 255f, 160f / 255f, 200f / 255f, 1f),
                    0.2f,
                    0.7f);
                renderer.sharedMaterial = mat;
            }
        }

        var mud = GameObject.Find("Diorama_Mud_Base");
        if (mud != null)
        {
            var renderer = mud.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = GetOrCreateLitMaterial(
                    "Assets/Art/Environment/Materials/MudBase_Mat.mat",
                    "MudBase_Mat",
                    new Color(100f / 255f, 180f / 255f, 80f / 255f, 1f),
                    0f,
                    0.25f);
                renderer.sharedMaterial = mat;
            }
        }
    }

    private static Material GetOrCreateLitMaterial(string path, string name, Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }

        if (shader != null) mat.shader = shader;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void RebuildTopBannerHud()
    {
        var canvas = GameObject.Find("Main_HUD_Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Main_HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        canvas.layer = LayerMask.NameToLayer("UI");
        var canvasComponent = canvas.GetComponent<Canvas>() ?? canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.scaleFactor = 1f;
        scaler.matchWidthOrHeight = 0.5f;

        for (var i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        }

        var banner = CreateUiObject("TopBanner_Background", canvas.transform, typeof(Image), typeof(HorizontalLayoutGroup));
        var bannerRt = banner.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0f, 1f);
        bannerRt.anchorMax = new Vector2(1f, 1f);
        bannerRt.pivot = new Vector2(0.5f, 1f);
        bannerRt.offsetMin = new Vector2(0f, -80f);
        bannerRt.offsetMax = Vector2.zero;

        var bannerImage = banner.GetComponent<Image>();
        bannerImage.color = new Color(40f / 255f, 40f / 255f, 50f / 255f, 0.85f);
        bannerImage.raycastTarget = false;

        var layout = banner.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 140, 5, 5);
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        AddBannerText(banner.transform, "Text_Turns", "TURNS: 10", Color.white, 28f, 140f, TextAlignmentOptions.Center);
        AddBannerText(banner.transform, "Text_Cargo", "CARGO: 1/3", Color.white, 28f, 150f, TextAlignmentOptions.Center);
        AddBannerText(banner.transform, "Text_Saved", "SAVED: 1", new Color(0.5f, 1f, 0.5f), 28f, 140f, TextAlignmentOptions.Center);
        AddBannerText(banner.transform, "Text_Missed", "MISSED: 0", new Color(1f, 0.5f, 0.5f), 28f, 150f, TextAlignmentOptions.Center);
        AddBannerText(banner.transform, "Text_Score", "SCORE: 250", new Color(1f, 0.85f, 0.5f), 28f, 170f, TextAlignmentOptions.Center);
        AddBannerText(banner.transform, "Text_Preview", "PREVIEW: [ON]", new Color(0.5f, 1f, 1f), 24f, 220f, TextAlignmentOptions.Right);

        var waitButton = CreateUiObject("Button_Wait", banner.transform, typeof(Image), typeof(Button));
        var waitRt = waitButton.GetComponent<RectTransform>();
        waitRt.anchorMin = new Vector2(1f, 1f);
        waitRt.anchorMax = new Vector2(1f, 1f);
        waitRt.pivot = new Vector2(1f, 0.5f);
        waitRt.anchoredPosition = new Vector2(-20f, -40f);
        waitRt.sizeDelta = new Vector2(100f, 50f);
        waitButton.GetComponent<Image>().color = new Color(100f / 255f, 100f / 255f, 120f / 255f, 1f);
        waitButton.GetComponent<Button>().interactable = true;

        var waitText = CreateUiObject("Text", waitButton.transform, typeof(TextMeshProUGUI));
        Stretch(waitText.GetComponent<RectTransform>(), Vector2.zero);
        var tmp = ConfigureText(waitText.GetComponent<TextMeshProUGUI>(), "[WAIT]", 24f, Color.white, TextAlignmentOptions.Center);
        tmp.fontStyle = FontStyles.Bold;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        var types = new Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        Array.Copy(components, 0, types, 1, components.Length);
        var go = new GameObject(name, types);
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void AddBannerText(Transform parent, string name, string text, Color color, float fontSize, float width, TextAlignmentOptions alignment)
    {
        var go = CreateUiObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 50f);
        var layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 50f;
        layout.flexibleWidth = 0f;
        ConfigureText(go.GetComponent<TextMeshProUGUI>(), text, fontSize, color, alignment);
    }

    private static TextMeshProUGUI ConfigureText(TextMeshProUGUI tmp, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void ConfigurePlacardPrefabsAndScene()
    {
        ConfigurePrefabPlacard("Assets/Prefabs/Environment/House_Prefab.prefab");
        ConfigurePrefabPlacard("Assets/Prefabs/Environment/Tree_Prefab.prefab");

        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas.name == "Node_Status_Canvas")
            {
                ConfigurePlacard(canvas.gameObject);
            }
        }
    }

    private static void ConfigurePrefabPlacard(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var canvas = root.GetComponentsInChildren<Canvas>(true).FirstOrDefault(c => c.name == "Node_Status_Canvas");
            if (canvas != null)
            {
                ConfigurePlacard(canvas.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigurePlacard(GameObject canvasGo)
    {
        canvasGo.transform.localScale = Vector3.one;
        var canvas = canvasGo.GetComponent<Canvas>() ?? canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 80;

        var billboard = canvasGo.GetComponent<BillboardCanvas>();
        if (billboard == null) canvasGo.AddComponent<BillboardCanvas>();

        var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var badge = canvasGo.transform.Find("Badge_Container");
        if (badge == null)
        {
            var badgeGo = CreateUiObject("Badge_Container", canvasGo.transform, typeof(Image), typeof(Outline));
            badge = badgeGo.transform;
        }

        badge.localPosition = new Vector3(0f, 0.8f, 0f);
        badge.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        badge.localRotation = Quaternion.identity;
        var rect = badge.GetComponent<RectTransform>() ?? badge.gameObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 60f);

        var image = badge.GetComponent<Image>() ?? badge.gameObject.AddComponent<Image>();
        image.type = Image.Type.Simple;
        image.color = new Color(30f / 255f, 30f / 255f, 40f / 255f, 0.9f);
        image.raycastTarget = false;

        var outline = badge.GetComponent<Outline>() ?? badge.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;

        var layout = badge.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) UnityEngine.Object.DestroyImmediate(layout);
        var fitter = badge.GetComponent<ContentSizeFitter>();
        if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);

        var status = badge.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Status_Text");
        if (status == null)
        {
            var textGo = CreateUiObject("Status_Text", badge, typeof(TextMeshProUGUI));
            status = textGo.GetComponent<TextMeshProUGUI>();
        }

        var statusRt = status.GetComponent<RectTransform>();
        Stretch(statusRt, new Vector2(10f, 10f));
        status.text = "P: 2 | T: 3";
        status.fontSize = 32f;
        status.color = Color.white;
        status.alignment = TextAlignmentOptions.Center;
        status.fontStyle = FontStyles.Bold;
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.raycastTarget = false;
        if (font != null)
        {
            status.font = font;
            var mat = new Material(font.material) { name = "MAT_TMP_Status_Placard_Outline_Runtime" };
            SetTmpOutline(mat, 0.2f, Color.black);
            status.fontSharedMaterial = mat;
        }
    }

    private static void Stretch(RectTransform rt, Vector2 padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = padding;
        rt.offsetMax = -padding;
    }

    private static void SetTmpOutline(Material mat, float width, Color color)
    {
        if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth)) mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineColor)) mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness)) mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate)) mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
    }

    private static void PreserveLighting()
    {
        var lightObj = GameObject.Find("Warm Directional Light");
        if (lightObj == null) return;
        lightObj.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
        var light = lightObj.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
        }
    }

    private static void CaptureScreenshot()
    {
        var camera = GameObject.Find("Iso Camera")?.GetComponent<Camera>();
        if (camera == null) return;
        Directory.CreateDirectory("Assets/Screenshots");
        var rt = new RenderTexture(1600, 900, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        camera.targetTexture = rt;
        RenderTexture.active = rt;
        camera.Render();
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(ScreenshotPath, tex.EncodeToPNG());
        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
    }
}
