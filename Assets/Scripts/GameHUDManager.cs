using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SquidGameUI;

public class GameHUDManager : MonoBehaviour
{
    private static readonly Color Bg = new Color(0.04f, 0.04f, 0.06f, 0.14f);
    private static readonly Color Card = new Color(0.055f, 0.06f, 0.08f, 0.86f);
    private static readonly Color CardBorder = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color Pink = new Color(1f, 0.176f, 0.42f, 1f);
    private static readonly Color Green = new Color(0f, 0.784f, 0.353f, 1f);
    private static readonly Color Amber = new Color(1f, 0.72f, 0.22f, 1f);
    private static readonly Color TextSoft = new Color(0.72f, 0.78f, 0.86f, 1f);

    private GameManager gameManager;
    private ChallengeManager challengeManager;
    private Canvas canvas;
    private TMP_FontAsset displayFont;
    private TMP_FontAsset bodyFont;
    private Sprite circleSprite;
    private Sprite roundedSprite;

    private RectTransform hudRoot;
    private RectTransform centerPanel;
    private RectTransform eventPanel;
    private RectTransform resultPanel;
    private RectTransform resultCard;
    private CanvasGroup resultCardGroup;
    private RectTransform strikeRoot;

    private TMP_Text playerNumberText;
    private TMP_Text playerLabelText;
    private TMP_Text playerTaglineText;
    private TMP_Text phaseText;
    private TMP_Text difficultyText;
    private TMP_Text timerText;
    private TMP_Text challengeTitle;
    private TMP_Text challengeDescription;
    private TMP_Text challengeProgress;
    private TMP_Text challengeTimer;
    private TMP_Text promptText;
    private TMP_Text eventText;
    private TMP_Text resultTitle;
    private TMP_Text resultBody;
    private TMP_Text resultTime;
    private TMP_Text resultDistance;
    private TMP_Text resultChallenges;
    private TMP_Text resultStrikes;
    private TMP_Text resultStatLabelA;
    private TMP_Text resultStatLabelB;
    private TMP_Text resultStatLabelC;
    private TMP_Text resultStatLabelD;
    private TMP_Text resultStatValueA;
    private TMP_Text resultStatValueB;
    private TMP_Text resultStatValueC;
    private TMP_Text resultStatValueD;
    private Button primaryButton;
    private Button secondaryButton;
    private TMP_Text primaryButtonLabel;
    private TMP_Text secondaryButtonLabel;
    private Image timerFill;
    private Image[] strikeDots;
    private Image flashOverlay;
    private Image resultBadge;
    private Image resultGlow;
    private int lastFailCount = -1;
    private Coroutine eventRoutine;

    public void Configure(GameManager manager, ChallengeManager managerChallenge, Text existingTimerText)
    {
        gameManager = manager;
        challengeManager = managerChallenge;
        EnsureCanvas();
        EnsureFonts();
        EnsureAssets();
        BuildHUD(existingTimerText);
        EnsureEventSystem();
    }

    public void ShowStartState(DifficultyLevel difficulty)
    {
        if (resultPanel != null)
            resultPanel.gameObject.SetActive(false);

        if (hudRoot != null)
            hudRoot.gameObject.SetActive(true);

        ShowRoundState("GET READY", Color.white);
        if (difficultyText != null)
            difficultyText.text = difficulty.ToString().ToUpperInvariant();
        ShowEvent("Three task fails and you are eliminated.", Amber, 4f);
    }

    public void Refresh(float timeRemaining, float totalTime, RoundPhase phase, DifficultyLevel difficulty, ChallengeSnapshot snapshot)
    {
        if (timerText != null)
            timerText.text = FormatTime(timeRemaining);

        if (difficultyText != null)
            difficultyText.text = difficulty.ToString().ToUpperInvariant();

        UpdateStrikeUI();
        UpdateTimerRing(timeRemaining, totalTime);
        UpdateChallenge(snapshot, phase);
        UpdatePrompt(phase, snapshot);
    }

    public void ShowRoundState(string message, Color color)
    {
        if (phaseText == null)
            return;

        phaseText.text = message;
        phaseText.color = color;
    }

