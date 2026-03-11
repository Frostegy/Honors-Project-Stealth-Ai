using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public GameObject gameOverCanvas;
    public GameObject winCanvas;

    private bool gameIsOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // always make sure time is running when the scene loads
        Time.timeScale = 1f;

        // hide the game over screen at the start
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
    }

    public void TriggerGameOver()
    {
        if (gameIsOver) return;

        gameIsOver = true;

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);

        Time.timeScale = 0f;
    }

    public void TriggerWin()
    {
        if (gameIsOver) return;

        gameIsOver = true;

        if (winCanvas != null)
            winCanvas.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayLevel1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Easy Level");
    }

    public void PlayLevel2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Hard Level");
    }

    public void QuitGame()
    {
        // this only works in a built game, not in the Unity editor
        Application.Quit();
    }
}