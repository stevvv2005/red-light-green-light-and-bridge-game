using UnityEngine;

public class DollWatcher : MonoBehaviour
{
    private GameManager gameManager;
    private Transform head;
    private AudioSource dollSing;
    private AudioSource dollHeadOff;
    private AudioSource dollHeadOn;
    private DifficultyLevel difficulty;
    private bool cycleActive;
    private bool isWatching;
    private float phaseTimer;

    [SerializeField]
    private float rotationSpeed = 4f;

    public bool IsWatching => isWatching;

    public void Configure(GameManager manager, Transform headReference, AudioSource sing, AudioSource headOff, AudioSource headOn)
    {
        gameManager = manager;
        head = headReference;
        dollSing = sing;
        dollHeadOff = headOff;
        dollHeadOn = headOn;
    }

    public void StartCycle(DifficultyLevel selectedDifficulty)
    {
        difficulty = selectedDifficulty;
        cycleActive = true;
        BeginGreenLight();
    }

    public void StopCycle()
    {
        cycleActive = false;

        if (dollSing != null)
            dollSing.Stop();
    }

    private void Update()
    {
        if (!cycleActive || gameManager == null || gameManager.IsRoundFinished)
            return;

        phaseTimer -= Time.deltaTime;

        if (phaseTimer <= 0f)
        {
            if (isWatching)
                BeginGreenLight();
            else
                BeginRedLight();
        }

        RotateHead();
    }

    private void BeginGreenLight()
    {
        isWatching = false;
        phaseTimer = GetGreenDuration();

        dollHeadOff?.Play(0);

        if (dollSing != null)
        {
            dollSing.Stop();
            dollSing.PlayDelayed(0.3f);
        }

        gameManager.HandleGreenLightStarted(phaseTimer);
    }

    private void BeginRedLight()
    {
        isWatching = true;
        phaseTimer = GetRedDuration();

        if (dollSing != null)
            dollSing.Stop();

        dollHeadOn?.Play(0);
        gameManager.HandleRedLightStarted(phaseTimer);
    }

    private float GetGreenDuration()
    {
        float progress = gameManager.GetRoundProgress01();

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                return Random.Range(5f, 6.4f) * Mathf.Lerp(1f, 0.9f, progress);
            case DifficultyLevel.Hard:
                return Random.Range(3.2f, 4.1f) * Mathf.Lerp(1f, 0.82f, progress);
            default:
                return Random.Range(4f, 5.1f) * Mathf.Lerp(1f, 0.86f, progress);
        }
    }

    private float GetRedDuration()
    {
        float progress = gameManager.GetRoundProgress01();

        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                return Random.Range(2f, 2.8f) * Mathf.Lerp(1f, 1.05f, progress);
            case DifficultyLevel.Hard:
                return Random.Range(2.8f, 3.8f) * Mathf.Lerp(1f, 1.12f, progress);
            default:
                return Random.Range(2.3f, 3.2f) * Mathf.Lerp(1f, 1.08f, progress);
        }
    }

    private void RotateHead()
    {
        if (head == null)
            return;

        float targetY = isWatching ? 180f : 0f;
        Quaternion targetRotation = Quaternion.Euler(head.localRotation.eulerAngles.x, targetY, head.localRotation.eulerAngles.z);
        head.localRotation = Quaternion.Lerp(head.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
