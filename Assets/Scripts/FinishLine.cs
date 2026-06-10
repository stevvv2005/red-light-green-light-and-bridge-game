using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private GameManager gameManager;

    public void Configure(GameManager manager)
    {
        gameManager = manager;
        EnsureTriggerMode();
    }

    private void Awake()
    {
        EnsureTriggerMode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.RoundFinished)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        gameManager?.HandlePlayerWin();
    }

    private void EnsureTriggerMode()
    {
        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }
}
