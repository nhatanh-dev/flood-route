using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }

    public void OpenInstructions()
    {
        Debug.Log("[MainMenu] Instructions panel not yet implemented.");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
