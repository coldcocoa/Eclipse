using UnityEngine;

public class FootstepEventHandler : MonoBehaviour
{
    private IntegratedPlayerController playerController;
    
    private void Awake()
    {
        // 부모 객체에서 IntegratedPlayerController 찾기
        playerController = GetComponentInParent<IntegratedPlayerController>();
        
        if (playerController == null)
        {
            Debug.LogError("FootstepEventHandler: 부모 객체에서 IntegratedPlayerController를 찾을 수 없습니다!");
        }
    }
    
    // 애니메이션 이벤트에서 호출되는 함수
    public void PlayFootstep()
    {
        if (playerController != null)
        {
            playerController.PlayFootstep();
        }
    }
} 