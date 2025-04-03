using UnityEngine;
using UnityEngine.UI; // Button 사용

// 인벤토리 UI 표시 및 관리 (인벤토리 패널에 부착)
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotsParent; // 슬롯 UI들이 생성될 부모 Transform (Content 오브젝트)
    [SerializeField] private GameObject inventorySlotPrefab; // 인벤토리 슬롯 UI 프리팹
    [SerializeField] private Button sortButton; // 정렬 버튼

    private InventorySystem inventorySystem;
    private InventorySlotUI[] slotUIs; // 생성된 슬롯 UI들 관리
    private Transform playerTransform; // 아이템 버릴 위치 계산용

    // Start 대신 Awake 사용
    void Awake()
    {
        // InventorySystem 인스턴스를 Awake에서 가져옵니다.
        inventorySystem = InventorySystem.Instance;
        if (inventorySystem == null)
        {
            // Awake 시점에서도 못 찾으면 심각한 문제이므로 오류 로그를 남기고 비활성화
            Debug.LogError("InventorySystem.Instance is null in Awake! Make sure InventorySystem exists and is active.");
            enabled = false; // 이 컴포넌트 비활성화
            return;
        }

        // 플레이어 Transform 찾기 (시작 시 한 번만)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        else Debug.LogError("플레이어를 찾을 수 없습니다. 'Player' 태그를 확인하세요.");

        // 슬롯 UI 초기 생성 (Awake에서 해도 무방)
        InitializeSlots();
    }

    void Start()
    {
        // Start에서는 이벤트 구독 및 초기 UI 업데이트만 수행
        if (inventorySystem != null) // Awake에서 실패했을 경우 대비
        {
            inventorySystem.OnInventoryChanged += UpdateUI;
        }
        sortButton?.onClick.AddListener(SortInventory);

        // 초기 UI 업데이트 (활성화 시점에 필요하면 호출)
        // 만약 인벤토리가 항상 비활성화 상태로 시작한다면, 이 Start의 UpdateUI는
        // UIManager의 ToggleInventoryPanel에서 호출하는 것으로 대체될 수 있습니다.
        // 하지만 안전하게 여기서도 호출하거나, OnEnable에서 호출하는 것을 고려할 수 있습니다.
        UpdateUI();
    }

    void OnEnable()
    {
        // 오브젝트가 활성화될 때마다 UI를 최신 상태로 갱신하는 것이 좋을 수 있습니다.
        // Start의 UpdateUI()를 여기로 옮기거나, 여기서도 호출하는 것을 고려해보세요.
        // UpdateUI();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged -= UpdateUI;
        }
        // 이벤트 구독 해제 (InitializeSlots에서 구독한 것)
        if (slotUIs != null)
        {
            foreach (var slotUI in slotUIs)
            {
                if (slotUI != null)
                {
                    slotUI.OnDiscardRequested -= HandleDiscardRequest; // 구독 해제
                }
            }
        }
    }

    // 슬롯 UI 초기 생성
    private void InitializeSlots()
    {
        slotUIs = new InventorySlotUI[inventorySystem.slots.Count];
        for (int i = 0; i < inventorySystem.slots.Count; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsParent);
            slotUIs[i] = slotGO.GetComponent<InventorySlotUI>();
            if (slotUIs[i] != null)
            {
                // 각 슬롯 UI의 버리기 요청 이벤트 구독
                slotUIs[i].OnDiscardRequested += HandleDiscardRequest;
            }
            else
            {
                Debug.LogError($"슬롯 프리팹에 InventorySlotUI 컴포넌트가 없습니다: {inventorySlotPrefab.name}");
            }
        }
    }

    // 인벤토리 데이터 변경 시 UI 업데이트
    public void UpdateUI()
    {
        if (slotUIs == null) return;

        for (int i = 0; i < inventorySystem.slots.Count; i++)
        {
            if (i < slotUIs.Length && slotUIs[i] != null)
            {
                if (inventorySystem.slots[i].itemData != null)
                {
                    // 아이템 정보로 슬롯 UI 업데이트
                    slotUIs[i].UpdateSlot(inventorySystem.slots[i]);
                }
                else
                {
                    // 빈 슬롯으로 업데이트
                    slotUIs[i].ClearSlot();
                }
            }
        }
        Debug.Log("인벤토리 UI 업데이트됨");
    }

    // 정렬 버튼 클릭 시 호출
    private void SortInventory()
    {
        inventorySystem.SortItemsByName();
        // UpdateUI(); // OnInventoryChanged 이벤트가 호출하므로 중복 호출 불필요
    }

    // 슬롯의 버리기 요청 처리 함수 (새로 추가)
    private void HandleDiscardRequest(InventorySlot slotToDiscard)
    {
        if (slotToDiscard == null || slotToDiscard.itemData == null) return;

        ItemData itemToDiscardData = slotToDiscard.itemData; // 데이터 임시 저장
        int quantityToDiscard = 1; // 우선 1개만 버리기

        // 1. 인벤토리에서 아이템 제거
        inventorySystem.RemoveItem(slotToDiscard, quantityToDiscard);

        // 2. 월드에 아이템 프리팹 생성 (플레이어 앞)
        if (itemToDiscardData.itemPrefab != null && playerTransform != null)
        {
            Vector3 dropPosition = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 0.5f;
            Instantiate(itemToDiscardData.itemPrefab, dropPosition, Quaternion.identity);
            Debug.Log($"{itemToDiscardData.itemName} {quantityToDiscard}개 버림");
        }
        else if (playerTransform == null)
        {
             Debug.LogError("아이템을 버릴 플레이어 위치를 찾을 수 없습니다!");
        }

        // UpdateUI(); // OnInventoryChanged 이벤트가 자동으로 호출하므로 필요 없을 수 있음
    }
} 