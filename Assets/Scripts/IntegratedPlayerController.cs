using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IntegratedPlayerController : MonoBehaviour
{
    // 애니메이션 파라미터 상수 정의 변경
    private const string ANIM_PARAM_SPEED = "Speed";       // 이동 속도 (0~1)
    private const string ANIM_PARAM_DIRECTION = "Direction"; // 이동 방향 (-1~1)
    private const string ANIM_PARAM_VERTICAL = "Vertical";   // 수직 입력 (-1~1)
    private const string ANIM_PARAM_HORIZONTAL = "Horizontal"; // 수평 입력 (-1~1)
    private const string ANIM_PARAM_IS_AIMING = "IsAiming";    // 에임 모드 여부
    private const string ANIM_PARAM_IS_CROUCHING = "IsCrouching"; // 앉기 상태
    private const string ANIM_PARAM_TRIGGER_SHOOT = "SHOOT";  // 발사 트리거
    // ... 필요시 기존 Boolean 파라미터도 유지 가능 (전환 기간 동안)

    [Header("이동 속도 설정")]
    [SerializeField] private float moveSpeed = 5f;      // 일반 이동 속도
    [SerializeField] private float sprintSpeed = 10f;   // 달리기(Shift)
    [SerializeField] private float crouchSpeed = 2f;    // 앉아서 이동 속도
    [SerializeField] private float crouchJogSpeed = 4f; // 앉아서 달리기 속도
    [SerializeField] private float gravityValue = -9.81f;

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;

    [Header("체력 & 스태미나")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float staminaDecreaseRate = 5f;
    [SerializeField] private float staminaRecoverRate = 3f;

    [Header("UI")]
    // [SerializeField] private Image hpBar;       // HP바 (Fill) -> Slider로 변경
    // [SerializeField] private Image staminaBar;  // 스태미나 바 (Fill) -> Slider로 변경
    [SerializeField] private Slider hpSlider;     // HP 슬라이더
    [SerializeField] private Slider staminaSlider; // 스태미나 슬라이더

    // 캐릭터 컨트롤러
    private CharacterController controller;
    // CameraController 참조 추가
    public CameraController cameraController;

    // 이동 관련
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;
    // 이동 상태 확인 프로퍼티 추가
    public bool IsMoving => controller.velocity.magnitude > 0.1f; // 속도가 0.1 이상이면 이동 중으로 간주

    // 조준 관련 상태
    [Header("에임 설정")]
    [SerializeField] private bool isAiming = false; // 현재 에임 모드 상태
    [SerializeField] private bool isShooting = false; // isShooting 변수 유지
    [SerializeField] private float aimTimeoutDuration = 4.0f; // 에임 상태 유지 최대 시간 (4초로 변경)
    [SerializeField] private Transform gunMuzzle; // 총구 위치 (Inspector에서 할당 필요)
    [SerializeField] private float weaponRange = 100f; // 무기 사거리

    private float lastAimInputTime; // 마지막 에임 관련 입력 시간

    // 발사 관련 변수
    [SerializeField] private float fireRate = 0.25f; // 발사 간격 (초)
    private float nextFireTime = 0f; // 다음 발사 가능 시간

    // 이동 상태 확인 프로퍼티 추가
    public bool IsAiming => isAiming;
    public bool IsShooting => isShooting; // isShooting 프로퍼티 추가

    // 시간 관련
    private float lastShootTime = 0f;   // 마지막으로 쏜 시간
    private float takeStartTime = 0f;   // 'Take' 애니메이션 시작 시간

    // 애니메이션 상태 열거형
    public enum PlayerAnimState
    {
        // --- Normal 상태 ---
        NormalIdle,
        Walk,
        Run,
        CrouchIdle,
        CrouchWalk,
        CrouchJog,
        Take,          // 브레이크 -> 총 변환
        Die1,          // 체력 0% 상태

        // --- Aiming 상태 ---
        AimIdle,
        AimShoot,
        AimJog,        // Shift+W
        AimWalkF,      // W
        AimWalkB,      // S
        AimWalkR,      // D
        AimWalkL,      // A
        AimCrouchIdle, // Aim 상태 + Ctrl
        AimCrouchWalk, // Aim + Ctrl + W
        AimCrouchShoot // Aim + Ctrl + 총 발사
    }

    private PlayerAnimState currentAnimState = PlayerAnimState.NormalIdle;

    // 애니메이션 파라미터 이름
    private readonly string ANIM_PARAM_IS_IDLE = "IDLE";
    private readonly string ANIM_PARAM_IS_WALK = "WALK";
    private readonly string ANIM_PARAM_IS_RUN = "RUN";
    private readonly string ANIM_PARAM_IS_CROUCH_IDLE = "CROUCH IDLE";
    private readonly string ANIM_PARAM_IS_CROUCH_WALK = "CROUCH WALK";
    private readonly string ANIM_PARAM_IS_CROUCH_JOG = "CROUCH JOG";
    //private readonly string ANIM_PARAM_IS_TAKE = "TAKE";
    private readonly string ANIM_PARAM_IS_DIE = "DIE1";
    private readonly string ANIM_PARAM_IS_AIM_IDLE = "IDLE 0";
    private readonly string ANIM_PARAM_IS_AIM_SHOOT = "SHOOT";
    private readonly string ANIM_PARAM_IS_AIM_JOG = "JOG";
    private readonly string ANIM_PARAM_IS_AIM_WALK_F = "WALK F";
    private readonly string ANIM_PARAM_IS_AIM_WALK_B = "WALK B";
    private readonly string ANIM_PARAM_IS_AIM_WALK_R = "WALK R";
    private readonly string ANIM_PARAM_IS_AIM_WALK_L = "WALK L";
    private readonly string ANIM_PARAM_IS_AIM_CROUCH_IDLE = "CROUCH IDLE 0";
    private readonly string ANIM_PARAM_IS_AIM_CROUCH_WALK = "CROUCH WALK 0";
    private readonly string ANIM_PARAM_IS_AIM_CROUCH_SHOOT = "CROUCH SHOOT";

    [Header("회전 속도 설정")] // 헤더 추가
    [SerializeField] private float rotationSpeed = 10f; // 캐릭터 회전 속도 추가

    [Header("카메라 설정")]
    [SerializeField] private Transform cameraTransform; // 카메라 Transform 참조 추가

    // 변수 추가
    private bool wasMovingLastFrame = false;

    [Header("무기 효과")]
    [SerializeField] private GameObject bulletTrailPrefab; // 총알 궤적 프리팹 (선택적)
    [SerializeField] private GameObject impactEffectPrefab; // 임팩트 효과 프리팹 (선택적)

    // 무기 컨트롤러 참조 추가
    [Header("무기 시스템")]
    [SerializeField] private WeaponController weaponController;

    private FootstepManager footstepManager;

    [Header("애니메이션 보간 설정")]
    [SerializeField] private float speedSmoothTime = 0.1f; // 속도 변화 부드러움
    [SerializeField] private float directionSmoothTime = 0.2f; // 방향 변화 부드러움

    // 보간에 사용할 변수들 - 필드 영역에 추가
    private float currentAnimSpeed = 0f;
    private float speedVelocity = 0f; // SmoothDamp용 속도 변수
    private float currentAnimDirection = 0f;
    private float directionVelocity = 0f; // SmoothDamp용 방향 변수 

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        // CameraController 컴포넌트 찾기 (Main Camera에 있다고 가정)
        
        // cameraTransform 초기화
        if (cameraTransform == null && cameraController != null)
        {
            cameraTransform = cameraController.transform;
        }
        else if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        // HP, Stamina 초기화
        currentHP = maxHP;
        currentStamina = maxStamina;

        CheckMuzzlePoint();
    }

    private void Start()
    {
        // FootstepManager 참조 찾기
        footstepManager = GetComponentInChildren<FootstepManager>();
        if (footstepManager == null)
        {
            // 찾지 못했다면 전체 계층에서 검색
            footstepManager = FindObjectOfType<FootstepManager>();
        }
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;

        // ------ 이동 처리 ------
        ProcessMovement();
        ApplyGravity();

        // ------ HP / 스태미나 ------
        HandleStamina();
        HandleHP();

        // ------ 마우스/키보드 입력 ------
        HandleInput();
        HandleAimInput();

        // ------ Normal / Aiming 상태 판별 ------
        if (!isAiming)
        {
            // Normal 상태
            UpdateNormalState();
        }
        else
        {
            // Aiming 상태
            UpdateAimingState();
        }

        // ------ HUD 업데이트 ------
        UpdateHUD();

        // ------ 현재 애니메이션 적용 ------
        ApplyAnimationState();

        // 애니메이션 파라미터 업데이트 (매 프레임)
        UpdateAnimation();

        // 발사 입력 처리
        HandleWeaponInput();
    }

    // =========================================================
    //  브레이크 처리
    // =========================================================
    private void UpdateNormalState()
    {
        // 1) Take 애니메이션 시작 후 지난 시간 확인
        if (currentAnimState == PlayerAnimState.Take)
        {
            // Take 애니메이션이 끝나면 => AimIdle로 변환
            // 최소 1초 이상 지나야 함
            if (Time.time - takeStartTime > 1f)
            {
                isAiming = true; // 총 변환 후 총 발사 가능
                SetAnimationState(PlayerAnimState.AimIdle);
            }
            return;
        }

        // 2) 10초 이상 지난 후 NormalIdle 상태로 변환
        //    예: 총 발사 후 10초 이상 지나면 Idle 상태로 변환
        float timeSinceShoot = Time.time - lastShootTime;

        // 3) 이동/조준 입력 처리
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // crouch 상태 초기화: Idle / Walk / Jog
        if (isCrouching)
        {
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
            {
                // 키 입력 초기화
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
            // 앉아서 이동 상태 Normal Idle / Walk / Run
            // SHIFT + W => Run
            bool forwardPressed = (v > 0.1f);
            if (forwardPressed && isSprinting)
            {
                SetAnimationState(PlayerAnimState.Run);
            }
            else if (Mathf.Abs(h) > 0.02f || Mathf.Abs(v) > 0.02f)
            {
                SetAnimationState(PlayerAnimState.Walk);
            }
            else
            {
                // IDLE
                // 10초 이상 지난 Idle 상태로 변환 가능? 
                // 최소 10초 이상 지나면 Idle 상태로 변환 필요(예외 처리)
                if (timeSinceShoot <= 10f)
                {
                    SetAnimationState(PlayerAnimState.NormalIdle);
                }
                // timeSinceShoot > 10f 이면, Idle 상태로 변환 필요 시 변환 후 처리 필요
            }
        }
    }

    // =========================================================
    //  총 처리
    // =========================================================
    private void UpdateAimingState()
    {
        // 에임 타임아웃 체크 (변수 사용)
        if (Time.time - lastShootTime > aimTimeoutDuration)
        {
            // 총 발사 불가능
            isAiming = false;
            // 브레이크 Idle 상태로 변환
            SetAnimationState(PlayerAnimState.NormalIdle);
            
            // 다음 번 마우스 클릭에 즉시 에임 모드로 전환될 수 있도록
            lastShootTime = 0f; // 단순히 0으로 설정
            return;
        }

        // 2) crouch 상태
        bool isCtrl = Input.GetKey(KeyCode.LeftControl);
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 3) 총 발사 (총 발사 입력 처리)
        //    총 발사 HandleInput() 메서드에서 lastShootTime 처리, isShooting = true 처리
        //    함수 내에서 처리 필요
        if (isShooting)
        {
            // 총 발사 후 AimCrouchShoot, 아니면 AimShoot
            if (isCtrl)
            {
                SetAnimationState(PlayerAnimState.AimCrouchShoot);
            }
            else
            {
                SetAnimationState(PlayerAnimState.AimShoot);
            }
            // 총 발사 처리 필요
            isShooting = false;
            return;
        }

        // 4) 이동 처리 + Shift 입력 처리 AimJog / AimWalkF/B/R/L 상태 판별
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

        if (isCtrl)
        {
            // 총 발사 상태
            if (Mathf.Abs(h) < 0.02f && Mathf.Abs(v) < 0.02f)
            {
                // 키 입력 초기화
                SetAnimationState(PlayerAnimState.AimCrouchIdle);
            }
            else
            {
                // 일반 이동 상태
                SetAnimationState(PlayerAnimState.AimCrouchWalk);
            }
        }
        else
        {
            // 일반 이동 상태
            if (shiftPressed && v > 0.02f)
            {
                // 총 발사 상태
                SetAnimationState(PlayerAnimState.AimJog);
            }
            else
            {
                // 예외 상태 AimWalkF/B/L/R
                if (v > 0.02f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkF);
                }
                else if (v < -0.02f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkB);
                }
                else if (h > 0.02f)
                {
                    SetAnimationState(PlayerAnimState.AimWalkR);
                }
                else if (h < -0.02f)
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
    //  이동 / 조준 처리
    // =========================================================
    private void ProcessMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 이동 입력 감지
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            wasMovingLastFrame = true;

            // 달리기 속도 처리
            float finalSpeed = moveSpeed;
            if (!isAiming && isSprinting) finalSpeed = sprintSpeed;
            if (isCrouching) finalSpeed = isSprinting ? crouchJogSpeed : crouchSpeed;

            // --- 이동 방향 계산 (카메라 기준) ---
            Transform camTransform = null;
            if (cameraController != null) camTransform = cameraController.transform;
            else if (cameraTransform != null) camTransform = cameraTransform;
            else camTransform = Camera.main?.transform;

            if (camTransform == null)
            {
                Debug.LogError("카메라 참조를 찾을 수 없습니다. 이동/회전 처리를 건너니다.");
                return;
            }

            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * v) + (camRight * h);
            moveDir.Normalize();

            // --- 캐릭터 이동 ---
            controller.Move(moveDir * finalSpeed * Time.deltaTime);
        }
        else
        {
            wasMovingLastFrame = false;
        }
    }

    private void ApplyGravity()
    {
        // 지면 체크
        isGrounded = controller.isGrounded;
        
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // 약간의 하향력 유지 (지면 감지 신뢰성 향상)
        }
        
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }

    // =========================================================
    // 마우스/키보드 입력: 총 발사, Take, etc.
    // =========================================================
    private void HandleInput()
    {
        // 입력 처리 & 체력 처리
        isSprinting = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0; // 스태미나가 있어야 달리기 가능
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        // 총 발사
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAiming)
            {
                // 브레이크 총 발사 => Take 애니메이션
                if (currentAnimState != PlayerAnimState.Die1)
                {
                    StartTakeAnimation();
                }
            }
            else
            {
                // 총 발사 => Shoot
                isShooting = true;
                lastShootTime = Time.time;
            }
        }

        // 에임 모드 전환 (마우스 좌클릭으로)
        if (Input.GetMouseButtonDown(0) && !isAiming && !isShooting)
        {
            // 딜레이 조건 제거 또는 최소화
            isAiming = true;
            SetAnimationState(PlayerAnimState.AimIdle);
            // IsAiming 파라미터 직접 설정 (중복 확인)
            animator.SetBool(ANIM_PARAM_IS_AIMING, true);
            Debug.Log("에임 모드 활성화");
        }

        // 에임 상태에서 마우스 좌클릭 시 발사
        if (Input.GetMouseButtonDown(0) && isAiming)
        {
            TryFireWeapon();
            lastShootTime = Time.time; // 마지막 발사 시간 업데이트
        }
    }

    private void StartTakeAnimation()
    {
        takeStartTime = Time.time;
        SetAnimationState(PlayerAnimState.Take);

        // --- 추가: 카메라 컨트롤러에 캐릭터 회전 요청 ---
        if (cameraController != null)
        {
            cameraController.AlignCharacterToCameraForward();
        }
        // ---------------------------------------------
    }

    // =========================================================
    //  HP / 스태미나 처리
    // =========================================================
    private void HandleStamina()
    {
        // --- 디버그 로그 활성화 ---
        //Debug.Log($"HandleStamina Check: isSprinting={isSprinting}, Velocity={controller.velocity} (Mag={controller.velocity.magnitude}), Stamina={currentStamina}, isGrounded={isGrounded}");
        // ----------------------

        bool staminaChanged = false;

        if (isSprinting && controller.velocity.magnitude > 0.02f) // 이 0.02f 값 확인!
        {
            // ... (스태미나 감소 로직) ...
        }
        else
        {
             if (isSprinting) // isSprinting이 true인데도 이쪽으로 온다면 velocity 문제
             {
                 //Debug.LogWarning($"Sprinting is TRUE, but velocity magnitude ({controller.velocity.magnitude}) is too low!");
             }
            // ... (스태미나 재생 로직) ...
        }

        if (staminaChanged)
        {
            UIManager.Instance?.UpdateStaminaUI(currentStamina, maxStamina);
        }
    }

    private void HandleHP()
    {
        if (currentHP <= 0f)
        {
            // 총 Die1 상태로 변환 후 처리
            if (currentAnimState != PlayerAnimState.Die1)
            {
                SetAnimationState(PlayerAnimState.Die1);
            }
        }
    }

    // 공통 함수 처리 함수
    public void TakeDamage(float dmg)
    {
        if (currentAnimState == PlayerAnimState.Die1) return; // 총 처리 불가능

        currentHP -= dmg;
        if (currentHP < 0f) currentHP = 0f;
    }

    // =========================================================
    // 애니메이션 Animator 파라미터 처리
    // =========================================================
    private void ApplyAnimationState()
    {
        // 이동 속도(일반 xz)
        Vector3 horizontalVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float speedValue = horizontalVel.magnitude;

        animator.SetFloat(ANIM_PARAM_SPEED, speedValue);

        // 모든 애니메이션 상태를 false로 초기화
        animator.SetBool(ANIM_PARAM_IS_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_WALK, false);
        animator.SetBool(ANIM_PARAM_IS_RUN, false);
        animator.SetBool(ANIM_PARAM_IS_CROUCH_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_CROUCH_WALK, false);
        animator.SetBool(ANIM_PARAM_IS_CROUCH_JOG, false);
        //animator.SetBool(ANIM_PARAM_IS_TAKE, false);
        animator.SetBool(ANIM_PARAM_IS_DIE, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_SHOOT, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_JOG, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_F, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_B, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_R, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_L, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_WALK, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_SHOOT, false);

        // 현재 상태에 해당하는 애니메이션만 true로 설정
        switch (currentAnimState)
        {
            // ---------- Normal 상태 ----------
            case PlayerAnimState.NormalIdle:
                animator.SetBool(ANIM_PARAM_IS_IDLE, true);
                break;
            case PlayerAnimState.Walk:
                animator.SetBool(ANIM_PARAM_IS_WALK, true);
                break;
            case PlayerAnimState.Run:
                animator.SetBool(ANIM_PARAM_IS_RUN, true);
                break;
            case PlayerAnimState.CrouchIdle:
                animator.SetBool(ANIM_PARAM_IS_CROUCH_IDLE, true);
                break;
            case PlayerAnimState.CrouchWalk:
                animator.SetBool(ANIM_PARAM_IS_CROUCH_WALK, true);
                break;
            case PlayerAnimState.CrouchJog:
                animator.SetBool(ANIM_PARAM_IS_CROUCH_JOG, true);
                break;
           //case PlayerAnimState.Take:
                //animator.SetBool(ANIM_PARAM_IS_TAKE, true);
                //break;
            case PlayerAnimState.Die1:
                animator.SetBool(ANIM_PARAM_IS_DIE, true);
                break;

            // ---------- Aiming 상태 ----------
            case PlayerAnimState.AimIdle:
                animator.SetBool(ANIM_PARAM_IS_AIM_IDLE, true);
                break;
            case PlayerAnimState.AimShoot:
                animator.SetBool(ANIM_PARAM_IS_AIM_SHOOT, true);
                break;
            case PlayerAnimState.AimJog:
                animator.SetBool(ANIM_PARAM_IS_AIM_JOG, true);
                break;
            case PlayerAnimState.AimWalkF:
                animator.SetBool(ANIM_PARAM_IS_AIM_WALK_F, true);
                break;
            case PlayerAnimState.AimWalkB:
                animator.SetBool(ANIM_PARAM_IS_AIM_WALK_B, true);
                break;
            case PlayerAnimState.AimWalkR:
                animator.SetBool(ANIM_PARAM_IS_AIM_WALK_R, true);
                break;
            case PlayerAnimState.AimWalkL:
                animator.SetBool(ANIM_PARAM_IS_AIM_WALK_L, true);
                break;
            case PlayerAnimState.AimCrouchIdle:
                animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_IDLE, true);
                break;
            case PlayerAnimState.AimCrouchWalk:
                animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_WALK, true);
                break;
            case PlayerAnimState.AimCrouchShoot:
                animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_SHOOT, true);
                break;
        }
    }

    // =========================================================
    //  애니메이션 상태 변경 함수 (enum 상태 처리)
    // =========================================================
    private void SetAnimationState(PlayerAnimState newState)
    {
        if (newState == currentAnimState) return; // 같은 상태로 변경 시 무시

        // 현재 애니메이션이 재생 중인지 확인 (normalizedTime < 1.0f 이면 재생 중)
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            // 특정 상태(Die, Take, Shoot)로는 즉시 전환 허용 (현재 애니메이션 중단)
            if (newState == PlayerAnimState.Die1 ||
                newState == PlayerAnimState.Take ||
                newState == PlayerAnimState.AimShoot ||
                newState == PlayerAnimState.AimCrouchShoot)
            {
                // 즉시 상태 변경하고 함수 종료
                currentAnimState = newState;
                // ApplyAnimationState()는 Update 마지막에 호출되므로 여기서 애니메이터 파라미터 설정 불필요
                return;
            }

            // --- 중요: 다른 상태들로의 전환 조건 ---
            // 현재 애니메이션이 % 미만으로 재생되었다면, 새로운 상태로 전환하지 않음
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.05f)
            {
                return; // 상태 변경 취소하고 함수 종료
            }
            // % 이상 재생되었으면 아래 코드로 진행하여 상태 변경
        }

        // 새로운 상태로 전환하기 전에 모든 애니메이션 파라미터 초기화
        ResetAllAnimationParameters();

        // 현재 애니메이션이 끝났거나, 50% 이상 재생된 경우 상태 변경
        currentAnimState = newState;
    }

    // =========================================================
    //  HUD
    // =========================================================
    private void UpdateHUD()
    {
        // if (hpBar) // Image 대신 Slider 사용
        // {
        //     hpBar.fillAmount = currentHP / maxHP;
        // }
        if (hpSlider != null) // hpSlider가 할당되었는지 확인
        {
            // Slider의 value는 0과 1 사이의 값이어야 함 (또는 min/max value 설정)
            // 여기서는 0~1 범위로 정규화하여 설정
            hpSlider.value = currentHP / maxHP;
        }

        // if (staminaBar) // Image 대신 Slider 사용
        // {
        //     staminaBar.fillAmount = currentStamina / maxStamina;
        // }
        if (staminaSlider != null) // staminaSlider가 할당되었는지 확인
        {
            staminaSlider.value = currentStamina / maxStamina;
        }
    }

    // 에임 입력 처리 함수 수정 (회전 관련 부분 확인)
    private void HandleAimInput()
    {
        // 마우스 좌클릭 감지
        if (Input.GetMouseButtonDown(0)) // 좌클릭
        {
            if (!isAiming)
            {
                Debug.Log("에임 상태로 진입 시도");
                // 노말 상태에서 좌클릭 -> 에임 모드 진입
                isAiming = true;
                lastAimInputTime = Time.time; // 타이머 초기화
                
                // 혼합형 회전 처리
                transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
                
                // 모든 노말 애니메이션 파라미터 비활성화
                ResetAllAnimationParameters();
                
                // 에임 애니메이션 활성화
                animator.SetBool(ANIM_PARAM_IS_AIM_IDLE, true);
            }
            else
            {
                // 에임 상태에서 좌클릭 -> 발사 시도
                TryFireWeapon();
                lastAimInputTime = Time.time; // 타이머 리셋
            }
        }
        
        // 에임 모드 타임아웃 체크
        if (isAiming && Time.time - lastAimInputTime > aimTimeoutDuration)
        {
            Debug.Log("에임 상태 타임아웃으로 종료");
            // 타임아웃 - 에임 모드 해제
            isAiming = false;
            
            // 모든 애니메이션 파라미터 초기화
            ResetAllAnimationParameters();
            
            // 현재 상태에 맞는 애니메이션 설정
            if (IsMoving)
            {
                if (isSprinting)
                    SetAnimationState(PlayerAnimState.Run);
                else
                    SetAnimationState(PlayerAnimState.NormalIdle);
            }
            else
            {
                SetAnimationState(PlayerAnimState.NormalIdle);
            }
        }
        
        // 에임 모드에서 입력 감지 시 타이머 리셋
        if (isAiming && (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || 
                         Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f ||
                         Input.GetButtonDown("Jump") ||
                         Input.GetKeyDown(KeyCode.F) || // 상호작용 키
                         Input.GetMouseButtonDown(0) || // 좌클릭
                         Input.GetMouseButtonDown(1)))  // 우클릭
        {
            lastAimInputTime = Time.time;
        }
        
        // 에임 모드에서 혼합형 회전 처리
        if (isAiming)
        {
            // 이동 입력이 없을 때 서서히 카메라 방향으로 회전
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            // cameraTransform이 null인 경우 처리 (ProcessMovement와 유사하게)
            if (cameraTransform == null)
            {
                if (cameraController != null)
                    cameraTransform = cameraController.transform;
                else
                    cameraTransform = Camera.main?.transform; // Null-conditional operator 사용

                // 카메라를 찾을 수 없는 경우 처리 종료
                if (cameraTransform == null)
                {
                    Debug.LogError("HandleAimInput: 카메라를 찾을 수 없습니다!");
                    return;
                }
            }

            // 에임 중 이동 시에는 CameraController에서 즉시 회전하므로,
            // 여기서는 이동 입력이 없을 때만 카메라 방향으로 정렬합니다.
            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
            {
                // 카메라 방향으로 부드럽게 회전
                float targetYRotation = cameraTransform.eulerAngles.y;
                float currentYRotation = transform.eulerAngles.y;

                // 회전 보간 (LerpAngle 사용 권장)
                float newYRotation = Mathf.LerpAngle(currentYRotation, targetYRotation, Time.deltaTime * rotationSpeed); // Slerp 대신 LerpAngle 사용, 속도 조절 가능
                transform.rotation = Quaternion.Euler(0, newYRotation, 0);
            }
        }
    }

    // 모든 애니메이션 파라미터 초기화 함수 수정
    private void ResetAllAnimationParameters()
    {
        // 노말 상태 애니메이션 파라미터
        animator.SetBool(ANIM_PARAM_IS_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_WALK, false);
        animator.SetBool(ANIM_PARAM_IS_RUN, false);
        animator.SetBool(ANIM_PARAM_IS_CROUCH_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_CROUCH_WALK, false);
        animator.SetBool(ANIM_PARAM_IS_CROUCH_JOG, false);
        
        // 에임 상태 애니메이션 파라미터
        animator.SetBool(ANIM_PARAM_IS_AIM_IDLE, false);
        // 발사는 트리거이므로 리셋에서 제외 (animator.ResetTrigger로 필요시 따로 처리)
        // animator.SetBool(ANIM_PARAM_IS_AIM_SHOOT, false); // 이 부분 제거 또는 주석 처리
        animator.SetBool(ANIM_PARAM_IS_AIM_JOG, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_F, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_B, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_L, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_WALK_R, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_IDLE, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_WALK, false);
        animator.SetBool(ANIM_PARAM_IS_AIM_CROUCH_SHOOT, false);
    }

    // 문제 1 해결: 206번째 줄의 TryFireWeapon() 메서드 수정
    // 여기선 단순히 Shoot()만 호출하도록 변경합니다
    private void TryFireWeapon()
    {
        if (isAiming && Time.time > nextFireTime)
        {
            // Shoot() 메서드에서 발사 관련 모든 처리 진행
            Shoot();
        }
    }

    // Shoot 메서드 수정 (레이캐스트 결과를 WeaponController에 전달)
    private void Shoot()
    {
        // 사격 딜레이 확인
        if (Time.time < nextFireTime) return;

        // 무기 컨트롤러 참조 획득
        WeaponController weaponController = GetComponentInChildren<WeaponController>();
        if (weaponController == null) return;

        // 무기 컨트롤러에 발사 명령
        bool shotFired = weaponController.TryShoot(Camera.main, weaponRange);
        
        if (shotFired)
        {
            // 다음 사격 시간 업데이트
            nextFireTime = Time.time + fireRate;
            
            // 슈팅 애니메이션 재생 (트리거 초기화)
            ResetAnimationTriggers();
            animator.SetTrigger(ANIM_PARAM_TRIGGER_SHOOT);
            
            // 사격 후 처리
            isShooting = true;
            lastShootTime = Time.time;
        }
    }

    // Start() 또는 Awake() 메서드에 추가 - 총구 위치 확인
    private void CheckMuzzlePoint()
    {
        if (gunMuzzle == null)
        {
            // 총구 위치 찾기 시도 (예시 경로, 실제 모델에 맞게 조정 필요)
            Transform weaponModel = transform.Find("WeaponModel"); // 적절한 경로로 변경
            if (weaponModel != null)
            {
                gunMuzzle = weaponModel.Find("MuzzlePoint");
            }
            
            // 여전히 null이면 임시 위치 생성
            if (gunMuzzle == null)
            {
                Debug.LogWarning("총구 위치를 찾을 수 없어 임시 위치를 생성합니다.");
                GameObject muzzleObj = new GameObject("TempMuzzlePoint");
                muzzleObj.transform.SetParent(transform);
                muzzleObj.transform.localPosition = new Vector3(0.2f, 1.5f, 0.5f); // 앞쪽 오른쪽 위치
                gunMuzzle = muzzleObj.transform;
            }
        }
    }

    // 현재 움직임에 기반하여 애니메이션 상태 업데이트
    private void UpdateAnimationBasedOnMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        
        if (isCrouching)
        {
            if (isSprinting && v > 0.1f)
            {
                SetAnimationState(PlayerAnimState.CrouchJog);
            }
            else if (isMoving)
            {
                SetAnimationState(PlayerAnimState.CrouchWalk);
            }
            else
            {
                SetAnimationState(PlayerAnimState.CrouchIdle);
            }
        }
        else if (isSprinting && v > 0.1f)
        {
            SetAnimationState(PlayerAnimState.Run);
        }
        else if (isMoving)
        {
            SetAnimationState(PlayerAnimState.Walk);
        }
        else
        {
            SetAnimationState(PlayerAnimState.NormalIdle);
        }
    }

    // 트리거 파라미터 초기화 함수
    private void ResetAnimationTriggers()
    {
        // 발사 트리거 초기화
        animator.ResetTrigger(ANIM_PARAM_TRIGGER_SHOOT);
    }

    // Update 함수 수정 (애니메이션 파라미터 업데이트 호출)
    private void UpdateAnimation()
    {
        // 이동 방향 및 속도 계산
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        
        // 목표 속도 계산 (기존 코드는 그대로 유지)
        float targetSpeed = 0f;
        if (v > 0)
            targetSpeed = sprint ? 1.0f : 0.5f; // 전진 + 달리기/걷기
        else if (v < 0)
            targetSpeed = 0.3f; // 후진 (항상 느리게)
        else if (Mathf.Abs(h) > 0.1f) 
            targetSpeed = sprint ? 0.8f : 0.4f; // 좌우 이동 (전진보다 약간 느리게)
        
        // 부드러운 속도 보간 추가
        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);
        
        // 이동 방향 계산 (전후좌우) - 기존 코드
        float direction = 0f;
        if (Mathf.Abs(h) > 0.1f)
            direction = h; // 좌우 입력이 있으면 해당 방향
        else if (v < -0.1f)
            direction = -0.5f; // 후진 (방향값 -0.5로 설정)
        
        // 부드러운 방향 보간 추가
        currentAnimDirection = Mathf.SmoothDamp(currentAnimDirection, direction, ref directionVelocity, directionSmoothTime);
        
        // 주요 상태 설정
        animator.SetBool(ANIM_PARAM_IS_AIMING, isAiming);
        animator.SetBool(ANIM_PARAM_IS_CROUCHING, isCrouching);
        
        // 보간된 값으로 애니메이터 파라미터 업데이트
        animator.SetFloat(ANIM_PARAM_SPEED, currentAnimSpeed);
        animator.SetFloat(ANIM_PARAM_DIRECTION, currentAnimDirection);
        
        // 수직/수평 입력값도 부드럽게 전달 (만약 사용하고 있다면)
        animator.SetFloat(ANIM_PARAM_VERTICAL, v);
        animator.SetFloat(ANIM_PARAM_HORIZONTAL, h);
    }

    // 체력을 최대값으로 초기화
    public void ResetHealth()
    {
        currentHP = maxHP;
        UpdateHealthUI();
    }

    // 스태미나를 최대값으로 초기화
    public void ResetStamina()
    {
        currentStamina = maxStamina;
        UpdateStaminaUI();
    }

    // 체력 UI 업데이트 메서드 (만약 없다면)
    private void UpdateHealthUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHP / maxHP;
        }
    }

    // 스태미나 UI 업데이트 메서드 (만약 없다면)
    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina / maxStamina;
        }
    }

    // TryFireWeapon 메서드는 삭제하고 대신 HandleWeaponInput에서 직접 처리
    private void HandleWeaponInput()
    {
        // R키 - 재장전
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 무기 컨트롤러 참조 확인
            WeaponController weaponController = GetComponentInChildren<WeaponController>();
            if (weaponController != null && !weaponController.IsReloading())
            {
                // 재장전 시작
                if (weaponController.StartReload())
                {
                    // 재장전 애니메이션 재생
                    if (animator != null)
                    {
                        animator.SetTrigger("Reload");
                    }
                }
            }
        }
        
        // 마우스 왼쪽 버튼 또는 좌측 Ctrl - 발사
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.LeftControl))
        {
            if (isAiming && Time.time > nextFireTime)
            {
                // 직접 발사 처리
                Shoot();
            }
        }
        
        // 우측 마우스 버튼 - 조준
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            lastAimInputTime = Time.time;
        }
        
        // 조준 해제 (마우스 오른쪽 버튼 해제 시)
        if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;  // 즉시 조준 해제 (코루틴 사용 없이)
        }
    }

    // 애니메이션 이벤트에서 호출되는 함수
    public void PlayFootstep()
    {
        if (footstepManager != null)
        {
            footstepManager.PlayFootstep();
        }
        else
        {
            Debug.LogWarning("FootstepManager를 찾을 수 없습니다!");
        }
    }
}
