using UnityEngine;

namespace FloodRoute.Environment
{
    [RequireComponent(typeof(Renderer))]
    public class SimpleStormCloudDrift : MonoBehaviour
    {
        [Header("Cloud Appearance")]
        public Color cloudTint = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Vector2 uvScale = new Vector2(1f, 1f);

        [Header("Drift Movement")]
        public float scrollSpeedX = 0.01f;
        public float scrollSpeedY = 0.01f;

        [Header("Debug")]
        public bool enableDebugLogs = false;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Vector2 _currentOffset = Vector2.zero;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            
            // Initialize appearance
            UpdateMaterialProperties();
        }

        private void Update()
        {
            _currentOffset.x += scrollSpeedX * Time.deltaTime;
            _currentOffset.y += scrollSpeedY * Time.deltaTime;

            UpdateMaterialProperties();
        }

        private void UpdateMaterialProperties()
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetColor("_BaseColor", cloudTint);
            // In URP, offset and scale for the base map is typically packed in a Vector4 _BaseMap_ST (Scale X, Scale Y, Offset X, Offset Y)
            _propBlock.SetVector("_BaseMap_ST", new Vector4(uvScale.x, uvScale.y, _currentOffset.x, _currentOffset.y));

            _renderer.SetPropertyBlock(_propBlock);

            if (enableDebugLogs && Time.frameCount % 300 == 0)
            {
                Debug.Log($"[StormCloudDrift] {gameObject.name} offset: {_currentOffset}");
            }
        }
    }
}
