using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlassBridgeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBridgeGameExists()
    {
        if (SceneManager.GetActiveScene().name != SceneFlow.NextGameSceneName)
            return;

        if (Object.FindFirstObjectByType<GlassBridgeGame>() != null)
            return;

        GameObject gameRoot = new GameObject("GlassBridgeGame");
        gameRoot.AddComponent<GlassBridgeGame>();
    }
}
