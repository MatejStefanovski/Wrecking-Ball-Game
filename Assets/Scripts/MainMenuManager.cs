using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Game";
    [SerializeField] private GameObject rulesPanel;

    public void StartGame()
    {
        if (!string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    public void ShowRules()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(true);
        }
    }

    public void HideRules()
    {
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
