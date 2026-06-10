using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Vector3 playerMovementInput;
    private Vector2 playerMouseInput;
    private float xRot;
    private bool isDying;
    private bool isInDeathZone;
    private GameManager gameManager;

    [SerializeField]
    private Animator anim;

    [SerializeField]
    private LayerMask FloorMask;

    [SerializeField]
    private Transform FeetTransform;

    [SerializeField]
    private Transform PlayerCamera;

    [SerializeField]
    private Rigidbody PlayerBody;

    [Space]

    [SerializeField]
    private float Speed = 3f;

    [SerializeField]
    private float Sensitivity = 3f;

    [SerializeField]
    private float Jumpforce;

    [SerializeField]
    private AudioSource feetSteps;

    [SerializeField]
    private AudioSource shoot;

    [SerializeField]
    private Transform deathZone;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        if (gameManager.IsRoundFinished)
        {
            StopPlayerMotion();
            UpdateAnimator(false, false, isDying);
            return;
        }

        if (isDying)
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        playerMovementInput = new Vector3(h, 0f, v).normalized;
        playerMouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        MovePlayerCamera();

        bool wantsMove = playerMovementInput.sqrMagnitude > 0.01f;
        bool canMove = gameManager.CanPlayerMove;

        if (canMove)
            MovePlayer();
        else
            StopPlayerMotion();

        HandleFootsteps(canMove && wantsMove);
        CheckDeathTime();
        UpdateAnimator(canMove && wantsMove, !canMove && wantsMove, isDying);
    }

    private void MovePlayer()
    {
        Vector3 moveVector = transform.TransformDirection(playerMovementInput) * Speed;
        PlayerBody.linearVelocity = new Vector3(moveVector.x, PlayerBody.linearVelocity.y, moveVector.z);
    }

    private void StopPlayerMotion()
    {
        PlayerBody.linearVelocity = new Vector3(0f, PlayerBody.linearVelocity.y, 0f);
        feetSteps.Stop();
    }

    private void MovePlayerCamera()
    {
        xRot -= playerMouseInput.y * Sensitivity;
        xRot = Mathf.Clamp(xRot, -65f, 65f);

        transform.Rotate(0f, playerMouseInput.x * Sensitivity, 0f);

        if (PlayerCamera != null)
            PlayerCamera.localRotation = Quaternion.Euler(xRot, 0f, 0f);
    }

    private void CheckDeathTime()
    {
        if (!gameManager.IsRedLight || !isInDeathZone)
            return;

        Vector3 horizontalVelocity = PlayerBody.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude < 0.05f)
            return;

        Die();
    }

    public void ForceElimination()
    {
        ExecuteDeath(false);
    }

    private void Die()
    {
        ExecuteDeath(true);
    }

    private void ExecuteDeath(bool notifyGameManager)
    {
        if (isDying)
            return;

        isDying = true;
        feetSteps.Stop();
        StopPlayerMotion();

        if (shoot != null)
            shoot.Play(0);

        UpdateAnimator(false, false, true);

        if (notifyGameManager)
            gameManager.HandlePlayerCaught();
    }

    private void HandleFootsteps(bool running)
    {
        if (feetSteps == null)
            return;

        if (running)
        {
            feetSteps.loop = true;

            if (!feetSteps.isPlaying)
                feetSteps.Play(0);
        }
        else
        {
            feetSteps.loop = false;
            feetSteps.Stop();
        }
    }

    private void UpdateAnimator(bool running, bool stopping, bool dying)
    {
        if (anim == null)
            return;

        SetBoolIfExists("run", running);
        SetBoolIfExists("stopping", stopping);
        SetBoolIfExists("die", dying);

        SetBoolIfExists("isWalking", running);
        SetBoolIfExists("isDying", dying);

        if (HasParameter("isJumping"))
        {
            bool isJumping = FeetTransform != null && !Physics.CheckSphere(FeetTransform.position, 0.1f, FloorMask);
            anim.SetBool("isJumping", isJumping);
        }
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasParameter(parameterName))
            anim.SetBool(parameterName, value);
    }

    private bool HasParameter(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in anim.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
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
