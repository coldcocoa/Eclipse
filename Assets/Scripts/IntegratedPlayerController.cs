using UnityEngine;
using UnityEngine.UI;

public class IntegratedPlayerController : MonoBehaviour
{
    [Header("ĳ���� ������ ����")]
    [SerializeField] private float moveSpeed = 5f;      // �Ϲ� �ȱ� �ӵ�
    [SerializeField] private float sprintSpeed = 10f;   // �޸���(Shift)
    [SerializeField] private float crouchSpeed = 2f;    // �ɾƼ� �ȱ� �ӵ�
    [SerializeField] private float crouchJogSpeed = 4f; // �ɾƼ� ���� �̵� �ӵ�
    [SerializeField] private float gravityValue = -9.81f;

    [Header("ī�޶� ����")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float cameraSensitivity = 2f;
    [SerializeField] private Vector2 cameraYRotationLimit = new Vector2(-40, 70);

    [Header("�ִϸ��̼� ����")]
    [SerializeField] private Animator animator;

    [Header("ü�� & ���¹̳�")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float maxStamina = 50f;
    [SerializeField] private float currentStamina = 50f;
    [SerializeField] private float staminaDecreaseRate = 5f;
    [SerializeField] private float staminaRecoverRate = 3f;

    [Header("UI")]
    [SerializeField] private Image hpBar;       // HP�� (Fill)
    [SerializeField] private Image staminaBar;  // ���¹̳� �� (Fill)

    // ĳ���� ��Ʈ�ѷ� �� ī�޶�
    private CharacterController controller;
    private Transform playerCamera;

    // ī�޶� ȸ�� ����
    private float cameraRotationX = 0f;

    // �̵� ����
    private Vector3 playerVelocity;
    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;

    // ������ �����̹� ��塯�ΰ����� ��Ÿ���� �÷���
    private bool isAiming = false;
    private bool isShooting = false;

    // �ð� ����
    private float lastShootTime = 0f;   // ���������� ���� �� ����
    private float takeStartTime = 0f;   // ��Take�� �ִϸ��̼� ���� ����

    // ���� ������: Normal / Aiming ���� ���� ���� ���� ����
    public enum PlayerAnimState
    {
        // --- Normal �� ---
        NormalIdle,
        Walk,
        Run,
        CrouchIdle,
        CrouchWalk,
        CrouchJog,
        Take,          // �븻 -> ���̹� ��ȯ
        Die1,          // ü�� 0�� ���

        // --- Aiming �� ---
        AimIdle,
        AimShoot,
        AimJog,        // Shift+W
        AimWalkF,      // W
        AimWalkB,      // S
        AimWalkR,      // D
        AimWalkL,      // A
        AimCrouchIdle, // Aim ���� + Ctrl
        AimCrouchWalk, // Aim + Ctrl + W
        AimCrouchShoot // Aim + Ctrl + ��Ŭ��
    }

    private PlayerAnimState currentAnimState = PlayerAnimState.NormalIdle;

    // �ִϸ����� �Ķ����
    //private readonly string ANIM_PARAM_STATE = "AnimState";
    private readonly string ANIM_PARAM_SPEED = "Speed";

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        var cam = Camera.main;
        if (cam != null) playerCamera = cam.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        // ���콺 ���
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // HP, Stamina �ʱ�ȭ
        currentHP = maxHP;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;

        // ------ �̵� ó�� ------
        ProcessMovement();
        ApplyGravity();

        // ------ ī�޶� ó�� ------
        HandleCameraRotation();
        UpdateCameraPosition();

        // ------ HP / ���¹̳� ------
        HandleStamina();
        HandleHP();

        // ------ ���콺/Ű���� �Է� ------
        HandleInput();

        // ------ Normal / Aiming ���� �б� ------
        if (!isAiming)
        {
            // Normal ���
            UpdateNormalState();
        }
        else
        {
            // Aiming ���
            UpdateAimingState();
        }

        // ------ HUD ������Ʈ ------
        UpdateHUD();

        // ------ ���� �ִϸ��̼� ���� ------
        ApplyAnimationState();
    }

    // =========================================================
    //  �븻 ���� ó��
    // =========================================================
    private void UpdateNormalState()
    {
        // 1) ��Take�� �ִϸ��̼� ������ ����
        if (currentAnimState == PlayerAnimState.Take)
        {
            // Take �ִϸ��̼��� �����ٸ� => AimIdle�� ��ȯ
            // ���⼱ ��1�ʡ���� ����
            if (Time.time - takeStartTime > 1f)
            {
                isAiming = true; // ���� ���̹� ��� ��ȯ
                SetAnimationState(PlayerAnimState.AimIdle);
            }
            return;
        }

        // 2) ��10�� �������� NormalIdle �ߵ��� ����
        //    ��: ������ ��� ����(lastShootTime)���κ��� 10�ʰ� ������ Idle �Ұ� ��
        //    ���⼭�� �ܼ��� ����� �� 10�� �̳��� Idle ���ɡ��̶�� ����
        float timeSinceShoot = Time.time - lastShootTime;

        // 3) �̵�/�Է� üũ�ؼ� ���� ����
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // crouch ����ȭ: Idle / Walk / Jog
        if (isCrouching)
        {
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
            {
                // ���ڸ� �ɱ�
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
            // �ɾ����� ������ Normal Idle / Walk / Run
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
                // ��10�� �̳��� Idle ���ɡ��̶��? 
                // ���⼭�� 10�ʰ� �Ѿ����� Idle �� ���ٰ� ����(�Ǵ� �ٸ� ó��)
                if (timeSinceShoot <= 10f)
                {
                    SetAnimationState(PlayerAnimState.NormalIdle);
                }
                // timeSinceShoot > 10f ���, Idle ��� �ٸ� ���� ������ ���� ����
            }
        }
    }

    // =========================================================
    //  ���̹� ���� ó��
    // =========================================================
    private void UpdateAimingState()
    {
        // 1) ��6�ʰ� ��Ŭ�� �� �ϸ� �븻 ���͡�
        if (Time.time - lastShootTime > 6f)
        {
            // ���̹� ��� ����
            isAiming = false;
            // �븻 Idle �� ����
            SetAnimationState(PlayerAnimState.NormalIdle);
            return;
        }

        // 2) crouch ����
        bool isCtrl = Input.GetKey(KeyCode.LeftControl);
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 3) ��Ŭ�� �� Shoot (���̹� �ڼ�����)
        //    �̹� HandleInput()���� lastShootTime ����, isShooting = true ����
        //    �Ʒ����� ���¸� �ٲ���
        if (isShooting)
        {
            // �ɾ������� AimCrouchShoot, �ƴϸ� AimShoot
            if (isCtrl)
            {
                SetAnimationState(PlayerAnimState.AimCrouchShoot);
            }
            else
            {
                SetAnimationState(PlayerAnimState.AimShoot);
            }
            // �� ���� ó��
            isShooting = false;
            return;
        }

        // 4) �̵� ���� + Shift Ȯ���ؼ� AimJog / AimWalkF/B/R/L �� �б�
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

        if (isCtrl)
        {
            // ���� ���̹�
            if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
            {
                // ���ڸ� �ɱ�
                SetAnimationState(PlayerAnimState.AimCrouchIdle);
            }
            else
            {
                // �ȱ�
                SetAnimationState(PlayerAnimState.AimCrouchWalk);
            }
        }
        else
        {
            // �� �ִ� ���̹�
            if (shiftPressed && v > 0.1f)
            {
                // ������ ����
                SetAnimationState(PlayerAnimState.AimJog);
            }
            else
            {
                // ���⺰�� AimWalkF/B/L/R
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
    //  �̵� / �߷� ó��
    // =========================================================
    private void ProcessMovement()
    {
        // �޸��� ���̸� sprintSpeed, �ɾ������� ���ݾ� �ٸ� �ӵ���
        float finalSpeed = moveSpeed;

        // �븻���� Shift => Run
        if (!isAiming && isSprinting)
        {
            finalSpeed = sprintSpeed;
        }

        // �ɾ����� ��
        if (isCrouching)
        {
            // Aiming�̵� �븻�̵� ���ɾƼ� �ȱ� �ӵ����� �켱 ���̽���
            finalSpeed = crouchSpeed;
            // Ȥ�� ��CrouchJog�� ���¶�� crouchJogSpeed�� �� ���� ����
            // (Shift+Ctrl+W ��)
            if (isSprinting)
            {
                finalSpeed = crouchJogSpeed;
            }
        }

        // ���̹� �������� Shift+W => AimJog���
        // (������ �̵��ӵ� �ణ �ٸ��� �� ���� ����)
        if (isAiming)
        {
            // �ʿ��ϴٸ� ������ �� �ӵ� ���ҡ� ���� �� ���� ����
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
    //  Ű �Է�: ��Ŭ������ Take, ���̹� �� ���, etc.
    // =========================================================
    private void HandleInput()
    {
        // ������Ʈ & ũ���ġ
        isSprinting = Input.GetKey(KeyCode.LeftShift);
        isCrouching = Input.GetKey(KeyCode.LeftControl);

        // ��Ŭ��
        if (Input.GetMouseButtonDown(0))
        {
            if (!isAiming)
            {
                // �븻 ���¿��� ��Ŭ�� => Take �ִϸ��̼�
                // (��, �̹� ��� ���°� �ƴ϶�� ����)
                if (currentAnimState != PlayerAnimState.Die1)
                {
                    StartTakeAnimation();
                }
            }
            else
            {
                // ���̹� �� ��Ŭ�� => Shoot
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
    //  HP / ���¹̳� ó��
    // =========================================================
    private void HandleStamina()
    {
        // �޸��� ��(Shift)��� ����
        // ��, �̵� �Է��� �ִ��� magnitude�� üũ
        if (isSprinting && controller.velocity.magnitude > 0.1f)
        {
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
            if (currentStamina < 0f)
            {
                currentStamina = 0f;
                // ���¹̳��� 0�̸� �� �޸� �� ����
                isSprinting = false;
            }
        }
        else
        {
            // ȸ��
            currentStamina += staminaRecoverRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
        }
    }

    private void HandleHP()
    {
        if (currentHP <= 0f)
        {
            // �̹� Die1 ���°� �ƴϸ� ��� ó��
            if (currentAnimState != PlayerAnimState.Die1)
            {
                SetAnimationState(PlayerAnimState.Die1);
            }
        }
    }

    // �ܺο��� ������ �ִ� �Լ�
    public void TakeDamage(float dmg)
    {
        if (currentAnimState == PlayerAnimState.Die1) return; // �̹� ���

        currentHP -= dmg;
        if (currentHP < 0f) currentHP = 0f;
    }

    // =========================================================
    //  ī�޶� ȸ�� & ��ġ
    // =========================================================
    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        // ���� ȸ��
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, cameraYRotationLimit.x, cameraYRotationLimit.y);
        cameraHolder.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        // �¿� ȸ��
        transform.Rotate(Vector3.up, mouseX);
    }

    private void UpdateCameraPosition()
    {
        playerCamera.position = cameraHolder.position;
        playerCamera.rotation = cameraHolder.rotation;
    }

    // =========================================================
    //  ���������� Animator�� �Ķ���� ����
    // =========================================================
    private void ApplyAnimationState()
    {
        // �̵��ӵ�(�¿�xz��)
        Vector3 horizontalVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float speedValue = horizontalVel.magnitude;

        animator.SetFloat(ANIM_PARAM_SPEED, speedValue);

        // ���� �ִϸ��̼� ���¿� ���� Animator �Ķ����(AnimState) ����
        // �Ʒ� ���ڴ� �����̸�, ���� Animator���� ������ ��� ���߽ø� �˴ϴ�.
        switch (currentAnimState)
        {
            // ---------- Normal �� ----------
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

            // ---------- Aiming �� ----------
            case PlayerAnimState.AimIdle:
                animator.SetTrigger("IDLE 0");
                break;
            case PlayerAnimState.AimShoot:
                animator.SetTrigger("SHOOT");
                // Shoot Ʈ���Ű� �ʿ��ϴٸ� animator.SetTrigger("Shoot") �߰� ����
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
                // ���������� Shoot Ʈ���� ��
                break;
        }
    }

    // =========================================================
    //  ���¸� �ٲٴ� �Լ� (enum ���� ����)
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
    //  ����׿�
    // =========================================================
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraHolder.position, playerCamera.position);
    }
}
