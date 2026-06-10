using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SquidGameUI;

public class GlassBridgeGame : MonoBehaviour
{
    private const int StepCount = 10;
    private const float BridgeHeight = 5.8f;
    private const float LaneOffset = 1.15f;
    private const float RowSpacing = 2.25f;
    private const float JumpHeight = 1.8f;
    private const float HoverMaxDistance = 160f;

    private readonly List<GlassBridgeTile> tiles = new List<GlassBridgeTile>();

    private Camera bridgeCamera;
    private Transform playerRoot;
    private Transform bridgeRoot;
    private Transform stageRoot;
    private Transform startPlatform;
    private Transform finishPlatform;
    private Canvas canvas;
    private Font defaultFont;
    private TMP_FontAsset displayFont;
    private TMP_FontAsset bodyFont;
    private Sprite roundedSprite;
    private RectTransform challengePanel;
    private CanvasGroup challengePanelGroup;
    private Image challengeAccent;
    private TMP_Text titleText;
    private TMP_Text statusText;
    private TMP_Text progressText;
    private TMP_Text infoText;
    private Coroutine challengeMessageRoutine;
    private bool challengePanelCompact;
    private GameObject resultPanel;
    private Text resultTitleText;
    private Text resultBodyText;
    private Button primaryButton;
    private Button secondaryButton;
    private GlassBridgeTile hoveredTile;

    private int currentStepIndex = -1;
    private bool[] safeLeftByStep;
    private bool isJumping;
    private bool roundEnded;

