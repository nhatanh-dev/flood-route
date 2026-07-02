using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class UIAudioPlayer : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Clips")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip missionStartClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float missionStartVolume = 0.70f;
    [SerializeField, Range(0f, 1f)] private float victoryVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float defeatVolume = 0.75f;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        ConfigureSource();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        ConfigureSource();
    }

    public void PlayClick()
    {
        Play(clickClip, clickVolume);
    }

    public void PlayMissionStart()
    {
        Play(missionStartClip, missionStartVolume);
    }

    public void PlayVictory()
    {
        Play(victoryClip, victoryVolume);
    }

    public void PlayDefeat()
    {
        Play(defeatClip, defeatVolume);
    }

    private void ConfigureSource()
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
