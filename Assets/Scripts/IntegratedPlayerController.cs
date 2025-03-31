using UnityEngine;
using UnityEngine.UI;

public class IntegratedPlayerController : MonoBehaviour
{
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
    [SerializeField] private float maxStamina = 50f;
    [SerializeField] private float currentStamina = 50f;
    [SerializeField] private float staminaDecreaseRate = 5f;
    [SerializeField] private float staminaRecoverRate = 3f;

    [Header("UI")]
    [SerializeField] private Image hpBar;       // HP바 (Fill)
    [SerializeField] private Image staminaBar;  // 스태미나 바 (Fill)

    // 캐릭터 컨트롤러
    private CharacterController controller;

    // 이동 관련
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;

    // 조준 관련 상태
    private bool isAiming = false;
    private bool isShooting = false;

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
    private readonly string ANIM_PARAM_SPEED = "Speed";
    private readonly string ANIM_PARAM_IS_IDLE = "IDLE";
    private readonly string ANIM_PARAM_IS_WALK = "WALK";
    private readonly string ANIM_PARAM_IS_RUN = "RUN";
    private readonly string ANIM_PARAM_IS_CROUCH_IDLE = "CROUCH IDLE";
    private readonly string ANIM_PARAM_IS_CROUCH_WALK = "CROUCH WALK";
    private readonly string ANIM_PARAM_IS_CROUCH_JOG = "CROUCH JOG";
    private readonly string ANIM_PARAM_IS_TAKE = "TAKE";
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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponent<Animator>();

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

        // ------ HP / 스태미나 ------
        HandleStamina();
        HandleHP();

        // ------ 마우스/키보드 입력 ------
        HandleInput();

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
            else if (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f)
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
        // 1) 6초 이상 지난 후 총 발사 가능
        if (Time.time - lastShootTime > 6f)
        {
            // 총 발사 불가능
            isAiming = false;
            // 브레이크 Idle 상태로 변환
            SetAnimationState(PlayerAnimState.NormalIdle);
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
            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
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
            if (shiftPressed && v > 0.1f)
            {
                // 총 발사 상태
                SetAnimationState(PlayerAnimState.AimJog);
            }
            else
            {
                // 예외 상태 AimWalkF/B/L/R
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
    //  이동 / 조준 처리
    // =========================================================
    private void ProcessMovement()
    {
        // 달리기 속도 처리
        float finalSpeed = moveSpeed;

        // 브레이크 처리
        if (!isAiming && isSprinting)
        {
            finalSpeed = sprintSpeed;
        }

        // 앉아서 이동 처리
        if (isCrouching)
        {
            // Aiming 이동 처리 앉아서 이동 속도 처리
            finalSpeed = crouchSpeed;
            // 예외 처리 CrouchJog 처리
            // (Shift+Ctrl+W 처리)
            if (isSprinting)
            {
                finalSpeed = crouchJogSpeed;
            }
        }

        // 총 발사 처리
        if (isAiming)
        {
            // 총 발사 중일 때 속도 처리
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
    // 마우스/키보드 입력: 총 발사, Take, etc.
    // =========================================================
    private void HandleInput()
    {
        // 입력 처리 & 체력 처리
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        // 총 발사
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAiming)
            {
                // 브레이크 총 발사 => Take 애니메이션
                // (예: 총 발사 후 체력 감소 처리)
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
        // 달리기 처리
        // 예: Shift 입력 시 체력 감소
        // 체력 처리 함수에서 처리 필요
        if (isSprinting && controller.velocity.magnitude > 0.1f)
        {
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
            if (currentStamina < 0f)
            {
                currentStamina = 0f;
                // 스태미나 0% 이면 달리기 불가능
                isSprinting = false;
            }
        }
        else
        {
            // 재생
            currentStamina += staminaRecoverRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
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
        animator.SetBool(ANIM_PARAM_IS_TAKE, false);
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
            case PlayerAnimState.Take:
                animator.SetBool(ANIM_PARAM_IS_TAKE, true);
                break;
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
        if (newState == currentAnimState) return;

        // 현재 애니메이션이 재생 중인지 확인
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            // 특정 상태에서는 즉시 전환 허용
            if (newState == PlayerAnimState.Die1 || 
                newState == PlayerAnimState.Take || 
                newState == PlayerAnimState.AimShoot || 
                newState == PlayerAnimState.AimCrouchShoot)
            {
                currentAnimState = newState;
                return;
            }

            // 다른 상태들은 현재 애니메이션이 거의 끝날 때만 전환
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.8f)
            {
                return;
            }
        }

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
}
