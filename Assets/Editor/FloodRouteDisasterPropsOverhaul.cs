using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FloodRouteDisasterPropsOverhaul
{
    private const string ModelFolder = "Assets/Models/DisasterProps";
    private const string PrefabFolder = "Assets/Prefabs/DisasterProps";

    [MenuItem("Window/Flood Route/Apply Disaster Props Overhaul")]
    public static void Apply()
    {
        Directory.CreateDirectory(ModelFolder);
        Directory.CreateDirectory(PrefabFolder);

        ConfigureBadges();
        ImportModels();
        RemoveStrayRootPropObjects();
        CreatePropPrefab("SubmergedPole");
        CreatePropPrefab("RuinedRoof");
        CreatePropPrefab("FloatingRuralCluster");
        ScatterDisasterClusters();
        VerifyAtmosphereSettings();
        CaptureReferenceScreenshot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Flood Route disaster props overhaul applied.");
    }

    private static void ConfigureBadges()
    {
        ConfigurePrefabBadge("Assets/Prefabs/Environment/House_Prefab.prefab");
        ConfigurePrefabBadge("Assets/Prefabs/Environment/Tree_Prefab.prefab");

        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (canvas.name == "Node_Status_Canvas")
            {
                ConfigureBadgeCanvas(canvas.gameObject);
            }
        }
    }

    private static void ConfigurePrefabBadge(string prefabPath)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var canvas = root.GetComponentsInChildren<Canvas>(true).FirstOrDefault(c => c.name == "Node_Status_Canvas");
            if (canvas != null)
            {
                ConfigureBadgeCanvas(canvas.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureBadgeCanvas(GameObject canvasGo)
    {
        canvasGo.transform.localScale = Vector3.one;

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
        }

        var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        var badge = canvasGo.transform.Find("Badge_Container");
        if (badge == null)
        {
            return;
        }

        badge.localPosition = new Vector3(0f, 0.3f, 0f);
        badge.localRotation = Quaternion.identity;
        badge.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var rect = badge.GetComponent<RectTransform>() ?? badge.gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 40f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = badge.GetComponent<Image>() ?? badge.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 180f / 255f);
        image.raycastTarget = false;

        var layout = badge.GetComponent<HorizontalLayoutGroup>() ?? badge.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = badge.GetComponent<ContentSizeFitter>() ?? badge.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var statusText = badge.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Status_Text");
        if (statusText == null)
        {
            var textGo = new GameObject("Status_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.layer = LayerMask.NameToLayer("UI");
            textGo.transform.SetParent(badge, false);
            statusText = textGo.GetComponent<TextMeshProUGUI>();
        }

        statusText.text = "P: 2 | T: 3";
        statusText.fontSize = 24f;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
        statusText.raycastTarget = false;

        var font = TMP_Settings.defaultFontAsset;
        if (font != null)
        {
            statusText.font = font;
            var mat = new Material(font.material)
            {
                name = "MAT_TMP_Status_NoOutline_Runtime"
            };
            ClearTmpEffects(mat);
            statusText.fontSharedMaterial = mat;
        }

        var textRect = statusText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(120f, 40f);
    }

    private static void ClearTmpEffects(Material mat)
    {
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty(ShaderUtilities.ID_OutlineWidth)) mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_OutlineSoftness)) mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_FaceDilate)) mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetX)) mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayOffsetY)) mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlayDilate)) mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        if (mat.HasProperty(ShaderUtilities.ID_UnderlaySoftness)) mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
    }

    private static void ImportModels()
    {
        foreach (var name in new[] { "SubmergedPole", "RuinedRoof", "FloatingRuralCluster" })
        {
            var path = $"{ModelFolder}/{name}.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"Missing model importer for {path}");
                continue;
            }

            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }
    }

    private static void CreatePropPrefab(string name)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{name}.fbx");
        if (model == null)
        {
            Debug.LogError($"Model not found: {name}");
            return;
        }

        var temp = new GameObject(name);
        var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        modelInstance.name = name + "_Model";
        modelInstance.transform.SetParent(temp.transform, false);

        var collider = temp.AddComponent<BoxCollider>();
        collider.enabled = false;

        PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabFolder}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(temp);
    }

    private static void RemoveStrayRootPropObjects()
    {
        foreach (var name in new[] { "SubmergedPole", "RuinedRoof", "FloatingRuralCluster" })
        {
            var go = GameObject.Find(name);
            if (go != null && go.transform.parent == null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    private static void ScatterDisasterClusters()
    {
        var root = GameObject.Find("Diorama_Edge_Decoration") ?? new GameObject("Diorama_Edge_Decoration");

        for (var i = root.transform.childCount - 1; i >= 0; i--)
        {
            var child = root.transform.GetChild(i);
            if (child.name.StartsWith("DisasterCluster_", StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        var pole = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/SubmergedPole.prefab");
        var roof = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/RuinedRoof.prefab");
        var rural = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/FloatingRuralCluster.prefab");

        CreateCluster(root.transform, "DisasterCluster_North", new Vector3(2.5f, -0.15f, 3.5f),
            new PropPlacement(pole, "SubmergedPole_North", new Vector3(2.0f, -0.15f, 3.2f), new Vector3(0f, 15f, -25f)),
            new PropPlacement(rural, "FloatingRuralCluster_North", new Vector3(3.2f, -0.1f, 3.8f), new Vector3(0f, -30f, 0f)));

        CreateCluster(root.transform, "DisasterCluster_Northeast", new Vector3(3.2f, -0.15f, 2.2f),
            new PropPlacement(roof, "RuinedRoof_Northeast", new Vector3(3.5f, -0.08f, 2.0f), new Vector3(15f, 45f, 0f)),
            new PropPlacement(rural, "FloatingRuralCluster_Northeast", new Vector3(2.8f, -0.1f, 2.5f), new Vector3(0f, 60f, 0f)));

        CreateCluster(root.transform, "DisasterCluster_East", new Vector3(3.8f, -0.15f, 0.5f),
            new PropPlacement(pole, "SubmergedPole_East", new Vector3(4.0f, -0.15f, 0.2f), new Vector3(0f, -20f, -25f)));

        CreateCluster(root.transform, "DisasterCluster_Southeast", new Vector3(3.0f, -0.15f, -2.0f),
            new PropPlacement(roof, "RuinedRoof_Southeast", new Vector3(3.2f, -0.08f, -2.2f), new Vector3(20f, -30f, 0f)),
            new PropPlacement(rural, "FloatingRuralCluster_Southeast", new Vector3(2.5f, -0.1f, -1.8f), new Vector3(0f, 80f, 0f)));

        CreateCluster(root.transform, "DisasterCluster_Southwest", new Vector3(0.5f, -0.15f, -3.2f),
            new PropPlacement(pole, "SubmergedPole_Southwest", new Vector3(0.2f, -0.15f, -3.0f), new Vector3(0f, 10f, -25f)),
            new PropPlacement(rural, "FloatingRuralCluster_Southwest", new Vector3(1.0f, -0.1f, -3.5f), new Vector3(0f, 120f, 0f)));

        CreateCluster(root.transform, "DisasterCluster_West", new Vector3(-2.0f, -0.15f, -1.0f),
            new PropPlacement(roof, "RuinedRoof_West", new Vector3(-2.2f, -0.08f, -1.2f), new Vector3(25f, 120f, 0f)),
            new PropPlacement(rural, "FloatingRuralCluster_West", new Vector3(-1.5f, -0.1f, -0.8f), new Vector3(0f, 200f, 0f)));
    }

    private static void CreateCluster(Transform root, string name, Vector3 position, params PropPlacement[] props)
    {
        var holder = new GameObject(name);
        holder.transform.SetParent(root, true);
        holder.transform.position = position;

        foreach (var prop in props)
        {
            if (prop.Prefab == null)
            {
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prop.Prefab);
            instance.name = prop.Name;
            instance.transform.SetParent(holder.transform, true);
            instance.transform.position = prop.Position;
            instance.transform.rotation = Quaternion.Euler(prop.Rotation);
        }
    }

    private static void VerifyAtmosphereSettings()
    {
        var water = GameObject.Find("WaterPlane_Grid");
        if (water != null)
        {
            water.transform.position = new Vector3(water.transform.position.x, -0.15f, water.transform.position.z);
        }

        var mud = GameObject.Find("Diorama_Mud_Base");
        if (mud != null)
        {
            mud.transform.position = new Vector3(mud.transform.position.x, -1f, mud.transform.position.z);
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

    private static void CaptureReferenceScreenshot()
    {
        var camera = GameObject.Find("Iso Camera")?.GetComponent<Camera>();
        if (camera == null)
        {
            Debug.LogWarning("Iso Camera not found; skipped reference screenshot.");
            return;
        }

        Directory.CreateDirectory("Assets/Screenshots");
        var rt = new RenderTexture(1600, 900, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        camera.targetTexture = rt;
        RenderTexture.active = rt;
        camera.Render();

        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes("Assets/Screenshots/FloodRoute_DisasterProps_Reference.png", texture.EncodeToPNG());

        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(rt);
    }

    private readonly struct PropPlacement
    {
        public PropPlacement(GameObject prefab, string name, Vector3 position, Vector3 rotation)
        {
            Prefab = prefab;
            Name = name;
            Position = position;
            Rotation = rotation;
        }

        public GameObject Prefab { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }
    }
}
