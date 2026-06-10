using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlow
{
    public const string MenuSceneName = "Menu";
    public const string GameSceneName = "Game";
    public const string NextGameSceneName = "GlassBridge";

    public static bool HasPendingDifficulty { get; private set; }
    public static DifficultyLevel PendingDifficulty { get; private set; } = DifficultyLevel.Medium;

    public static void SetDifficulty(DifficultyLevel difficulty)
    {
        PendingDifficulty = difficulty;
        HasPendingDifficulty = true;
    }

    public static void LoadMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MenuSceneName);
    }

    public static void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneName);
    }

    public static void StartGame(DifficultyLevel difficulty)
    {
        SetDifficulty(difficulty);
        StartGame();
    }

    public static void LoadNextGame()
    {
        Debug.Log("Glass Bridge clicked");

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(NextGameSceneName);
    }

    public static DifficultyLevel ConsumeDifficulty(DifficultyLevel fallback)
    {
        if (!HasPendingDifficulty)
            return fallback;

        HasPendingDifficulty = false;
        return PendingDifficulty;
    }

    public static void RestartCurrent()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void LoadNextScene()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Scene activeScene = SceneManager.GetActiveScene();
        int nextBuildIndex = activeScene.buildIndex + 1;

        if (nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            LoadMenu();
            return;
        }

        SceneManager.LoadScene(nextBuildIndex);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
