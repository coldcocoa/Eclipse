using UnityEngine;
using UnityEngine.UI;

public class IntegratedPlayerController : MonoBehaviour
{
    [Header("캐릭터 움직임 설정")]
    [SerializeField] private float moveSpeed = 5f;      // 일반 걷기 속도
    [SerializeField] private float sprintSpeed = 10f;   // 달리기(Shift)
    [SerializeField] private float crouchSpeed = 2f;    // 앉아서 걷기 속도
    [SerializeField] private float crouchJogSpeed = 4f; // 앉아서 빠른 이동 속도
    [SerializeField] private float gravityValue = -9.81f;

    [Header("카메라 설정")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float cameraSensitivity = 2f;
    [SerializeField] private Vector2 cameraYRotationLimit = new Vector2(-40, 70);

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;

    [Header("체력 & 스태미나")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxStamina = 50f;
    [SerializeField] private float currentStamina = 50f;
    [SerializeField] private float staminaDecreaseRate = 5f;
    [SerializeField] private float staminaRecoverRate = 3f;

    [Header("UI")]
    [SerializeField] private Image hpBar;       // HP바 (Fill)
    [SerializeField] private Image staminaBar;  // 스태미나 바 (Fill)

    // 캐릭터 컨트롤러 및 카메라
    private CharacterController controller;
    private Transform playerCamera;

    // 카메라 회전 제어
    private float cameraRotationX = 0f;

    // 이동 관련
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;

    // “현재 ‘에이밍 모드’인가”를 나타내는 플래그
    private bool isAiming = false;
    private bool isShooting = false;

    // 시간 관련
    private float lastShootTime = 0f;   // 마지막으로 총을 쏜 시점
    private float takeStartTime = 0f;   // ‘Take’ 애니메이션 시작 시점

    // ■■■ 열거형: Normal / Aiming 각각 세부 상태 나눔 ■■■
    public enum PlayerAnimState
    {
        // --- Normal 쪽 ---
        NormalIdle,
        Walk,
        Run,
        CrouchIdle,
        CrouchWalk,
        CrouchJog,
        Take,          // 노말 -> 에이밍 전환
        Die1,          // 체력 0시 사망

        // --- Aiming 쪽 ---
        AimIdle,
        AimShoot,
        AimJog,        // Shift+W
        AimWalkF,      // W
        AimWalkB,      // S
        AimWalkR,      // D
        AimWalkL,      // A
        AimCrouchIdle, // Aim 상태 + Ctrl
        AimCrouchWalk, // Aim + Ctrl + W
        AimCrouchShoot // Aim + Ctrl + 좌클릭
    }

    private PlayerAnimState currentAnimState = PlayerAnimState.NormalIdle;

    // 애니메이터 파라미터
    //private readonly string ANIM_PARAM_STATE = "AnimState";
    private readonly string ANIM_PARAM_SPEED = "Speed";

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        var cam = Camera.main;
        if (cam != null) playerCamera = cam.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        // 마우스 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // HP, Stamina 초기화
        currentHP = maxHP;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;

        // ------ 이동 처리 ------
        ProcessMovement();
        ApplyGravity();

        // ------ 카메라 처리 ------
        HandleCameraRotation();
        UpdateCameraPosition();

        // ------ HP / 스태미나 ------
        HandleStamina();
        HandleHP();

        // ------ 마우스/키보드 입력 ------
        HandleInput();

        // ------ Normal / Aiming 상태 분기 ------
        if (!isAiming)
        {
            // Normal 모드
            UpdateNormalState();
        }
        else
        {
            // Aiming 모드
            UpdateAimingState();
        }

        // ------ HUD 업데이트 ------
        UpdateHUD();

        // ------ 최종 애니메이션 적용 ------
        ApplyAnimationState();
    }

    // =========================================================
    //  노말 상태 처리
    // =========================================================
    private void UpdateNormalState()
    {
        // 1) ‘Take’ 애니메이션 중인지 여부
        if (currentAnimState == PlayerAnimState.Take)
        {
            // Take 애니메이션이 끝났다면 => AimIdle로 전환
            // 여기선 “1초”라고 가정
            if (Time.time - takeStartTime > 1f)
            {
                isAiming = true; // 이제 에이밍 모드 전환
                SetAnimationState(PlayerAnimState.AimIdle);
            }
            return;
        }

        // 2) “10초 내에서만 NormalIdle 발동” 조건
        //    예: 마지막 사격 시점(lastShootTime)으로부터 10초가 넘으면 Idle 불가 등
        //    여기서는 단순히 “사격 후 10초 이내면 Idle 가능”이라고 가정
        float timeSinceShoot = Time.time - lastShootTime;

        // 3) 이동/입력 체크해서 상태 결정
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // crouch 세분화: Idle / Walk / Jog
        if (isCrouching)
        {
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
            {
                // 제자리 앉기
                SetAnimationState(PlayerAnimState.CrouchIdle);
            }
            else
            {
                if (shiftPressed && v > 0.1f)
                {
                    // Crouch Jog
                    SetAnimationState(PlayerAnimState.CrouchJog);
                }
                else
                {
                    // Crouch Walk
                    SetAnimationState(PlayerAnimState.CrouchWalk);
                }
            }
        }
        else
        {
            // 앉아있지 않으면 Normal Idle / Walk / Run
            // SHIFT + W => Run
            bool forwardPressed = (v > 0.1f);
            if (forwardPressed && isSprinting)
            {
                SetAnimationState(PlayerAnimState.Run);
            }
            else if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                SetAnimationState(PlayerAnimState.Walk);
            }
            else
            {
                // IDLE
                // “10초 이내만 Idle 가능”이라면? 
                // 여기서는 10초가 넘었으면 Idle 못 간다고 가정(또는 다른 처리)
                if (timeSinceShoot <= 10f)
                {
                    SetAnimationState(PlayerAnimState.NormalIdle);
                }
                // timeSinceShoot > 10f 라면, Idle 대신 다른 상태 유지할 수도 있음
            }
        }
    }