    public void ShowResult(string title, string body, string primaryLabel, UnityAction primaryAction, string secondaryLabel, UnityAction secondaryAction)
    {
        if (resultPanel == null)
            return;

        bool isWin = title.ToLowerInvariant().Contains("win");

        resultPanel.gameObject.SetActive(true);
        if (resultCard != null)
        {
            resultCard.sizeDelta = GetResultCardSize();
            resultCard.anchoredPosition = new Vector2(0f, 42f);
            resultCard.localScale = Vector3.one * 0.96f;
        }

        if (resultCardGroup != null)
            resultCardGroup.alpha = 0f;

        if (hudRoot != null)
            hudRoot.gameObject.SetActive(false);
        if (eventPanel != null)
            eventPanel.gameObject.SetActive(false);

        resultTitle.text = isWin ? "YOU WIN" : "GAME OVER";
        resultBody.text = body;
        resultBody.alignment = TextAlignmentOptions.Center;
        resultBody.color = TextSoft;

        if (resultTitle != null)
            resultTitle.color = isWin ? Green : Pink;
        if (resultGlow != null)
            resultGlow.color = isWin ? new Color(0.2f, 1f, 0.6f, 0.11f) : new Color(1f, 0.18f, 0.42f, 0.12f);

        resultStatLabelA.text = "Time Survived";
        resultStatValueA.text = FormatDuration(gameManager != null ? gameManager.TimeSurvived : 0f);
        resultStatLabelB.text = isWin ? "Challenges" : "Challenges Failed";
        resultStatValueB.text = gameManager != null ? gameManager.ChallengesPassed.ToString() : "0";
        resultStatLabelC.text = "Distance";
        resultStatValueC.text = (gameManager != null ? gameManager.DistanceCovered.ToString("0.0") : "0.0") + " m";
        resultStatLabelD.text = "Strikes";
        resultStatValueD.text = gameManager != null ? gameManager.PlayerFailCount + " / " + gameManager.PlayerFailLimit : "0 / 3";

        string firstLabel = isWin ? "Continue" : "Replay";
        UnityAction firstAction = isWin ? primaryAction : primaryAction;
        UnityAction secondActionResolved = secondaryAction;
        string secondLabel = isWin ? "Replay" : "Main Menu";

        if (isWin)
        {
            firstAction = primaryAction;
            secondActionResolved = secondaryAction;
        }

        BindButton(primaryButton, primaryButtonLabel, firstLabel, firstAction);
        BindButton(secondaryButton, secondaryButtonLabel, secondLabel, secondActionResolved);

        if (resultCardGroup != null)
            UIAnimationHelper.FadeScaleIn(resultCardGroup, resultCard, 0.24f);
    }

    public void ShowEvent(string message, Color color, float duration = 3f)
    {
        if (eventText == null)
            return;

        if (eventRoutine != null)
            StopCoroutine(eventRoutine);

        eventRoutine = StartCoroutine(EventRoutine(message, color, duration));
    }

