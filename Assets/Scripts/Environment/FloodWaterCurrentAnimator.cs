using UnityEngine;

[RequireComponent(typeof(Renderer))]
public sealed class FloodWaterCurrentAnimator : MonoBehaviour
{
    static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");
    static readonly int BumpMapSt = Shader.PropertyToID("_BumpMap_ST");

    Renderer waterRenderer;
    MaterialPropertyBlock properties;
    Vector4 baseMapSt;
    Vector4 bumpMapSt;
    bool hasBumpMap;

    void Awake()
    {
        waterRenderer = GetComponent<Renderer>();
        properties = new MaterialPropertyBlock();

        var material = waterRenderer.sharedMaterial;
        baseMapSt = material.GetVector(BaseMapSt);
        hasBumpMap = material.HasProperty(BumpMapSt);
        if (hasBumpMap)
            bumpMapSt = material.GetVector(BumpMapSt);
    }

    void Update()
    {
        float speedX = Time.time * 0.02f;
        float speedZ = Time.time * 0.01f;

        waterRenderer.GetPropertyBlock(properties);
        baseMapSt.z = speedX;
        baseMapSt.w = speedZ;
        properties.SetVector(BaseMapSt, baseMapSt);

        if (hasBumpMap)
        {
            bumpMapSt.z = speedX * 1.2f;
            bumpMapSt.w = speedZ * 1.2f;
            properties.SetVector(BumpMapSt, bumpMapSt);
        }

        waterRenderer.SetPropertyBlock(properties);
    }
}
