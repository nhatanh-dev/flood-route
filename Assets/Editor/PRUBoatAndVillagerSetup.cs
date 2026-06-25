using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PRUBoatAndVillagerSetup
{
    private const string ExternalRoot = "Assets/_FloodRoute/Art/External/PRU_FBX";
    private const string SourceRoot = ExternalRoot + "/Source";
    private const string MaterialsRoot = ExternalRoot + "/Materials";
    private const string PrefabsRoot = ExternalRoot + "/Prefabs";
    private const string ControllersRoot = ExternalRoot + "/AnimationControllers";

    private const string ExternalBoatSource = @"D:\Documents\FPT_docu\PRU\boat.fbx";
    private const string ImportedBoatPath = SourceRoot + "/Boat_New/boat.fbx";
    private const string NewBoatPrefabPath = PrefabsRoot + "/PF_PRU_New_Boat.prefab";
    private const string BoatBaseMapPath = ExternalRoot + "/BlenderExtractedTextures/Boat_New/boat_basecolor.png";
    private const string BoatNormalMapPath = ExternalRoot + "/BlenderExtractedTextures/Boat_New/boat_normal.png";

    private struct VillagerBinding
    {
        public string PrefabPath;
        public string SourceFbxPath;
        public string ControllerPath;

        public VillagerBinding(string prefabPath, string sourceFbxPath, string controllerPath)
        {
            PrefabPath = prefabPath;
            SourceFbxPath = sourceFbxPath;
            ControllerPath = controllerPath;
        }
    }

    [MenuItem("FloodRoute/Assets/Import New Boat And Fix Villager Animations")]
    public static void RunSetup()
    {
        EnsureFolder(ExternalRoot);
        EnsureFolder(SourceRoot);
        EnsureFolder(MaterialsRoot);
        EnsureFolder(PrefabsRoot);
        EnsureFolder(ControllersRoot);
        EnsureFolder(SourceRoot + "/Boat_New");

        ImportNewBoat();
        CreateOrUpdateBoatPrefab();
        FixVillagerAnimations();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PRUBoatAndVillagerSetup: imported new boat prefab and fixed villager animation controllers.");
    }

    private static void ImportNewBoat()
    {
        if (!File.Exists(ExternalBoatSource))
        {
            Debug.LogWarning("PRUBoatAndVillagerSetup: missing external boat source: " + ExternalBoatSource);
            return;
        }

        string absoluteImportedPath = Path.GetFullPath(ImportedBoatPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteImportedPath));
        File.Copy(ExternalBoatSource, absoluteImportedPath, true);
        AssetDatabase.ImportAsset(ImportedBoatPath, ImportAssetOptions.ForceUpdate);

        ModelImporter importer = AssetImporter.GetAtPath(ImportedBoatPath) as ModelImporter;
        if (importer != null)
        {
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }
    }

    private static void CreateOrUpdateBoatPrefab()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedBoatPath);
        if (model == null)
        {
            Debug.LogWarning("PRUBoatAndVillagerSetup: boat model was not imported: " + ImportedBoatPath);
            return;
        }

        Material hull = CreateMaterial("MAT_PRU_NewBoat_Hull_Orange_URP", new Color32(214, 101, 45, 255), 0.28f, 0f);
        Material wetWood = CreateMaterial("MAT_PRU_NewBoat_WetWood_URP", new Color32(76, 55, 36, 255), 0.36f, 0f);
        Material rubber = CreateMaterial("MAT_PRU_NewBoat_DarkRubber_URP", new Color32(36, 38, 35, 255), 0.42f, 0f);
        Material metal = CreateMaterial("MAT_PRU_NewBoat_DullMetal_URP", new Color32(118, 124, 119, 255), 0.4f, 0.12f);
        Material light = CreateMaterial("MAT_PRU_NewBoat_WarmLight_URP", new Color32(232, 196, 119, 255), 0.25f, 0f);
        Material texturedBoat = CreateTexturedMaterial("MAT_PRU_NewBoat_Textured_URP", BoatBaseMapPath, BoatNormalMapPath, new Color32(255, 255, 255, 255), 0.32f, 0f);

        GameObject root = new GameObject("PF_PRU_New_Boat");
        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null)
        {
            Object.DestroyImmediate(root);
            return;
        }

        instance.name = "Boat_Model";
        instance.transform.SetParent(root.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] materials = renderer.sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                string key = ((materials[j] != null ? materials[j].name : string.Empty) + " " + renderer.name).ToLowerInvariant();
                if (key.Contains("wood") || key.Contains("plank") || key.Contains("seat") || key.Contains("deck"))
                {
                    materials[j] = wetWood;
                }
                else if (key.Contains("rubber") || key.Contains("tire") || key.Contains("black") || key.Contains("rope"))
                {
                    materials[j] = rubber;
                }
                else if (key.Contains("metal") || key.Contains("prop") || key.Contains("engine"))
                {
                    materials[j] = metal;
                }
                else if (key.Contains("light") || key.Contains("lamp"))
                {
                    materials[j] = light;
                }
                else
                {
                    materials[j] = texturedBoat != null ? texturedBoat : hull;
                }
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        PrefabUtility.SaveAsPrefabAsset(root, NewBoatPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void FixVillagerAnimations()
    {
        VillagerBinding[] bindings =
        {
            new VillagerBinding(
                PrefabsRoot + "/PF_PRU_Civilian_Waving.prefab",
                SourceRoot + "/Civilian_Waving/nguoi_dan_tpose@Waving.fbx",
                ControllersRoot + "/AC_PRU_Civilian_Waving.controller"),
            new VillagerBinding(
                PrefabsRoot + "/PF_PRU_Woman_StandingYell.prefab",
                SourceRoot + "/Woman_StandingYell/woman_tpose@Standing Yell.fbx",
                ControllersRoot + "/AC_PRU_Woman_StandingYell.controller"),
            new VillagerBinding(
                PrefabsRoot + "/PF_PRU_Civilian_Grandma_LayingMildCough.prefab",
                SourceRoot + "/Civilian_Grandma_LayingMildCough/civilian_grandma_tpose@Laying Mild Cough.fbx",
                ControllersRoot + "/AC_PRU_Civilian_Grandma_LayingMildCough.controller")
        };

        for (int i = 0; i < bindings.Length; i++)
        {
            ApplyVillagerAnimation(bindings[i]);
        }
    }

    private static void ApplyVillagerAnimation(VillagerBinding binding)
    {
        AnimationClip clip = FindPrimaryClip(binding.SourceFbxPath);
        if (clip == null)
        {
            Debug.LogWarning("PRUBoatAndVillagerSetup: no usable animation clip found in " + binding.SourceFbxPath);
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(binding.ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(binding.ControllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        ChildAnimatorState[] states = stateMachine.states;
        AnimatorState state = states.Length > 0 ? states[0].state : stateMachine.AddState(clip.name);
        state.name = clip.name;
        state.motion = clip;
        state.speed = 1f;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);

        GameObject contents = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
        if (contents == null)
        {
            Debug.LogWarning("PRUBoatAndVillagerSetup: missing villager prefab " + binding.PrefabPath);
            return;
        }

        Animator animator = contents.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = contents.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Collider[] colliders = contents.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        PrefabUtility.SaveAsPrefabAsset(contents, binding.PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static AnimationClip FindPrimaryClip(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = true;
                    clips[i].loopPose = true;
                }

                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
            {
                return clip;
            }
        }

        return null;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        string path = MaterialsRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateTexturedMaterial(string name, string baseMapPath, string normalMapPath, Color tint, float smoothness, float metallic)
    {
        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath);
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
        if (baseMap == null)
        {
            return null;
        }

        ConfigureTextureImporter(baseMapPath, false);
        if (normalMap != null)
        {
            ConfigureTextureImporter(normalMapPath, true);
        }

        Material material = CreateMaterial(name, tint, smoothness, metallic);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", baseMap);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", baseMap);
        }

        if (normalMap != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureTextureImporter(string path, bool isNormalMap)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        TextureImporterType targetType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType != targetType)
        {
            importer.textureType = targetType;
            changed = true;
        }

        if (!isNormalMap && !importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath);
        string name = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            parent = parent.Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
