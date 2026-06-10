using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlayGameOnce
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string MarkerPath = "Temp/PlayGameOnce.txt";

    [InitializeOnLoadMethod]
    private static void PlayIfMarked()
    {
        EditorApplication.delayCall += TryPlay;
    }

    public static void OpenAndPlayGame()
    {
        EditorApplication.delayCall += OpenGameSceneAndQueuePlay;
    }

    private static void TryPlay()
    {
        string markerPath = Path.Combine(Directory.GetCurrentDirectory(), MarkerPath);
        if (!File.Exists(markerPath))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += TryPlay;
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryPlay;
            return;
        }

        File.Delete(markerPath);
        OpenGameSceneAndPlay();
    }

    private static void OpenGameSceneAndPlay()
    {
        EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        EditorApplication.update += TryEnterPlayMode;
    }

    private static void OpenGameSceneAndQueuePlay()
    {
        OpenGameSceneAndPlay();
    }

    private static void TryEnterPlayMode()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= TryEnterPlayMode;
        EditorApplication.EnterPlaymode();
    }
}
