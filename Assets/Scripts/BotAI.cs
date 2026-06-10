using UnityEngine;

public enum BotTaskDecision
{
    Run,
    Fail,
    Die
}

public struct BotTaskContext
{
    public ChallengeKind ChallengeKind;
    public DifficultyLevel Difficulty;
    public float TimeLimit;
    public int RequiredPresses;
    public int SequenceLength;
    public float RoundPressure;
}

public struct BotPersonality
{
    public float Reaction;
    public float Discipline;
    public float Stamina;
    public float Memory;
    public float Composure;
    public float Speed;
}

public struct BotTaskOutcome
{
    public BotTaskDecision Decision;
    public float SolveDelay;
    public float StopDelay;
    public float FatalDelay;
    public string Reason;
}

public static class BotTaskResolver
{
    public static BotTaskOutcome Resolve(BotPersonality personality, BotTaskContext context, float distanceToGoal01)
    {
        float finishPressure = 1f - Mathf.Clamp01(distanceToGoal01);
        float urgency = Mathf.Clamp01(Mathf.Lerp(context.RoundPressure, 1f, finishPressure));

        switch (context.ChallengeKind)
        {
            case ChallengeKind.PressKey:
                return ResolvePressKey(personality, context, urgency);

            case ChallengeKind.SpamKey:
                return ResolveSpamKey(personality, context, urgency);

            case ChallengeKind.Sequence:
                return ResolveSequence(personality, context, urgency);

            default:
                return new BotTaskOutcome
                {
                    Decision = BotTaskDecision.Fail,
                    Reason = "could not read the task."
                };
        }
    }

    private static BotTaskOutcome ResolvePressKey(BotPersonality personality, BotTaskContext context, float urgency)
    {
        float responseTime = Mathf.Lerp(1.05f, 0.18f, personality.Reaction);
        responseTime += urgency * Mathf.Lerp(0.05f, 0.22f, 1f - personality.Composure);
        float confidence = personality.Reaction * 0.65f + personality.Discipline * 0.35f;

        if (responseTime <= context.TimeLimit * 0.92f && confidence >= 0.36f)
        {
            return new BotTaskOutcome
            {
                Decision = BotTaskDecision.Run,
                SolveDelay = responseTime,
                StopDelay = ComputeStopDelay(personality, urgency, 0.18f),
                Reason = "read the key cue cleanly."
            };
        }

        return new BotTaskOutcome
        {
            Decision = BotTaskDecision.Fail,
            Reason = "missed the key cue."
        };
    }

    private static BotTaskOutcome ResolveSpamKey(BotPersonality personality, BotTaskContext context, float urgency)
    {
        float outputRate = Mathf.Lerp(2.8f, 8.2f, (personality.Stamina + personality.Reaction) * 0.5f);
        float requiredTime = context.RequiredPresses / outputRate;
        requiredTime += urgency * Mathf.Lerp(0.08f, 0.25f, 1f - personality.Composure);
        float staminaThreshold = Mathf.Lerp(0.22f, 0.52f, Mathf.InverseLerp(4f, 12f, context.RequiredPresses));

        if (requiredTime <= context.TimeLimit * 0.95f && personality.Stamina >= staminaThreshold)
        {
            return new BotTaskOutcome
            {
                Decision = BotTaskDecision.Run,
                SolveDelay = requiredTime,
                StopDelay = ComputeStopDelay(personality, urgency, 0.42f + (1f - personality.Stamina) * 0.28f),
                Reason = "powered through the mash test."
            };
        }

        return new BotTaskOutcome
        {
            Decision = BotTaskDecision.Fail,
            Reason = "ran out of stamina on the mash test."
        };
    }

