using UnityEngine;

namespace Round1
{
    public class Round1RainController : MonoBehaviour
    {
        [Header("Global Settings")]
        public bool enableRainAudio = true;
        public Vector3 windVelocity = new Vector3(1.5f, -13f, 1.5f);

        [Header("Near Rain (Layer A)")]
        [Range(0, 10000)] public int nearParticleCount = 2000;
        [Range(1f, 50f)] public float nearRainSpeed = 13f;
        [Range(0f, 1f)] public float nearRainAlpha = 0.25f;
        [Range(0.001f, 0.5f)] public float nearParticleWidth = 0.015f;
        [Range(0f, 50f)] public float nearParticleStretchLength = 4f;
        public Vector3 nearSpawnAreaSize = new Vector3(15f, 1f, 15f);

        [Header("Far Rain (Layer B)")]
        [Range(0, 10000)] public int farParticleCount = 6000;
        [Range(1f, 50f)] public float farRainSpeed = 13f;
        [Range(0f, 1f)] public float farRainAlpha = 0.35f;
        [Range(0.001f, 0.5f)] public float farParticleWidth = 0.02f;
        [Range(0f, 50f)] public float farParticleStretchLength = 4.5f;
        public Vector3 farSpawnAreaSize = new Vector3(40f, 1f, 40f);

        [Header("References")]
        public ParticleSystem psNear;
        public ParticleSystem psFar;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }

        public void ApplySettings()
        {
            if (psNear != null) ApplyToSystem(psNear, nearParticleCount, nearRainSpeed, nearRainAlpha, nearParticleWidth, nearParticleStretchLength, nearSpawnAreaSize);
            if (psFar != null) ApplyToSystem(psFar, farParticleCount, farRainSpeed, farRainAlpha, farParticleWidth, farParticleStretchLength, farSpawnAreaSize);

            if (audioSource != null)
            {
                if (enableRainAudio && !audioSource.isPlaying) audioSource.Play();
                else if (!enableRainAudio && audioSource.isPlaying) audioSource.Stop();
            }
        }

        private void ApplyToSystem(ParticleSystem ps, int count, float speed, float alpha, float width, float stretch, Vector3 area)
        {
            var main = ps.main;
            main.startSpeed = speed;
            main.startSize = width;
            main.startColor = new Color(0.7f, 0.75f, 0.8f, alpha);

            var emission = ps.emission;
            emission.rateOverTime = count;

            var shape = ps.shape;
            shape.scale = area;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(windVelocity.x);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(windVelocity.y);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(windVelocity.z);

            var pr = ps.GetComponent<ParticleSystemRenderer>();
            if (pr != null)
            {
                pr.renderMode = ParticleSystemRenderMode.Stretch;
                pr.lengthScale = stretch;
                pr.velocityScale = 0f;
            }
        }
    }
}
