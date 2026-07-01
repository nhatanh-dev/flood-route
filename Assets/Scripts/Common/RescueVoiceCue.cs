using System.Collections;
using UnityEngine;

public class RescueVoiceCue : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] rescueClips;
    [Header("Optional References")]
    public Transform player;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    [Header("Distance Settings")]
    public float maxDistance = 25f;
    public float fullVolumeDistance = 6f;
    public float maxVolume = 0.7f;

    [Header("Timing")]
    public float repeatIntervalMin = 5f;
    public float repeatIntervalMax = 8f;

    [Header("Stop Conditions")]
    public Round2RescueZone round2RescueZone;
    public Round1.R1RealtimeRoundController r1Controller;
    public Round2RealtimeRoundController r2Controller;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        StartCoroutine(VoiceRoutine());
    }

    private float nextPlayTime = 0f;

    private IEnumerator VoiceRoutine()
    {
        while (true)
        {
            // Check frequently instead of waiting for the full interval
            yield return new WaitForSeconds(0.5f);

            if (!gameObject.activeInHierarchy)
                break;

            if (round2RescueZone != null && round2RescueZone.civiliansAvailable <= 0)
            {
                StopVoice();
                break;
            }

            if (r1Controller != null && r1Controller.IsGameOver)
            {
                StopVoice();
                break;
            }

            if (r2Controller != null && r2Controller.currentState != Round2GameState.Playing)
            {
                StopVoice();
                break;
            }

            if (audioSource.isPlaying)
                continue;

            if (Time.time < nextPlayTime)
                continue;

            if (player != null && rescueClips != null && rescueClips.Length > 0)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                
                if (distance <= maxDistance)
                {
                    float t = Mathf.InverseLerp(maxDistance, fullVolumeDistance, distance);
                    float volume = Mathf.Lerp(0f, maxVolume, t);

                    if (enableDebugLogs) Debug.Log($"RescueVoiceCue ({gameObject.name}) Distance: {distance}, Volume: {volume}, NextPlayTime: {nextPlayTime}, Time: {Time.time}");

                    if (volume > 0.01f) // Play even if quiet
                    {
                        AudioClip clip = rescueClips[Random.Range(0, rescueClips.Length)];
                        audioSource.PlayOneShot(clip, volume);
                        // Schedule next play
                        nextPlayTime = Time.time + Random.Range(repeatIntervalMin, repeatIntervalMax);
                    }
                }
            }
        }
    }

    private void StopVoice()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
