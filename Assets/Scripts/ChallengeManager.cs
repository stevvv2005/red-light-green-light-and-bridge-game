using System.Text;
using UnityEngine;

public enum ChallengeKind
{
    PressKey,
    SpamKey,
    Sequence
}

public struct ChallengeSnapshot
{
    public bool IsVisible;
    public bool IsActive;
    public string Title;
    public string Description;
    public string Progress;
    public float RemainingTime;
    public float TotalTime;
}

public class ChallengeManager : MonoBehaviour
{
    private readonly KeyCode[] singleKeyPool = { KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.C };
    private readonly KeyCode[] sequenceKeyPool = { KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.C, KeyCode.X, KeyCode.V };

    private GameManager gameManager;
    private ChallengeKind currentKind;
    private bool activeChallenge;
    private bool challengeFailedThisGreen;
    private KeyCode targetKey;
    private int requiredPresses;
    private int currentPresses;
    private KeyCode[] sequence;
    private int sequenceIndex;
    private float remainingTime;
    private float totalTime;
    private string title;
    private string description;

    public void Configure(GameManager manager)
    {
        gameManager = manager;
    }

    private void Update()
    {
        if (!activeChallenge || gameManager == null || gameManager.IsRoundFinished)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            FailChallenge("Challenge failed - wait for next GREEN LIGHT");
            return;
        }

