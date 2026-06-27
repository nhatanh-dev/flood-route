using UnityEngine;
using System.Collections;

namespace Round1
{
    [RequireComponent(typeof(AudioSource))]
    public class R1RainAmbienceController : MonoBehaviour
    {
        [Header("Ambience Settings")]
        [Range(0f, 1f)] public float rainVolume = 0.35f;
        public float fadeInDuration = 1.5f;

        [Header("Endgame Settings")]
        [Range(0f, 1f)] public float endgameVolume = 0.18f;
        public float fadeToEndgameDuration = 1.0f;

        private AudioSource audioSource;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.volume = 0f;
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
                
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeVolume(0f, rainVolume, fadeInDuration));
            }
        }

        public void FadeToEndgameVolume()
        {
            if (audioSource != null && gameObject.activeInHierarchy)
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeVolume(audioSource.volume, endgameVolume, fadeToEndgameDuration));
            }
        }

        private IEnumerator FadeVolume(float startVol, float endVol, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, endVol, elapsed / duration);
                yield return null;
            }
            audioSource.volume = endVol;
        }
    }
}
