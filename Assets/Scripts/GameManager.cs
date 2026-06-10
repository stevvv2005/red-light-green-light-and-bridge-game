using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}

public enum RoundPhase
{
    GreenChallenge,
    GreenRun,
    GreenFailed,
    RedLight,
    Won,
    Lost
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Round")]
    [SerializeField]
    private DifficultyLevel difficulty = DifficultyLevel.Medium;

    [SerializeField]
    private int minutes = 2;

    [SerializeField]
    private int playerFailLimit = 3;

    [SerializeField]
    private int playerFailCount;

    [Header("Legacy Scene References")]
    [SerializeField]
    private Transform Head;

    [SerializeField]
    private Text timeText;

    [SerializeField]
    private AudioSource dollSing;

    [SerializeField]
    private AudioSource dollHeadOff;

    [SerializeField]
    private AudioSource dollHeadOn;

    [Header("Optional Bots")]
    [SerializeField]
    private int totalBots;

    [SerializeField]
    private GameObject Bot;

    [SerializeField]
    private Transform SpawnArea;

    private float roundDuration;
    private float timeRemaining;
    private float roundStartTime;
    private float finalSurvivalTime;
    private float distanceCovered;
    private Vector3 lastTrackedPosition;
    private PlayerMovement player;
    private ChallengeManager challengeManager;
    private GameHUDManager uiManager;
    private DollWatcher dollWatcher;
    private FinishLine finishLine;

    public RoundPhase CurrentPhase { get; private set; } = RoundPhase.RedLight;
    public DifficultyLevel SelectedDifficulty => difficulty;
    public int PlayerFailCount => playerFailCount;
    public int PlayerFailLimit => playerFailLimit;
    public int ChallengesPassed { get; private set; }
    public float TimeSurvived => IsRoundFinished ? finalSurvivalTime : Mathf.Max(0f, Time.unscaledTime - roundStartTime);
    public float DistanceCovered => distanceCovered;
    public bool IsGreenLight => CurrentPhase == RoundPhase.GreenChallenge || CurrentPhase == RoundPhase.GreenRun || CurrentPhase == RoundPhase.GreenFailed;
    public bool IsRedLight => CurrentPhase == RoundPhase.RedLight;
    public bool CanPlayerMove => CurrentPhase == RoundPhase.GreenRun;
    public bool IsRoundFinished => CurrentPhase == RoundPhase.Won || CurrentPhase == RoundPhase.Lost;
    public static bool RoundFinished => Instance != null && Instance.IsRoundFinished;

    private void Awake()
    {
        Instance = this;
        difficulty = SceneFlow.ConsumeDifficulty(difficulty);
        SetupManagers();
        ConfigureLighting();
        EnsureFinishLine();
    }

    private void Start()
    {
        roundDuration = Mathf.Max(30f, minutes * 60f);
        timeRemaining = roundDuration;
        playerFailCount = 0;
        ChallengesPassed = 0;
        roundStartTime = Time.unscaledTime;
        finalSurvivalTime = 0f;
        distanceCovered = 0f;
        lastTrackedPosition = player != null ? player.transform.position : Vector3.zero;
        SpawnBots();
        uiManager.ShowStartState(difficulty);
        dollWatcher.StartCycle(difficulty);
    }

    private void Update()
    {
        if (IsRoundFinished)
            return;

        timeRemaining -= Time.deltaTime;
        TrackPlayerDistance();

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            HandleRoundTimeout();
            return;
        }