    private static BotTaskOutcome ResolveSequence(BotPersonality personality, BotTaskContext context, float urgency)
    {
        float memoryDemand = Mathf.Lerp(0.35f, 0.7f, Mathf.InverseLerp(2f, 4f, context.SequenceLength));
        float solveTime = context.SequenceLength * Mathf.Lerp(0.48f, 0.24f, personality.Memory);
        solveTime += 0.18f + urgency * Mathf.Lerp(0.05f, 0.18f, 1f - personality.Composure);
        float memoryScore = personality.Memory * 0.55f + personality.Discipline * 0.25f + personality.Reaction * 0.2f;

        if (memoryScore >= memoryDemand && solveTime <= context.TimeLimit * 0.95f)
        {
            return new BotTaskOutcome
            {
                Decision = BotTaskDecision.Run,
                SolveDelay = solveTime,
                StopDelay = ComputeStopDelay(personality, urgency, 0.56f),
                Reason = "decoded the sequence."
            };
        }

        bool panicDeath = memoryScore < memoryDemand && personality.Composure < 0.42f && urgency > 0.45f;

        if (panicDeath)
        {
            return new BotTaskOutcome
            {
                Decision = BotTaskDecision.Die,
                FatalDelay = Mathf.Min(context.TimeLimit * 0.75f, solveTime + 0.08f),
                Reason = "panicked after mixing up the sequence."
            };
        }

        return new BotTaskOutcome
        {
            Decision = BotTaskDecision.Fail,
            Reason = "froze after forgetting the sequence."
        };
    }

    private static float ComputeStopDelay(BotPersonality personality, float urgency, float taskStress)
    {
        float stopScore = personality.Discipline * 0.72f + personality.Composure * 0.28f;
        stopScore -= taskStress * 0.22f;
        stopScore -= urgency * 0.25f;

        if (stopScore >= 0.45f)
            return 0f;

        return Mathf.Clamp(0.18f + (0.45f - stopScore) * 0.35f, 0.18f, 0.35f);
    }
}

public class BotAI : MonoBehaviour
{
    private enum BotState
    {
        WaitingForTask,
        SolvingTask,
        Running,
        Blocked,
        FatalMistake,
        Finished,
        Dead
    }

    private Transform targetEnd;
    private Transform deathZone;
    private GameManager gameManager;
    private BotPersonality personality;
    private BotState currentState = BotState.WaitingForTask;
    private ChallengeKind currentTaskKind;
    private bool isInDeathZone;
    private bool isDying;
    private float solveTimer;
    private float fatalTimer;
    private float stopDelayRemaining;
    private float initialDistanceToGoal = 1f;
    private string pendingFatalReason = string.Empty;

    [SerializeField]
    private float speed = 2.5f;

    [SerializeField]
    private Animator anim;

    [SerializeField]
    private AudioSource feetSteps;

    [SerializeField]
    private AudioSource shoot;

    public bool IsCompeting => !isDying && currentState != BotState.Dead && currentState != BotState.Finished;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        targetEnd = GameObject.Find("TargetEnd")?.transform;
        deathZone = GameObject.Find("DeathZone")?.transform;
        speed = Mathf.Max(1.4f, speed - Random.Range(0f, 0.8f));
        InitializePersonality();