    private void BuildHUD(Text existingTimerText)
    {
        if (canvas == null)
            return;

        hudRoot = CreateRect("HUDRoot", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hudRoot.gameObject.AddComponent<CanvasGroup>();

        CreateImage("SubtleVignette", hudRoot, Bg).raycastTarget = false;

        RectTransform topBar = CreateRect("TopBar", hudRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        topBar.sizeDelta = new Vector2(0f, 132f);
        CreateImage("TopBarFill", topBar, new Color(0.02f, 0.02f, 0.03f, 0.88f)).raycastTarget = false;
        CreateLine("TopLine", topBar, Pink, new Vector2(0f, -1f), new Vector2(1f, 2f));

        BuildPlayerBadge(topBar);
        BuildPhaseBanner(topBar);
        BuildStrikeTimer(topBar, existingTimerText);

        centerPanel = CreateResponsiveCard(
            "ChallengeBanner",
            hudRoot,
            new Vector2(0.16f, 1f),
            new Vector2(0.84f, 1f),
            new Vector2(0f, -142f),
            new Vector2(0f, 132f),
            Card);
        centerPanel.gameObject.SetActive(true);
        RectTransform accent = CreateRect("Accent", centerPanel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 5f), new Vector2(0.5f, 1f));
        accent.anchoredPosition = new Vector2(0f, -5f);
        accent.gameObject.AddComponent<Image>().color = Pink;
        challengeTitle = CreateText("ChallengeTitle", centerPanel, "GREEN LIGHT", 25, Green, TextAlignmentOptions.Center, new Vector2(0f, -20f), new Vector2(900f, 32f));
        challengeDescription = CreateText("ChallengeDescription", centerPanel, "Wait for the next cue. Move only when allowed.", 17, TextSoft, TextAlignmentOptions.Center, new Vector2(0f, -55f), new Vector2(860f, 42f));
        challengeProgress = CreateText("ChallengeProgress", centerPanel, "Blocked until next GREEN LIGHT", 15, Amber, TextAlignmentOptions.Center, new Vector2(-82f, -96f), new Vector2(620f, 24f));
        challengeTimer = CreateText("ChallengeTimer", centerPanel, "0.0s", 15, Amber, TextAlignmentOptions.Center, new Vector2(290f, -96f), new Vector2(120f, 24f));
        centerPanel.gameObject.SetActive(false);

        BuildBottomPrompt();
        BuildEventPanel();
        BuildResultPanel();

        if (existingTimerText != null)
            existingTimerText.gameObject.SetActive(false);
    }

    private void BuildPlayerBadge(Transform parent)
    {
        RectTransform badge = CreateCard("PlayerBadge", parent, new Vector2(44f, -16f), new Vector2(0f, 1f), new Vector2(330f, 82f), new Color(0f, 0f, 0f, 0.72f));
        CreateCircle("BadgeDot", badge, Pink, new Vector2(36f, -41f), 44f);
        playerNumberText = CreateText("PlayerNumber", badge, "067", 20, Color.white, TextAlignmentOptions.Center, new Vector2(14f, -25f), new Vector2(44f, 28f));
        playerLabelText = CreateText("PlayerLabel", badge, "PLAYER 067", 18, Color.white, TextAlignmentOptions.Left, new Vector2(84f, -20f), new Vector2(200f, 24f));
        playerTaglineText = CreateText("PlayerTagline", badge, "Survive. Win. Repeat.", 13, TextSoft, TextAlignmentOptions.Left, new Vector2(84f, -46f), new Vector2(220f, 20f));
    }

    private void BuildPhaseBanner(Transform parent)
    {
        RectTransform phasePanel = CreateCard("PhasePanel", parent, new Vector2(0f, -16f), new Vector2(0.5f, 1f), new Vector2(390f, 82f), new Color(0f, 0f, 0f, 0.74f));
        phaseText = CreateText("PhaseText", phasePanel, "PAUSED", 28, Pink, TextAlignmentOptions.Center, new Vector2(0f, -23f), new Vector2(320f, 36f));
        difficultyText = CreateText("DifficultyText", phasePanel, "MEDIUM", 13, TextSoft, TextAlignmentOptions.Center, new Vector2(0f, -54f), new Vector2(120f, 18f));
    }

    private void BuildStrikeTimer(Transform parent, Text existingTimerText)
    {
        RectTransform panel = CreateCard("RightPanel", parent, new Vector2(-44f, -16f), new Vector2(1f, 1f), new Vector2(290f, 82f), new Color(0f, 0f, 0f, 0.72f));
        strikeRoot = CreateRect("StrikeRoot", panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(124f, 34f), new Vector2(0f, 0.5f));
        strikeRoot.anchoredPosition = new Vector2(22f, 0f);

        strikeDots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image dot = CreateCircle("Strike_" + i, strikeRoot, new Color(1f, 0.176f, 0.42f, 0.2f), new Vector2(16f + i * 34f, 0f), 18f);
            strikeDots[i] = dot;
        }

        RectTransform timerRing = CreateRect("TimerRing", panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(52f, 52f), new Vector2(1f, 0.5f));
        timerRing.anchoredPosition = new Vector2(-20f, 0f);
        CreateCircle("TimerRingBg", timerRing, new Color(1f, 1f, 1f, 0.08f), Vector2.zero, 48f);
        timerFill = CreateCircle("TimerRingFill", timerRing, Green, Vector2.zero, 48f);
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Radial360;
        timerFill.fillClockwise = true;
        timerFill.fillAmount = 1f;

        if (existingTimerText != null)
        {
            timerText = existingTimerText.GetComponent<TMP_Text>();
            if (timerText == null)
            {
                GameObject timerGo = existingTimerText.gameObject;
                timerText = timerGo.AddComponent<TextMeshProUGUI>();
                Destroy(existingTimerText);
            }
        }

        if (timerText == null)
            timerText = CreateText("TimerText", panel, "02:00", 24, Color.white, TextAlignmentOptions.Center, new Vector2(-20f, -19f), new Vector2(104f, 28f));
        else
        {
            timerText.transform.SetParent(panel, false);
            timerText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void BuildBottomPrompt()
    {
        RectTransform promptPanel = CreateCard("PromptPanel", hudRoot, new Vector2(0f, 28f), new Vector2(0.5f, 0f), new Vector2(540f, 72f), new Color(0f, 0f, 0f, 0.68f));
        promptText = CreateText("PromptText", promptPanel, "DON'T MOVE", 24, Pink, TextAlignmentOptions.Center, new Vector2(0f, -22f), new Vector2(500f, 28f));
    }

    private void BuildEventPanel()
    {
        eventPanel = CreateResponsiveCard(
            "EventPanel",
            hudRoot,
            new Vector2(0.22f, 1f),
            new Vector2(0.78f, 1f),
            new Vector2(0f, -284f),
            new Vector2(0f, 52f),
            new Color(0f, 0f, 0f, 0.66f));
        eventText = CreateText("EventText", eventPanel, "", 18, TextSoft, TextAlignmentOptions.Center, new Vector2(0f, -16f), new Vector2(660f, 22f));
        eventPanel.gameObject.SetActive(true);
    }

    private void BuildResultPanel()
    {
        resultPanel = CreateRect("ResultPanel", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        CreateImage("ResultBackdrop", resultPanel, new Color(0.03f, 0.03f, 0.04f, 0.58f)).raycastTarget = false;

        Vector2 cardSize = GetResultCardSize();
        resultCard = CreateCard("ResultCard", resultPanel, Vector2.zero, new Vector2(0.5f, 0.5f), cardSize, new Color(0.07f, 0.08f, 0.1f, 0.9f));
        resultCard.anchoredPosition = new Vector2(0f, 42f);
        resultCardGroup = resultCard.GetComponent<CanvasGroup>();
        resultCard.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.45f);

        resultGlow = CreateCircle("ResultGlow", resultCard, new Color(1f, 0.18f, 0.42f, 0.12f), new Vector2(0f, -34f), 180f);
        resultGlow.transform.SetAsFirstSibling();

        RectTransform titleGroup = CreateRect("TitleGroup", resultCard, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(cardSize.x - 80f, 92f), new Vector2(0.5f, 1f));
        titleGroup.anchoredPosition = new Vector2(0f, -36f);
        resultTitle = CreateText("ResultTitle", titleGroup, "GAME OVER", 38, Pink, TextAlignmentOptions.Center, Vector2.zero, new Vector2(cardSize.x - 100f, 46f));
        resultTitle.rectTransform.anchoredPosition = new Vector2(0f, -6f);
        resultTitle.gameObject.AddComponent<Shadow>().effectColor = new Color(1f, 0.14f, 0.38f, 0.45f);

        resultBody = CreateText("ResultBody", resultCard, "You failed 3 tasks. Wait for the next GREEN LIGHT.", 18, TextSoft, TextAlignmentOptions.Center, new Vector2(0f, -88f), new Vector2(cardSize.x - 92f, 48f));

        float statGap = 10f;
        float statWidth = Mathf.Floor((cardSize.x - 100f - statGap) * 0.5f);
        float statLeftX = -statWidth * 0.5f - statGap * 0.5f;
        float statRightX = statWidth * 0.5f + statGap * 0.5f;
        CreateStatCard("StatTime", resultCard, new Vector2(statLeftX, -144f), new Vector2(statWidth, 56f), out resultStatLabelA, out resultStatValueA);
        CreateStatCard("StatChallenges", resultCard, new Vector2(statRightX, -144f), new Vector2(statWidth, 56f), out resultStatLabelB, out resultStatValueB);
        CreateStatCard("StatDistance", resultCard, new Vector2(statLeftX, -208f), new Vector2(statWidth, 56f), out resultStatLabelC, out resultStatValueC);
        CreateStatCard("StatStrikes", resultCard, new Vector2(statRightX, -208f), new Vector2(statWidth, 56f), out resultStatLabelD, out resultStatValueD);

        RectTransform buttonRow = CreateRect("ButtonRow", resultCard, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(cardSize.x - 72f, 50f), new Vector2(0.5f, 0f));
        buttonRow.anchoredPosition = new Vector2(0f, 26f);
        float buttonGap = 12f;
        float buttonWidth = Mathf.Min(200f, Mathf.Floor((buttonRow.sizeDelta.x - buttonGap) * 0.5f));
        float buttonOffset = (buttonWidth * 0.5f) + (buttonGap * 0.5f);
        primaryButton = CreateButton("PrimaryButton", buttonRow, new Vector2(-buttonOffset, 0f), new Vector2(buttonWidth, 50f), Pink, out primaryButtonLabel);
        secondaryButton = CreateButton("SecondaryButton", buttonRow, new Vector2(buttonOffset, 0f), new Vector2(buttonWidth, 50f), new Color(0.13f, 0.14f, 0.17f, 1f), out secondaryButtonLabel);

        resultPanel.gameObject.SetActive(false);
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color, out TMP_Text label)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), size, new Vector2(0.5f, 0.5f));
        rect.anchoredPosition = anchoredPosition;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        label = CreateText(name + "_Label", rect, name, 16, Color.white, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(16f, 10f));
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        label.rectTransform.offsetMin = new Vector2(8f, 4f);
        label.rectTransform.offsetMax = new Vector2(-8f, -4f);
        label.textWrappingMode = TextWrappingModes.NoWrap;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
        outline.effectDistance = new Vector2(1f, -1f);

