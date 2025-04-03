using UnityEngine;
using System.Collections.Generic;

// 드랍 테이블 ScriptableObject 정의
[CreateAssetMenu(fileName = "New DropTable", menuName = "Inventory/Drop Table")]
public class DropTable : ScriptableObject
{
    [Header("골드 드랍")]
    public int minGold = 15;
    public int maxGold = 30;

    [Header("아이템 드랍 목록")]
    public List<DropTableEntry> itemDrops;

    // 에디터에서 각 Entry의 OnValidate 호출을 위해 필요
    private void OnValidate()
    {
        if (itemDrops != null)
        {
            foreach (var entry in itemDrops)
            {
                entry.OnValidate();
            }
        }
        if (maxGold < minGold)
        {
            maxGold = minGold;
        }
    }

    // 이 드랍 테이블을 기반으로 실제 드랍될 아이템 목록과 골드를 계산하는 함수
    public void GetDrops(out int goldAmount, out List<KeyValuePair<ItemData, int>> droppedItems)
    {
        // 골드 계산
        goldAmount = Random.Range(minGold, maxGold + 1);

        // 아이템 드랍 계산
        droppedItems = new List<KeyValuePair<ItemData, int>>();
        if (itemDrops != null)
        {
            foreach (var entry in itemDrops)
            {
                if (Random.value <= entry.dropChance) // Random.value는 0.0 이상 1.0 미만 반환
                {
                    int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                    droppedItems.Add(new KeyValuePair<ItemData, int>(entry.itemData, quantity));
                }
            }
        }
    }
} 