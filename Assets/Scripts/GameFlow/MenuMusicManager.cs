using System.Collections;
using UnityEngine;

public sealed class MenuMusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float maxVolume = 0.35f;
    [SerializeField] private float fadeDuration = 0.8f;

    private Coroutine fadeCoroutine;
    private float savedTime = 0f;

    private void Awake()
    {
        ConfigureAudioSource();
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // Force 2D playback
            if (musicClip != null)
            {
                audioSource.clip = musicClip;
            }
        }
    }

    public void PlayFromStartWithFade()
    {
        if (audioSource == null || musicClip == null) return;

        ConfigureAudioSource();
        audioSource.volume = 0f;
        savedTime = 0f;
        audioSource.time = 0f;
        audioSource.Play();

        StartFade(maxVolume);
    }

    public IEnumerator ResumeWithFade()
    {
        if (audioSource == null || musicClip == null) yield break;

        ConfigureAudioSource();

        // Safely clamp playhead to clip length
        float clipLength = musicClip.length;
        if (savedTime < 0f || savedTime >= clipLength)
        {
            savedTime = 0f;
        }

        audioSource.time = savedTime;
        audioSource.Play();

        yield return StartFade(maxVolume);
    }

    public IEnumerator PauseWithFade()
    {
        if (audioSource == null) yield break;

        // Save current playhead time for resuming later
        if (audioSource.isPlaying)
        {
            savedTime = audioSource.time;
        }

        yield return StartFade(0f);

        audioSource.Pause();
    }

    public IEnumerator StopWithFade()
    {
        if (audioSource == null) yield break;

        yield return StartFade(0f);

        audioSource.Stop();
        savedTime = 0f; // Reset saved playhead
    }

    private Coroutine StartFade(float targetVolume)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume));
        return fadeCoroutine;
    }

    private IEnumerator FadeRoutine(float targetVolume)
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}
