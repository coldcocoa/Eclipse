using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Transform characterBody;  // 캐릭터 모델
    [SerializeField] private Transform cameraArm;     // 카메라 암
    [SerializeField] private float mouseSensitivity = 2f; // 마우스 감도

    // IntegratedPlayerController 참조
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
        
        // ESC 키로 마우스 잠금 해제/재잠금 토글 (디버깅용)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? 
                               CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }
        
        LookAround();
    }

    private void LookAround()
    {
        // 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // 카메라 Pitch 업데이트 (상하 회전)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -70f, 70f);
        
        // 모든 상태에서 항상 에임 모드 카메라 시스템 적용
        // 마우스 X 입력으로 캐릭터 회전
        characterBody.Rotate(Vector3.up * mouseX);
        
        // 카메라 암은 X축 회전(Pitch)만 담당
        cameraArm.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        
        // 글로벌 Y축 회전 값 업데이트 (캐릭터의 회전을 따름)
        cameraYaw = characterBody.eulerAngles.y;
    }

    // 이동 시작 시 호출될 함수 (필요 시)
    public void AlignCharacterToCameraForward()
    {
        if (characterBody == null) return;
        
        // 카메라 전방 방향 (수평만)
        Vector3 cameraForward = cameraArm.forward;
        cameraForward.y = 0;
        
        if (cameraForward.sqrMagnitude > 0.01f)
        {
            // 지연 효과 추가 (더 자연스러운 회전)
            float rotationSpeed = 15f;
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            characterBody.rotation = Quaternion.Slerp(
                characterBody.rotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed);
        }
    }
}
