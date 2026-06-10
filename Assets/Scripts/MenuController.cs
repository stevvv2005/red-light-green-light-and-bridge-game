using UnityEngine;

public class MenuController : MonoBehaviour
{
    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        SceneFlow.StartGame();
    }

    public void StartEasy()
    {
        SceneFlow.StartGame(DifficultyLevel.Easy);
    }

    public void StartMedium()
    {
        SceneFlow.StartGame(DifficultyLevel.Medium);
    }

    public void StartHard()
    {
        SceneFlow.StartGame(DifficultyLevel.Hard);
    }

    public void ReplayGame()
    {
        SceneFlow.StartGame();
    }

    public void LoadMenu()
    {
        SceneFlow.LoadMenu();
    }

    public void LoadNextGame()
    {
        SceneFlow.LoadNextGame();
    }

    public void QuitGame()
    {
        SceneFlow.Quit();
    }
}
