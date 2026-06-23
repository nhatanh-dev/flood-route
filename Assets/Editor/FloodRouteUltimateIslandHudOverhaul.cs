using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FloodRouteUltimateIslandHudOverhaul
{
    private const string TerrainPath = "Assets/Models/DisasterProps/Diorama_Terrain_Mesh.fbx";
    private const string ScreenshotPath = "Assets/Screenshots/FloodRoute_Ultimate_Island_HUD_Final.png";
    private static TMP_FontAsset font;

    [MenuItem("Window/Flood Route/Ultimate Island HUD Overhaul")]
    public static void Apply()
    {
        font = TMP_Settings.defaultFontAsset;

        ImportAndApplyTerrain();
        RestoreNodeMeshesAndHeights();
        PolishWaterAndLighting();
        RebuildTopBanner();
        ConfigurePlacards();
        CaptureScreenshot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("PHASE 3 EXTRA COMPLETE: Environment fully stylized into modular islands, HUD top banner deployed, and placard billboarding corrected to flat signboards.");
    }

    private static void ImportAndApplyTerrain()
    {
        AssetDatabase.ImportAsset(TerrainPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(TerrainPath) as ModelImporter;
        if (importer != null)
        {
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        var mesh = AssetDatabase.LoadAllAssetsAtPath(TerrainPath).OfType<Mesh>().FirstOrDefault(m => m.name.Contains("Diorama_Terrain_Mesh"));
        var terrain = GameObject.Find("Diorama_Mud_Base");
        if (terrain == null || mesh == null)
        {
            Debug.LogError("Could not apply Diorama_Terrain_Mesh to Diorama_Mud_Base.");
            return;
        }

        terrain.name = "Diorama_Mud_Base";
        terrain.transform.position = new Vector3(0f, -2f, 0f);
        terrain.transform.rotation = Quaternion.identity;
        terrain.transform.localScale = Vector3.one;

        var filter = terrain.GetComponent<MeshFilter>() ?? terrain.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        var renderer = terrain.GetComponent<MeshRenderer>() ?? terrain.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[]
        {
            GetOrCreateLitMaterial("Assets/Art/Environment/Materials/Terrain_Island_Grass_Mat.mat", "Terrain_Island_Grass_Mat", new Color(100f / 255f, 180f / 255f, 80f / 255f, 1f), 0f, 0.35f),
            GetOrCreateLitMaterial("Assets/Art/Environment/Materials/Terrain_Carved_Mud_Mat.mat", "Terrain_Carved_Mud_Mat", new Color(130f / 255f, 105f / 255f, 85f / 255f, 1f), 0f, 0.25f)
        };
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

    private static void RestoreNodeMeshesAndHeights()
    {
        foreach (var nodeName in new[] { "Base_Node_A", "House_Node_D", "House_Node_E", "Tree_Node_F", "Tree_Node_G" })
        {
            var node = GameObject.Find(nodeName);
            if (node == null) continue;
            node.transform.position = new Vector3(node.transform.position.x, 0f, node.transform.position.z);

            var visual = node.transform.Find("Substitution_Visual_Model");
            if (visual == null) continue;

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(15f, 15f, 15f);
            if (visual.childCount > 0)
            {
                var child = visual.GetChild(0);
                child.localRotation = Quaternion.identity;
                child.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            }
        }
    }

    private static void PolishWaterAndLighting()
    {
        var water = GameObject.Find("WaterPlane_Grid");
        if (water != null)
        {
            water.transform.position = new Vector3(0f, -0.15f, 0f);
            var renderer = water.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetOrCreateLitMaterial(
                    "Assets/Art/Environment/Materials/Water_Mat.mat",
                    "Water_Mat",
                    new Color(70f / 255f, 160f / 255f, 200f / 255f, 1f),
                    0.15f,
                    0.75f);
            }
        }

        var lightObj = GameObject.Find("Warm Directional Light");
        if (lightObj != null)
        {
            lightObj.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
            var light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.intensity = 1.25f;
                light.shadows = LightShadows.Soft;
            }
        }
    }

    private static void RebuildTopBanner()
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
        scaler.matchWidthOrHeight = 0.5f;

        for (var i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        }

        var banner = CreateUiObject("TopBanner_Background", canvas.transform, typeof(Image), typeof(HorizontalLayoutGroup));
        var rt = banner.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -80f);
        rt.offsetMax = Vector2.zero;

        var image = banner.GetComponent<Image>();
        image.color = new Color(40f / 255f, 40f / 255f, 50f / 255f, 0.85f);
        image.raycastTarget = false;

        var layout = banner.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(30, 170, 10, 10);
        layout.spacing = 40f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        AddBannerText(banner.transform, "Text_Turns", "TURNS: 10", Color.white, 28f, 150f);
        AddBannerText(banner.transform, "Text_Cargo", "CARGO: 1/3", Color.white, 28f, 170f);
        AddBannerText(banner.transform, "Text_Saved", "SAVED: 1", new Color(120f / 255f, 240f / 255f, 120f / 255f), 28f, 150f);
        AddBannerText(banner.transform, "Text_Missed", "MISSED: 0", new Color(240f / 255f, 120f / 255f, 120f / 255f), 28f, 170f);
        AddBannerText(banner.transform, "Text_Score", "SCORE: 250", new Color(1f, 215f / 255f, 100f / 255f), 28f, 190f);
        AddBannerText(banner.transform, "Text_Preview", "PREVIEW: [ON]", new Color(100f / 255f, 220f / 255f, 1f), 28f, 240f);

        var button = CreateUiObject("Button_Wait", banner.transform, typeof(Image), typeof(Button));
        var brt = button.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 0.5f);
        brt.anchoredPosition = new Vector2(-30f, -40f);
        brt.sizeDelta = new Vector2(120f, 50f);
        button.GetComponent<Image>().color = new Color(100f / 255f, 100f / 255f, 120f / 255f, 1f);
        button.GetComponent<Button>().interactable = true;

        var text = CreateUiObject("Text", button.transform, typeof(TextMeshProUGUI));
        Stretch(text.GetComponent<RectTransform>(), Vector2.zero);
        ConfigureText(text.GetComponent<TextMeshProUGUI>(), "[WAIT]", 24f, Color.white, TextAlignmentOptions.Center, true);
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

    private static void AddBannerText(Transform parent, string name, string value, Color color, float size, float width)
    {
        var go = CreateUiObject(name, parent, typeof(TextMeshProUGUI), typeof(LayoutElement));
        var layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 50f;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 50f);
        ConfigureText(go.GetComponent<TextMeshProUGUI>(), value, size, color, TextAlignmentOptions.Center, true);
    }

    private static TextMeshProUGUI ConfigureText(TextMeshProUGUI tmp, string value, float size, Color color, TextAlignmentOptions align, bool bold)
    {
        tmp.text = value;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void ConfigurePlacards()
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
        canvas.sortingOrder = 90;

        if (canvasGo.GetComponent<BillboardCanvas>() == null)
        {
            canvasGo.AddComponent<BillboardCanvas>();
        }

        var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var badge = canvasGo.transform.Find("Badge_Container");
        if (badge == null)
        {
            badge = CreateUiObject("Badge_Container", canvasGo.transform, typeof(Image), typeof(Outline)).transform;
        }

        badge.localPosition = new Vector3(0f, 0.8f, 0f);
        badge.localRotation = Quaternion.identity;
        badge.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        var rect = badge.GetComponent<RectTransform>() ?? badge.gameObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200f, 60f);

        var image = badge.GetComponent<Image>() ?? badge.gameObject.AddComponent<Image>();
        image.type = Image.Type.Simple;
        image.color = new Color(30f / 255f, 30f / 255f, 40f / 255f, 0.95f);
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
            status = CreateUiObject("Status_Text", badge, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        }

        Stretch(status.GetComponent<RectTransform>(), new Vector2(10f, 10f));
        ConfigureText(status, "P: 2 | T: 3", 32f, Color.white, TextAlignmentOptions.Center, true);
    }

    private static void Stretch(RectTransform rt, Vector2 padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = padding;
        rt.offsetMax = -padding;
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
