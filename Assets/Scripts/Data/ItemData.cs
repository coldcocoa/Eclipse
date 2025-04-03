using UnityEngine;

// 아이템 데이터 ScriptableObject 정의
[CreateAssetMenu(fileName = "New ItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName = "New Item";
    [TextArea] public string description = "Item Description";
    public Sprite itemIcon = null; // 인벤토리 및 LootableObject에 표시될 아이콘

    [Header("기능 정보")]
    public bool isStackable = true;
    public int maxStackSize = 99;
    public GameObject itemPrefab = null; // 월드에 버릴 때 생성될 프리팹

    // 필요에 따라 아이템 타입(장비, 소비, 재료 등), 사용 효과 등을 추가할 수 있습니다.
    // public ItemType itemType;
    // public virtual void Use() { // 아이템 사용 로직 }
} 