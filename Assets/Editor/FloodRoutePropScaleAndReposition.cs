using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FloodRoutePropScaleAndReposition
{
    private const string ModelFolder = "Assets/Models/DisasterProps";
    private const string PrefabFolder = "Assets/Prefabs/DisasterProps";

    [MenuItem("Window/Flood Route/Apply Prop Scale And Reposition")]
    public static void Apply()
    {
        Directory.CreateDirectory(PrefabFolder);

        ReimportModels();
        RebuildPropPrefab("SubmergedPole");
        RebuildPropPrefab("RuinedRoof");
        RebuildPropPrefab("FloatingRuralCluster");
        UpdateNodeBadges();
        RepositionCornerVignettes();
        PreserveAtmosphere();
        CaptureScreenshot();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Flood Route prop scale, badge visibility, and corner vignette repositioning applied.");
    }

    private static void ReimportModels()
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
            importer.globalScale = 0.01f;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }
    }

    private static void RebuildPropPrefab(string name)
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
        modelInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        var collider = temp.AddComponent<BoxCollider>();
        collider.enabled = false;
        PrefabUtility.SaveAsPrefabAsset(temp, $"{PrefabFolder}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(temp);
    }

    private static void UpdateNodeBadges()
    {
        UpdatePrefabBadge("Assets/Prefabs/Environment/House_Prefab.prefab", 1.8f);
        UpdatePrefabBadge("Assets/Prefabs/Environment/Tree_Prefab.prefab", 1.4f);

        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas.name != "Node_Status_Canvas")
            {
                continue;
            }

            var root = canvas.transform.root.name;
            if (root.StartsWith("House_Node", StringComparison.Ordinal))
            {
                ConfigureBadge(canvas.gameObject, 1.8f);
            }
            else if (root.StartsWith("Tree_Node", StringComparison.Ordinal))
            {
                ConfigureBadge(canvas.gameObject, 1.4f);
            }
        }
    }

    private static void UpdatePrefabBadge(string prefabPath, float y)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var canvas = root.GetComponentsInChildren<Canvas>(true).FirstOrDefault(c => c.name == "Node_Status_Canvas");
            if (canvas != null)
            {
                ConfigureBadge(canvas.gameObject, y);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureBadge(GameObject canvasGo, float y)
    {
        canvasGo.transform.localPosition = new Vector3(0f, y, 0f);
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

        badge.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        var rect = badge.GetComponent<RectTransform>() ?? badge.gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 40f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var statusText = badge.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Status_Text");
        if (statusText != null)
        {
            statusText.text = "P: 2 | T: 3";
            statusText.fontSize = 24f;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.textWrappingMode = TextWrappingModes.NoWrap;
            if (statusText.font != null)
            {
                var mat = new Material(statusText.font.material) { name = "MAT_TMP_Status_NoOutline_Runtime" };
                ClearTmpEffects(mat);
                statusText.fontSharedMaterial = mat;
            }
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

    private static void RepositionCornerVignettes()
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

        RemoveStrayRootProps();

        var pole = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/SubmergedPole.prefab");
        var roof = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/RuinedRoof.prefab");
        var rural = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/FloatingRuralCluster.prefab");

        CreateCluster(root.transform, "DisasterCluster_Northwest", new Vector3(-3.5f, -0.15f, 3.0f),
            new Placement(pole, "SubmergedPole_A", new Vector3(-3.8f, -0.15f, 3.2f), new Vector3(12f, -15f, -8f)),
            new Placement(rural, "FloatingRuralCluster_A", new Vector3(-3.0f, -0.12f, 2.8f), new Vector3(0f, 45f, 0f)));

        CreateCluster(root.transform, "DisasterCluster_Northeast", new Vector3(3.5f, -0.15f, 3.0f),
            new Placement(roof, "RuinedRoof_A", new Vector3(3.8f, -0.08f, 3.2f), new Vector3(18f, 60f, 5f)),
            new Placement(pole, "SubmergedPole_B", new Vector3(3.0f, -0.15f, 2.8f), new Vector3(-10f, 30f, -12f)));

        CreateCluster(root.transform, "DisasterCluster_Southwest", new Vector3(-3.5f, -0.15f, -3.0f),
            new Placement(rural, "FloatingRuralCluster_B", new Vector3(-3.8f, -0.12f, -3.2f), new Vector3(0f, 190f, 0f)),
            new Placement(roof, "RuinedRoof_B", new Vector3(-3.0f, -0.08f, -2.8f), new Vector3(15f, -120f, 0f)));

        CreateCluster(root.transform, "DisasterCluster_Southeast", new Vector3(3.5f, -0.15f, -3.0f),
            new Placement(pole, "SubmergedPole_C", new Vector3(3.8f, -0.15f, -3.2f), new Vector3(8f, 170f, -15f)),
            new Placement(rural, "FloatingRuralCluster_C", new Vector3(3.0f, -0.12f, -2.8f), new Vector3(0f, 280f, 0f)));
    }

    private static void RemoveStrayRootProps()
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

    private static void CreateCluster(Transform root, string name, Vector3 position, params Placement[] placements)
    {
        var holder = new GameObject(name);
        holder.transform.SetParent(root, true);
        holder.transform.position = position;

        foreach (var placement in placements)
        {
            if (placement.Prefab == null)
            {
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(placement.Prefab);
            instance.name = placement.Name;
            instance.transform.SetParent(holder.transform, true);
            instance.transform.position = placement.Position;
            instance.transform.rotation = Quaternion.Euler(placement.Rotation);
            instance.transform.localScale = Vector3.one;
        }
    }

    private static void PreserveAtmosphere()
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

    private static void CaptureScreenshot()
    {
        var camera = GameObject.Find("Iso Camera")?.GetComponent<Camera>();
        if (camera == null)
        {
            Debug.LogWarning("Iso Camera not found; skipped final screenshot.");
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
        File.WriteAllBytes("Assets/Screenshots/FloodRoute_CornerVignettes_Final.png", texture.EncodeToPNG());

        camera.targetTexture = oldTarget;
        RenderTexture.active = oldActive;
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(rt);
    }

    private readonly struct Placement
    {
        public Placement(GameObject prefab, string name, Vector3 position, Vector3 rotation)
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
