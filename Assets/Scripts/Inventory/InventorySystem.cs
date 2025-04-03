using UnityEngine;
using System.Collections.Generic;
using System.Linq; // OrderBy 사용 위해 추가
using System; // Action 사용 위해 추가

// 인벤토리 데이터 관리 시스템 (플레이어 또는 GameManager에 부착)
public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int inventorySize = 15; // 인벤토리 슬롯 개수
    public List<InventorySlot> slots; // 실제 인벤토리 슬롯 리스트

    // 인벤토리 변경 시 UI 업데이트를 위한 이벤트
    public event Action OnInventoryChanged;

    // 싱글톤 또는 다른 방식으로 접근 가능하게 만들 수 있음
    public static InventorySystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환 시 유지 필요하면 사용
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 인벤토리 초기화
        slots = new List<InventorySlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    // 아이템 추가 시도
    public bool AddItem(ItemData itemToAdd, int quantityToAdd)
    {
        if (itemToAdd == null || quantityToAdd <= 0) return false;

        int remainingQuantity = quantityToAdd;

        // 1. 스택 가능한 아이템이고, 이미 인벤토리에 존재하며 공간이 남는 슬롯 찾기
        if (itemToAdd.isStackable)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.itemData == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
                {
                    remainingQuantity = slot.AddQuantity(remainingQuantity);
                    if (remainingQuantity == 0)
                    {
                        OnInventoryChanged?.Invoke(); // 인벤토리 변경 알림
                        return true; // 모든 수량 추가 완료
                    }
                }
            }
        }

        // 2. 남은 수량을 빈 슬롯에 추가
        while (remainingQuantity > 0)
        {
            InventorySlot emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                Debug.Log("인벤토리가 가득 찼습니다.");
                OnInventoryChanged?.Invoke(); // 일부만 추가되었을 수도 있으므로 알림
                // UIManager.Instance.ShowMessage("인벤토리가 가득 찼습니다."); // UI 메시지 표시
                return false; // 빈 슬롯 없음
            }

            int amountToAddInNewSlot = remainingQuantity;
            if (itemToAdd.isStackable)
            {
                amountToAddInNewSlot = Mathf.Min(remainingQuantity, itemToAdd.maxStackSize);
            }

            emptySlot.itemData = itemToAdd;
            emptySlot.quantity = amountToAddInNewSlot;
            remainingQuantity -= amountToAddInNewSlot;

            if (!itemToAdd.isStackable) // 스택 불가능 아이템은 하나씩만 추가
            {
                if (remainingQuantity > 0)
                {
                     // 스택 불가능 아이템 여러 개 추가 시도 시, 첫 번째만 추가하고 나머지는 실패 처리
                     Debug.LogWarning("스택 불가능한 아이템은 한 슬롯에 하나만 추가 가능합니다.");
                     // UIManager.Instance.ShowMessage("인벤토리가 가득 찼습니다."); // 또는 다른 메시지
                     OnInventoryChanged?.Invoke();
                     return false;
                }
                 break; // 스택 불가능 아이템은 루프 종료
            }
        }

        OnInventoryChanged?.Invoke(); // 인벤토리 변경 알림
        return true; // 아이템 추가 완료 (전부 또는 일부)
    }

    // 아이템 제거 (특정 슬롯에서)
    public void RemoveItem(InventorySlot slotToRemove, int quantityToRemove = 1)
    {
        if (slotToRemove == null || slotToRemove.itemData == null || quantityToRemove <= 0) return;

        slotToRemove.quantity -= quantityToRemove;
        if (slotToRemove.quantity <= 0)
        {
            slotToRemove.ClearSlot(); // 슬롯 비우기
        }
        OnInventoryChanged?.Invoke(); // 인벤토리 변경 알림
    }

     // 아이템 제거 (ItemData와 수량으로) - 첫 번째로 찾은 슬롯에서 제거
    public bool RemoveItem(ItemData itemToRemove, int quantityToRemove = 1)
    {
        if (itemToRemove == null || quantityToRemove <= 0) return false;

        InventorySlot targetSlot = FindItemSlot(itemToRemove);
        if (targetSlot != null && targetSlot.quantity >= quantityToRemove)
        {
            RemoveItem(targetSlot, quantityToRemove);
            return true;
        }
        return false; // 아이템이 없거나 수량이 부족
    }


    // 빈 슬롯 찾기
    private InventorySlot FindEmptySlot()
    {
        return slots.FirstOrDefault(slot => slot.itemData == null);
    }

    // 특정 아이템이 있는 첫 번째 슬롯 찾기
    private InventorySlot FindItemSlot(ItemData itemToFind)
    {
        return slots.FirstOrDefault(slot => slot.itemData == itemToFind);
    }


    // 아이템 이름순 정렬
    public void SortItemsByName()
    {
        // 실제 데이터 정렬: 빈 슬롯은 뒤로, 아이템 있는 슬롯은 이름순 정렬
        slots = slots.OrderBy(slot => slot.itemData == null) // 1. 빈 슬롯을 뒤로
                     .ThenBy(slot => slot.itemData?.itemName) // 2. 아이템 이름순 (null 체크)
                     .ToList();
        OnInventoryChanged?.Invoke(); // 인벤토리 변경 알림
    }
} 