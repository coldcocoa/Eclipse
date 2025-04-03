using UnityEngine;
using System; // Serializable을 위해 추가

// 드랍 테이블 내의 개별 아이템 항목 정의 (ScriptableObject 아님)
[Serializable]
public class DropTableEntry
{
    public ItemData itemData; // 드랍될 아이템 데이터
    [Range(1, 100)] public int minQuantity = 1; // 최소 드랍 수량
    [Range(1, 100)] public int maxQuantity = 1; // 최대 드랍 수량
    [Range(0f, 1f)] public float dropChance = 0.5f; // 드랍 확률 (0.0 ~ 1.0)

    // 수량 유효성 검사 (에디터에서 값 변경 시 호출)
    public void OnValidate()
    {
        if (maxQuantity < minQuantity)
        {
            maxQuantity = minQuantity;
        }
    }
} 