        return button;
    }

    private void CreateStatCard(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, out TMP_Text label, out TMP_Text value)
    {
        RectTransform card = CreateRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), size, new Vector2(0.5f, 1f));
        card.anchoredPosition = anchoredPosition;

        Image image = card.gameObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.12f, 0.16f, 0.9f);
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;

        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
        outline.effectDistance = new Vector2(1f, -1f);

        label = CreateText(name + "_Label", card, "", 11, TextSoft, TextAlignmentOptions.Left, new Vector2(14f, -11f), new Vector2(size.x - 28f, 14f));
        value = CreateText(name + "_Value", card, "", 16, Color.white, TextAlignmentOptions.Left, new Vector2(14f, -31f), new Vector2(size.x - 28f, 20f));
    }

    private Vector2 GetResultCardSize()
    {
        float width = Screen.width <= 640f
            ? Mathf.Clamp(Screen.width * 0.9f, 300f, 520f)
            : Mathf.Clamp(Screen.width * 0.34f, 420f, 520f);
        float height = Screen.height <= 900f
            ? Mathf.Clamp(Screen.height * 0.5f, 360f, 500f)
            : Mathf.Clamp(Screen.height * 0.44f, 420f, 520f);
        return new Vector2(width, height);
    }

    private void UpdateChallenge(ChallengeSnapshot snapshot, RoundPhase phase)
    {
        if (centerPanel == null)
            return;

        bool show = snapshot.IsVisible || phase == RoundPhase.GreenFailed;
        centerPanel.gameObject.SetActive(show && !gameManager.IsRoundFinished);
        if (!show || gameManager.IsRoundFinished)
            return;

        challengeTitle.text = string.IsNullOrWhiteSpace(snapshot.Title) ? "CHALLENGE" : snapshot.Title.ToUpperInvariant();
        challengeDescription.text = string.IsNullOrWhiteSpace(snapshot.Description)
            ? "Wait for the next cue."
            : snapshot.Description;
        challengeProgress.text = string.IsNullOrWhiteSpace(snapshot.Progress)
            ? "Blocked until next GREEN LIGHT"
            : snapshot.Progress;
        challengeTimer.text = snapshot.IsActive && snapshot.TotalTime > 0f
            ? snapshot.RemainingTime.ToString("0.0") + "s"
            : "WAIT";

        Color accent = snapshot.IsActive ? Green : Amber;
        challengeTitle.color = accent;
        challengeProgress.color = accent;
        challengeTimer.color = accent;
    }

    private void UpdatePrompt(RoundPhase phase, ChallengeSnapshot snapshot)
    {
        if (promptText == null)
            return;

        switch (phase)
        {
            case RoundPhase.RedLight:
                promptText.text = "DON'T MOVE";
                promptText.color = Pink;
                break;
            case RoundPhase.GreenChallenge:
                promptText.text = snapshot.IsVisible && snapshot.IsActive ? "SOLVE THE TASK" : "RUN";
                promptText.color = Green;
                break;
            case RoundPhase.GreenRun:
                promptText.text = "RUN!";
                promptText.color = Green;
                break;
            case RoundPhase.GreenFailed:
                promptText.text = "WAIT FOR GREEN LIGHT";
                promptText.color = Amber;
                break;
            case RoundPhase.Won:
                promptText.text = "ROUND CLEARED";
                promptText.color = Green;
                break;
            case RoundPhase.Lost:
                promptText.text = "ELIMINATED";
                promptText.color = Pink;
                break;
        }
    }

    private void UpdateStrikeUI()
    {
        if (gameManager == null || strikeDots == null)
            return;

        int failCount = gameManager.PlayerFailCount;
        if (failCount != lastFailCount)
        {
            if (failCount > lastFailCount && failCount > 0)
                TriggerStrikeFeedback(Mathf.Min(failCount - 1, strikeDots.Length - 1));

            lastFailCount = failCount;
        }

        for (int i = 0; i < strikeDots.Length; i++)
            strikeDots[i].color = i < failCount ? Pink : new Color(1f, 0.176f, 0.42f, 0.18f);
    }

    private void UpdateTimerRing(float timeRemaining, float totalTime)
    {
        if (timerFill == null || totalTime <= 0f)
            return;

        timerFill.fillAmount = Mathf.Clamp01(timeRemaining / totalTime);
        timerFill.color = timeRemaining <= 10f ? Pink : Green;
    }

    private void TriggerStrikeFeedback(int index)
    {
        if (flashOverlay == null)
        {
            flashOverlay = CreateImage("FlashOverlay", canvas.transform, new Color(1f, 0.176f, 0.42f, 0f));
            RectTransform flashRect = flashOverlay.rectTransform;
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            flashOverlay.raycastTarget = false;
        }

        StartCoroutine(StrikeFlash());
        UIAnimationHelper.PunchScale(strikeDots[index].rectTransform, 0.22f);
    }

    private IEnumerator StrikeFlash()
    {
        Color c = flashOverlay.color;
        c.a = 0.45f;
        flashOverlay.color = c;
        yield return new WaitForSecondsRealtime(0.12f);
        c.a = 0f;
        flashOverlay.color = c;
    }

    private IEnumerator EventRoutine(string message, Color color, float duration)
    {
        eventText.text = message;
        eventText.color = color;
        eventPanel.gameObject.SetActive(true);
        eventPanel.localScale = Vector3.one * 0.96f;
        UIAnimationHelper.FadeScaleIn(eventPanel.GetComponent<CanvasGroup>(), eventPanel, 0.2f);

        yield return new WaitForSecondsRealtime(duration);

        if (eventPanel != null)
            eventPanel.gameObject.SetActive(false);
    }

    private void BindButton(Button button, TMP_Text label, string buttonLabel, UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        label.text = buttonLabel.ToUpperInvariant();
    }

    private void EnsureCanvas()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.42f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureFonts()
    {
        bodyFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/Roboto-Bold SDF");
        displayFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/Anton SDF");

        if (bodyFont == null)
            bodyFont = Resources.Load<TMP_FontAsset>("TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF");
        if (displayFont == null)
            displayFont = bodyFont;
    }

    private void EnsureAssets()
    {
        circleSprite = Resources.Load<Sprite>("UI/MenuSprites/CircleFilled");
        if (circleSprite == null)
            circleSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f);

        roundedSprite = CreateRoundedRectangleSprite(32, 10);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private RectTransform CreateCard(string name, Transform parent, Vector2 anchoredPosition, Vector2 anchor, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, size, new Vector2(0.5f, 0.5f));
        rect.anchoredPosition = anchoredPosition;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = CardBorder;
        outline.effectDistance = new Vector2(1f, -1f);
        rect.gameObject.AddComponent<CanvasGroup>();
        return rect;
    }

    private RectTransform CreateResponsiveCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, sizeDelta, new Vector2(0.5f, 1f));
        rect.anchoredPosition = anchoredPosition;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = CardBorder;
        outline.effectDistance = new Vector2(1f, -1f);
        rect.gameObject.AddComponent<CanvasGroup>();
        return rect;
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private Image CreateCircle(string name, Transform parent, Color color, Vector2 anchoredPosition, float size)
    {
        Image image = CreateImage(name, parent, color);
        image.sprite = circleSprite;
        image.type = Image.Type.Simple;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
        return image;
    }

    private void CreateLine(string name, Transform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, new Vector2(1f, 1f), size, new Vector2(0.5f, 0.5f));
        rect.anchoredPosition = anchoredPosition;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
    }

    private TMP_Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = content;
        text.font = fontSize >= 28 ? displayFont : bodyFont;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(11f, fontSize * 0.72f);
        text.fontSizeMax = fontSize;
        text.fontStyle = fontSize >= 24 ? FontStyles.Bold : FontStyles.Normal;
        text.characterSpacing = fontSize >= 28 ? 2f : 0f;
        return text;
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainder = Mathf.FloorToInt(seconds % 60f);
        return minutes.ToString("00") + ":" + remainder.ToString("00");
    }

    private string FormatDuration(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainder = Mathf.FloorToInt(seconds % 60f);
        return minutes.ToString("00") + ":" + remainder.ToString("00");
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
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(px, py));
                texture.SetPixel(x, y, distance <= edge ? solid : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, Vector4.one * radius);
    }
}
