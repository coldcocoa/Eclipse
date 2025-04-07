using UnityEngine;

// 플레이어의 상호작용 처리 스크립트 (플레이어에 부착)
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private KeyCode interactionKey = KeyCode.E; // 상호작용 키

    private LootableObject currentInteractable = null; // 현재 상호작용 가능한 오브젝트
    private bool isLooting = false; // 현재 루팅 중인지 여부

    void Update()
    {
        // 루팅 중이 아니고, 상호작용 가능한 오브젝트가 있고, 상호작용 키를 눌렀을 때
        if (!isLooting && currentInteractable != null && Input.GetKeyDown(interactionKey))
        {
            if (currentInteractable.AttemptLoot(this))
            {
                isLooting = true; // 루팅 시작 상태로 변경
                UIManager.Instance.StartCoroutine(UIManager.Instance.ShowInteractionPrompt(2f));
                // UIManager가 루팅 진행 UI를 표시하고 완료/취소 시 콜백 호출
            }
        }

        // 루팅 중에 상호작용 키를 떼거나 움직이면 취소 (간단한 예시)
        if (isLooting)
        {
             // 예: 움직임 감지 또는 키 떼기 감지 시 취소
             // if (Input.GetKeyUp(interactionKey) || IsPlayerMoving())
             // {
             //     CancelCurrentLoot();
             // }
        }
    }

    // LootableObject가 플레이어 범위 안에 들어왔을 때 호출
    public void SetInteractableObject(LootableObject lootable)
    {
        currentInteractable = lootable;
        // 여기에 "E키 눌러 상호작용" UI 힌트 표시 로직 추가
         //UIManager.Instance.ShowInteractionPrompt(true);
        Debug.Log($"상호작용 가능: {lootable.gameObject.name}");
    }

    // LootableObject가 범위에서 벗어났거나 파괴되었을 때 호출
    public void ClearInteractableObject(LootableObject lootable)
    {
        // 현재 상호작용 대상이 맞는지 확인 후 초기화
        if (currentInteractable == lootable)
        {
            // 루팅 중이었다면 강제 취소
            if (isLooting)
            {
                CancelCurrentLoot();
            }
            currentInteractable = null;
            // UI 힌트 숨기기 로직 추가
            // UIManager.Instance.ShowInteractionPrompt(false);
            Debug.Log("상호작용 대상 없음");
        }
    }

    // UIManager 등 외부에서 루팅 완료/취소 시 호출하여 상태 업데이트
    public void NotifyLootingFinished(bool success)
    {
        isLooting = false;
        // 필요시 추가 로직 (예: 루팅 성공/실패 사운드 재생)
    }

    // 현재 루팅 강제 취소
    private void CancelCurrentLoot()
    {
        if (isLooting && currentInteractable != null)
        {
            currentInteractable.CancelLoot(); // LootableObject에게 취소 알림
            // UIManager.Instance.StopLootingProgress(); // UI 중지
            isLooting = false;
        }
    }

    // 플레이어 이동 감지 (예시 - CharacterController 사용 시)
    // private bool IsPlayerMoving()
    // {
    //     CharacterController controller = GetComponent<CharacterController>();
    //     return controller != null && controller.velocity.sqrMagnitude > 0.1f;
    // }
} 