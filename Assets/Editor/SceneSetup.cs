using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SceneSetup
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string MenuScenePath = ScenesFolder + "/Menu.unity";
    private const string GameScenePath = ScenesFolder + "/Game.unity";
    private const string GlassBridgeScenePath = ScenesFolder + "/GlassBridge.unity";

    [MenuItem("Tools/Squid Game/Refresh Scenes")]
    public static void RefreshScenes()
    {
        EnsureSceneFolder();
        CreateMenuScene();
        CreateGlassBridgeScene();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Squid Game/Create Glass Bridge Scene")]
    public static void CreateGlassBridgeSceneAsset()
    {
        EnsureSceneFolder();
        CreateGlassBridgeScene();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureSceneFolder()
    {
        if (!AssetDatabase.IsValidFolder(ScenesFolder))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static void CreateMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Font font = LoadFont();
        MenuController controller = new GameObject("MenuController").AddComponent<MenuController>();
        CreateCamera("MenuCamera", new Color(0.05f, 0.07f, 0.11f, 1f));
        Canvas canvas = CreateCanvas();
        CreateFullScreenImage("Background", canvas.transform, new Color(0.08f, 0.1f, 0.14f, 1f));
        CreateFullScreenImage("Glow", canvas.transform, new Color(0.58f, 0.09f, 0.12f, 0.12f));
        CreateText("Title", canvas.transform, font, "Squid Game 3D", 70, TextAnchor.MiddleCenter, new Vector2(0f, 220f), new Vector2(900f, 90f), Color.white);
        CreateText("Subtitle", canvas.transform, font, "Survive Red Light, Green Light and move into the next game.", 26, TextAnchor.MiddleCenter, new Vector2(0f, 140f), new Vector2(900f, 70f), new Color(0.86f, 0.9f, 0.94f, 1f));
        GameObject card = CreatePanel("MenuCard", canvas.transform, new Vector2(620f, 360f), new Color(0.1f, 0.13f, 0.17f, 0.95f));
        CreateText("CardTitle", card.transform, font, "Ready to Play?", 38, TextAnchor.MiddleCenter, new Vector2(0f, 95f), new Vector2(500f, 60f), Color.white);
        CreateText("CardBody", card.transform, font, "Start from the menu, finish the round, and the project will move to the next scene automatically.", 24, TextAnchor.MiddleCenter, new Vector2(0f, 30f), new Vector2(500f, 100f), new Color(0.82f, 0.87f, 0.92f, 1f));
        Button startButton = CreateButton("StartButton", card.transform, font, "Start Game", new Vector2(0f, -40f), new Color(0.82f, 0.19f, 0.2f, 1f));
        Button quitButton = CreateButton("QuitButton", card.transform, font, "Quit", new Vector2(0f, -120f), new Color(0.2f, 0.24f, 0.29f, 1f));
        UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    private static void CreateGlassBridgeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera("GlassBridgeCamera", new Color(0.07f, 0.09f, 0.13f, 1f));
        CreateEventSystem();
        new GameObject("GlassBridgeGame").AddComponent<GlassBridgeGame>();
        EditorSceneManager.SaveScene(scene, GlassBridgeScenePath);
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
            new EditorBuildSettingsScene(GlassBridgeScenePath, true)
        };
    }

    private static Font LoadFont()
    {
        Font font = null;

        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (System.ArgumentException)
        {
        }

        if (font != null)
            return font;

        return Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 16);
    }

    private static void CreateCamera(string name, Color backgroundColor)
    {
        Camera camera = new GameObject(name).AddComponent<Camera>();
        camera.backgroundColor = backgroundColor;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.gameObject.tag = "MainCamera";
        camera.gameObject.AddComponent<AudioListener>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        CreateEventSystem();

        return canvas;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject CreateFullScreenImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;

        return imageObject;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;

        return panelObject;
    }

    private static Text CreateText(string name, Transform parent, Font font, string content, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 64f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text labelText = labelObject.GetComponent<Text>();
        labelText.font = font;
        labelText.text = label;
        labelText.fontSize = 26;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;

        return button;
    }
}
