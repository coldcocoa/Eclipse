using UnityEngine;

public class IntegratedPlayerController : MonoBehaviour
{
    [Header("캐릭터 움직임 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("카메라 설정")]
    [SerializeField] private Transform cameraHolder;        // 카메라 기준 회전축(캐릭터 머리 근처)
    [SerializeField] private float cameraSensitivity = 2f;
    //[SerializeField] private float cameraDistance = 5f;     // 일반 상태일 때 카메라 거리
    //[SerializeField] private float cameraHeight = 2f;       // 필요 시 사용 (현재는 cameraHolder 위치로 대체 가능)
    [SerializeField] private Vector2 cameraYRotationLimit = new Vector2(-40, 70);

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;

    // 캐릭터 컨트롤러 및 카메라 참조
    private CharacterController controller;
    private Transform playerCamera;

    // 카메라 제어 변수
    private float cameraRotationX = 0f;

    // 이동 관련 변수
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;
    private bool isJumping;
    private bool isCrouching;
    private bool isProne;
    private bool isAiming;
    private bool isShooting;
    private bool isReloading;

    // 애니메이션 상태 열거형
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

    // 현재 애니메이션 상태
    private PlayerAnimState currentAnimState = PlayerAnimState.Idle;

    // 애니메이션 파라미터 이름
    private readonly string ANIM_PARAM_STATE = "AnimState";
    private readonly string ANIM_PARAM_SPEED = "Speed";
    private readonly string ANIM_PARAM_IS_GROUNDED = "IsGrounded";
    private readonly string ANIM_PARAM_IS_AIMING = "IsAiming";

    private void Awake()
    {
        // 필요한 컴포넌트 참조 가져오기
        controller = GetComponent<CharacterController>();
        playerCamera = Camera.main.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        // 마우스 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 입력 및 상태 감지
        ProcessInputs();

        // 이동 처리
        HandleMovement();

        // 점프 처리
        HandleJump();

        // 카메라 회전 처리
        HandleCameraRotation();

        // 카메라 위치 업데이트
        UpdateCameraPosition();

        // 애니메이션 상태 결정
        DetermineAnimationState();

        // 애니메이션 적용
        ApplyAnimationState();
    }

    private void ProcessInputs()
    {
        // 기본 이동 및 액션 입력 감지
        isGrounded = controller.isGrounded;
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isJumping = Input.GetButtonDown("Jump") && isGrounded;
        isCrouching = Input.GetKey(KeyCode.C);
        isProne = Input.GetKey(KeyCode.X);
        isAiming = Input.GetMouseButton(1);  // 우클릭 조준
        isShooting = Input.GetMouseButtonDown(0) && isAiming; // 좌클릭 발사 (조준 중)
        isReloading = Input.GetKeyDown(KeyCode.R); // R키 재장전
    }

    private void HandleMovement()
    {
        // 이동 속도 결정 (달리기/걷기)
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // 앉기/엎드리기 상태에서 속도 감소
        if (isCrouching)
            currentSpeed *= 0.5f;
        else if (isProne)
            currentSpeed *= 0.25f;

        // 이동 입력 처리
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 캐릭터 기준으로 이동 방향 계산
        Vector3 moveDirection = transform.forward * vertical + transform.right * horizontal;
        moveDirection.Normalize();

        // 이동 적용
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 중력 적용
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        // 점프 처리
        if (isJumping)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    /// <summary>
    /// 마우스 입력을 받아 카메라 홀더(머리 축)와 플레이어를 회전시킵니다.
    /// - 수직회전(X축): cameraHolder의 로컬 회전
    /// - 수평회전(Y축): 캐릭터 전체를 회전( transform.Rotate )
    /// </summary>
    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        // 수직 회전 (X 축 기준)
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, cameraYRotationLimit.x, cameraYRotationLimit.y);
        cameraHolder.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        // 수평 회전 (Y 축 기준) - 플레이어 전체가 좌우로 도는 느낌
        transform.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// 카메라를 cameraHolder 뒤쪽(전방 반대 방향)으로 배치하면서,
    /// 레이캐스트로 벽 등에 막히면 거리를 조절합니다. 마지막으로
    /// cameraHolder의 회전을 그대로 따라가도록(cameraHolder.rotation) 세팅합니다.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 조준 중이면 카메라 살짝 더 가깝게
       // float targetDistance = isAiming ? cameraDistance * 0.6f : cameraDistance;

        // cameraHolder 기준 뒤쪽으로 targetDistance만큼 떨어진 위치 산출
        Vector3 desiredPosition = cameraHolder.position - cameraHolder.forward ;

        // 레이캐스트로 벽 감지하여 충돌 시 카메라가 너무 뚫고 들어가지 않게 처리
        RaycastHit hit;
        if (Physics.Linecast(cameraHolder.position, desiredPosition, out hit))
        {
            desiredPosition = hit.point + hit.normal * 0.2f;
        }

        // 카메라 배치 (회전은 cameraHolder의 회전을 그대로 사용)
        playerCamera.position = desiredPosition;
        playerCamera.rotation = cameraHolder.rotation;
    }

    private void DetermineAnimationState()
    {
        // 이동 속도 계산
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float moveSpeedValue = horizontalVelocity.magnitude;

        // 우선순위 기반으로 애니메이션 상태 결정
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
        // 이동 속도 계산
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float moveSpeedValue = horizontalVelocity.magnitude;

        // 기본 애니메이션 파라미터 업데이트
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
                // 트리거 애니메이션 사용하는 경우
                animator.SetTrigger("Shoot");
                break;

            case PlayerAnimState.Reload:
                animator.SetInteger(ANIM_PARAM_STATE, 9);
                // 트리거 애니메이션 사용하는 경우
                animator.SetTrigger("Reload");
                break;

            case PlayerAnimState.Hurt:
                animator.SetInteger(ANIM_PARAM_STATE, 10);
                break;

            case PlayerAnimState.Death:
                animator.SetInteger(ANIM_PARAM_STATE, 11);
                break;

            default:
                animator.SetInteger(ANIM_PARAM_STATE, 0); // 기본값은 Idle
                break;
        }
    }

    // 외부에서 호출 가능한 공개 메서드
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

    // 디버그용 시각화 (필요시 사용)
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 카메라 홀더와 실제 카메라 위치 사이를 빨간 선으로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraHolder.position, playerCamera.position);
    }
}
