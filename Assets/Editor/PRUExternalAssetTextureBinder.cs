using UnityEditor;
using UnityEngine;

public static class PRUExternalAssetTextureBinder
{
    private const string Root = "Assets/_FloodRoute/Art/External/PRU_FBX";
    private const string MaterialsRoot = Root + "/Materials";
    private const string TexturesRoot = Root + "/BlenderExtractedTextures";
    private const string PrefabsRoot = Root + "/Prefabs";

    private struct Binding
    {
        public string PrefabPath;
        public string MaterialName;
        public string BaseMapPath;
        public string NormalMapPath;
        public Color Tint;

        public Binding(string prefabPath, string materialName, string baseMapPath, string normalMapPath, Color tint)
        {
            PrefabPath = prefabPath;
            MaterialName = materialName;
            BaseMapPath = baseMapPath;
            NormalMapPath = normalMapPath;
            Tint = tint;
        }
    }

    [InitializeOnLoadMethod]
    private static void AutoBindAfterImport()
    {
        if (SessionState.GetBool("PRUExternalAssetTextureBinder.AutoBound", false))
        {
            return;
        }

        SessionState.SetBool("PRUExternalAssetTextureBinder.AutoBound", true);
        EditorApplication.delayCall += BindTexturesAndPrefabs;
    }

    [MenuItem("FloodRoute/Assets/Rebind PRU External Asset Textures")]
    public static void BindTexturesAndPrefabs()
    {
        AssetDatabase.Refresh();
        EnsureFolder("Assets/_FloodRoute");
        EnsureFolder("Assets/_FloodRoute/Art");
        EnsureFolder("Assets/_FloodRoute/Art/External");
        EnsureFolder(Root);
        EnsureFolder(MaterialsRoot);

        Binding[] bindings =
        {
            new Binding(
                PrefabsRoot + "/PF_PRU_HoiAn_House_2_Variant.prefab",
                "MAT_PRU_HoiAnHouse2Variant_BlenderTexture_URP",
                TexturesRoot + "/HoiAn_House_2_Variant/yellow_townhouse_3d_model_Clone1_basecolor.png",
                TexturesRoot + "/HoiAn_House_2_Variant/yellow_townhouse_3d_model_Clone1_normal.png",
                Color.white),
            new Binding(
                PrefabsRoot + "/PF_PRU_Weathered_House.prefab",
                "MAT_PRU_WeatheredHouse_BlenderTexture_URP",
                TexturesRoot + "/Weathered_House/weathered_house_basecolor.png",
                TexturesRoot + "/Weathered_House/weathered_house_normal.png",
                Color.white),
            new Binding(
                PrefabsRoot + "/PF_PRU_Civilian_Waving.prefab",
                "MAT_PRU_CivilianWaving_BlenderTexture_URP",
                TexturesRoot + "/Civilian_Waving/Color.png",
                TexturesRoot + "/Civilian_Waving/Normal.png",
                Color.white),
            new Binding(
                PrefabsRoot + "/PF_PRU_Woman_StandingYell.prefab",
                "MAT_PRU_WomanStandingYell_BlenderTexture_URP",
                TexturesRoot + "/Woman_StandingYell/Color.png",
                TexturesRoot + "/Woman_StandingYell/Normal.png",
                Color.white),
            new Binding(
                PrefabsRoot + "/PF_PRU_Civilian_Grandma_LayingMildCough.prefab",
                "MAT_PRU_CivilianGrandma_BlenderTexture_URP",
                TexturesRoot + "/Civilian_Grandma_LayingMildCough/Color.png",
                TexturesRoot + "/Civilian_Grandma_LayingMildCough/Normal.png",
                Color.white)
        };

        int updated = 0;
        for (int i = 0; i < bindings.Length; i++)
        {
            if (ApplyBinding(bindings[i]))
            {
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PRUExternalAssetTextureBinder: updated " + updated + " textured external prefabs.");
    }

    private static bool ApplyBinding(Binding binding)
    {
        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(binding.BaseMapPath);
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(binding.NormalMapPath);
        if (baseMap == null)
        {
            Debug.LogWarning("PRUExternalAssetTextureBinder: missing base texture " + binding.BaseMapPath);
            return false;
        }

        ConfigureTextureImporter(binding.BaseMapPath, false);
        if (normalMap != null)
        {
            ConfigureTextureImporter(binding.NormalMapPath, true);
        }

        Material material = CreateOrUpdateMaterial(binding.MaterialName, baseMap, normalMap, binding.Tint);
        if (material == null)
        {
            return false;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(binding.PrefabPath);
        if (contents == null)
        {
            Debug.LogWarning("PRUExternalAssetTextureBinder: missing prefab " + binding.PrefabPath);
            return false;
        }

        Renderer[] renderers = contents.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }

            renderers[i].sharedMaterials = materials;
            EditorUtility.SetDirty(renderers[i]);
        }

        PrefabUtility.SaveAsPrefabAsset(contents, binding.PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        return renderers.Length > 0;
    }

    private static Material CreateOrUpdateMaterial(string materialName, Texture2D baseMap, Texture2D normalMap, Color tint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogWarning("PRUExternalAssetTextureBinder: cannot find URP/Lit or Standard shader.");
            return null;
        }

        string materialPath = MaterialsRoot + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = shader;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", baseMap);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", baseMap);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }

        if (normalMap != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.32f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
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

        string parent = System.IO.Path.GetDirectoryName(folderPath);
        string name = System.IO.Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent))
        {
            parent = parent.Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
