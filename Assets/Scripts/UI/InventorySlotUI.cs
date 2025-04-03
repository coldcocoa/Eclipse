using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // IPointerClickHandler 사용
using TMPro; // TextMeshPro 사용 시
using System; // Action 사용

// 인벤토리의 개별 슬롯 UI 처리
public class InventorySlotUI : MonoBehaviour // IPointerClickHandler 제거 가능 (우클릭 필요 없으면)
{
    [SerializeField] private Image itemIconImage; // 아이템 아이콘 표시 이미지
    [SerializeField] private TextMeshProUGUI quantityText; // 아이템 수량 표시 텍스트 (TMP 사용 시)
    // [SerializeField] private Text quantityText; // 일반 Text 사용 시
    [SerializeField] private Button discardButton; // 버리기 버튼 참조 추가

    // 버리기 버튼 클릭 시 발생하는 이벤트 (InventorySlot 데이터를 전달)
    public event Action<InventorySlot> OnDiscardRequested;

    private InventorySlot currentSlotData; // 현재 슬롯 데이터 저장

    private void Awake() // Start 대신 Awake 사용 가능
    {
        // 버리기 버튼 리스너 연결
        discardButton?.onClick.AddListener(OnDiscardButtonClicked);
        // 초기에는 버튼 비활성화 (아이템 있을 때만 활성화)
        if (discardButton != null) discardButton.gameObject.SetActive(false);
    }

    // 슬롯 UI 업데이트 (아이템 정보 표시)
    public void UpdateSlot(InventorySlot slot)
    {
        currentSlotData = slot; // 현재 데이터 저장

        if (slot.itemData != null)
        {
            itemIconImage.sprite = slot.itemData.itemIcon;
            itemIconImage.enabled = true; // 아이콘 이미지 활성화

            // 수량 표시 (스택 가능하고 1개 초과일 때만)
            if (slot.itemData.isStackable && slot.quantity > 1)
            {
                quantityText.text = slot.quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false; // 수량 텍스트 숨김
            }
            // 아이템이 있으면 버리기 버튼 활성화
            if (discardButton != null) discardButton.gameObject.SetActive(true);
        }
        else
        {
            ClearSlot(); // 빈 슬롯 처리
        }
    }

    // 슬롯 UI 비우기 (아이템 없음 표시)
    public void ClearSlot()
    {
        currentSlotData = null; // 데이터 초기화
        itemIconImage.sprite = null;
        itemIconImage.enabled = false; // 아이콘 이미지 비활성화
        quantityText.enabled = false; // 수량 텍스트 비활성화
        // 아이템 없으면 버리기 버튼 비활성화
        if (discardButton != null) discardButton.gameObject.SetActive(false);
    }

    // 버리기 버튼 클릭 시 호출될 함수
    private void OnDiscardButtonClicked()
    {
        // 유효한 슬롯 데이터가 있을 때만 이벤트 발생
        if (currentSlotData != null && currentSlotData.itemData != null)
        {
            Debug.Log($"버리기 요청: {currentSlotData.itemData.itemName}");
            OnDiscardRequested?.Invoke(currentSlotData); // InventoryUI에게 알림
        }
    }

    // IPointerClickHandler 인터페이스와 OnPointerClick 함수는 제거 가능
} 