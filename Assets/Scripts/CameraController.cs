using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float cameraSensitivity = 2f;
    [SerializeField] private Vector2 cameraYRotationLimit = new Vector2(-40, 70);

    private Transform playerCamera;
    private float cameraRotationX = 0f;

    private void Awake()
    {
        var cam = Camera.main;
        if (cam != null) playerCamera = cam.transform;

        // 마우스 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCameraRotation();
        UpdateCameraPosition();
    }

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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(cameraHolder.position, playerCamera.position);
    }
} 