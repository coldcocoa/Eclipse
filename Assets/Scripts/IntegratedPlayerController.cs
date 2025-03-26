using UnityEngine;

public class IntegratedPlayerController : MonoBehaviour
{
    [Header("ĳ���� ������ ����")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("ī�޶� ����")]
    [SerializeField] private Transform cameraHolder;        // ī�޶� ���� ȸ����(ĳ���� �Ӹ� ��ó)
    [SerializeField] private float cameraSensitivity = 2f;
    //[SerializeField] private float cameraDistance = 5f;     // �Ϲ� ������ �� ī�޶� �Ÿ�
    //[SerializeField] private float cameraHeight = 2f;       // �ʿ� �� ��� (����� cameraHolder ��ġ�� ��ü ����)
    [SerializeField] private Vector2 cameraYRotationLimit = new Vector2(-40, 70);

    [Header("�ִϸ��̼� ����")]
    [SerializeField] private Animator animator;

    // ĳ���� ��Ʈ�ѷ� �� ī�޶� ����
    private CharacterController controller;
    private Transform playerCamera;

    // ī�޶� ���� ����
    private float cameraRotationX = 0f;

    // �̵� ���� ����
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;
    private bool isJumping;
    private bool isCrouching;
    private bool isProne;
    private bool isAiming;
    private bool isShooting;
    private bool isReloading;

    // �ִϸ��̼� ���� ������
    public enum PlayerAnimState
    {
        Idle,
        Walk,
        Run,
        Jump,
        Fall,
        Crouch,
        Prone,
        Aim,
        Shoot,
        Reload,
        Hurt,
        Death
    }

    // ���� �ִϸ��̼� ����
    private PlayerAnimState currentAnimState = PlayerAnimState.Idle;

    // �ִϸ��̼� �Ķ���� �̸�
    private readonly string ANIM_PARAM_STATE = "AnimState";
    private readonly string ANIM_PARAM_SPEED = "Speed";
    private readonly string ANIM_PARAM_IS_GROUNDED = "IsGrounded";
    private readonly string ANIM_PARAM_IS_AIMING = "IsAiming";

    private void Awake()
    {
        // �ʿ��� ������Ʈ ���� ��������
        controller = GetComponent<CharacterController>();
        playerCamera = Camera.main.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        // ���콺 Ŀ�� ���
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // �Է� �� ���� ����
        ProcessInputs();

        // �̵� ó��
        HandleMovement();

        // ���� ó��
        HandleJump();

        // ī�޶� ȸ�� ó��
        HandleCameraRotation();

        // ī�޶� ��ġ ������Ʈ
        UpdateCameraPosition();

        // �ִϸ��̼� ���� ����
        DetermineAnimationState();

        // �ִϸ��̼� ����
        ApplyAnimationState();
    }

    private void ProcessInputs()
    {
        // �⺻ �̵� �� �׼� �Է� ����
        isGrounded = controller.isGrounded;
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isJumping = Input.GetButtonDown("Jump") && isGrounded;
        isCrouching = Input.GetKey(KeyCode.C);
        isProne = Input.GetKey(KeyCode.X);
        isAiming = Input.GetMouseButton(1);  // ��Ŭ�� ����
        isShooting = Input.GetMouseButtonDown(0) && isAiming; // ��Ŭ�� �߻� (���� ��)
        isReloading = Input.GetKeyDown(KeyCode.R); // RŰ ������
    }

    private void HandleMovement()
    {
        // �̵� �ӵ� ���� (�޸���/�ȱ�)
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // �ɱ�/���帮�� ���¿��� �ӵ� ����
        if (isCrouching)
            currentSpeed *= 0.5f;
        else if (isProne)
            currentSpeed *= 0.25f;

        // �̵� �Է� ó��
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // ĳ���� �������� �̵� ���� ���
        Vector3 moveDirection = transform.forward * vertical + transform.right * horizontal;
        moveDirection.Normalize();

        // �̵� ����
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // �߷� ����
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        // ���� ó��
        if (isJumping)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    /// <summary>
    /// ���콺 �Է��� �޾� ī�޶� Ȧ��(�Ӹ� ��)�� �÷��̾ ȸ����ŵ�ϴ�.
    /// - ����ȸ��(X��): cameraHolder�� ���� ȸ��
    /// - ����ȸ��(Y��): ĳ���� ��ü�� ȸ��( transform.Rotate )
    /// </summary>
    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        // ���� ȸ�� (X �� ����)
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, cameraYRotationLimit.x, cameraYRotationLimit.y);
        cameraHolder.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        // ���� ȸ�� (Y �� ����) - �÷��̾� ��ü�� �¿�� ���� ����
        transform.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// ī�޶� cameraHolder ����(���� �ݴ� ����)���� ��ġ�ϸ鼭,
    /// ����ĳ��Ʈ�� �� � ������ �Ÿ��� �����մϴ�. ����������
    /// cameraHolder�� ȸ���� �״�� ���󰡵���(cameraHolder.rotation) �����մϴ�.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // ���� ���̸� ī�޶� ��¦ �� ������
       // float targetDistance = isAiming ? cameraDistance * 0.6f : cameraDistance;

