using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Round2MissionBriefingController : MonoBehaviour
{
    [SerializeField] private Button startRound2Button;
    [SerializeField] private string round2GameplaySceneName = "Round2_RealtimePrototype";
    [SerializeField] private MenuMusicManager menuMusicManager;

    private bool isSceneLoading;

    private void Awake()
    {
        if (startRound2Button == null)
        {
            Debug.LogError("[Round2MissionBriefing] Start button is not assigned.");
            return;
        }

        startRound2Button.onClick.RemoveAllListeners();
        startRound2Button.onClick.AddListener(StartRound2);
    }

    private void Start()
    {
        if (menuMusicManager != null)
        {
            menuMusicManager.PlayFromStartWithFade();
        }
    }

    public void StartRound2()
    {
        if (isSceneLoading || string.IsNullOrWhiteSpace(round2GameplaySceneName))
            return;

        isSceneLoading = true;
        startRound2Button.interactable = false;
        Time.timeScale = 1f;

        StartCoroutine(StartRound2TransitionRoutine());
    }

    private IEnumerator StartRound2TransitionRoutine()
    {
        if (menuMusicManager != null)
        {
            yield return menuMusicManager.StopWithFade();
        }

        SceneManager.LoadScene(round2GameplaySceneName);
    }
}