        uiManager.Refresh(timeRemaining, roundDuration, CurrentPhase, difficulty, challengeManager.GetSnapshot());
    }

    public void HandleGreenLightStarted(float greenDuration)
    {
        if (IsRoundFinished)
            return;

        CurrentPhase = RoundPhase.GreenChallenge;
        BotTaskContext taskContext = challengeManager.BeginGreenChallenge(difficulty, greenDuration);
        NotifyBotsOfTask(taskContext);
        uiManager.ShowRoundState("GREEN LIGHT", new Color(0.36f, 0.85f, 0.46f, 1f));
    }

    public void HandleRedLightStarted(float redDuration)
    {
        if (IsRoundFinished)
            return;

        CurrentPhase = RoundPhase.RedLight;
        challengeManager.EndCurrentWindow();
        NotifyBotsOfRedLight();
        uiManager.ShowRoundState("RED LIGHT", new Color(0.92f, 0.24f, 0.24f, 1f));
    }

    public void HandleChallengeSuccess()
    {
        if (CurrentPhase != RoundPhase.GreenChallenge)
            return;

        CurrentPhase = RoundPhase.GreenRun;
        ChallengesPassed++;
        uiManager.ShowRoundState("RUN", new Color(0.3f, 0.88f, 0.5f, 1f));
    }

    public void HandleChallengeFailed(string reason)
    {
        if (CurrentPhase != RoundPhase.GreenChallenge)
            return;

        if (RegisterPlayerTaskFail(reason))
            return;

        CurrentPhase = RoundPhase.GreenFailed;
        uiManager.ShowRoundState("WAIT FOR NEXT GREEN", new Color(1f, 0.72f, 0.24f, 1f));
    }

    public void HandlePlayerCaught()
    {
        LoseRound("You moved during RED LIGHT.");
    }

    public void ReportBotElimination(string message)
    {
        if (IsRoundFinished)
            return;

        uiManager.ShowEvent(message, new Color(1f, 0.54f, 0.38f, 1f), 3.2f);
    }

    public void HandlePlayerWin()
    {
        if (IsRoundFinished)
            return;

        CurrentPhase = RoundPhase.Won;
        CaptureFinalStats();
        dollWatcher.StopCycle();
        challengeManager.ResetManager();
        PrepareResultScreen();

        uiManager.ShowResult(
            "You Win",
            "You reached the finish line. Continue to the glass bridge or replay the round.",
            "Continue",
            SceneFlow.LoadNextGame,
            "Replay",
            SceneFlow.RestartCurrent);
    }

    public float GetRoundProgress01()
    {
        if (roundDuration <= 0f)
            return 0f;

        return 1f - (timeRemaining / roundDuration);
    }

    private void LoseRound(string message)
    {
        if (IsRoundFinished)
            return;

        CurrentPhase = RoundPhase.Lost;
        CaptureFinalStats();
        dollWatcher.StopCycle();
        challengeManager.ResetManager();
        PrepareResultScreen();
        uiManager.ShowResult(
            "You Lose",
            "You failed 3 tasks. Wait for the next GREEN LIGHT.",
            "Replay",
            SceneFlow.RestartCurrent,
            "Main Menu",
            SceneFlow.LoadMenu);
    }

    private void HandleRoundTimeout()
    {
        if (IsRoundFinished)
            return;

        CurrentPhase = RoundPhase.Lost;
        CaptureFinalStats();
        dollWatcher.StopCycle();
        challengeManager.ResetManager();
        PrepareResultScreen();
        uiManager.ShowResult(
            "Time Over",
            "You did not reach the finish line in time.",
            "Replay",
            SceneFlow.RestartCurrent,
            "Main Menu",
            SceneFlow.LoadMenu);
    }

    private bool RegisterPlayerTaskFail(string reason)
    {
        playerFailCount++;
        uiManager.ShowEvent(
            "Task failed: " + playerFailCount + " / " + playerFailLimit + " strikes.",
            new Color(1f, 0.8f, 0.35f, 1f),
            2.8f);

        if (playerFailCount < playerFailLimit)
            return false;

        HandleFailLimitReached(reason);
        return true;
    }

    private void HandleFailLimitReached(string reason)
    {
        if (IsRoundFinished)
            return;

        CurrentPhase = RoundPhase.Lost;
        CaptureFinalStats();
        dollWatcher.StopCycle();
        challengeManager.ResetManager();
        player?.ForceElimination();
        uiManager.ShowRoundState("GAME OVER", new Color(1f, 0.26f, 0.26f, 1f));
        PrepareResultScreen();
        uiManager.ShowResult(
            "Game Over",
            "You failed 3 tasks. Wait for the next GREEN LIGHT.",
            "Replay",
            SceneFlow.RestartCurrent,
            "Main Menu",
            SceneFlow.LoadMenu);
    }

    private void PrepareResultScreen()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private void CaptureFinalStats()
    {
        if (finalSurvivalTime <= 0f)
            finalSurvivalTime = Mathf.Max(0f, Time.unscaledTime - roundStartTime);
    }

    private void TrackPlayerDistance()
    {
        if (player == null || IsRoundFinished)
            return;

        Vector3 currentPosition = player.transform.position;
        distanceCovered += Vector3.Distance(lastTrackedPosition, currentPosition);
        lastTrackedPosition = currentPosition;
    }

    private void SetupManagers()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        challengeManager = GetComponent<ChallengeManager>();
        uiManager = GetComponent<GameHUDManager>();
        dollWatcher = GetComponent<DollWatcher>();

        if (challengeManager == null)
            challengeManager = gameObject.AddComponent<ChallengeManager>();

        if (uiManager == null)
            uiManager = gameObject.AddComponent<GameHUDManager>();

        if (dollWatcher == null)
            dollWatcher = gameObject.AddComponent<DollWatcher>();

        if (Head == null)
            Head = FindHeadTransform();

        challengeManager.Configure(this);
        uiManager.Configure(this, challengeManager, timeText);
        dollWatcher.Configure(this, Head, dollSing, dollHeadOff, dollHeadOn);
    }

    private void EnsureFinishLine()
    {
        GameObject finishLineObject = GameObject.Find("FinalLine");

        if (finishLineObject == null)
            return;

        finishLine = finishLineObject.GetComponent<FinishLine>();

        if (finishLine == null)
            finishLine = finishLineObject.AddComponent<FinishLine>();

        finishLine.Configure(this);
    }

    private Transform FindHeadTransform()
    {
        GameObject headObject = GameObject.Find("doll_head");

        if (headObject != null)
            return headObject.transform;

        GameObject dollObject = GameObject.Find("Doll");

        if (dollObject == null)
            return null;

        foreach (Transform child in dollObject.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLower().Contains("head"))
                return child;
        }

        return dollObject.transform;
    }

    private void SpawnBots()
    {
        if (Bot == null || SpawnArea == null || totalBots <= 0)
            return;

        for (int i = 0; i < totalBots; i++)
            Instantiate(Bot, RandomPosition(), SpawnArea.rotation);
    }

    private Vector3 RandomPosition()
    {
        Vector3 origin = SpawnArea.position;
        Vector3 range = SpawnArea.localScale / 2f;
        Vector3 randomRange = new Vector3(
            Random.Range(-range.x, range.x),
            Random.Range(-range.y, range.y),
            Random.Range(-range.z, range.z));

        return origin + randomRange;
    }

    private void NotifyBotsOfTask(BotTaskContext context)
    {
        BotAI[] bots = FindObjectsByType<BotAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (bots.Length == 0)
            return;

        int runners = 0;
        int blocked = 0;
        int doomed = 0;

        foreach (BotAI bot in bots)
        {
            if (!bot.IsCompeting)
                continue;

            BotTaskOutcome outcome = bot.AssignTask(context);

            switch (outcome.Decision)
            {
                case BotTaskDecision.Run:
                    runners++;
                    break;
                case BotTaskDecision.Die:
                    doomed++;
                    break;
                default:
                    blocked++;
                    break;
            }
        }

        uiManager.ShowEvent(
            BuildBotForecastMessage(context, runners, blocked, doomed),
            doomed > 0 ? new Color(1f, 0.74f, 0.34f, 1f) : new Color(0.72f, 0.88f, 1f, 1f),
            3.2f);
    }

    private void NotifyBotsOfRedLight()
    {
        BotAI[] bots = FindObjectsByType<BotAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (BotAI bot in bots)
        {
            if (bot.IsCompeting)
                bot.HandleRedLightStarted();
        }
    }

    private string BuildBotForecastMessage(BotTaskContext context, int runners, int blocked, int doomed)
    {
        switch (context.ChallengeKind)
        {
            case ChallengeKind.PressKey:
                return "Bots on the key cue: " + runners + " reacted in time, " + blocked + " hesitated.";

            case ChallengeKind.SpamKey:
                return "Bots on the mash test: " + runners + " kept the rhythm, " + blocked + " ran out of speed.";

            case ChallengeKind.Sequence:
                if (doomed > 0)
                    return "Bots on the sequence: " + runners + " remembered it, " + blocked + " froze, " + doomed + " panicked.";

                return "Bots on the sequence: " + runners + " remembered it, " + blocked + " froze.";

            default:
                return "Bots are reacting to the current task.";
        }
    }

    private void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.8f, 0.83f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.63f, 0.7f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.27f, 0.24f);
        RenderSettings.ambientIntensity = 2.35f;
        RenderSettings.reflectionIntensity = 1.25f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.83f, 0.88f, 0.95f);
        RenderSettings.fogDensity = 0.0028f;

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 60f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.pixelLightCount = 4;

        Light keyLight = FindSceneDirectionalLight();

        if (keyLight == null)
            keyLight = CreateOrUpdateDirectionalLight("RuntimeKeyLight", new Vector3(48f, -36f, 0f), new Color(1f, 0.98f, 0.9f), 1.9f, true);
        else
            ConfigureDirectionalLight(keyLight, new Vector3(48f, -36f, 0f), new Color(1f, 0.98f, 0.9f), 1.9f, true);

        RenderSettings.sun = keyLight;
        CreateOrUpdateDirectionalLight("RuntimeFillLight", new Vector3(26f, 145f, 0f), new Color(0.8f, 0.9f, 1f), 1.05f, false);
        CreateOrUpdateDirectionalLight("RuntimeBounceLight", new Vector3(75f, 210f, 0f), new Color(1f, 0.94f, 0.85f), 0.6f, false);
    }

    private Light FindSceneDirectionalLight()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Light lightSource in lights)
        {
            if (lightSource.type == LightType.Directional && lightSource.transform.parent != transform)
                return lightSource;
        }

        return null;
    }

    private Light CreateOrUpdateDirectionalLight(string lightName, Vector3 eulerAngles, Color color, float intensity, bool castShadows)
    {
        Transform lightTransform = transform.Find(lightName);
        Light lightSource;

        if (lightTransform == null)
        {
            GameObject lightObject = new GameObject(lightName);
            lightObject.transform.SetParent(transform, false);
            lightSource = lightObject.AddComponent<Light>();
        }
        else
        {
            lightSource = lightTransform.GetComponent<Light>();

            if (lightSource == null)
                lightSource = lightTransform.gameObject.AddComponent<Light>();
        }

        ConfigureDirectionalLight(lightSource, eulerAngles, color, intensity, castShadows);
        return lightSource;
    }

    private void ConfigureDirectionalLight(Light lightSource, Vector3 eulerAngles, Color color, float intensity, bool castShadows)
    {
        lightSource.type = LightType.Directional;
        lightSource.color = color;
        lightSource.intensity = intensity;
        lightSource.shadows = castShadows ? LightShadows.Soft : LightShadows.None;
        lightSource.shadowStrength = castShadows ? 0.28f : 0f;
        lightSource.shadowBias = castShadows ? 0.04f : 0f;
        lightSource.shadowNormalBias = castShadows ? 0.25f : 0f;
        lightSource.transform.rotation = Quaternion.Euler(eulerAngles);
    }
}