    // =========================================================
    //  에이밍 상태 처리
    // =========================================================
    private void UpdateAimingState()
    {
        // 1) “6초간 좌클릭 안 하면 노말 복귀”
        if (Time.time - lastShootTime > 6f)
        {
            // 에이밍 모드 해제
            isAiming = false;
            // 노말 Idle 로 복귀
            SetAnimationState(PlayerAnimState.NormalIdle);
            return;
        }

        // 2) crouch 여부
        bool isCtrl = Input.GetKey(KeyCode.LeftControl);
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 3) 좌클릭 시 Shoot (에이밍 자세에서)
        //    이미 HandleInput()에서 lastShootTime 갱신, isShooting = true 세팅
        //    아래에선 상태만 바꿔줌
        if (isShooting)
        {
            // 앉아있으면 AimCrouchShoot, 아니면 AimShoot
            if (isCtrl)
            {
                SetAnimationState(PlayerAnimState.AimCrouchShoot);
            }
            else
            {
                SetAnimationState(PlayerAnimState.AimShoot);
            }
            // 한 번만 처리
            isShooting = false;
            return;
        }

        // 4) 이동 방향 + Shift 확인해서 AimJog / AimWalkF/B/R/L 등 분기
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

        if (isCtrl)
        {
            // 앉은 에이밍
            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
            {
                // 제자리 앉기
                SetAnimationState(PlayerAnimState.AimCrouchIdle);
            }
            else
            {
                // 걷기
                SetAnimationState(PlayerAnimState.AimCrouchWalk);
            }
        }
        else
        {
            // 서 있는 에이밍
            if (shiftPressed && v > 0.1f)
            {
                // 앞으로 조깅
                SetAnimationState(PlayerAnimState.AimJog);
            }
            else
            {
                // 방향별로 AimWalkF/B/L/R
                if (v > 0.1f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkF);
                }
                else if (v < -0.1f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkB);
                }
                else if (h > 0.1f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkR);
                }
                else if (h < -0.1f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkL);
                }
                else
                {
                    SetAnimationState(PlayerAnimState.AimIdle);
                }
            }
        }
    }

    // =========================================================
    //  이동 / 중력 처리
    // =========================================================
    private void ProcessMovement()
    {
        // 달리는 중이면 sprintSpeed, 앉아있으면 조금씩 다른 속도로
        float finalSpeed = moveSpeed;

        // 노말에서 Shift => Run
        if (!isAiming && isSprinting)
        {
            finalSpeed = sprintSpeed;
        }

        // 앉아있을 때
        if (isCrouching)
        {
            // Aiming이든 노말이든 “앉아서 걷기 속도”를 우선 베이스로
            finalSpeed = crouchSpeed;
            // 혹시 “CrouchJog” 상태라면 crouchJogSpeed를 쓸 수도 있음
            // (Shift+Ctrl+W 등)
            if (isSprinting)
            {
                finalSpeed = crouchJogSpeed;
            }
        }

        // 에이밍 중이지만 Shift+W => AimJog라면
        // (실제론 이동속도 약간 다르게 할 수도 있음)
        if (isAiming)
        {
            // 필요하다면 “조준 중 속도 감소” 같은 것 적용 가능
            // ex) finalSpeed *= 0.8f;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 moveDir = (transform.forward * v) + (transform.right * h);
        moveDir.Normalize();

        controller.Move(moveDir * finalSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // =========================================================
    //  키 입력: 좌클릭으로 Take, 에이밍 중 사격, etc.
    // =========================================================
    private void HandleInput()
    {
        // 스프린트 & 크라우치
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        // 좌클릭
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAiming)
            {
                // 노말 상태에서 좌클릭 => Take 애니메이션
                // (단, 이미 사망 상태가 아니라는 전제)
                if (currentAnimState != PlayerAnimState.Die1)
                {
                    StartTakeAnimation();
                }
            }
            else
            {
                // 에이밍 중 좌클릭 => Shoot
                isShooting = true;
                lastShootTime = Time.time;
            }
        }
    }

    private void StartTakeAnimation()
    {
        takeStartTime = Time.time;
        SetAnimationState(PlayerAnimState.Take);
    }

    // =========================================================
    //  HP / 스태미나 처리
    // =========================================================
    private void HandleStamina()
    {
        // 달리기 중(Shift)라면 감소
        // 단, 이동 입력이 있는지 magnitude로 체크
        if (isSprinting && controller.velocity.magnitude > 0.1f)
        {
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
            if (currentStamina < 0f)
            {
                currentStamina = 0f;
                // 스태미나가 0이면 더 달릴 수 없음
                isSprinting = false;
            }
        }
        else
        {
            // 회복
            currentStamina += staminaRecoverRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
        }
    }

    private void HandleHP()
    {
        if (currentHP <= 0f)
        {
            // 이미 Die1 상태가 아니면 사망 처리
            if (currentAnimState != PlayerAnimState.Die1)
            {
                SetAnimationState(PlayerAnimState.Die1);
            }
        }
    }

    // 외부에서 데미지 주는 함수
    public void TakeDamage(float dmg)
    {
        if (currentAnimState == PlayerAnimState.Die1) return; // 이미 사망

        currentHP -= dmg;
        if (currentHP < 0f) currentHP = 0f;
    }

    // =========================================================
    //  카메라 회전 & 위치
    // =========================================================
    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        // 상하 회전
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, cameraYRotationLimit.x, cameraYRotationLimit.y);
        cameraHolder.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        // 좌우 회전
        transform.Rotate(Vector3.up, mouseX);
    }

    private void UpdateCameraPosition()
    {
        playerCamera.position = cameraHolder.position;
        playerCamera.rotation = cameraHolder.rotation;
    }

    // =========================================================
    //  최종적으로 Animator에 파라미터 주입
    // =========================================================
    private void ApplyAnimationState()
    {
        // 이동속도(좌우xz만)
        Vector3 horizontalVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float speedValue = horizontalVel.magnitude;

        animator.SetFloat(ANIM_PARAM_SPEED, speedValue);

        // 실제 애니메이션 상태에 따라 Animator 파라미터(AnimState) 세팅
        // 아래 숫자는 예시이며, 실제 Animator에서 설정한 대로 맞추시면 됩니다.
        switch (currentAnimState)
        {
            // ---------- Normal 쪽 ----------
            case PlayerAnimState.NormalIdle:
                animator.SetTrigger("IDLE");
                break;
            case PlayerAnimState.Walk:
                animator.SetTrigger("WALK");
                break;
            case PlayerAnimState.Run:
                animator.SetTrigger("RUN");
                break;
            case PlayerAnimState.CrouchIdle:
                animator.SetTrigger("CROUCH IDLE");
                break;
            case PlayerAnimState.CrouchWalk:
                animator.SetTrigger("CROUCH WALK");
                break;
            case PlayerAnimState.CrouchJog:
                animator.SetTrigger("CROUCH JOG");
                break;
            case PlayerAnimState.Take:
                animator.SetTrigger("TAKE");
                break;
            case PlayerAnimState.Die1:
                animator.SetTrigger("DIE1");
                break;

            // ---------- Aiming 쪽 ----------
            case PlayerAnimState.AimIdle:
                animator.SetTrigger("IDLE 0");
                break;
            case PlayerAnimState.AimShoot:
                animator.SetTrigger("SHOOT");
                // Shoot 트리거가 필요하다면 animator.SetTrigger("Shoot") 추가 가능
                break;
            case PlayerAnimState.AimJog:
                animator.SetTrigger("JOG");
                break;
            case PlayerAnimState.AimWalkF:
                animator.SetTrigger("WALK F");
                break;
            case PlayerAnimState.AimWalkB:
                animator.SetTrigger("WALK B");
                break;
            case PlayerAnimState.AimWalkR:
                animator.SetTrigger("WALK R");
                break;
            case PlayerAnimState.AimWalkL:
                animator.SetTrigger("WALK L");
                break;
            case PlayerAnimState.AimCrouchIdle:
                animator.SetTrigger("CROUCH IDLE 0");
                break;
            case PlayerAnimState.AimCrouchWalk:
                animator.SetTrigger("CROUCH WALK 0");
                break;
            case PlayerAnimState.AimCrouchShoot:
                animator.SetTrigger("CROUCH SHOOT");
                // 마찬가지로 Shoot 트리거 등
                break;
        }
    }

    // =========================================================
    //  상태를 바꾸는 함수 (enum 값만 변경)
    // =========================================================
    private void SetAnimationState(PlayerAnimState newState)
    {
        if (newState == currentAnimState) return;
        currentAnimState = newState;
    }

    // =========================================================
    //  HUD
    // =========================================================
    private void UpdateHUD()
    {
        if (hpBar)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }
        if (staminaBar)
        {
            staminaBar.fillAmount = currentStamina / maxStamina;
        }
    }

    // =========================================================
    //  디버그용
    // =========================================================
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraHolder.position, playerCamera.position);
    }
}
