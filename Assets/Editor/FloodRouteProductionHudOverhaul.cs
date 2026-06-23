using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FloodRouteProductionHudOverhaul
{
    private const string IconFolder = "Assets/Art/UI/Generated";
    private static TMP_FontAsset font;

    [MenuItem("Window/Flood Route/Apply Production HUD Overhaul")]
    public static void Apply()
    {
        font = TMP_Settings.defaultFontAsset;
        Directory.CreateDirectory(IconFolder);

        GenerateIconSprites();
        ConfigureNodeBadges();
        BoostDisasterProps();
        RebuildMainHud();
        CaptureScreenshot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Flood Route production HUD and visual refinement applied.");
    }

    private static void ConfigureNodeBadges()
    {
        ConfigurePrefabBadge("Assets/Prefabs/Environment/House_Prefab.prefab", 2.1f);
        ConfigurePrefabBadge("Assets/Prefabs/Environment/Tree_Prefab.prefab", 1.7f);

        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas.name != "Node_Status_Canvas")
            {
                continue;
            }

            var rootName = canvas.transform.root.name;
            if (rootName.StartsWith("House_Node", StringComparison.Ordinal))
            {
                ConfigureBadgeCanvas(canvas.gameObject, 2.1f);
            }
            else if (rootName.StartsWith("Tree_Node", StringComparison.Ordinal))
            {
                ConfigureBadgeCanvas(canvas.gameObject, 1.7f);
            }
        }
    }

    private static void ConfigurePrefabBadge(string path, float y)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var canvas = root.GetComponentsInChildren<Canvas>(true).FirstOrDefault(c => c.name == "Node_Status_Canvas");
            if (canvas != null)
            {
                ConfigureBadgeCanvas(canvas.gameObject, y);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureBadgeCanvas(GameObject canvasGo, float y)
    {
        canvasGo.transform.localPosition = new Vector3(0f, y, 0f);
        canvasGo.transform.localScale = Vector3.one;

        var billboard = canvasGo.GetComponent<BillboardCanvas>();
        if (billboard == null)
        {
            canvasGo.AddComponent<BillboardCanvas>();
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 60;
        }

        var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var badge = canvasGo.transform.Find("Badge_Container");
        if (badge == null)
        {
            return;
        }

        badge.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        var rect = badge.GetComponent<RectTransform>() ?? badge.gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140f, 50f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var layout = badge.GetComponent<HorizontalLayoutGroup>() ?? badge.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var text = badge.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Status_Text");
        if (text == null)
        {
            return;
        }

        text.text = "P: 2 | T: 3";
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        if (font != null)
        {
            text.font = font;
            var mat = new Material(font.material) { name = "MAT_TMP_Status_NoOutline_Runtime" };
            ClearTmpEffects(mat);
            text.fontSharedMaterial = mat;
        }
    }

    private static void ClearTmpEffects(Material mat)
    {
        if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth)) mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness)) mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate)) mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX)) mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY)) mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayDilate)) mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness)) mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
    }

    private static void BoostDisasterProps()
    {
        var decor = GameObject.Find("Diorama_Edge_Decoration");
        if (decor != null)
        {
            foreach (Transform cluster in decor.transform)
            {
                if (!cluster.name.StartsWith("DisasterCluster_", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (Transform prop in cluster)
                {
                    prop.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                }
            }
        }

        SetMaterialEmission("Assets/Models/DisasterProps/SubmergedPole.fbx", new Color(20f / 255f, 20f / 255f, 20f / 255f), 0.1f);
        SetMaterialEmission("Assets/Models/DisasterProps/RuinedRoof.fbx", new Color(30f / 255f, 20f / 255f, 10f / 255f), 0.15f);
    }

    private static void SetMaterialEmission(string modelPath, Color color, float intensity)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        foreach (var mat in assets.OfType<Material>())
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", color * intensity);
            }

            EditorUtility.SetDirty(mat);
        }
    }

    private static void RebuildMainHud()
    {
        var canvas = GameObject.Find("Main_HUD_Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Main_HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        var canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.layer = LayerMask.NameToLayer("UI");

        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RemoveChild(canvas.transform, "LeftPanel_RescueStatusDashboard");
        RemoveChild(canvas.transform, "StormPhaseTracker_Header");
        RemoveChild(canvas.transform, "PathPreviewQueue_Bar");

        // Hide old raw HUD blocks without deleting unrelated future UI.
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "HUD_Panel" || child.name == "Panel" || child.name.Contains("Turn_Text"))
            {
                child.gameObject.SetActive(false);
            }
        }

        BuildLeftDashboard(canvas.transform);
        BuildStormHeader(canvas.transform);
        BuildPathPreview(canvas.transform);
    }

    private static void RemoveChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite = null)
    {
        var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions alignment, bool bold = false)
    {
        if (go.GetComponent<Image>() != null && go.GetComponent<TextMeshProUGUI>() == null)
        {
            var child = new GameObject("Text", typeof(RectTransform));
            child.layer = LayerMask.NameToLayer("UI");
            child.transform.SetParent(go.transform, false);
            var childRt = child.GetComponent<RectTransform>();
            childRt.anchorMin = Vector2.zero;
            childRt.anchorMax = Vector2.one;
            childRt.offsetMin = Vector2.zero;
            childRt.offsetMax = Vector2.zero;
            go = child;
        }

        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        if (font != null)
        {
            tmp.font = font;
        }

        return tmp;
    }

    private static void BuildLeftDashboard(Transform canvas)
    {
        var panel = CreateUiObject("LeftPanel_RescueStatusDashboard", canvas);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -40f);
        rt.sizeDelta = new Vector2(300f, 280f);
        AddImage(panel, new Color(20f / 255f, 20f / 255f, 20f / 255f, 0.85f), LoadSprite("PanelRound"));

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddStatRow(panel.transform, "Icon_Turns", "TURNS LEFT", "15", new Color(1f, 1f, 100f / 255f));
        AddStatRow(panel.transform, "Icon_Cargo", "CARGO", "0/3", new Color(200f / 255f, 200f / 255f, 200f / 255f));
        AddStatRow(panel.transform, "Icon_Rescued", "RESCUED", "0", new Color(100f / 255f, 200f / 255f, 100f / 255f));
        AddStatRow(panel.transform, "Icon_Lost", "LOST", "0", new Color(1f, 100f / 255f, 100f / 255f));
        AddStatRow(panel.transform, "Icon_Score", "SCORE", "0", new Color(1f, 215f / 255f, 100f / 255f));
        AddStatRow(panel.transform, "Icon_WaterLevel", "WATER LVL", "6/10", new Color(100f / 255f, 150f / 255f, 1f));
    }

    private static void AddStatRow(Transform parent, string iconName, string label, string value, Color valueColor)
    {
        var row = CreateUiObject("Row_" + label.Replace(" ", ""), parent);
        var rt = row.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 36f);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var icon = CreateUiObject("Icon", row.transform);
        var iconRt = icon.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(36f, 36f);
        icon.AddComponent<LayoutElement>().preferredWidth = 36f;
        AddImage(icon, Color.white, LoadSprite(iconName));

        var labelText = CreateUiObject("Label", row.transform);
        labelText.AddComponent<LayoutElement>().preferredWidth = 150f;
        AddText(labelText, label, 14f, Color.white, TextAlignmentOptions.Left, true);

        var valueText = CreateUiObject("Value", row.transform);
        var valueLayout = valueText.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 70f;
        AddText(valueText, value, 18f, valueColor, TextAlignmentOptions.Right, true);
    }

    private static void BuildStormHeader(Transform canvas)
    {
        var header = CreateUiObject("StormPhaseTracker_Header", canvas);
        var rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(500f, 70f);
        AddImage(header, new Color(40f / 255f, 30f / 255f, 20f / 255f, 0.9f), LoadSprite("PanelRound"));

        var layout = header.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 8, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateUiObject("Title_StormIntensity", header.transform);
        title.AddComponent<LayoutElement>().preferredHeight = 22f;
        AddText(title, "STORM INTENSITY", 16f, new Color(1f, 150f / 255f, 0f), TextAlignmentOptions.Center, true);

        var row = CreateUiObject("LevelCountdown_Row", header.transform);
        row.AddComponent<LayoutElement>().preferredHeight = 28f;
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 15f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var badge = CreateUiObject("Level_Badge", row.transform);
        badge.AddComponent<LayoutElement>().preferredWidth = 130f;
        AddImage(badge, new Color(200f / 255f, 80f / 255f, 40f / 255f, 1f), LoadSprite("PanelRound"));
        AddText(badge, "LEVEL 2", 20f, Color.white, TextAlignmentOptions.Center, true);

        var barRoot = CreateUiObject("CountdownBar", row.transform);
        barRoot.AddComponent<LayoutElement>().preferredWidth = 300f;
        AddImage(barRoot, new Color(0.12f, 0.13f, 0.12f, 1f), LoadSprite("PanelRound"));
        var fill = CreateUiObject("Fill_RemainingTurns", barRoot.transform);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0.6f, 1f);
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        AddImage(fill, new Color(1f, 210f / 255f, 70f / 255f, 1f), LoadSprite("PanelRound"));
        AddText(barRoot, "Flood Rising in 3 turns", 14f, Color.white, TextAlignmentOptions.Center, false);
    }

    private static void BuildPathPreview(Transform canvas)
    {
        var bar = CreateUiObject("PathPreviewQueue_Bar", canvas);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 30f);
        rt.sizeDelta = new Vector2(600f, 100f);
        AddImage(bar, new Color(30f / 255f, 30f / 255f, 40f / 255f, 0.9f), LoadSprite("PanelRound"));

        var layout = bar.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var header = CreateUiObject("Header_PlannedRoute", bar.transform);
        header.AddComponent<LayoutElement>().preferredHeight = 20f;
        AddText(header, "PLANNED ROUTE: Step-by-step preview", 14f, new Color(150f / 255f, 200f / 255f, 1f), TextAlignmentOptions.Left, true);

        var slots = CreateUiObject("Slots_Container", bar.transform);
        slots.AddComponent<LayoutElement>().preferredHeight = 58f;
        var slotsLayout = slots.AddComponent<HorizontalLayoutGroup>();
        slotsLayout.spacing = 8f;
        slotsLayout.childAlignment = TextAnchor.MiddleCenter;
        slotsLayout.childControlWidth = false;
        slotsLayout.childControlHeight = true;
        slotsLayout.childForceExpandWidth = false;
        slotsLayout.childForceExpandHeight = false;

        for (var i = 0; i < 6; i++)
        {
            AddQueueSlot(slots.transform, i);
        }
    }

    private static void AddQueueSlot(Transform parent, int index)
    {
        var slot = CreateUiObject("QueueSlot_" + (index + 1), parent);
        var rt = slot.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(56f, 56f);
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 56f;
        le.preferredHeight = 56f;
        AddImage(slot, new Color(60f / 255f, 70f / 255f, 90f / 255f, index < 3 ? 1f : 0.45f), LoadSprite("Circle"));

        if (index < 3)
        {
            var icon = CreateUiObject("InnerIcon", slot.transform);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(30f, 30f);
            AddImage(icon, new Color(150f / 255f, 200f / 255f, 1f, 1f), LoadSprite(index == 0 ? "Icon_Cargo" : index == 1 ? "Icon_Rescued" : "Icon_WaterLevel"));

            var badge = CreateUiObject("StepNumber", slot.transform);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(2f, -2f);
            brt.sizeDelta = new Vector2(20f, 20f);
            AddImage(badge, new Color(200f / 255f, 100f / 255f, 50f / 255f, 1f), LoadSprite("Circle"));
            AddText(badge, (index + 1).ToString(), 12f, Color.white, TextAlignmentOptions.Center, true);
        }
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");
    }

    private static void GenerateIconSprites()
    {
        Directory.CreateDirectory(IconFolder);
        WriteSprite("PanelRound", 64, 64, (x, y, w, h) => RoundedRect(x, y, w, h, 14));
        WriteSprite("Circle", 64, 64, (x, y, w, h) => Circle(x, y, w, h));
        WriteSprite("Icon_Turns", 40, 40, DrawClock);
        WriteSprite("Icon_Cargo", 40, 40, DrawBoat);
        WriteSprite("Icon_Rescued", 40, 40, DrawBuoy);
        WriteSprite("Icon_Lost", 40, 40, DrawCross);
        WriteSprite("Icon_Score", 40, 40, DrawStar);
        WriteSprite("Icon_WaterLevel", 40, 40, DrawDrop);
        AssetDatabase.Refresh();

        foreach (var path in Directory.GetFiles(IconFolder, "*.png"))
        {
            var projectPath = path.Replace("\\", "/");
            if (projectPath.StartsWith(Application.dataPath))
            {
                projectPath = "Assets" + projectPath.Substring(Application.dataPath.Length);
            }

            var importer = AssetImporter.GetAtPath(projectPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static void WriteSprite(string name, int width, int height, Func<int, int, int, int, bool> draw)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, draw(x, y, width, height) ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        File.WriteAllBytes($"{IconFolder}/{name}.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static bool RoundedRect(int x, int y, int w, int h, int r)
    {
        var cx = x < r ? r : x > w - r - 1 ? w - r - 1 : x;
        var cy = y < r ? r : y > h - r - 1 ? h - r - 1 : y;
        var dx = x - cx;
        var dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    }

    private static bool Circle(int x, int y, int w, int h)
    {
        var dx = x - w / 2f + 0.5f;
        var dy = y - h / 2f + 0.5f;
        return dx * dx + dy * dy <= (w * 0.48f) * (h * 0.48f);
    }

    private static bool DrawClock(int x, int y, int w, int h)
    {
        var dx = x - 20;
        var dy = y - 20;
        var d = dx * dx + dy * dy;
        return (d < 15 * 15 && d > 12 * 12) || (Math.Abs(dx) < 2 && dy >= 0 && dy < 10) || (Math.Abs(dy - dx) < 2 && dx >= 0 && dx < 8);
    }

    private static bool DrawBoat(int x, int y, int w, int h)
    {
        return (y >= 10 && y <= 16 && x >= 9 && x <= 31 && Math.Abs(x - 20) + y < 38) || (y >= 17 && y <= 20 && x >= 12 && x <= 28);
    }

    private static bool DrawBuoy(int x, int y, int w, int h)
    {
        var dx = x - 20;
        var dy = y - 20;
        var d = dx * dx + dy * dy;
        return d < 15 * 15 && d > 8 * 8 || (Math.Abs(dx) < 3 && Math.Abs(dy) < 16) || (Math.Abs(dy) < 3 && Math.Abs(dx) < 16);
    }

    private static bool DrawCross(int x, int y, int w, int h)
    {
        return Math.Abs(x - y) < 3 || Math.Abs((w - 1 - x) - y) < 3;
    }

    private static bool DrawStar(int x, int y, int w, int h)
    {
        var cx = 20f;
        var cy = 20f;
        var a = Math.Atan2(y - cy, x - cx);
        var r = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        var limit = 8f + 7f * Math.Abs(Math.Cos(5 * a));
        return r < limit;
    }

    private static bool DrawDrop(int x, int y, int w, int h)
    {
        var dx = x - 20f;
        var dy = y - 15f;
        var bulb = dx * dx + dy * dy < 11f * 11f;
        var tip = y > 20 && Math.Abs(dx) < (36 - y) * 0.6f;
        return bulb || tip;
    }

    private static void CaptureScreenshot()
    {
        var camera = GameObject.Find("Iso Camera")?.GetComponent<Camera>();
        if (camera == null)
        {
            Debug.LogWarning("Iso Camera not found; skipped production HUD screenshot.");
            return;
        }

        Directory.CreateDirectory("Assets/Screenshots");
        var rt = new RenderTexture(1600, 900, 24);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        camera.targetTexture = rt;
        RenderTexture.active = rt;
        camera.Render();
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes("Assets/Screenshots/FloodRoute_ProductionHUD_Final.png", texture.EncodeToPNG());
        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(rt);
    }
}