        switch (currentKind)
        {
            case ChallengeKind.PressKey:
                ProcessPressKey();
                break;
            case ChallengeKind.SpamKey:
                ProcessSpamKey();
                break;
            case ChallengeKind.Sequence:
                ProcessSequence();
                break;
        }
    }

    public BotTaskContext BeginGreenChallenge(DifficultyLevel difficulty, float greenDuration)
    {
        challengeFailedThisGreen = false;
        activeChallenge = true;
        currentPresses = 0;
        sequenceIndex = 0;
        requiredPresses = 0;
        sequence = null;

        currentKind = GetRandomChallenge(difficulty);
        totalTime = CalculateChallengeTime(difficulty, greenDuration);
        remainingTime = totalTime;

        switch (currentKind)
        {
            case ChallengeKind.PressKey:
                targetKey = singleKeyPool[Random.Range(0, singleKeyPool.Length)];
                title = "PRESS THE KEY";
                description = "Press " + targetKey + " before RED LIGHT returns";
                break;

            case ChallengeKind.SpamKey:
                targetKey = singleKeyPool[Random.Range(0, singleKeyPool.Length)];
                requiredPresses = GetSpamCount(difficulty);
                title = "MASH THE KEY";
                description = "Press " + targetKey + " " + requiredPresses + " times quickly";
                break;

            case ChallengeKind.Sequence:
                sequence = BuildSequence(difficulty);
                title = "TYPE THE SEQUENCE";
                description = FormatSequence(sequence);
                break;
        }

        return BuildBotTaskContext(difficulty);
    }

    public void EndCurrentWindow()
    {
        activeChallenge = false;
    }

    public void ResetManager()
    {
        activeChallenge = false;
        challengeFailedThisGreen = false;
        currentPresses = 0;
        sequenceIndex = 0;
        requiredPresses = 0;
        sequence = null;
        remainingTime = 0f;
        totalTime = 0f;
        title = string.Empty;
        description = string.Empty;
    }

    public ChallengeSnapshot GetSnapshot()
    {
        ChallengeSnapshot snapshot = new ChallengeSnapshot
        {
            IsVisible = activeChallenge || challengeFailedThisGreen,
            IsActive = activeChallenge,
            Title = title,
            Description = description,
            RemainingTime = remainingTime,
            TotalTime = totalTime
        };

        switch (currentKind)
        {
            case ChallengeKind.PressKey:
                snapshot.Progress = activeChallenge ? "Target key: " + targetKey : "Blocked until next GREEN LIGHT";
                break;
            case ChallengeKind.SpamKey:
                snapshot.Progress = activeChallenge
                    ? currentPresses + " / " + requiredPresses
                    : "Blocked until next GREEN LIGHT";
                break;
            case ChallengeKind.Sequence:
                snapshot.Progress = activeChallenge
                    ? "Step " + (sequenceIndex + 1) + " / " + sequence.Length
                    : "Blocked until next GREEN LIGHT";
                break;
        }

        return snapshot;
    }

    private void ProcessPressKey()
    {
        if (Input.GetKeyDown(targetKey))
            SucceedChallenge();
    }

    private void ProcessSpamKey()
    {
        if (!Input.GetKeyDown(targetKey))
            return;

        currentPresses++;

        if (currentPresses >= requiredPresses)
            SucceedChallenge();
    }

    private void ProcessSequence()
    {
        foreach (KeyCode key in sequenceKeyPool)
        {
            if (!Input.GetKeyDown(key))
                continue;

            if (key == sequence[sequenceIndex])
            {
                sequenceIndex++;

                if (sequenceIndex >= sequence.Length)
                    SucceedChallenge();
            }
            else
            {
                FailChallenge("Wrong sequence - wait for next GREEN LIGHT");
            }

            return;
        }
    }

    private void SucceedChallenge()
    {
        activeChallenge = false;
        challengeFailedThisGreen = false;
        gameManager.HandleChallengeSuccess();
    }

    private void FailChallenge(string failMessage)
    {
        activeChallenge = false;
        challengeFailedThisGreen = true;
        title = "CHALLENGE FAILED";
        description = failMessage;
        gameManager.HandleChallengeFailed(failMessage);
    }

    private ChallengeKind GetRandomChallenge(DifficultyLevel difficulty)
    {
        int roll = Random.Range(0, 100);

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                if (roll < 60) return ChallengeKind.PressKey;
                if (roll < 90) return ChallengeKind.SpamKey;
                return ChallengeKind.Sequence;

            case DifficultyLevel.Hard:
                if (roll < 15) return ChallengeKind.PressKey;
                if (roll < 45) return ChallengeKind.SpamKey;
                return ChallengeKind.Sequence;

            default:
                if (roll < 35) return ChallengeKind.PressKey;
                if (roll < 70) return ChallengeKind.SpamKey;
                return ChallengeKind.Sequence;
        }
    }

    private float CalculateChallengeTime(DifficultyLevel difficulty, float greenDuration)
    {
        float baseTime;
        float reservedRunTime;

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                baseTime = 2.7f;
                reservedRunTime = 1.75f;
                break;
            case DifficultyLevel.Hard:
                baseTime = 1.45f;
                reservedRunTime = 1.25f;
                break;
            default:
                baseTime = 2.05f;
                reservedRunTime = 1.45f;
                break;
        }

        float maxAllowed = Mathf.Max(0.9f, greenDuration - reservedRunTime);
        return Mathf.Min(baseTime, maxAllowed);
    }

    private int GetSpamCount(DifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                return Random.Range(4, 6);
            case DifficultyLevel.Hard:
                return Random.Range(9, 13);
            default:
                return Random.Range(6, 9);
        }
    }

    private KeyCode[] BuildSequence(DifficultyLevel difficulty)
    {
        int length;

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                length = 2;
                break;
            case DifficultyLevel.Hard:
                length = 4;
                break;
            default:
                length = 3;
                break;
        }

        KeyCode[] result = new KeyCode[length];

        for (int i = 0; i < length; i++)
            result[i] = sequenceKeyPool[Random.Range(0, sequenceKeyPool.Length)];

        return result;
    }

    private string FormatSequence(KeyCode[] keys)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < keys.Length; i++)
        {
            builder.Append(keys[i]);

            if (i < keys.Length - 1)
                builder.Append(" - ");
        }

        return builder.ToString();
    }

    private BotTaskContext BuildBotTaskContext(DifficultyLevel difficulty)
    {
        return new BotTaskContext
        {
            ChallengeKind = currentKind,
            Difficulty = difficulty,
            TimeLimit = totalTime,
            RequiredPresses = requiredPresses,
            SequenceLength = sequence != null ? sequence.Length : 0,
            RoundPressure = gameManager != null ? gameManager.GetRoundProgress01() : 0f
        };
    }
}
