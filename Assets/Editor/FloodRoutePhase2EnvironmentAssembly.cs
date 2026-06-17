using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FloodRoutePhase2EnvironmentAssembly
{
    private const string ModelFolder = "Assets/Models/DisasterProps";
    private const string PrefabFolder = "Assets/Prefabs/DisasterProps";
    private const string VisualChildName = "Substitution_Visual_Model";

    [MenuItem("Window/Flood Route/Phase 2 Environment Assembly")]
    public static void Apply()
    {
        Directory.CreateDirectory(PrefabFolder);

        ReimportModel("SubstitutionAsset_House_TraditionalHut");
        ReimportModel("SubstitutionAsset_Tree_TropicalPalm");
        ReimportModel("SubstitutionAsset_Base_DockHelipad");

        var hutPrefab = CreatePrefab("SubstitutionAsset_House_TraditionalHut");
        var palmPrefab = CreatePrefab("SubstitutionAsset_Tree_TropicalPalm");
        var basePrefab = CreatePrefab("SubstitutionAsset_Base_DockHelipad");

        ReplaceNodeVisual("Base_Node_A", basePrefab, Vector3.zero, Quaternion.identity, Vector3.one);
        ReplaceNodeVisual("House_Node_D", hutPrefab, Vector3.zero, Quaternion.identity, Vector3.one);
        ReplaceNodeVisual("House_Node_E", hutPrefab, Vector3.zero, Quaternion.identity, Vector3.one);
        ReplaceNodeVisual("Tree_Node_F", palmPrefab, Vector3.zero, Quaternion.identity, Vector3.one);
        ReplaceNodeVisual("Tree_Node_G", palmPrefab, Vector3.zero, Quaternion.identity, Vector3.one);

        ApplyEnvironmentPalette();
        PreserveLighting();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("PHASE 2 COMPLETE: Environment color shift and asset substitution successful. Ready for UI overhaul.");
    }

    private static void ReimportModel(string assetName)
    {
        var path = $"{ModelFolder}/{assetName}.fbx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"Missing model importer for {path}");
            return;
        }

        importer.importAnimation = false;
        importer.importBlendShapes = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.Import;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
    }

    private static GameObject CreatePrefab(string assetName)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{assetName}.fbx");
        if (model == null)
        {
            Debug.LogError($"Missing FBX model for {assetName}");
            return null;
        }

        var temp = new GameObject(assetName);
        var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        modelInstance.name = assetName + "_Model";
        modelInstance.transform.SetParent(temp.transform, false);
        // Blender FBX imports in this project carry a 100x child unit scale.
        // Keep public prefab/node scale at 1 and normalize the imported child.
        modelInstance.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var prefabPath = $"{PrefabFolder}/{assetName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        return prefab;
    }

    private static void ReplaceNodeVisual(string nodeName, GameObject visualPrefab, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        var node = GameObject.Find(nodeName);
        if (node == null)
        {
            Debug.LogError($"Node not found: {nodeName}");
            return;
        }

        if (visualPrefab == null)
        {
            Debug.LogError($"Visual prefab missing for node: {nodeName}");
            return;
        }

        for (var i = node.transform.childCount - 1; i >= 0; i--)
        {
            var child = node.transform.GetChild(i);
            if (ShouldPreserveNodeChild(child))
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
        visual.name = VisualChildName;
        visual.transform.SetParent(node.transform, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;
        visual.transform.localScale = localScale;
    }

    private static bool ShouldPreserveNodeChild(Transform child)
    {
        if (child.name == "Node_Status_Canvas")
        {
            return true;
        }

        if (child.GetComponent<Canvas>() != null || child.GetComponentInChildren<Canvas>(true) != null)
        {
            return true;
        }

        return child.name.StartsWith("Node_Status", StringComparison.Ordinal) ||
               child.name.StartsWith("Badge_", StringComparison.Ordinal) ||
               child.name.StartsWith("Status_", StringComparison.Ordinal);
    }

    private static void ApplyEnvironmentPalette()
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
                    new Color(60f / 255f, 140f / 255f, 200f / 255f, 1f),
                    0.1f,
                    0.85f);
                renderer.sharedMaterial = mat;
            }
        }

        var mudBase = GameObject.Find("Diorama_Mud_Base");
        if (mudBase != null)
        {
            var renderer = mudBase.GetComponent<Renderer>();
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

        if (shader != null)
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void PreserveLighting()
    {
        var lightObj = GameObject.Find("Warm Directional Light");
        if (lightObj == null)
        {
            return;
        }

        lightObj.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
        var light = lightObj.GetComponent<Light>();
        if (light != null)
        {
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
        }
    }
}
