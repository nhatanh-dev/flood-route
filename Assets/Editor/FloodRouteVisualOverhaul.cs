using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FloodRouteVisualOverhaul
{
    private static TMP_FontAsset statusFont;
    private static Material cleanTmpMaterial;

    [MenuItem("Window/Flood Route/Apply Visual Overhaul")]
    public static void Apply()
    {
        CacheTmpAssets();

        RebuildPrefabBadge("Assets/Prefabs/Environment/House_Prefab.prefab", 1.35f);
        RebuildPrefabBadge("Assets/Prefabs/Environment/Tree_Prefab.prefab", 1.55f);
        RebuildSceneGameplayBadges();
        ApplyMaterialsLightingAndPost();
        FixEventSystem();
        RebuildEdgeDecoration();

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Flood Route visual overhaul applied.");
    }

    private static void CacheTmpAssets()
    {
        statusFont = TMP_Settings.defaultFontAsset;
        if (statusFont == null)
        {
            statusFont = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(asset => asset != null);
        }

        if (statusFont == null || statusFont.material == null)
        {
            Debug.LogWarning("Default TMP font asset was not found. Status text will rely on TMP fallback.");
            return;
        }

        cleanTmpMaterial = new Material(statusFont.material)
        {
            name = "MAT_TMP_Status_Clean_Runtime"
        };

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        }

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_OutlineSoftness))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        }

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        }

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        }

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        }

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlayDilate))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        }

        if (cleanTmpMaterial.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
        {
            cleanTmpMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
        }
    }

    private static void RebuildPrefabBadge(string path, float y)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            BuildStatusCanvas(root, y);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RebuildSceneGameplayBadges()
    {
        var gameplayNodes = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude)
            .Where(go => go != null && go.scene.IsValid())
            .Where(go =>
                go.name.StartsWith("House_Node", StringComparison.Ordinal) ||
                go.name.StartsWith("Tree_Node", StringComparison.Ordinal))
            .ToList();

        foreach (var go in gameplayNodes)
        {
            if (go == null)
            {
                continue;
            }

            BuildStatusCanvas(go, go.name.StartsWith("Tree_Node", StringComparison.Ordinal) ? 1.55f : 1.35f);
        }
    }

    private static void BuildStatusCanvas(GameObject root, float y)
    {
        RemoveStatusCanvases(root);

        var uiLayer = LayerMask.NameToLayer("UI");
        var canvasGo = new GameObject("Node_Status_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = uiLayer;
        canvasGo.transform.SetParent(root.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, y, 0f);
        canvasGo.transform.localRotation = Quaternion.Euler(30f, 45f, 0f);
        canvasGo.transform.localScale = Vector3.one;

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.sizeDelta = new Vector2(100f, 50f);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        var badgeGo = new GameObject("Badge_Container", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        badgeGo.layer = uiLayer;
        badgeGo.transform.SetParent(canvasGo.transform, false);

        var badgeRect = badgeGo.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.anchoredPosition3D = Vector3.zero;
        badgeRect.sizeDelta = new Vector2(100f, 50f);
        badgeRect.offsetMin = new Vector2(-50f, -25f);
        badgeRect.offsetMax = new Vector2(50f, 25f);

        var image = badgeGo.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 180f / 255f);
        image.raycastTarget = false;

        var layout = badgeGo.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = badgeGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = new GameObject("Status_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.layer = uiLayer;
        textGo.transform.SetParent(badgeGo.transform, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(100f, 50f);

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = "P: 2 | T: 3";
        text.richText = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 42f;
        text.color = Color.white;
        text.raycastTarget = false;
        if (statusFont != null)
        {
            text.font = statusFont;
        }

        if (cleanTmpMaterial != null)
        {
            text.fontSharedMaterial = cleanTmpMaterial;
        }

        badgeGo.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(badgeRect);
    }

    private static void RemoveStatusCanvases(GameObject root)
    {
        var canvases = root.GetComponentsInChildren<Canvas>(true)
            .Where(canvas => canvas.name == "Node_Status_Canvas")
            .ToList();

        foreach (var canvas in canvases)
        {
            UnityEngine.Object.DestroyImmediate(canvas.gameObject);
        }
    }

    private static void ApplyMaterialsLightingAndPost()
    {
        var waterMat = GetOrCreateLitMaterial(
            "Assets/Art/Environment/Materials/Water_Mat.mat",
            "Water_Mat",
            new Color(25f / 255f, 55f / 255f, 85f / 255f, 1f),
            0.1f,
            0.85f);

        var mudMat = GetOrCreateLitMaterial(
            "Assets/Art/Environment/Materials/MudBase_Mat.mat",
            "MudBase_Mat",
            new Color(110f / 255f, 85f / 255f, 60f / 255f, 1f),
            0f,
            0.2f);

        var waterObj = GameObject.Find("WaterPlane_Grid");
        if (waterObj != null)
        {
            waterObj.transform.position = new Vector3(waterObj.transform.position.x, -0.15f, waterObj.transform.position.z);
            var renderer = waterObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = waterMat;
            }
        }

        var baseObj = GameObject.Find("Diorama_Mud_Base");
        if (baseObj != null)
        {
            baseObj.transform.position = new Vector3(baseObj.transform.position.x, -1f, baseObj.transform.position.z);
            var renderer = baseObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mudMat;
            }
        }

        var lightObj = GameObject.Find("Warm Directional Light");
        if (lightObj != null)
        {
            lightObj.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
            var light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.95f, 0.85f, 1f);
                light.shadows = LightShadows.Soft;
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.25f, 0.28f, 0.3f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;

        var cameraObj = GameObject.Find("Iso Camera");
        if (cameraObj != null)
        {
            var camera = cameraObj.GetComponent<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.25f, 0.28f, 0.3f, 1f);
            }

            var cameraDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (cameraDataType != null)
            {
                var cameraData = cameraObj.GetComponent(cameraDataType) ?? cameraObj.AddComponent(cameraDataType);
                var property = cameraDataType.GetProperty("renderPostProcessing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    property.SetValue(cameraData, true);
                }
            }
        }

        ApplyVolumeOverrides();
    }

    private static Material GetOrCreateLitMaterial(string path, string name, Color color, float metallic, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        if (shader != null)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyVolumeOverrides()
    {
        var volumeObj = GameObject.Find("FloodRoute_PostProcess_GlobalVolume");
        if (volumeObj == null)
        {
            volumeObj = new GameObject("FloodRoute_PostProcess_GlobalVolume");
        }

        var volume = volumeObj.GetComponent<Volume>() ?? volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        if (volume.profile == null)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        var profile = volume.profile;

        UnityEngine.Rendering.Universal.Vignette vignette;
        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
        }

        vignette.active = true;
        vignette.color.Override(Color.black);
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.45f);

        UnityEngine.Rendering.Universal.Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        }

        bloom.active = true;
        bloom.threshold.Override(1f);
        bloom.intensity.Override(1.5f);

        UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments;
        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        }

        colorAdjustments.active = true;
        colorAdjustments.contrast.Override(15f);

        EditorUtility.SetDirty(profile);
    }

    private static void FixEventSystem()
    {
        var eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            return;
        }

        var standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            UnityEngine.Object.DestroyImmediate(standalone);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static void RebuildEdgeDecoration()
    {
        var decor = GameObject.Find("Diorama_Edge_Decoration") ?? new GameObject("Diorama_Edge_Decoration");
        for (var i = decor.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(decor.transform.GetChild(i).gameObject);
        }

        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Tree_Prefab.prefab");
        var debrisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/DebrisX3_Prefab.prefab");

        var treePositions = new[]
        {
            new Vector3(4.9f, -0.28f, 3.4f),
            new Vector3(5.4f, -0.35f, 3.8f),
            new Vector3(4.4f, -0.32f, 3.9f),
            new Vector3(5.1f, -0.45f, 4.35f),
            new Vector3(5.8f, -0.38f, 4.25f),
            new Vector3(4.65f, -0.40f, 4.75f),
            new Vector3(5.35f, -0.42f, 4.85f),
            new Vector3(6.1f, -0.48f, 3.6f),
            new Vector3(4.0f, -0.36f, 4.45f)
        };

        var treeScales = new[] { 1.15f, 0.9f, 0.85f, 1.05f, 0.8f, 0.95f, 1.2f, 0.75f, 0.88f };
        for (var i = 0; i < treePositions.Length; i++)
        {
            var tree = treePrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(treePrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            tree.name = "Decor_FloodedForest_Tree_" + (i + 1);
            tree.transform.SetParent(decor.transform, true);
            tree.transform.position = treePositions[i];
            tree.transform.rotation = Quaternion.Euler((i % 3 - 1) * 4f, 20f + i * 31f, i % 2 == 0 ? 3f : -4f);
            tree.transform.localScale = new Vector3(treeScales[i], treeScales[i] * (i == 6 ? 1.2f : 1f), treeScales[i]);
            RemoveStatusCanvases(tree);
        }

        var debrisPositions = new[]
        {
            new Vector3(-5.4f, -0.05f, -4.5f),
            new Vector3(-4.85f, 0.02f, -4.15f),
            new Vector3(-5.75f, 0.08f, -3.95f),
            new Vector3(-4.35f, 0.12f, -4.65f),
            new Vector3(-5.15f, 0.18f, -3.55f),
            new Vector3(-6.05f, 0.22f, -4.35f)
        };

        var debrisRotations = new[]
        {
            new Vector3(8f, 23f, 14f),
            new Vector3(12f, 145f, -9f),
            new Vector3(-6f, 82f, 18f),
            new Vector3(18f, 214f, 4f),
            new Vector3(-14f, 310f, 22f),
            new Vector3(5f, 265f, -18f)
        };

        var debrisScales = new[] { 1.05f, 0.9f, 1.2f, 0.85f, 1.1f, 0.95f };
        for (var i = 0; i < debrisPositions.Length; i++)
        {
            var debris = debrisPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(debrisPrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            debris.name = "Decor_DebrisPile_X3_" + (i + 1);
            debris.transform.SetParent(decor.transform, true);
            debris.transform.position = debrisPositions[i];
            debris.transform.rotation = Quaternion.Euler(debrisRotations[i]);
            debris.transform.localScale = new Vector3(debrisScales[i], debrisScales[i], debrisScales[i]);
            RemoveStatusCanvases(debris);
        }
    }
}
