using SquidGameUI;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Menu.unity";
    private const string AutoBuildMarker = "Temp/RebuildMainMenuOnce.txt";
    private const int BuilderVersion = 2;
    private static TMP_FontAsset displayFont;
    private static TMP_FontAsset bodyFont;
    private static Material displayMaterial;

    [InitializeOnLoadMethod]
    private static void BuildMarkedMenuScene()
    {
        EditorApplication.delayCall += TryBuildMarkedMenuScene;
    }

    private static void TryBuildMarkedMenuScene()
    {
        string markerPath = Path.Combine(Directory.GetCurrentDirectory(), AutoBuildMarker);
        if (!File.Exists(markerPath))
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += TryBuildMarkedMenuScene;
            return;
        }

        File.Delete(markerPath);
        BuildMainMenuScene();
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Squid Game UI/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject canvasObject = new GameObject("Canvas_MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.55f;

        CreateCamera();
        CreateEventSystem();
        CreateMainMenu(canvasObject.transform);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MenuScenePath);
        Debug.Log("Main Menu scene rebuilt at Assets/Scenes/Menu.unity");
    }

    private static void CreateMainMenu(Transform root)
    {
        UIColorPalette palette = EnsurePalette();
        EnsureFonts();
        Sprite circle = EnsureSprite("CircleOutline", DrawCircleOutline);
        Sprite square = EnsureSprite("SquareOutline", DrawSquareOutline);
        Sprite triangle = EnsureSprite("TriangleOutline", DrawTriangleOutline);
        Sprite dot = EnsureSprite("CircleFilled", DrawCircleFilled);

        GameObject uiManagerObject = new GameObject("UIManager");
        uiManagerObject.AddComponent<SquidGameUI.UIManager>();

        RectTransform main = CreatePanel("Panel_MainMenu", root, Vector2.zero, Vector2.one, new Color(0.039f, 0.039f, 0.063f, 1f));
        CreateImage("Image_Frame", main, new Color(1f, 1f, 1f, 0.055f)).raycastTarget = false;

        RectTransform left = CreatePanel("Panel_Left", main, new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Color(0.039f, 0.039f, 0.071f, 0.985f));
        RectTransform right = CreatePanel("Panel_Right", main, new Vector2(0.38f, 0f), Vector2.one, new Color(0.045f, 0.043f, 0.075f, 1f));
        CreateImage("Image_DarkVeil", right, new Color(0f, 0f, 0f, 0.22f)).raycastTarget = false;

        RectTransform accentPink = CreateImage("Glow_Pink", right, new Color(1f, 0.176f, 0.42f, 0.075f)).rectTransform;
        PinCenter(accentPink, new Vector2(200f, 30f), new Vector2(760f, 760f));
        RectTransform accentGreen = CreateImage("Glow_Green", right, new Color(0f, 0.784f, 0.353f, 0.035f)).rectTransform;
        PinBottomRight(accentGreen, new Vector2(-140f, 90f), new Vector2(520f, 360f));

        TMP_Text logo = CreateText("Logo_SquidGame", left, "SQUID\nGAME", 74, palette.textPrimary, TextAlignmentOptions.Left, new Vector2(58f, -58f), new Vector2(430f, 170f));
        logo.lineSpacing = -20f;
        logo.characterSpacing = 6f;
        TMP_Text subtitle = CreateText("Subtitle_TheChallenge", left, "THE CHALLENGE", 22, palette.accentPink, TextAlignmentOptions.Left, new Vector2(92f, -222f), new Vector2(360f, 34f));
        subtitle.characterSpacing = 7f;
        RectTransform subtitleLine = CreateImage("Subtitle_Line", left, palette.accentPink).rectTransform;
        PinTopLeft(subtitleLine, new Vector2(58f, -236f), new Vector2(26f, 2f));

        RectTransform nav = CreateEmpty("Nav", left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(500f, 485f), new Vector2(0f, 1f));
        nav.anchoredPosition = new Vector2(58f, -302f);
        SquidButton play = CreateMenuButton("Btn_Play", nav, SquidButton.Style.PinkFilled, ">", "PLAY", palette, 0f);
        SquidButton multiplayer = CreateMenuButton("Btn_Multiplayer", nav, SquidButton.Style.DarkOutline, "MP", "MULTIPLAYER", palette, -78f);
        SquidButton settings = CreateMenuButton("Btn_Settings", nav, SquidButton.Style.DarkOutline, "*", "SETTINGS", palette, -156f);
        SquidButton leaderboard = CreateMenuButton("Btn_Leaderboard", nav, SquidButton.Style.DarkOutline, "#", "LEADERBOARD", palette, -234f);
        SquidButton exit = CreateMenuButton("Btn_Exit", nav, SquidButton.Style.DarkOutline, "X", "EXIT", palette, -390f);

        RectTransform badge = CreateBadge("Badge_Player", left, palette, dot);
        PinBottomLeft(badge, new Vector2(58f, 48f), new Vector2(420f, 82f));

        CreateCornerButton("Btn_Gear", right, "*", palette, new Vector2(-104f, -42f));
        CreateCornerButton("Btn_Power", right, "X", palette, new Vector2(-48f, -42f));
        CreateRing(right, "Ring_Outer", circle, palette.accentPink, new Vector2(95f, 28f), 420f, 0.14f);
        CreateRing(right, "Ring_Middle", circle, palette.accentGreen, new Vector2(95f, 28f), 292f, 0.12f);
        CreateRing(right, "Ring_Inner", circle, palette.accentPink, new Vector2(95f, 28f), 172f, 0.16f);
        CreateShape(right, "Shape_Circle", circle, palette.accentPink, new Vector2(-330f, -130f), 58f, 0.18f);
        CreateShape(right, "Shape_Square", square, palette.textPrimary, new Vector2(420f, -205f), 48f, 0.14f);
        CreateShape(right, "Shape_Triangle", triangle, palette.accentGreen, new Vector2(-390f, 180f), 56f, 0.16f);
        CreateShape(right, "Shape_Circle_Small", circle, palette.accentPink, new Vector2(460f, 170f), 42f, 0.15f);

        TMP_Text challengeLabel = CreateText("Text_CurrentChallengeLabel", right, "CURRENT CHALLENGE", 18, new Color(1f, 1f, 1f, 0.28f), TextAlignmentOptions.Center, Vector2.zero, new Vector2(420f, 28f));
        PinBottomCenter(challengeLabel.rectTransform, new Vector2(0f, 122f), new Vector2(420f, 28f));
        challengeLabel.characterSpacing = 5f;
        TMP_Text challengeName = CreateText("Text_CurrentChallengeName", right, "GREEN LIGHT", 42, palette.accentGreen, TextAlignmentOptions.Center, Vector2.zero, new Vector2(520f, 56f));
        PinBottomCenter(challengeName.rectTransform, new Vector2(0f, 72f), new Vector2(520f, 56f));
        challengeName.characterSpacing = 6f;
        CreateStrikeRow(right, palette, dot);
        TMP_Text version = CreateText("Text_Version", right, "v1.0.0 - UNITY 6.3 LTS", 14, new Color(1f, 1f, 1f, 0.18f), TextAlignmentOptions.Center, Vector2.zero, new Vector2(320f, 24f));
        PinBottomCenter(version.rectTransform, new Vector2(0f, 24f), new Vector2(320f, 24f));

        GameObject controllerObject = new GameObject("MainMenuUI");
        controllerObject.transform.SetParent(root, false);
        MainMenuUI controller = controllerObject.AddComponent<MainMenuUI>();

        RectTransform mode = CreateModePanel(right, palette, circle);
        RectTransform multi = CreateMultiplayerPanel(right, palette);
        RectTransform settingsPanel = CreateSettingsPanel(right, palette);
        RectTransform leader = CreateLeaderboardPanel(right, palette);
        AddBackButton(mode, controller, palette);
        AddBackButton(multi, controller, palette);
        AddBackButton(settingsPanel, controller, palette);
        AddBackButton(leader, controller, palette);

        SerializedObject so = new SerializedObject(controller);
        Set(so, "rootGroup", root.GetComponent<CanvasGroup>());
        Set(so, "panelLeft", left);
        Set(so, "panelRight", right);
        Set(so, "logoText", logo);
        Set(so, "subtitleText", subtitle);
        Set(so, "buttonColumn", nav);
        Set(so, "playerBadgeNumber", badge.Find("Number").GetComponent<TMP_Text>());
        Set(so, "playerBadgeLabel", badge.Find("Label").GetComponent<TMP_Text>());
        Set(so, "playButton", play);
        Set(so, "multiplayerButton", multiplayer);
        Set(so, "settingsButton", settings);
        Set(so, "leaderboardButton", leaderboard);
        Set(so, "exitButton", exit);
        Set(so, "modeSelectionPanel", mode);
        Set(so, "multiplayerPanel", multi);
        Set(so, "settingsPanel", settingsPanel);
        Set(so, "leaderboardPanel", leader);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform CreateModePanel(Transform parent, UIColorPalette palette, Sprite circle)
    {
        RectTransform panel = CreateOverlay("Panel_ModeSelection", parent, palette, "CHOOSE GAME MODE");
        string[] titles = { "SINGLE PLAYER", "MULTIPLAYER", "CHALLENGE MODE" };
        string[] desc = { "Play alone and beat the challenge.", "Compete with other players online.", "Complete special challenges." };
        Color[] colors = { palette.accentGreen, palette.accentPink, palette.accentGold };
        string[] icons = { "1P", "MP", "*" };
        for (int i = 0; i < 3; i++)
            CreateModeCard(panel, palette, titles[i], desc[i], icons[i], colors[i], new Vector2(-248f + i * 248f, -182f));
        return panel;
    }

    private static RectTransform CreateSettingsPanel(Transform parent, UIColorPalette palette)
    {
        RectTransform panel = CreateOverlay("Panel_Settings", parent, palette, "SETTINGS");
        CreateSettingRow(panel, palette, "MUSIC VOLUME", "70%", 0);
        CreateSettingRow(panel, palette, "SFX VOLUME", "80%", 1);
        CreateSettingRow(panel, palette, "SUBTITLES", "ON", 2);
        CreateSettingRow(panel, palette, "GRAPHICS", "ULTRA", 3);
        return panel;
    }

    private static RectTransform CreateMultiplayerPanel(Transform parent, UIColorPalette palette)
    {
        RectTransform panel = CreateOverlay("Panel_Multiplayer", parent, palette, "MULTIPLAYER");
        CreateOverlayButton(panel, palette, "QUICK MATCH", ">", 0, SquidButton.Style.PinkFilled);
        CreateOverlayButton(panel, palette, "JOIN ROOM", "+", 1, SquidButton.Style.DarkOutline);
        CreateOverlayButton(panel, palette, "CREATE ROOM", "#", 2, SquidButton.Style.DarkOutline);
        RectTransform stat = CreatePanel("OnlineStat", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Color(1f, 1f, 1f, 0.035f));
        stat.pivot = new Vector2(0f, 1f);
        stat.sizeDelta = new Vector2(400f, 120f);
        stat.anchoredPosition = new Vector2(54f, -360f);
        CreateText("Label", stat, "PLAYERS ONLINE", 16, palette.textSecondary, TextAlignmentOptions.Left, new Vector2(20f, -18f), new Vector2(240f, 24f));
        CreateText("Value", stat, "1,248", 44, palette.accentPink, TextAlignmentOptions.Left, new Vector2(20f, -48f), new Vector2(240f, 54f));
        CreateText("Rooms", stat, "in 87 active rooms", 16, palette.textSecondary, TextAlignmentOptions.Left, new Vector2(20f, -94f), new Vector2(240f, 24f));
        return panel;
    }

    private static RectTransform CreateLeaderboardPanel(Transform parent, UIColorPalette palette)
    {
        RectTransform panel = CreateOverlay("Panel_Leaderboard", parent, palette, "LEADERBOARD");
        string[] rows = { "#1   PLAYER 001        12,450", "#2   PLAYER 067        11,280", "#3   PLAYER 456  YOU    9,840", "#4   PLAYER 212         8,200", "#5   PLAYER 199         7,650" };
        for (int i = 0; i < rows.Length; i++)
        {
            RectTransform row = CreatePanel("Rank_" + i, panel, new Vector2(0f, 1f), new Vector2(1f, 1f), i == 2 ? new Color(1f, 0.176f, 0.42f, 0.09f) : new Color(1f, 1f, 1f, 0.03f));
            row.pivot = new Vector2(0.5f, 1f);
            row.offsetMin = new Vector2(54f, 0f);
            row.offsetMax = new Vector2(-54f, 0f);
            row.sizeDelta = new Vector2(row.sizeDelta.x, 54f);
            row.anchoredPosition = new Vector2(0f, -128f - i * 66f);
            CreateText("Text", row, rows[i], 22, i == 2 ? palette.textPrimary : palette.textSecondary, TextAlignmentOptions.Left, new Vector2(18f, -14f), new Vector2(620f, 32f));
        }
        return panel;
    }

    private static RectTransform CreateOverlay(string name, Transform parent, UIColorPalette palette, string title)
    {
        RectTransform panel = CreatePanel(name, parent, Vector2.zero, Vector2.one, new Color(0.039f, 0.039f, 0.063f, 0.975f));
        TMP_Text text = CreateText("Title", panel, title, 40, palette.textPrimary, TextAlignmentOptions.Left, new Vector2(54f, -46f), new Vector2(640f, 56f));
        text.characterSpacing = 4f;
        panel.gameObject.SetActive(false);
        return panel;
    }

    private static void AddBackButton(RectTransform panel, MainMenuUI menu, UIColorPalette palette)
    {
        SquidButton back = CreateOverlayButton(panel, palette, "BACK", "<", 6, SquidButton.Style.BackButton);
        RectTransform rt = back.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(54f, 42f);
        back.gameObject.AddComponent<MenuBackButton>().SetMenu(menu);
    }

    private static void CreateModeCard(RectTransform parent, UIColorPalette palette, string title, string desc, string icon, Color color, Vector2 pos)
    {
        RectTransform card = CreatePanel("Card_" + title.Replace(" ", ""), parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Color(1f, 1f, 1f, 0.035f));
        card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(214f, 280f);
        card.anchoredPosition = pos;
        card.GetComponent<Outline>().effectColor = new Color(color.r, color.g, color.b, 0.38f);
        TMP_Text iconText = CreateText("Icon", card, icon, 36, color, TextAlignmentOptions.Center, new Vector2(0f, -42f), new Vector2(180f, 46f));
        PinTopCenter(iconText.rectTransform, new Vector2(0f, -42f), new Vector2(180f, 46f));
        TMP_Text titleText = CreateText("Title", card, title, 22, palette.textPrimary, TextAlignmentOptions.Center, new Vector2(0f, -108f), new Vector2(190f, 34f));
        PinTopCenter(titleText.rectTransform, new Vector2(0f, -108f), new Vector2(190f, 34f));
        TMP_Text descText = CreateText("Desc", card, desc, 15, palette.textSecondary, TextAlignmentOptions.Center, new Vector2(0f, -152f), new Vector2(170f, 56f));
        PinTopCenter(descText.rectTransform, new Vector2(0f, -152f), new Vector2(170f, 56f));
        CreateOverlayButton(card, palette, "SELECT", "", 0, color == palette.accentGreen ? SquidButton.Style.GreenFilled : color == palette.accentGold ? SquidButton.Style.GoldFilled : SquidButton.Style.PinkFilled).GetComponent<RectTransform>().anchoredPosition = new Vector2(20f, -226f);
    }

    private static void CreateSettingRow(RectTransform parent, UIColorPalette palette, string label, string value, int index)
    {
        RectTransform row = CreatePanel("Setting_" + label.Replace(" ", ""), parent, new Vector2(0f, 1f), new Vector2(0f, 1f), Color.clear);
        row.pivot = new Vector2(0f, 1f);
        row.sizeDelta = new Vector2(520f, 60f);
        row.anchoredPosition = new Vector2(54f, -130f - index * 70f);
        CreateText("Label", row, label, 18, palette.textSecondary, TextAlignmentOptions.Left, new Vector2(0f, -16f), new Vector2(240f, 28f));
        TMP_Text valueText = CreateText("Value", row, value, 18, value == "ON" ? palette.accentGreen : palette.accentPink, TextAlignmentOptions.Right, new Vector2(362f, -16f), new Vector2(120f, 28f));
        valueText.characterSpacing = 1.5f;
        RectTransform line = CreateImage("Line", row, new Color(1f, 1f, 1f, 0.075f)).rectTransform;
        PinBottomLeft(line, new Vector2(0f, 0f), new Vector2(520f, 1f));
    }

    private static SquidButton CreateOverlayButton(Transform parent, UIColorPalette palette, string label, string icon, int index, SquidButton.Style style)
    {
        SquidButton button = CreateMenuButton("Btn_" + label.Replace(" ", ""), parent, style, icon, label, palette, -130f - index * 72f);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 52f);
        return button;
    }

    private static SquidButton CreateMenuButton(string name, Transform parent, SquidButton.Style style, string icon, string label, UIColorPalette palette, float y)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(Outline), typeof(SquidButton));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(430f, 58f);
        rt.anchoredPosition = new Vector2(0f, y);
        TMP_Text iconText = CreateText("Icon", go.transform, icon, 20, palette.textPrimary, TextAlignmentOptions.Center, new Vector2(16f, -15f), new Vector2(48f, 30f));
        TMP_Text labelText = CreateText("Label", go.transform, label, 22, palette.textPrimary, TextAlignmentOptions.Left, new Vector2(76f, -15f), new Vector2(300f, 30f));
        labelText.characterSpacing = 2.5f;
        SquidButton button = go.GetComponent<SquidButton>();
        SerializedObject so = new SerializedObject(button);
        SetEnum(so, "buttonStyle", (int)style);
        Set(so, "palette", palette);
        Set(so, "label", labelText);
        Set(so, "icon", iconText);
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    private static RectTransform CreateBadge(string name, Transform parent, UIColorPalette palette, Sprite dot)
    {
        RectTransform badge = CreatePanel(name, parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(1f, 1f, 1f, 0.04f));
        badge.GetComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.1f);
        Image circle = CreateImage("Circle", badge, palette.accentPink);
        circle.sprite = dot;
        PinMiddleLeft(circle.rectTransform, new Vector2(42f, 0f), new Vector2(42f, 42f));
        CreateText("Number", badge, "16", 20, palette.textPrimary, TextAlignmentOptions.Center, new Vector2(21f, -24f), new Vector2(42f, 26f));
        CreateText("Label", badge, "PLAYER 456", 18, palette.textPrimary, TextAlignmentOptions.Left, new Vector2(84f, -18f), new Vector2(240f, 28f));
        CreateText("Tagline", badge, "Survive. Win. Repeat.", 15, palette.textSecondary, TextAlignmentOptions.Left, new Vector2(84f, -46f), new Vector2(260f, 24f));
        return badge;
    }

    private static void CreateStrikeRow(Transform parent, UIColorPalette palette, Sprite dot)
    {
        for (int i = 0; i < 3; i++)
        {
            Image img = CreateImage("Strike_" + i, parent, i == 0 ? palette.accentPink : new Color(1f, 0.176f, 0.42f, 0.22f));
            img.sprite = dot;
            PinBottomCenter(img.rectTransform, new Vector2(-24f + i * 24f, 50f), new Vector2(12f, 12f));
        }
    }

    private static void CreateRing(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, float size, float alpha)
    {
        Image img = CreateImage(name, parent, new Color(color.r, color.g, color.b, alpha));
        img.sprite = sprite;
        img.raycastTarget = false;
        PinCenter(img.rectTransform, pos, new Vector2(size, size));
    }

    private static void CreateShape(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, float size, float alpha)
    {
        Image img = CreateImage(name, parent, new Color(color.r, color.g, color.b, alpha));
        img.sprite = sprite;
        img.raycastTarget = false;
        PinCenter(img.rectTransform, pos, new Vector2(size, size));
    }

    private static SquidButton CreateCornerButton(string name, Transform parent, string icon, UIColorPalette palette, Vector2 pos)
    {
        SquidButton button = CreateMenuButton(name, parent, SquidButton.Style.DarkOutline, icon, "", palette, 0f);
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.one;
        rt.anchorMax = Vector2.one;
        rt.pivot = Vector2.one;
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.anchoredPosition = pos;
        Transform label = rt.Find("Label");
        if (label != null)
            Object.DestroyImmediate(label.gameObject);
        return button;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.055f);
        outline.effectDistance = new Vector2(1f, -1f);
        return rt;
    }

    private static RectTransform CreateEmpty(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.pivot = pivot;
        return rt;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string content, int size, Color color, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = content;
        text.font = size >= 32 ? displayFont : bodyFont;
        if (text.font == null)
            text.font = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
        if (text.font != null && text.fontMaterial == null && size >= 32 && displayMaterial != null)
            text.fontSharedMaterial = displayMaterial;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = size >= 32 ? FontStyles.Bold : FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.fontSizeMin = size * 0.7f;
        text.fontSizeMax = size;
        text.margin = Vector4.zero;
        return text;
    }

    private static void PinTopLeft(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void PinTopCenter(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void PinBottomLeft(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero; rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void PinBottomRight(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void PinBottomCenter(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void PinMiddleLeft(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(0f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void PinCenter(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }

    private static UIColorPalette EnsurePalette()
    {
        UIColorPalette palette = Resources.Load<UIColorPalette>("UI/UIColorPalette");
        if (palette != null)
            return palette;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");

        palette = ScriptableObject.CreateInstance<UIColorPalette>();
        AssetDatabase.CreateAsset(palette, "Assets/Resources/UI/UIColorPalette.asset");
        AssetDatabase.SaveAssets();
        return palette;
    }

    private static void EnsureFonts()
    {
        if (bodyFont == null)
            bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        if (displayFont == null)
            displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Anton SDF.asset");
        if (displayMaterial == null && displayFont != null)
            displayMaterial = displayFont.material;
    }

    private static Sprite EnsureSprite(string name, System.Action<Texture2D> draw)
    {
        string folder = "Assets/Resources/UI/MenuSprites";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources/UI", "MenuSprites");

        string path = folder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Clear(tex);
            draw(tex);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 128f;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Clear(Texture2D tex)
    {
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color[] pixels = new Color[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        tex.SetPixels(pixels);
        tex.Apply();
    }

    private static void DrawCircleOutline(Texture2D tex)
    {
        Vector2 c = new Vector2(63.5f, 63.5f);
        for (int y = 0; y < 128; y++)
        for (int x = 0; x < 128; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            if (d > 56f && d < 60f)
                tex.SetPixel(x, y, Color.white);
        }
        tex.Apply();
    }

    private static void DrawCircleFilled(Texture2D tex)
    {
        Vector2 c = new Vector2(63.5f, 63.5f);
        for (int y = 0; y < 128; y++)
        for (int x = 0; x < 128; x++)
            if (Vector2.Distance(new Vector2(x, y), c) < 60f)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
    }

    private static void DrawSquareOutline(Texture2D tex)
    {
        for (int y = 28; y < 100; y++)
        for (int x = 28; x < 100; x++)
            if (x < 32 || x > 95 || y < 32 || y > 95)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
    }

    private static void DrawTriangleOutline(Texture2D tex)
    {
        Vector2 a = new Vector2(64f, 18f);
        Vector2 b = new Vector2(106f, 106f);
        Vector2 c = new Vector2(22f, 106f);
        for (int y = 0; y < 128; y++)
        for (int x = 0; x < 128; x++)
        {
            Vector2 p = new Vector2(x, y);
            if (DistanceToSegment(p, a, b) < 3f || DistanceToSegment(p, b, c) < 3f || DistanceToSegment(p, c, a) < 3f)
                tex.SetPixel(x, y, Color.white);
        }
        tex.Apply();
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return Vector2.Distance(p, a + ab * t);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.tag = "MainCamera";
    }

    private static void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void Set(SerializedObject so, string name, Object value)
    {
        SerializedProperty prop = so.FindProperty(name);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    private static void SetEnum(SerializedObject so, string name, int value)
    {
        SerializedProperty prop = so.FindProperty(name);
        if (prop != null)
            prop.enumValueIndex = value;
    }
}