        if (targetEnd != null)
            initialDistanceToGoal = Mathf.Max(0.1f, Vector3.Distance(transform.position, targetEnd.position));
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null || targetEnd == null)
            return;

        if (gameManager.IsRoundFinished)
        {
            Stop();
            return;
        }

        if (isDying || currentState == BotState.Dead || currentState == BotState.Finished)
            return;

        UpdateFatalMistake();

        if (isDying || currentState == BotState.Dead)
            return;

        if (gameManager.IsGreenLight)
            UpdateGreenBehaviour();
        else
            UpdateRedBehaviour();
    }

    public BotTaskOutcome AssignTask(BotTaskContext context)
    {
        if (!IsCompeting)
            return default;

        currentTaskKind = context.ChallengeKind;

        BotTaskOutcome outcome = BotTaskResolver.Resolve(personality, context, GetDistanceToGoal01());
        stopDelayRemaining = outcome.StopDelay;
        pendingFatalReason = outcome.Reason;

        switch (outcome.Decision)
        {
            case BotTaskDecision.Run:
                currentState = BotState.SolvingTask;
                solveTimer = outcome.SolveDelay;
                fatalTimer = 0f;
                Stop();
                break;

            case BotTaskDecision.Die:
                currentState = BotState.FatalMistake;
                fatalTimer = Mathf.Max(0.05f, outcome.FatalDelay);
                solveTimer = 0f;
                Stop();
                break;

            default:
                currentState = BotState.Blocked;
                solveTimer = 0f;
                fatalTimer = 0f;
                Stop();
                break;
        }

        return outcome;
    }

    public void HandleRedLightStarted()
    {
        if (currentState == BotState.SolvingTask)
            currentState = BotState.Blocked;
    }

    private void InitializePersonality()
    {
        personality = new BotPersonality
        {
            Reaction = Random.Range(0.35f, 0.95f),
            Discipline = Random.Range(0.35f, 0.95f),
            Stamina = Random.Range(0.35f, 0.95f),
            Memory = Random.Range(0.3f, 0.95f),
            Composure = Random.Range(0.3f, 0.95f),
            Speed = Mathf.InverseLerp(1.4f, 3f, speed)
        };
    }

    private void UpdateFatalMistake()
    {
        if (currentState != BotState.FatalMistake)
            return;

        fatalTimer -= Time.deltaTime;

        if (fatalTimer <= 0f)
            Die("Bot eliminated: " + pendingFatalReason);
    }

    private void UpdateGreenBehaviour()
    {
        switch (currentState)
        {
            case BotState.SolvingTask:
                solveTimer -= Time.deltaTime;
                Stop();

                if (solveTimer <= 0f)
                    currentState = BotState.Running;
                break;

            case BotState.Running:
                Walk();

                if (HasReachedFinish())
                    ReachFinish();
                break;

            default:
                Stop();
                break;
        }
    }

    private void UpdateRedBehaviour()
    {
        if (currentState == BotState.Running && stopDelayRemaining > 0f)
        {
            stopDelayRemaining -= Time.deltaTime;
            Walk();
            CheckRedLightDeath();
            return;
        }

        if (currentState == BotState.Running || currentState == BotState.SolvingTask)
            currentState = BotState.Blocked;

        Stop();
    }

    private void Walk()
    {
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetEnd.position, step);
        UpdateAnimator(true, false, false);

        if (feetSteps != null)
        {
            feetSteps.loop = true;

            if (!feetSteps.isPlaying)
                feetSteps.Play(0);
        }
    }

    private void Stop()
    {
        UpdateAnimator(false, true, false);

        if (feetSteps != null)
        {
            feetSteps.loop = false;
            feetSteps.Stop();
        }
    }

    private void CheckRedLightDeath()
    {
        if (!gameManager.IsRedLight || !isInDeathZone)
            return;

        Die("Bot eliminated: " + BuildRedLightDeathReason());
    }

    private string BuildRedLightDeathReason()
    {
        switch (currentTaskKind)
        {
            case ChallengeKind.PressKey:
                return "it hesitated on the key cue and stopped too late.";

            case ChallengeKind.SpamKey:
                return "it exhausted itself on the mash task and could not stop.";

            case ChallengeKind.Sequence:
                return "it panicked on the sequence and kept moving during RED LIGHT.";

            default:
                return "it moved during RED LIGHT.";
        }
    }

    private bool HasReachedFinish()
    {
        return Vector3.Distance(transform.position, targetEnd.position) <= 0.05f;
    }

    private float GetDistanceToGoal01()
    {
        if (targetEnd == null)
            return 1f;

        return Mathf.Clamp01(Vector3.Distance(transform.position, targetEnd.position) / initialDistanceToGoal);
    }

    private void ReachFinish()
    {
        currentState = BotState.Finished;
        Stop();
    }

    private void Die(string reason)
    {
        if (isDying)
            return;

        isDying = true;
        currentState = BotState.Dead;
        Stop();
        UpdateAnimator(false, false, true);
        shoot?.Play(0);
        gameManager?.ReportBotElimination(reason);
    }

    private void UpdateAnimator(bool run, bool stopping, bool die)
    {
        if (anim == null)
            return;

        SetBoolIfExists("run", run);
        SetBoolIfExists("stopping", stopping);
        SetBoolIfExists("die", die);
        SetBoolIfExists("isWalking", run);
        SetBoolIfExists("isDying", die);
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (deathZone != null && other.transform == deathZone)
            isInDeathZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (deathZone != null && other.transform == deathZone)
            isInDeathZone = false;
    }
}
