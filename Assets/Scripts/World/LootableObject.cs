using UnityEngine;
using System.Collections.Generic;

// 월드에 드랍되는 루팅 가능 오브젝트 스크립트
[RequireComponent(typeof(Collider))] // 상호작용 감지를 위한 Collider 필요
public class LootableObject : MonoBehaviour
{
    [SerializeField] private float lifeTime = 30f; // 자동 소멸 시간
    [SerializeField] private SpriteRenderer iconRenderer; // 내용물 대표 아이콘 표시 (범용 주머니 이미지 사용 시)
    [SerializeField] private bool isAiming; // 현재 에임 모드 상태

    private List<KeyValuePair<ItemData, int>> containedItems = new List<KeyValuePair<ItemData, int>>();
    private bool isBeingLooted = false;

    private void Start()
    {
        // 자동 소멸 타이머 시작
        Destroy(gameObject, lifeTime);

        // 초기 아이콘 설정 (필요시)
        // if (iconRenderer != null) iconRenderer.sprite = defaultLootIcon;
    }

    // 몬스터 AI가 이 함수를 호출하여 내용물 설정
    public void Initialize(List<KeyValuePair<ItemData, int>> items)
    {
        containedItems = items;
        // 필요하다면 내용물에 따라 아이콘 변경 로직 추가
        // if (iconRenderer != null && items.Count > 0) iconRenderer.sprite = items[0].Key.itemIcon;
    }

    // 플레이어가 상호작용을 시도할 때 호출될 함수 (PlayerInteraction에서 호출)
    public bool AttemptLoot(PlayerInteraction looter)
    {
        if (isBeingLooted || containedItems.Count == 0)
        {
            return false; // 이미 다른 사람이 줍고 있거나 내용물이 없음
        }

        // 루팅 시작 (실제 아이템 전달은 루팅 완료 후 Loot()에서)
        isBeingLooted = true;
        // UIManager에게 루팅 시작 알림 (looter가 UIManager 참조 가지고 있다고 가정)
        UIManager.Instance.StartLootingProgress(this, 2.0f); // 2초 루팅 시작
        return true;
    }

    // 루팅이 성공적으로 완료되었을 때 호출될 함수 (UIManager 또는 PlayerInteraction에서 호출)
    public void CompleteLoot()
    {
        if (!isBeingLooted) return;

        bool lootedAll = true;
        // InventorySystem 참조 (싱글톤 또는 다른 방식으로)
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            foreach (var itemPair in containedItems)
            {
                if (!inventory.AddItem(itemPair.Key, itemPair.Value))
                {
                    // 하나라도 추가 실패 시 (인벤토리 가득 참)
                    lootedAll = false;
                    UIManager.Instance.ShowMessage("인벤토리가 가득 찼습니다.");
                    break; // 루팅 중단
                }
            }

            if (lootedAll)
            {
                Debug.Log("루팅 완료!");
                containedItems.Clear(); // 내용물 비우기
                Destroy(gameObject); // 오브젝트 제거
            }
            else
            {
                // 일부만 루팅된 경우, isBeingLooted를 false로 되돌려 다시 루팅 시도 가능하게 함
                isBeingLooted = false;
                // 실패한 아이템부터 다시 루팅 시도할 수 있도록 목록 유지
                // (더 복잡한 로직: 성공한 아이템만 목록에서 제거)
                // 여기서는 간단히 isBeingLooted만 해제
            }
        }
        else
        {
            Debug.LogError("InventorySystem을 찾을 수 없습니다!");
            isBeingLooted = false; // 오류 발생 시 루팅 상태 해제
        }
    }

    // 루팅이 취소되었을 때 호출될 함수 (UIManager 또는 PlayerInteraction에서 호출)
    public void CancelLoot()
    {
        if (isBeingLooted)
        {
            Debug.Log("루팅 취소됨");
            isBeingLooted = false;
            UIManager.Instance.StopLootingProgress(); // UI 중지 요청
        }
    }

    // 플레이어가 멀어지거나 다른 이유로 상호작용이 중단될 때 호출될 수 있음
    public void ForceCancelLoot()
    {
         CancelLoot();
    }

    // 플레이어가 이 오브젝트의 트리거 범위에 들어왔을 때 (PlayerInteraction에서 사용)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // 플레이어 태그 확인
        {
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.SetInteractableObject(this);
            }
        }
    }

    // 플레이어가 트리거 범위에서 나갔을 때
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInteraction playerInteraction = other.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.ClearInteractableObject(this);
            }
        }
    }
} 