    private void Awake()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneFlow.NextGameSceneName)
        {
            enabled = false;
            return;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        defaultFont = LoadRuntimeFont();
        LoadChallengeUiAssets();
    }

    private void Start()
    {
        ClearLegacyPlaceholder();
        EnsureEventSystem();
        ConfigureLighting();
        PrepareCamera();
        BuildStage();
        BuildUI();
        StartBridgeRound();
    }

    private void Update()
    {
        if (roundEnded)
            return;

        UpdateHoveredTile();

        if (isJumping)
            return;

        if (Input.GetMouseButtonDown(0))
            HandleBridgeClick();

        if (Input.GetKeyDown(KeyCode.Escape))
            SceneFlow.LoadMenu();
    }

    private void LateUpdate()
    {
        if (bridgeCamera == null || playerRoot == null)
            return;

        Vector3 targetPosition = new Vector3(0f, 9.6f, playerRoot.position.z - 8f);
        bridgeCamera.transform.position = Vector3.Lerp(bridgeCamera.transform.position, targetPosition, Time.deltaTime * 3.5f);
        bridgeCamera.transform.LookAt(playerRoot.position + new Vector3(0f, 1.2f, 5.5f));
    }

    private void ClearLegacyPlaceholder()
    {
        Canvas[] oldCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas oldCanvas in oldCanvases)
        {
            if (oldCanvas != null)
                Destroy(oldCanvas.gameObject);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void ConfigureLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.76f, 0.82f, 0.92f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.59f, 0.68f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.23f, 0.21f);
        RenderSettings.ambientIntensity = 2.05f;
        RenderSettings.reflectionIntensity = 1.2f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.84f, 0.89f, 0.96f);
        RenderSettings.fogDensity = 0.003f;

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 55f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.pixelLightCount = 4;

        CreateLight("BridgeSun", LightType.Directional, new Vector3(52f, -32f, 0f), new Color(1f, 0.98f, 0.92f), 1.75f, LightShadows.Soft);
        CreateLight("BridgeFill", LightType.Directional, new Vector3(30f, 145f, 0f), new Color(0.8f, 0.9f, 1f), 0.9f, LightShadows.None);
        CreateLight("BridgeLeftGlow", LightType.Point, new Vector3(-4.6f, 7.8f, 10f), new Color(0.42f, 0.93f, 1f), 8.5f, LightShadows.None, 34f);
        CreateLight("BridgeRightGlow", LightType.Point, new Vector3(4.6f, 7.8f, 10f), new Color(1f, 0.54f, 0.68f), 8.5f, LightShadows.None, 34f);
    }

    private void PrepareCamera()
    {
        bridgeCamera = Camera.main;

        if (bridgeCamera == null)
            bridgeCamera = FindFirstObjectByType<Camera>();

        if (bridgeCamera == null)
        {
            GameObject cameraObject = new GameObject("BridgeCamera");
            bridgeCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        bridgeCamera.clearFlags = CameraClearFlags.SolidColor;
        bridgeCamera.backgroundColor = new Color(0.07f, 0.09f, 0.13f);
        bridgeCamera.fieldOfView = 60f;
        bridgeCamera.nearClipPlane = 0.1f;
        bridgeCamera.farClipPlane = 250f;
        bridgeCamera.tag = "MainCamera";
    }

    private void BuildStage()
    {
        stageRoot = new GameObject("GlassBridgeStage").transform;
        bridgeRoot = new GameObject("BridgeTiles").transform;
        bridgeRoot.SetParent(stageRoot, false);

        CreateFloor();
        CreateBridgeFrame();
        CreatePlatforms();
        CreatePlayer();
    }

    private void StartBridgeRound()
    {
        safeLeftByStep = new bool[StepCount];

        for (int i = 0; i < StepCount; i++)
            safeLeftByStep[i] = Random.value > 0.5f;

        BuildTiles();
        UpdateProgress();
        SetStatus("Glass Bridge", new Color(0.72f, 0.9f, 1f, 1f));
        infoText.text = "Click a panel in the next row. One side holds. The other side breaks.";
        ShowBridgeChallengeMessage("Safe Landing", "Choose between the first two panels. One side holds.");
    }

    private void BuildTiles()
    {
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (tiles[i] != null)
                Destroy(tiles[i].gameObject);
        }

        tiles.Clear();
        hoveredTile = null;

        Color idleColor = new Color(0.78f, 0.9f, 1f, 1f);
        Color safeColor = new Color(0.35f, 0.94f, 0.92f, 1f);
        Color failColor = new Color(1f, 0.32f, 0.38f, 1f);

        for (int step = 0; step < StepCount; step++)
        {
            float z = GetStepZ(step);
            CreateGlassTile(step, true, z, idleColor, safeColor, failColor);
            CreateGlassTile(step, false, z, idleColor, safeColor, failColor);
        }
    }

    private void CreateGlassTile(int stepIndex, bool leftSide, float zPosition, Color idleColor, Color safeColor, Color failColor)
    {
        GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tileObject.name = (leftSide ? "Left" : "Right") + "Glass_" + (stepIndex + 1);
        tileObject.transform.SetParent(bridgeRoot, false);
        tileObject.transform.position = new Vector3(leftSide ? -LaneOffset : LaneOffset, BridgeHeight, zPosition);
        tileObject.transform.localScale = new Vector3(1.5f, 0.16f, 1.8f);

        GlassBridgeTile tile = tileObject.AddComponent<GlassBridgeTile>();
        bool isSafe = leftSide == safeLeftByStep[stepIndex];
        tile.Configure(stepIndex, isSafe, idleColor, safeColor, failColor);
        tiles.Add(tile);
    }

    private void CreateFloor()
    {
        GameObject floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorObject.name = "BridgeFloor";
        floorObject.transform.SetParent(stageRoot, false);
        floorObject.transform.position = new Vector3(0f, -9f, 8f);
        floorObject.transform.localScale = new Vector3(6f, 1f, 8f);

        Renderer floorRenderer = floorObject.GetComponent<Renderer>();
        floorRenderer.sharedMaterial = CreateLitMaterial(new Color(0.08f, 0.09f, 0.11f), new Color(0.02f, 0.02f, 0.02f), 0.02f, 0.25f);
    }

    private void CreateBridgeFrame()
    {
        float midZ = (GetStepZ(0) + GetStepZ(StepCount - 1)) * 0.5f;
        float bridgeLength = Mathf.Abs(GetStepZ(StepCount - 1) - GetStepZ(0)) + 7f;

        CreateFramePiece("LeftRail", new Vector3(-3.3f, 7.5f, midZ), new Vector3(0.18f, 0.18f, bridgeLength), new Color(0.24f, 0.95f, 0.98f));
        CreateFramePiece("RightRail", new Vector3(3.3f, 7.5f, midZ), new Vector3(0.18f, 0.18f, bridgeLength), new Color(1f, 0.35f, 0.56f));
        CreateFramePiece("LeftSupport", new Vector3(-2.6f, 6.2f, midZ), new Vector3(0.12f, 2.6f, bridgeLength), new Color(0.18f, 0.21f, 0.24f));
        CreateFramePiece("RightSupport", new Vector3(2.6f, 6.2f, midZ), new Vector3(0.12f, 2.6f, bridgeLength), new Color(0.18f, 0.21f, 0.24f));

        for (int i = 0; i < StepCount + 2; i++)
        {
            float z = GetStepZ(0) - RowSpacing + (i * RowSpacing);
            CreateFramePiece("CrossBar_" + i, new Vector3(0f, 7.5f, z), new Vector3(6.9f, 0.1f, 0.12f), new Color(0.13f, 0.16f, 0.2f));
        }
    }

    private void CreatePlatforms()
    {
        startPlatform = CreatePlatform("StartPlatform", new Vector3(0f, BridgeHeight - 0.05f, GetStartZ()), new Vector3(4.2f, 0.3f, 2.8f), new Color(0.24f, 0.28f, 0.32f));
        finishPlatform = CreatePlatform("FinishPlatform", new Vector3(0f, BridgeHeight - 0.05f, GetFinishZ()), new Vector3(4.2f, 0.3f, 2.8f), new Color(0.22f, 0.34f, 0.24f));
    }

    private void CreatePlayer()
    {
        playerRoot = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
        playerRoot.name = "BridgePlayer";
        playerRoot.SetParent(stageRoot, false);
        playerRoot.position = startPlatform.position + new Vector3(0f, 1.05f, 0.7f);
        playerRoot.localScale = new Vector3(0.85f, 1.05f, 0.85f);

        Renderer playerRenderer = playerRoot.GetComponent<Renderer>();
        playerRenderer.sharedMaterial = CreateLitMaterial(new Color(0.18f, 0.84f, 0.72f), new Color(0.03f, 0.19f, 0.16f), 0.05f, 0.42f);
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("BridgeCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.42f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        BuildBridgeChallengePanel();

        GameObject footerCard = CreatePanel("FooterCard", canvas.transform, new Vector2(-26f, 26f), new Vector2(500f, 78f), new Vector2(1f, 0f), new Color(0.07f, 0.1f, 0.14f, 0.82f));
        CreateText("Controls", footerCard.transform, "Mouse: choose the next glass   |   Esc: main menu", 20, TextAnchor.MiddleRight, new Vector2(-22f, 0f), new Vector2(456f, 42f), new Color(0.84f, 0.9f, 0.96f, 1f), FontStyle.Normal);

        resultPanel = CreatePanel("ResultPanel", canvas.transform, Vector2.zero, new Vector2(560f, 300f), new Vector2(0.5f, 0.5f), new Color(0.05f, 0.07f, 0.1f, 0.95f));
        resultTitleText = CreateText("ResultTitle", resultPanel.transform, "Bridge Complete", 38, TextAnchor.UpperCenter, new Vector2(0f, -28f), new Vector2(480f, 48f), Color.white, FontStyle.Bold);
        resultBodyText = CreateText("ResultBody", resultPanel.transform, "Placeholder", 23, TextAnchor.UpperCenter, new Vector2(0f, -92f), new Vector2(480f, 90f), new Color(0.84f, 0.9f, 0.97f, 1f), FontStyle.Normal);
        primaryButton = CreateButton("PrimaryButton", resultPanel.transform, new Vector2(-118f, -108f), new Vector2(190f, 58f), new Color(0.16f, 0.76f, 0.56f, 1f), "Main Menu");
        secondaryButton = CreateButton("SecondaryButton", resultPanel.transform, new Vector2(118f, -108f), new Vector2(190f, 58f), new Color(0.84f, 0.28f, 0.3f, 1f), "Replay");
        resultPanel.SetActive(false);
    }

    private void BuildBridgeChallengePanel()
    {
        challengePanel = CreatePanel("BridgeChallengePanel", canvas.transform, new Vector2(28f, -28f), GetChallengePanelSize(false), new Vector2(0f, 1f), new Color(0.055f, 0.06f, 0.08f, 0.78f)).GetComponent<RectTransform>();
        Image panelImage = challengePanel.GetComponent<Image>();
        panelImage.sprite = roundedSprite;
        panelImage.type = Image.Type.Sliced;

        Outline outline = challengePanel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.42f, 0.93f, 1f, 0.22f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);

        Shadow shadow = challengePanel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
        shadow.effectDistance = new Vector2(0f, -8f);

        challengePanelGroup = challengePanel.gameObject.AddComponent<CanvasGroup>();

        GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accentObject.transform.SetParent(challengePanel, false);
        challengeAccent = accentObject.GetComponent<Image>();
        challengeAccent.color = new Color(1f, 0.176f, 0.42f, 1f);
        RectTransform accentRect = challengeAccent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -5f);
        accentRect.sizeDelta = new Vector2(0f, 5f);

        titleText = CreateTmpText("BridgeTitle", challengePanel, "Glass Bridge", 18, new Color(0.94f, 0.98f, 1f, 1f), TextAlignmentOptions.Left, new Vector2(24f, -22f), new Vector2(520f, 26f), false);
        statusText = CreateTmpText("ChallengeName", challengePanel, "Safe Landing", 27, new Color(0f, 0.784f, 0.353f, 1f), TextAlignmentOptions.Left, new Vector2(24f, -54f), new Vector2(520f, 34f), true);
        progressText = CreateTmpText("ChallengeProgress", challengePanel, "Safe steps: 0 / 10", 18, new Color(0.72f, 0.9f, 1f, 1f), TextAlignmentOptions.Left, new Vector2(24f, -96f), new Vector2(520f, 26f), false);
        infoText = CreateTmpText("ChallengeDescription", challengePanel, "Good. Now choose between the next two panels.", 17, new Color(0.72f, 0.78f, 0.86f, 1f), TextAlignmentOptions.Left, new Vector2(24f, -126f), new Vector2(520f, 46f), false);

        SetChallengePanelCompact(false);
    }

    private void ShowBridgeChallengeMessage(string challengeName, string description)
    {
        if (challengePanel == null)
            return;

        if (challengeMessageRoutine != null)
            StopCoroutine(challengeMessageRoutine);

        statusText.text = challengeName;
        infoText.text = description;
        SetChallengePanelCompact(false);
        challengePanel.gameObject.SetActive(true);
        UIAnimationHelper.FadeScaleIn(challengePanelGroup, challengePanel, 0.22f);
        challengeMessageRoutine = StartCoroutine(MinimizeChallengeAfterDelay(3.4f));
    }

    private IEnumerator MinimizeChallengeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetChallengePanelCompact(true);
        UIAnimationHelper.FadeScaleIn(challengePanelGroup, challengePanel, 0.18f);
    }

    private void SetChallengePanelCompact(bool compact)
    {
        if (challengePanel == null)
            return;

        challengePanel.sizeDelta = GetChallengePanelSize(compact);
        challengePanelCompact = compact;
        float contentWidth = challengePanel.sizeDelta.x - (compact ? 40f : 48f);

        titleText.rectTransform.anchoredPosition = compact ? new Vector2(20f, -18f) : new Vector2(24f, -22f);
        titleText.rectTransform.sizeDelta = new Vector2(contentWidth, compact ? 24f : 26f);
        titleText.fontSize = compact ? 18 : 18;

        statusText.gameObject.SetActive(!compact);
        infoText.gameObject.SetActive(!compact);

        progressText.rectTransform.anchoredPosition = compact ? new Vector2(20f, -46f) : new Vector2(24f, -96f);
        progressText.rectTransform.sizeDelta = new Vector2(contentWidth, compact ? 24f : 26f);
        progressText.fontSize = compact ? 16 : 18;
        progressText.text = GetProgressText(compact);

        statusText.rectTransform.sizeDelta = new Vector2(contentWidth, 34f);
        infoText.rectTransform.sizeDelta = new Vector2(contentWidth, 46f);

        if (challengeAccent != null)
            challengeAccent.color = compact ? new Color(0.42f, 0.93f, 1f, 0.9f) : new Color(1f, 0.176f, 0.42f, 1f);
    }

    private Vector2 GetChallengePanelSize(bool compact)
    {
        if (compact)
            return new Vector2(320f, 82f);

        float width = Screen.width <= 700 ? Mathf.Clamp(Screen.width * 0.9f, 320f, 560f) : Mathf.Clamp(Screen.width * 0.31f, 450f, 650f);
        return new Vector2(width, 188f);
    }

    private void UpdateHoveredTile()
    {
        GlassBridgeTile nextTile = GetHoveredTile();

        if (hoveredTile == nextTile)
            return;

        if (hoveredTile != null)
            hoveredTile.SetHovered(false);

        hoveredTile = nextTile;

        if (hoveredTile != null)
            hoveredTile.SetHovered(true);
    }

    private GlassBridgeTile GetHoveredTile()
    {
        if (bridgeCamera == null || isJumping)
            return null;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return null;

        Ray ray = bridgeCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hitInfo, HoverMaxDistance))
            return null;

        GlassBridgeTile tile = hitInfo.collider.GetComponent<GlassBridgeTile>();

        if (tile == null)
            return null;

        return tile.StepIndex == currentStepIndex + 1 ? tile : null;
    }

    private void HandleBridgeClick()
    {
        GlassBridgeTile targetTile = GetHoveredTile();

        if (targetTile == null)
        {
            infoText.text = "Only the next row is valid. Pick left or right on the closest row.";
            return;
        }

        StartCoroutine(JumpToTileRoutine(targetTile));
    }

    private IEnumerator JumpToTileRoutine(GlassBridgeTile targetTile)
    {
        isJumping = true;
        SetStatus("Jumping...", new Color(0.98f, 0.86f, 0.46f, 1f));
        hoveredTile = null;
        infoText.text = "Commit to the jump.";

        Vector3 startPosition = playerRoot.position;
        Vector3 targetPosition = targetTile.transform.position + new Vector3(0f, 1.02f, 0f);
        float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 horizontal = Vector3.Lerp(startPosition, targetPosition, t);
            float arc = Mathf.Sin(t * Mathf.PI) * JumpHeight;
            playerRoot.position = horizontal + Vector3.up * arc;
            yield return null;
        }

        playerRoot.position = targetPosition;

        if (targetTile.IsSafe)
        {
            targetTile.RevealSafe();
            currentStepIndex = targetTile.StepIndex;
            UpdateProgress();

            if (currentStepIndex >= StepCount - 1)
            {
                yield return StartCoroutine(JumpToFinishRoutine());
                yield break;
            }

            SetStatus("Safe Landing", new Color(0.36f, 0.92f, 0.68f, 1f));
            infoText.text = "Good. Now choose between the next two panels.";
            ShowBridgeChallengeMessage("Safe Landing", "Good. Now choose between the next two panels.");
            isJumping = false;
            yield break;
        }

        yield return StartCoroutine(FailureRoutine(targetTile));
    }

    private IEnumerator FailureRoutine(GlassBridgeTile brokenTile)
    {
        SetStatus("Glass Broke", new Color(1f, 0.46f, 0.5f, 1f));
        infoText.text = "That panel was weak. One mistake and the bridge drops you.";
        brokenTile.BreakTile(playerRoot.position.x < 0f ? -2.2f : 2.2f);

        Vector3 dropStart = playerRoot.position;
        Vector3 drift = new Vector3(brokenTile.transform.position.x * 0.5f, 0f, 0.8f);
        float duration = 1.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 fallPosition = Vector3.Lerp(dropStart, dropStart + drift + Vector3.down * 17f, t);
            fallPosition.x += Mathf.Sin(t * 9f) * 0.08f;
            playerRoot.position = fallPosition;
            yield return null;
        }

        roundEnded = true;
        ShowResult(
            "Bridge Failed",
            "You stepped on fragile glass.",
            "Replay Bridge",
            () => SceneFlow.RestartCurrent(),
            "Main Menu",
            () => SceneFlow.LoadMenu());
    }

    private IEnumerator JumpToFinishRoutine()
    {
        SetStatus("Final Jump", new Color(0.84f, 0.95f, 0.42f, 1f));
        infoText.text = "One last leap to clear the bridge.";

        Vector3 startPosition = playerRoot.position;
        Vector3 targetPosition = finishPlatform.position + new Vector3(0f, 1.02f, 0.4f);
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 horizontal = Vector3.Lerp(startPosition, targetPosition, t);
            float arc = Mathf.Sin(t * Mathf.PI) * (JumpHeight + 0.35f);
            playerRoot.position = horizontal + Vector3.up * arc;
            yield return null;
        }

        playerRoot.position = targetPosition;
        SetStatus("Bridge Cleared", new Color(0.48f, 0.96f, 0.78f, 1f));
        infoText.text = "You survived both games.";
        roundEnded = true;

        ShowResult(
            "Bridge Cleared",
            "You finished the first game and survived the glass bridge. Nice run.",
            "Main Menu",
            () => SceneFlow.LoadMenu(),
            "Replay Bridge",
            () => SceneFlow.RestartCurrent());
    }

    private void ShowResult(string title, string body, string primaryLabel, UnityEngine.Events.UnityAction primaryAction, string secondaryLabel, UnityEngine.Events.UnityAction secondaryAction)
    {
        if (challengePanel != null)
            challengePanel.gameObject.SetActive(false);

        resultTitleText.text = title;
        resultBodyText.text = body;
        ConfigureButton(primaryButton, primaryLabel, primaryAction);
        ConfigureButton(secondaryButton, secondaryLabel, secondaryAction);
        resultPanel.SetActive(true);
    }

    private void ConfigureButton(Button button, string label, UnityEngine.Events.UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        Text labelText = button.GetComponentInChildren<Text>();

        if (labelText != null)
            labelText.text = label;
    }

    private void UpdateProgress()
    {
        if (progressText == null)
            return;

        progressText.text = GetProgressText(challengePanelCompact);
    }

    private string GetProgressText(bool compact)
    {
        int safeSteps = Mathf.Max(0, currentStepIndex + 1);
        return compact ? "Safe steps: " + safeSteps + "/" + StepCount : "Safe steps: " + safeSteps + " / " + StepCount;
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.color = color;
    }

    private Transform CreatePlatform(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = objectName;
        platform.transform.SetParent(stageRoot, false);
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(color, color * 0.12f, 0.03f, 0.32f);
        return platform.transform;
    }

    private void CreateFramePiece(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = objectName;
        piece.transform.SetParent(stageRoot, false);
        piece.transform.position = position;
        piece.transform.localScale = scale;
        piece.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(color, color * 0.12f, 0.08f, 0.4f);
    }

    private Material CreateLitMaterial(Color baseColor, Color emissionColor, float metallic, float smoothness)
    {
        Shader standardShader = Shader.Find("Standard");
        Material material = new Material(standardShader);
        material.color = baseColor;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissionColor);
        return material;
    }

    private void CreateLight(string lightName, LightType type, Vector3 eulerAngles, Color color, float intensity, LightShadows shadows, float range = 10f)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.rotation = Quaternion.Euler(eulerAngles);

        Light lightSource = lightObject.AddComponent<Light>();
        lightSource.type = type;
        lightSource.color = color;
        lightSource.intensity = intensity;
        lightSource.shadows = shadows;
        lightSource.range = range;
        lightSource.shadowStrength = shadows == LightShadows.Soft ? 0.3f : 0f;
        lightSource.shadowBias = shadows == LightShadows.Soft ? 0.04f : 0f;
        lightSource.shadowNormalBias = shadows == LightShadows.Soft ? 0.25f : 0f;

        if (type == LightType.Point)
            lightObject.transform.position = eulerAngles;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, Color color)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private Text CreateText(string objectName, Transform parent, string content, int fontSize, TextAnchor anchor, Vector2 anchoredPosition, Vector2 size, Color color, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        Vector2 anchorVector = GetAnchorVector(anchor);
        rect.anchorMin = anchorVector;
        rect.anchorMax = anchorVector;
        rect.pivot = anchorVector;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = defaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.fontStyle = fontStyle;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);

        return text;
    }

    private TMP_Text CreateTmpText(string objectName, Transform parent, string content, int fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size, bool displayStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Outline));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = displayStyle ? displayFont : bodyFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontSizeMin = Mathf.Max(11f, fontSize * 0.74f);
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = displayStyle ? FontStyles.Bold : FontStyles.Normal;
        text.characterSpacing = displayStyle ? 2f : 0f;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);

        return text;
    }

    private Vector2 GetAnchorVector(TextAnchor anchor)
    {
        float x = 0.5f;
        float y = 0.5f;

        switch (anchor)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                x = 0f;
                break;

            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                x = 1f;
                break;
        }

        switch (anchor)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                y = 1f;
                break;

            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                y = 0f;
                break;
        }

        return new Vector2(x, y);
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Color buttonColor, string label)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = Color.Lerp(buttonColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(buttonColor, Color.black, 0.1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateCenteredLabel(buttonObject.transform, label, 25);
        return button;
    }

    private void CreateCenteredLabel(Transform parent, string label, int fontSize)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = defaultFont;
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontStyle = FontStyle.Bold;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);
    }

    private float GetStartZ()
    {
        return GetStepZ(0) - RowSpacing - 1.25f;
    }

    private float GetFinishZ()
    {
        return GetStepZ(StepCount - 1) + RowSpacing + 1.25f;
    }

    private float GetStepZ(int stepIndex)
    {
        return stepIndex * RowSpacing;
    }

    private void LoadChallengeUiAssets()
    {
        bodyFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/Roboto-Bold SDF");
        displayFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/Anton SDF");

        if (bodyFont == null)
            bodyFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
        if (displayFont == null)
            displayFont = bodyFont;

        roundedSprite = CreateRoundedRectangleSprite(32, 10);
    }

    private Sprite CreateRoundedRectangleSprite(int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;
        float edge = radius - 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x < radius ? radius : x >= size - radius ? size - radius - 1 : x;
                float py = y < radius ? radius : y >= size - radius ? size - radius - 1 : y;
                float dx = x - px;
                float dy = y - py;
                texture.SetPixel(x, y, dx * dx + dy * dy <= edge * edge ? solid : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * radius);
    }

    private static Font LoadRuntimeFont()
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
}
