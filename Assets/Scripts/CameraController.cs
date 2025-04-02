using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Transform characterBody;  // 캐릭터 모델
    [SerializeField] private Transform cameraArm;     // 카메라 암
    [SerializeField] private float mouseSensitivity = 2f; // 마우스 감도

    // IntegratedPlayerController 참조 (public으로 유지하거나 Awake에서 다시 할당)
    public IntegratedPlayerController playerController;
    private float cameraPitch = 0f; // 카메라의 상하 각도 (X축)
    private float cameraYaw = 0f;   // 카메라의 좌우 각도 (월드 Y축)

    void Awake()
    {
        // playerController가 Inspector에서 할당되지 않았다면 여기서 찾기
        if (playerController == null && characterBody != null)
        {
            playerController = characterBody.GetComponent<IntegratedPlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("Player Controller가 할당되지 않았습니다.", this);
        }

        // 초기 카메라 각도 설정 (월드 기준)
        // EulerAngles는 0~360 범위이므로, 음수 각도를 표현하기 위해 보정
        cameraPitch = cameraArm.eulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;
        cameraYaw = cameraArm.eulerAngles.y;

        // 마우스 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (playerController == null) return;
        LookAround();
    }

    private void LookAround()
    {
        // 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 공통: 카메라 Pitch 업데이트 (상하 회전)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -70f, 70f);

        if (playerController.IsAiming) // 1. 조준 상태
        {
            // 캐릭터는 마우스 X로 회전, 카메라는 로컬 Pitch 적용
            RotateCharacterAndAim(mouseX);
            // 현재 캐릭터의 월드 Y 회전값을 카메라 Yaw로 동기화 (상태 전환 대비)
            cameraYaw = characterBody.eulerAngles.y;
        }
        else // 2. 일반 상태
        {
            if (playerController.IsMoving) // 2-1. 일반 상태 + 이동 중
            {
                // 캐릭터는 이동 방향으로 회전 (PlayerController 담당)
                // 카메라는 캐릭터 뒤에서 로컬 Pitch만 적용
                RotateCameraLocalPitchOnly();
                // 현재 캐릭터의 월드 Y 회전값을 카메라 Yaw로 동기화
                cameraYaw = characterBody.eulerAngles.y;
            }
            else // 2-2. 일반 상태 + 정지 상태
            {
                // 캐릭터는 고정, 카메라만 마우스 X/Y로 월드 기준 자유 회전 (공전)
                RotateCameraWorldOrbit(mouseX);
            }
        }
    }

    // 조준 상태: 캐릭터 회전 + 카메라 로컬 Pitch
    private void RotateCharacterAndAim(float mouseX)
    {
        characterBody.Rotate(Vector3.up * mouseX);
        // 로컬 회전: 카메라가 항상 캐릭터의 등 뒤에서 상하 각도만 조절
        cameraArm.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // 일반 이동 상태: 카메라 로컬 Pitch만 적용
    private void RotateCameraLocalPitchOnly()
    {
        // 로컬 회전: 카메라가 캐릭터 회전을 따라가도록 Yaw는 0으로 고정
        cameraArm.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // 일반 정지 상태: 카메라 월드 회전 (캐릭터 주위 공전)
    private void RotateCameraWorldOrbit(float mouseX)
    {
        // 카메라 Yaw 업데이트 (월드 기준)
        cameraYaw += mouseX;
        // 카메라 암의 월드 회전 설정 (Pitch와 Yaw 사용)
        // 이렇게 하면 부모(캐릭터)가 회전하지 않아도 카메라만 독립적으로 회전
        cameraArm.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    // --- 추가: 캐릭터를 카메라 정면으로 즉시 회전시키는 함수 ---
    public void AlignCharacterToCameraForward()
    {
        if (characterBody == null) return;

        // 카메라 암의 현재 forward 방향 (Y축은 무시)
        Vector3 cameraForward = cameraArm.forward;
        cameraForward.y = 0; // 수평 방향만 사용

        if (cameraForward.sqrMagnitude > 0.01f)
        {
            // 해당 방향을 바라보도록 캐릭터 회전 (즉시)
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            characterBody.rotation = targetRotation;

            // 중요: 캐릭터 회전 후, 카메라 Yaw를 캐릭터에 맞추고 로컬 Pitch 적용
            // 다음 프레임부터 카메라가 캐릭터 뒤를 따라가도록 설정
            cameraYaw = characterBody.eulerAngles.y;
            cameraArm.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }
    // -------------------------------------------------------
}