        // cameraHolder ���� �������� targetDistance��ŭ ������ ��ġ ����
        Vector3 desiredPosition = cameraHolder.position - cameraHolder.forward ;

        // ����ĳ��Ʈ�� �� �����Ͽ� �浹 �� ī�޶� �ʹ� �հ� ���� �ʰ� ó��
        RaycastHit hit;
        if (Physics.Linecast(cameraHolder.position, desiredPosition, out hit))
        {
            desiredPosition = hit.point + hit.normal * 0.2f;
        }

        // ī�޶� ��ġ (ȸ���� cameraHolder�� ȸ���� �״�� ���)
        playerCamera.position = desiredPosition;
        playerCamera.rotation = cameraHolder.rotation;
    }

    private void DetermineAnimationState()
    {
        // �̵� �ӵ� ���
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float moveSpeedValue = horizontalVelocity.magnitude;

        // �켱���� ������� �ִϸ��̼� ���� ����
        if (!isGrounded && playerVelocity.y < -0.1f)
        {
            SetAnimationState(PlayerAnimState.Fall);
        }
        else if (isJumping || (!isGrounded && playerVelocity.y > 0))
        {
            SetAnimationState(PlayerAnimState.Jump);
        }
        else if (isShooting)
        {
            SetAnimationState(PlayerAnimState.Shoot);
        }
        else if (isReloading)
        {
            SetAnimationState(PlayerAnimState.Reload);
        }
        else if (isProne)
        {
            SetAnimationState(PlayerAnimState.Prone);
        }
        else if (isCrouching)
        {
            SetAnimationState(PlayerAnimState.Crouch);
        }
        else if (isAiming)
        {
            SetAnimationState(PlayerAnimState.Aim);
        }
        else if (moveSpeedValue > 0.1f)
        {
            if (isSprinting)
                SetAnimationState(PlayerAnimState.Run);
            else
                SetAnimationState(PlayerAnimState.Walk);
        }
        else
        {
            SetAnimationState(PlayerAnimState.Idle);
        }
    }

    private void SetAnimationState(PlayerAnimState newState)
    {
        if (newState != currentAnimState)
        {
            currentAnimState = newState;
        }
    }

    private void ApplyAnimationState()
    {
        // �̵� �ӵ� ���
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float moveSpeedValue = horizontalVelocity.magnitude;

        // �⺻ �ִϸ��̼� �Ķ���� ������Ʈ
        animator.SetFloat(ANIM_PARAM_SPEED, moveSpeedValue);
        animator.SetBool(ANIM_PARAM_IS_GROUNDED, isGrounded);
        animator.SetBool(ANIM_PARAM_IS_AIMING, isAiming);

        switch (currentAnimState)
        {
            case PlayerAnimState.Idle:
                animator.SetInteger(ANIM_PARAM_STATE, 0);
                break;

            case PlayerAnimState.Walk:
                animator.SetInteger(ANIM_PARAM_STATE, 1);
                break;

            case PlayerAnimState.Run:
                animator.SetInteger(ANIM_PARAM_STATE, 2);
                break;

            case PlayerAnimState.Jump:
                animator.SetInteger(ANIM_PARAM_STATE, 3);
                break;

            case PlayerAnimState.Fall:
                animator.SetInteger(ANIM_PARAM_STATE, 4);
                break;

            case PlayerAnimState.Crouch:
                animator.SetInteger(ANIM_PARAM_STATE, 5);
                break;

            case PlayerAnimState.Prone:
                animator.SetInteger(ANIM_PARAM_STATE, 6);
                break;

            case PlayerAnimState.Aim:
                animator.SetInteger(ANIM_PARAM_STATE, 7);
                break;

            case PlayerAnimState.Shoot:
                animator.SetInteger(ANIM_PARAM_STATE, 8);
                // Ʈ���� �ִϸ��̼� ����ϴ� ���
                animator.SetTrigger("Shoot");
                break;

            case PlayerAnimState.Reload:
                animator.SetInteger(ANIM_PARAM_STATE, 9);
                // Ʈ���� �ִϸ��̼� ����ϴ� ���
                animator.SetTrigger("Reload");
                break;

            case PlayerAnimState.Hurt:
                animator.SetInteger(ANIM_PARAM_STATE, 10);
                break;

            case PlayerAnimState.Death:
                animator.SetInteger(ANIM_PARAM_STATE, 11);
                break;

            default:
                animator.SetInteger(ANIM_PARAM_STATE, 0); // �⺻���� Idle
                break;
        }
    }

    // �ܺο��� ȣ�� ������ ���� �޼���
    public void PlayHurtAnimation()
    {
        SetAnimationState(PlayerAnimState.Hurt);
        ApplyAnimationState();
    }

    public void PlayDeathAnimation()
    {
        SetAnimationState(PlayerAnimState.Death);
        ApplyAnimationState();
    }

    // ����׿� �ð�ȭ (�ʿ�� ���)
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // ī�޶� Ȧ���� ���� ī�޶� ��ġ ���̸� ���� ������ ǥ��
        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraHolder.position, playerCamera.position);
    }
}
