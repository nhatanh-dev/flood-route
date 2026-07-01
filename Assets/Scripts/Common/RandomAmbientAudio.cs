using UnityEngine;
using System.Collections;

public class RandomAmbientAudio : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] clips;
    public float minInterval = 10f;
    public float maxInterval = 30f;
    [Range(0f, 1f)]
    public float volume = 0.8f;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            if (clips != null && clips.Length > 0 && audioSource != null)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip, volume);
                }
            }
        }
    }
}
