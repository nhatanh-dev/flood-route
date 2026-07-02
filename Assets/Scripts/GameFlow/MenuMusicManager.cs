using System.Collections;
using UnityEngine;

public sealed class MenuMusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float maxVolume = 0.35f;
    [SerializeField] private float fadeInDuration = 0.45f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float freshStartOffsetSeconds = 4.2f;

    private Coroutine fadeCoroutine;
    private Coroutine freshStartCoroutine;
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
        ConfigureAudioSource();
        if (audioSource == null || musicClip == null) return;

        CancelFreshStart();
        savedTime = 0f;
        freshStartCoroutine = StartCoroutine(PlayFreshStartRoutine());
    }

    private IEnumerator PlayFreshStartRoutine()
    {
        if (musicClip.loadState == AudioDataLoadState.Unloaded &&
            !musicClip.LoadAudioData())
        {
            freshStartCoroutine = null;
            yield break;
        }

        while (musicClip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (audioSource == null || musicClip == null ||
            musicClip.loadState != AudioDataLoadState.Loaded ||
            musicClip.length <= 0f)
        {
            freshStartCoroutine = null;
            yield break;
        }

        float latestValidTime = Mathf.Max(0f, musicClip.length - 0.01f);
        float startTime = Mathf.Clamp(freshStartOffsetSeconds, 0f, latestValidTime);

        audioSource.volume = 0f;
        audioSource.time = startTime;
        audioSource.Play();
        freshStartCoroutine = null;
        StartFade(maxVolume, fadeInDuration);
    }

    public IEnumerator ResumeWithFade()
    {
        ConfigureAudioSource();
        if (audioSource == null || musicClip == null) yield break;
        CancelFreshStart();

        // Safely clamp playhead to clip length
        float clipLength = musicClip.length;
        if (savedTime < 0f || savedTime >= clipLength)
        {
            savedTime = 0f;
        }

        audioSource.time = savedTime;
        audioSource.Play();

        yield return StartFade(maxVolume, fadeInDuration);
    }

    public IEnumerator PauseWithFade()
    {
        CancelFreshStart();
        if (audioSource == null) yield break;

        // Save current playhead time for resuming later
        if (audioSource.isPlaying)
        {
            savedTime = audioSource.time;
        }

        yield return StartFade(0f, fadeOutDuration);

        audioSource.Pause();
    }

    public IEnumerator StopWithFade()
    {
        CancelFreshStart();
        if (audioSource == null) yield break;

        yield return StartFade(0f, fadeOutDuration);

        audioSource.Stop();
        savedTime = 0f; // Reset saved playhead
    }

    private void CancelFreshStart()
    {
        if (freshStartCoroutine == null) return;

        StopCoroutine(freshStartCoroutine);
        freshStartCoroutine = null;
    }

    private Coroutine StartFade(float targetVolume, float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume, duration));
        return fadeCoroutine;
    }

    private IEnumerator FadeRoutine(float targetVolume, float duration)
    {
        if (audioSource == null) yield break;

        if (duration <= 0f)
        {
            audioSource.volume = targetVolume;
            fadeCoroutine = null;
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}
