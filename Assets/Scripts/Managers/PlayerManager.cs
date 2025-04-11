using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    [SerializeField] private IntegratedPlayerController playerController;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 플레이어 컨트롤러 참조 찾기
            if (playerController == null)
            {
                playerController = FindObjectOfType<IntegratedPlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("IntegratedPlayerController를 찾을 수 없습니다!");
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 플레이어 위치 반환
    public Vector3 GetPlayerPosition()
    {
        if (playerController != null)
        {
            return playerController.transform.position;
        }
        
        // 플레이어 컨트롤러를 못 찾았을 경우 기본값 반환
        Debug.LogWarning("플레이어 위치를 가져올 수 없습니다. 기본값(0,0,0) 반환.");
        return Vector3.zero;
    }
    
    // 플레이어 상태 초기화 (체력, 스태미나 100%)
    public void ResetPlayerStatus()
    {
        if (playerController != null)
        {
            // 플레이어 체력 리셋 (IntegratedPlayerController에 해당 기능이 있다고 가정)
            playerController.ResetHealth();
            playerController.ResetStamina();
        }
        else
        {
            Debug.LogError("플레이어 상태를 초기화할 수 없습니다: 플레이어 컨트롤러 참조 없음");
        }
    }
    
    // 플레이어 위치 이동 (텔레포트)
    public void TeleportPlayer(Vector3 position)
    {
        if (playerController != null)
        {
            CharacterController charController = playerController.GetComponent<CharacterController>();
            
            // CharacterController가 있으면 비활성화하고 이동 후 다시 활성화
            if (charController != null)
            {
                charController.enabled = false;
                playerController.transform.position = position;
                charController.enabled = true;
            }
            else
            {
                playerController.transform.position = position;
            }
        }
        else
        {
            Debug.LogError("플레이어를 이동시킬 수 없습니다: 플레이어 컨트롤러 참조 없음");
        }
    }
    
    // 플레이어 리스폰
    public void RespawnPlayer(Vector3 respawnPosition)
    {
        // 플레이어 위치 이동
        TeleportPlayer(respawnPosition);
        
        // 체력, 스태미나 회복
        ResetPlayerStatus();
    }
    
    // 플레이어 컨트롤 활성화/비활성화 메서드 추가
    public void SetPlayerControlEnabled(bool enabled)
    {
        if (playerController != null)
        {
            // IntegratedPlayerController 스크립트 자체를 비활성화
            playerController.enabled = enabled;
            
            // 캐릭터 컨트롤러도 필요시 비활성화
            CharacterController charController = playerController.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = enabled;
            }
            
            // 추가적인 입력 관련 스크립트가 있다면 비활성화
            // 예: 무기 교체, 공격 입력 등 처리 컴포넌트
        }
        else
        {
            Debug.LogWarning("플레이어 컨트롤러를 찾을 수 없어 컨트롤 상태를 변경할 수 없습니다.");
        }
    }
} 