using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class Round1CodexVisualPolish
{
    const string RootName = "R1_Codex_VisualPolish";
    const string MaterialFolder = "Assets/ManualMaterials/CodexVisualPolish";

    static readonly Dictionary<string, Vector3> NodePositions = new()
    {
        { "Base", new Vector3(-6f, 0.9f, 2f) },
        { "Kenh", new Vector3(-4.5f, 0.9f, 2f) },
        { "Cho", new Vector3(-1.5f, 0.9f, 2f) },
        { "GoCao", new Vector3(1f, 0.9f, 2f) },
        { "BaiDinh", new Vector3(4f, 0.9f, 2f) },
        { "BenPhu", new Vector3(-4.5f, 0.9f, 6f) },
        { "CauTre", new Vector3(-1.5f, 0.9f, 6f) },
        { "NhaBa", new Vector3(-1.5f, 0.9f, -2f) },
        { "DuongTre", new Vector3(1f, 0.9f, -2f) },
        { "NhaTu", new Vector3(4f, 0.9f, -2f) },
    };

    static readonly (string Name, string A, string B, bool Blocked)[] Routes =
    {
        ("BASE_KENH", "Base", "Kenh", false),
        ("KENH_CHO", "Kenh", "Cho", false),
        ("CHO_GO_CAO", "Cho", "GoCao", false),
        ("CHO_NHA_BA", "Cho", "NhaBa", false),
        ("NHA_BA_DUONG_TRE", "NhaBa", "DuongTre", false),
        ("DUONG_TRE_NHA_TU", "DuongTre", "NhaTu", false),
        ("NHA_TU_BAI_DINH", "NhaTu", "BaiDinh", false),
        ("KENH_BEN_PHU", "Kenh", "BenPhu", false),
        ("CAU_TRE_CHO", "CauTre", "Cho", false),
        ("BEN_PHU_CAU_TRE", "BenPhu", "CauTre", true),
    };

    [MenuItem("FloodRoute/Round 1/Apply Codex Visual Polish")]
    public static void Apply()
    {
        EnsureMaterialFolder();

        Material waterBase = CreateLitMaterial("MAT_Codex_R1_Muddy_Water_Base", new Color(0.26f, 0.34f, 0.35f, 1f), 0.2f, 0.25f, false);
        Material waterSurface = CreateLitMaterial("MAT_Codex_R1_Muddy_Water_Surface", new Color(0.38f, 0.49f, 0.49f, 0.55f), 0.45f, 0.35f, true);
        Material foamTrail = CreateLitMaterial("MAT_Codex_R1_Foam_Trail", new Color(0.78f, 0.86f, 0.80f, 0.46f), 0.15f, 0.18f, true);
        Material selectedTrail = CreateLitMaterial("MAT_Codex_R1_Selected_Trail", new Color(0.95f, 0.88f, 0.42f, 0.72f), 0.1f, 0.2f, true);
        Material blockedTrail = CreateLitMaterial("MAT_Codex_R1_Blocked_Debris", new Color(0.58f, 0.32f, 0.18f, 0.9f), 0.25f, 0.12f, false);
        Material nodeSoft = CreateLitMaterial("MAT_Codex_R1_Node_Debug_Soft", new Color(0.70f, 0.82f, 0.70f, 0.22f), 0.15f, 0.1f, true);

        Material clay = CreateLitMaterial("MAT_Codex_R1_Mud_Clay", new Color(0.33f, 0.26f, 0.20f, 1f), 0.4f, 0.2f, false);
        Material bamboo = CreateLitMaterial("MAT_Codex_R1_Bamboo", new Color(0.47f, 0.55f, 0.25f, 1f), 0.45f, 0.15f, false);
        Material leaf = CreateLitMaterial("MAT_Codex_R1_Banana_Leaf", new Color(0.23f, 0.44f, 0.20f, 1f), 0.4f, 0.2f, false);
        Material roof = CreateLitMaterial("MAT_Codex_R1_Wet_Metal_Roof", new Color(0.43f, 0.50f, 0.52f, 1f), 0.15f, 0.34f, false);
        Material wall = CreateLitMaterial("MAT_Codex_R1_Flooded_Wall", new Color(0.73f, 0.62f, 0.47f, 1f), 0.45f, 0.12f, false);
        Material supply = CreateLitMaterial("MAT_Codex_R1_Rescue_Supply", new Color(0.88f, 0.74f, 0.30f, 1f), 0.35f, 0.18f, false);
        Material flag = CreateLitMaterial("MAT_Codex_R1_Red_Flag", new Color(0.68f, 0.08f, 0.06f, 1f), 0.35f, 0.1f, false);
        Material wood = CreateLitMaterial("MAT_Codex_R1_Wet_Wood", new Color(0.31f, 0.20f, 0.12f, 1f), 0.5f, 0.15f, false);
        Material plastic = CreateLitMaterial("MAT_Codex_R1_Plastic_Box", new Color(0.08f, 0.36f, 0.62f, 1f), 0.25f, 0.25f, false);

        ApplyWater(waterBase, waterSurface);
        SoftenGameplayRenderers(nodeSoft, foamTrail, blockedTrail);

        GameObject root = RebuildRoot();
        BuildRouteTrails(root.transform, foamTrail, selectedTrail, blockedTrail, wood, roof, plastic);
        BuildLandmarks(root.transform, clay, bamboo, leaf, roof, wall, supply, flag, wood, plastic);
        BuildAtmosphere(root.transform, foamTrail, waterSurface);
        ImproveLighting();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Round 1 Codex visual polish applied. Gameplay node/edge IDs and controller objects were left unchanged.");
    }

    static void ApplyWater(Material waterBase, Material waterSurface)
    {
        AssignRendererMaterial("Flood_Water_Base", waterBase);
        AssignRendererMaterial("Flood_Water_Surface", waterSurface);

        ScaleObject("Flood_Water_Base", new Vector3(150f, 1f, 150f));
        ScaleObject("Flood_Water_Surface", new Vector3(150f, 1f, 150f));
    }

    static void SoftenGameplayRenderers(Material nodeSoft, Material routeSoft, Material blocked)
    {
        Transform nodes = Find("R1_Manual_Layout/R1_Nodes");
        if (nodes != null)
        {
            foreach (Transform child in nodes)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = nodeSoft;
            }
        }

        Transform routes = Find("R1_Manual_Layout/R1_Routes");
        if (routes != null)
        {
            foreach (Transform child in routes)
            {
                MeshRenderer renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = child.name.Contains("BEN_PHU_CAU_TRE") ? blocked : routeSoft;
            }
        }
    }

    static void BuildRouteTrails(Transform root, Material foam, Material selected, Material blocked, Material wood, Material roof, Material plastic)
    {
        Transform parent = NewChild(root, "Subtle_Route_Foam_Trails");
        foreach (var route in Routes)
        {
            Vector3 a = NodePositions[route.A];
            Vector3 b = NodePositions[route.B];
            Material mat = route.Blocked ? blocked : route.Name == "CHO_GO_CAO" ? selected : foam;
            CreateTrail(parent, "TRAIL_" + route.Name, a, b, mat, route.Blocked ? 0.18f : 0.09f, route.Blocked ? 0.035f : 0.018f);

            if (route.Blocked)
                BuildBlockedDebris(parent, a, b, wood, roof, plastic);
        }
    }

    static void BuildLandmarks(Transform root, Material clay, Material bamboo, Material leaf, Material roof, Material wall, Material supply, Material flag, Material wood, Material plastic)
    {
        Transform parent = NewChild(root, "Central_Vietnam_Landmarks");

        BuildDock(parent, NodePositions["Base"], wood, supply, flag);
        BuildFloodHouse(parent, "LM_FloodedHouse_NhaBa", NodePositions["NhaBa"], wall, roof, supply);
        BuildFloodHouse(parent, "LM_FloodedHouse_NhaTu", NodePositions["NhaTu"], wall, roof, supply);
        BuildSafeZone(parent, "LM_SafeZone_BaiDinh", NodePositions["BaiDinh"], clay, wall, roof, flag);
        BuildHighGround(parent, "LM_HighGround_GoCao", NodePositions["GoCao"], clay, bamboo, leaf, supply);
        BuildBambooMarker(parent, "LM_Canal_Kenh", NodePositions["Kenh"], bamboo, leaf);
        BuildMarketMarker(parent, "LM_Market_Cho", NodePositions["Cho"], wood, roof, plastic);
        BuildBridgeMarker(parent, "LM_BambooBridge_BenPhu", NodePositions["BenPhu"], bamboo, wood);
        BuildBridgeMarker(parent, "LM_BambooBridge_CauTre", NodePositions["CauTre"], bamboo, wood);
        BuildBambooMarker(parent, "LM_BambooRoad_DuongTre", NodePositions["DuongTre"], bamboo, leaf);

        BuildPropClusters(parent, bamboo, leaf, roof, wood, plastic, supply);
    }

    static void BuildAtmosphere(Transform root, Material foam, Material waterSurface)
    {
        Transform parent = NewChild(root, "Water_Detail_And_Rain");

        for (int i = 0; i < 28; i++)
        {
            float x = Mathf.Lerp(-7.4f, 5.8f, Halton(i + 1, 2));
            float z = Mathf.Lerp(-3.8f, 7.2f, Halton(i + 3, 3));
            GameObject ripple = CreateCube("Soft_Ripple_" + i, parent, new Vector3(x, 0.925f, z), new Vector3(0.42f, 0.006f, 0.035f), Quaternion.Euler(0f, (i * 37f) % 180f, 0f), foam);
            WaterlinePulse pulse = ripple.AddComponent<WaterlinePulse>();
            SerializedObject so = new SerializedObject(pulse);
            so.FindProperty("pulseSpeed").floatValue = 0.45f + i * 0.015f;
            so.FindProperty("scaleAmount").floatValue = 0.08f;
            so.FindProperty("minAlpha").floatValue = 0.035f;
            so.FindProperty("maxAlpha").floatValue = 0.11f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject mist = new GameObject("Fine_Rain_Ripple_Particles");
        mist.transform.SetParent(parent, false);
        mist.transform.position = new Vector3(-1f, 5.6f, 2f);
        ParticleSystem ps = mist.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.7f;
        main.startSpeed = 7f;
        main.startSize = 0.035f;
        main.startColor = new Color(0.72f, 0.80f, 0.82f, 0.35f);
        main.maxParticles = 450;
        var emission = ps.emission;
        emission.rateOverTime = 60f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(15f, 0.2f, 12f);
        mist.transform.rotation = Quaternion.Euler(68f, 0f, 8f);
        ParticleSystemRenderer psr = mist.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = waterSurface;
    }

    static void ImproveLighting()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.48f, 0.55f, 0.55f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.33f, 0.39f, 0.38f);

        GameObject lightGo = GameObject.Find("Storm_Directional_Light");
        if (lightGo != null && lightGo.TryGetComponent(out Light light))
        {
            light.color = new Color(0.82f, 0.86f, 0.80f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        GameObject fill = GameObject.Find("R1_Codex_Warm_Window_Fill");
        if (fill == null)
            fill = new GameObject("R1_Codex_Warm_Window_Fill");
        Light fillLight = fill.GetComponent<Light>();
        if (fillLight == null)
            fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(1f, 0.72f, 0.42f);
        fillLight.intensity = 1.2f;
        fillLight.range = 5f;
        fill.transform.position = new Vector3(-1.5f, 2.6f, -1.2f);

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.42f, 0.50f, 0.50f);
            camera.fieldOfView = 42f;
            camera.transform.position = new Vector3(0.4f, 8.9f, -9.8f);
            camera.transform.rotation = Quaternion.Euler(34f, 0f, 0f);
        }

        Volume volume = null;
        foreach (Volume candidate in Resources.FindObjectsOfTypeAll<Volume>())
        {
            if (candidate != null && candidate.gameObject.scene.IsValid())
            {
                volume = candidate;
                break;
            }
        }
        if (volume != null)
        {
            volume.gameObject.SetActive(true);
            volume.isGlobal = true;
            if (volume.profile == null)
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            if (!volume.profile.TryGet(out ColorAdjustments color))
                color = volume.profile.Add<ColorAdjustments>();
            color.postExposure.Override(-0.25f);
            color.contrast.Override(18f);
            color.saturation.Override(-8f);

            if (!volume.profile.TryGet(out Vignette vignette))
                vignette = volume.profile.Add<Vignette>();
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.55f);
        }
    }

    static void BuildDock(Transform parent, Vector3 pos, Material wood, Material supply, Material flag)
    {
        Transform root = NewChild(parent, "LM_Base_Rescue_Dock");
        root.position = pos + new Vector3(-0.15f, 0.17f, 0.05f);
        CreateCube("Dock_Plank_A", root, root.position + new Vector3(0f, 0f, 0.38f), new Vector3(1.2f, 0.08f, 0.12f), Quaternion.identity, wood);
        CreateCube("Dock_Plank_B", root, root.position + new Vector3(0f, 0f, 0.12f), new Vector3(1.2f, 0.08f, 0.12f), Quaternion.identity, wood);
        CreateCube("Rice_Bag_Stack", root, root.position + new Vector3(-0.45f, 0.18f, -0.18f), new Vector3(0.35f, 0.18f, 0.28f), Quaternion.Euler(0f, 18f, 0f), supply);
        CreateCube("Supply_Crate", root, root.position + new Vector3(0.35f, 0.16f, -0.2f), new Vector3(0.32f, 0.25f, 0.28f), Quaternion.Euler(0f, -12f, 0f), supply);
        CreatePoleWithFlag(root, root.position + new Vector3(-0.72f, 0.15f, -0.1f), wood, flag);
    }

    static void BuildFloodHouse(Transform parent, string name, Vector3 pos, Material wall, Material roof, Material supply)
    {
        Transform root = NewChild(parent, name);
        root.position = pos;
        CreateCube("Flooded_Wall", root, pos + new Vector3(0f, 0.28f, 0f), new Vector3(0.82f, 0.42f, 0.58f), Quaternion.identity, wall);
        CreateCube("Tin_Roof", root, pos + new Vector3(0f, 0.62f, 0f), new Vector3(1.02f, 0.12f, 0.72f), Quaternion.Euler(0f, 0f, 8f), roof);
        CreateCube("Civilian_Flag", root, pos + new Vector3(0.38f, 0.88f, -0.26f), new Vector3(0.12f, 0.12f, 0.12f), Quaternion.identity, supply);
    }

    static void BuildSafeZone(Transform parent, string name, Vector3 pos, Material clay, Material wall, Material roof, Material flag)
    {
        Transform root = NewChild(parent, name);
        root.position = pos;
        CreateCube("High_Ground_Platform", root, pos + new Vector3(0f, 0.08f, 0f), new Vector3(1.6f, 0.18f, 1.15f), Quaternion.identity, clay);
        CreateCube("Village_Hall", root, pos + new Vector3(0f, 0.55f, 0.05f), new Vector3(1.1f, 0.62f, 0.75f), Quaternion.identity, wall);
        CreateCube("Hall_Tin_Roof", root, pos + new Vector3(0f, 0.93f, 0.05f), new Vector3(1.28f, 0.12f, 0.88f), Quaternion.Euler(0f, 0f, -6f), roof);
        CreatePoleWithFlag(root, pos + new Vector3(0.82f, 0.3f, -0.28f), clay, flag);
    }

    static void BuildHighGround(Transform parent, string name, Vector3 pos, Material clay, Material bamboo, Material leaf, Material supply)
    {
        Transform root = NewChild(parent, name);
        root.position = pos;
        CreateCube("Raised_Mud_Mound", root, pos + new Vector3(0f, 0.05f, 0f), new Vector3(1.35f, 0.16f, 1f), Quaternion.Euler(0f, 22f, 0f), clay);
        CreateCube("Rescue_Tent", root, pos + new Vector3(0f, 0.35f, 0.05f), new Vector3(0.9f, 0.36f, 0.68f), Quaternion.Euler(0f, 22f, 0f), supply);
        BuildBananaTree(root, pos + new Vector3(-0.75f, 0.15f, 0.3f), bamboo, leaf, 0.85f);
    }

    static void BuildBambooMarker(Transform parent, string name, Vector3 pos, Material bamboo, Material leaf)
    {
        Transform root = NewChild(parent, name);
        root.position = pos;
        for (int i = 0; i < 4; i++)
            CreateCube("Bamboo_Pole_" + i, root, pos + new Vector3(-0.28f + i * 0.18f, 0.45f, 0.25f + (i % 2) * 0.12f), new Vector3(0.055f, 0.88f, 0.055f), Quaternion.Euler(0f, 0f, -7f + i * 4f), bamboo);
        BuildBananaTree(root, pos + new Vector3(0.48f, 0.06f, -0.35f), bamboo, leaf, 0.62f);
    }

    static void BuildMarketMarker(Transform parent, string name, Vector3 pos, Material wood, Material roof, Material plastic)
    {
        Transform root = NewChild(parent, name);
        root.position = pos;
        CreateCube("Market_Table", root, pos + new Vector3(0f, 0.24f, 0.32f), new Vector3(0.78f, 0.12f, 0.36f), Quaternion.identity, wood);
        CreateCube("Small_Tin_Awning", root, pos + new Vector3(0f, 0.72f, 0.32f), new Vector3(0.98f, 0.08f, 0.55f), Quaternion.Euler(0f, 0f, 6f), roof);
        CreateCube("Blue_Plastic_Box", root, pos + new Vector3(-0.32f, 0.38f, 0.14f), new Vector3(0.25f, 0.22f, 0.25f), Quaternion.Euler(0f, 12f, 0f), plastic);
        CreateCube("Floating_Crate", root, pos + new Vector3(0.42f, 0.08f, -0.25f), new Vector3(0.34f, 0.08f, 0.24f), Quaternion.Euler(0f, -16f, 0f), plastic);
    }

    static void BuildBridgeMarker(Transform parent, string name, Vector3 pos, Material bamboo, Material wood)
    {
        Transform root = NewChild(parent, name);
        root.position = pos;
        CreateCube("Bridge_Log_A", root, pos + new Vector3(0f, 0.16f, 0.18f), new Vector3(1.15f, 0.07f, 0.07f), Quaternion.Euler(0f, 0f, 0f), wood);
        CreateCube("Bridge_Log_B", root, pos + new Vector3(0f, 0.16f, -0.02f), new Vector3(1.15f, 0.07f, 0.07f), Quaternion.Euler(0f, 0f, 0f), wood);
        for (int i = 0; i < 5; i++)
            CreateCube("Bridge_Slat_" + i, root, pos + new Vector3(-0.48f + i * 0.24f, 0.21f, 0.08f), new Vector3(0.06f, 0.04f, 0.34f), Quaternion.identity, bamboo);
    }

    static void BuildPropClusters(Transform parent, Material bamboo, Material leaf, Material roof, Material wood, Material plastic, Material supply)
    {
        Transform root = NewChild(parent, "Vietnam_Flood_Prop_Clusters");
        BuildBananaTree(root, new Vector3(-6.9f, 0.9f, -0.8f), bamboo, leaf, 0.82f);
        BuildBananaTree(root, new Vector3(4.9f, 0.9f, 4.8f), bamboo, leaf, 0.78f);
        BuildBananaTree(root, new Vector3(5.4f, 0.9f, -0.5f), bamboo, leaf, 0.68f);

        for (int i = 0; i < 8; i++)
        {
            Vector3 p = new Vector3(Mathf.Lerp(-6.7f, 5.2f, Halton(i + 5, 2)), 0.94f, Mathf.Lerp(-3.1f, 7.2f, Halton(i + 8, 3)));
            Material mat = i % 3 == 0 ? roof : i % 3 == 1 ? wood : plastic;
            CreateCube("Floating_Local_Debris_" + i, root, p, new Vector3(0.38f, 0.05f, 0.12f + 0.04f * (i % 2)), Quaternion.Euler(0f, i * 31f, 0f), mat);
        }

        CreateCube("Rice_Bags_By_House", root, new Vector3(-2.35f, 0.99f, -1.55f), new Vector3(0.48f, 0.16f, 0.32f), Quaternion.Euler(0f, 20f, 0f), supply);
    }

    static void BuildBlockedDebris(Transform parent, Vector3 a, Vector3 b, Material wood, Material roof, Material plastic)
    {
        Vector3 mid = (a + b) * 0.5f + new Vector3(0f, 0.08f, 0f);
        CreateCube("Blocked_Wood_Log_A", parent, mid + new Vector3(-0.32f, 0f, 0.05f), new Vector3(0.72f, 0.07f, 0.09f), Quaternion.Euler(0f, 18f, 0f), wood);
        CreateCube("Blocked_Wood_Log_B", parent, mid + new Vector3(0.15f, 0f, -0.08f), new Vector3(0.58f, 0.06f, 0.08f), Quaternion.Euler(0f, -28f, 0f), wood);
        CreateCube("Blocked_Roof_Metal", parent, mid + new Vector3(0.42f, 0.02f, 0.08f), new Vector3(0.55f, 0.035f, 0.28f), Quaternion.Euler(0f, 8f, 6f), roof);
        CreateCube("Blocked_Plastic_Box", parent, mid + new Vector3(-0.05f, 0.02f, 0.26f), new Vector3(0.22f, 0.12f, 0.18f), Quaternion.Euler(0f, -14f, 0f), plastic);
    }

    static void BuildBananaTree(Transform parent, Vector3 pos, Material trunk, Material leaf, float scale)
    {
        Transform root = NewChild(parent, "Banana_Tree");
        root.position = pos;
        CreateCube("Banana_Trunk", root, pos + new Vector3(0f, 0.42f * scale, 0f), new Vector3(0.08f * scale, 0.85f * scale, 0.08f * scale), Quaternion.Euler(0f, 0f, -6f), trunk);
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.26f * scale, 0.86f * scale, 0f);
            CreateCube("Banana_Leaf_" + i, root, pos + offset, new Vector3(0.12f * scale, 0.035f * scale, 0.55f * scale), Quaternion.Euler(12f, angle, 0f), leaf);
        }
    }

    static void CreatePoleWithFlag(Transform parent, Vector3 pos, Material poleMat, Material flagMat)
    {
        CreateCube("Flag_Pole", parent, pos + new Vector3(0f, 0.45f, 0f), new Vector3(0.045f, 0.9f, 0.045f), Quaternion.identity, poleMat);
        CreateCube("Flag_Cloth", parent, pos + new Vector3(0.16f, 0.76f, 0f), new Vector3(0.32f, 0.18f, 0.035f), Quaternion.identity, flagMat);
    }

    static void CreateTrail(Transform parent, string name, Vector3 a, Vector3 b, Material mat, float width, float height)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 delta = b - a;
        float length = new Vector2(delta.x, delta.z).magnitude;
        float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        CreateCube(name, parent, new Vector3(mid.x, 0.935f, mid.z), new Vector3(width, height, length), Quaternion.Euler(0f, angle, 0f), mat);
    }

    static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;
        if (go.TryGetComponent(out Collider collider))
            Object.DestroyImmediate(collider);
        if (go.TryGetComponent(out MeshRenderer renderer))
            renderer.sharedMaterial = material;
        return go;
    }

    static GameObject RebuildRoot()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Object.DestroyImmediate(old);
        GameObject root = new GameObject(RootName);
        root.transform.position = Vector3.zero;
        return root;
    }

    static Transform NewChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void AssignRendererMaterial(string objectName, Material material)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null && go.TryGetComponent(out MeshRenderer renderer))
            renderer.sharedMaterial = material;
    }

    static void ScaleObject(string objectName, Vector3 scale)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null)
            go.transform.localScale = scale;
    }

    static Transform Find(string path)
    {
        GameObject go = GameObject.Find(path);
        return go != null ? go.transform : null;
    }

    static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ManualMaterials"))
            AssetDatabase.CreateFolder("Assets", "ManualMaterials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/ManualMaterials", "CodexVisualPolish");
    }

    static Material CreateLitMaterial(string name, Color color, float roughness, float smoothness, bool transparent)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);

        if (transparent)
            MakeTransparent(mat);
        else
            MakeOpaque(mat);

        return mat;
    }

    static void MakeTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_AlphaClip"))
            mat.SetFloat("_AlphaClip", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        if (mat.HasProperty("_SrcBlend"))
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);
    }

    static void MakeOpaque(Material mat)
    {
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 0f);
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = -1;
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 1f);
    }

    static float Halton(int index, int b)
    {
        float f = 1f;
        float r = 0f;
        while (index > 0)
        {
            f /= b;
            r += f * (index % b);
            index /= b;
        }
        return r;
    }
}
