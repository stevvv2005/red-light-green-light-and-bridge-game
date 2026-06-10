using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlayMenuOnce
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string MarkerPath = "Temp/PlayMenuOnce.txt";

    [InitializeOnLoadMethod]
    private static void PlayIfMarked()
    {
        EditorApplication.delayCall += TryPlay;
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
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }
}
