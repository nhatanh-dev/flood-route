using UnityEditor;
using UnityEngine;

public class FloodRouteVisualValidator : EditorWindow
{
    [MenuItem("Window/Flood Route/Validate Scene")]
    public static void ValidateScene()
    {
        Debug.Log("=== FLOOD ROUTE VISUAL VALIDATION START ===");
        int issueCount = 0;

        var badges = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Exclude);
        Debug.Log($"Found {badges.Length} TMPro components");

        foreach (var badge in badges)
        {
            if (badge.name == "Status_Text")
            {
                var rect = badge.transform.parent.GetComponent<RectTransform>();
                if (rect && rect.localScale.magnitude > 0.2f)
                {
                    Debug.LogWarning($"Badge Container '{badge.transform.parent.name}' has LocalScale > 0.2: {rect.localScale}. Might render blurry.");
                    issueCount++;
                }

                var canvas = badge.GetComponentInParent<Canvas>();
                if (canvas && canvas.renderMode == RenderMode.WorldSpace)
                {
                    if (Mathf.Abs(canvas.transform.localScale.x - 1.0f) > 0.01f)
                    {
                        Debug.LogWarning($"World Canvas '{canvas.name}' scale is not reset to (1,1,1). Current: {canvas.transform.localScale}");
                        issueCount++;
                    }
                }
            }
        }

        GameObject waterMesh = GameObject.Find("WaterPlane_Grid");
        if (waterMesh)
        {
            var waterRend = waterMesh.GetComponent<Renderer>();
            if (waterRend && waterRend.sharedMaterial)
            {
                var waterColor = waterRend.sharedMaterial.GetColor("_BaseColor");
                if (waterColor.r > 0.5f && waterColor.g < 0.2f)
                {
                    Debug.LogWarning($"Water color is too brown/red: {waterColor}. Shift toward blue-green.");
                    issueCount++;
                }

                var waterSmooth = waterRend.sharedMaterial.GetFloat("_Smoothness");
                if (waterSmooth < 0.7f)
                {
                    Debug.LogWarning($"Water smoothness too low ({waterSmooth}). Expected shiny water.");
                    issueCount++;
                }
            }
        }

        GameObject baseMesh = GameObject.Find("Diorama_Mud_Base");
        if (baseMesh)
        {
            var baseRend = baseMesh.GetComponent<Renderer>();
            if (baseRend && baseRend.sharedMaterial)
            {
                var baseSmooth = baseRend.sharedMaterial.GetFloat("_Smoothness");
                if (baseSmooth > 0.4f)
                {
                    Debug.LogWarning($"Diorama base smoothness too high ({baseSmooth}). Expected rough mud texture.");
                    issueCount++;
                }
            }
        }

        GameObject lightObj = GameObject.Find("Warm Directional Light");
        Light dirLight = lightObj ? lightObj.GetComponent<Light>() : null;
        if (dirLight)
        {
            var euler = dirLight.transform.eulerAngles;
            if (Mathf.Abs(euler.x - 50f) > 15f || Mathf.Abs(euler.y - 45f) > 15f)
            {
                Debug.LogWarning($"Warm Directional Light rotation off: {euler}. Expected close to (50, 45, 0) for Isometric compatibility.");
                issueCount++;
            }
        }

        GameObject edgeDecor = GameObject.Find("Diorama_Edge_Decoration");
        if (edgeDecor)
        {
            Transform[] decorChildren = edgeDecor.GetComponentsInChildren<Transform>();
            Debug.Log($"Found {decorChildren.Length - 1} decorative props inside Diorama_Edge_Decoration");

            foreach (var child in decorChildren)
            {
                if (child.GetComponentInChildren<Canvas>())
                {
                    Debug.LogWarning($"Decoration item '{child.name}' illegally contains a gameplay Status Canvas! Remove it.");
                    issueCount++;
                }
            }
        }

        Debug.Log($"=== VALIDATION COMPLETE: {issueCount} ISSUES FOUND ===");

        if (issueCount == 0)
        {
            Debug.Log("All visual checks PASSED. Level environment layout is completely optimized.");
        }
        else
        {
            Debug.LogError($"{issueCount} visual configuration errors detected. Check console warnings.");
        }
    }
}
