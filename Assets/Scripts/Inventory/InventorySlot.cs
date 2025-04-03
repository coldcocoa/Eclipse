using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인벤토리 슬롯 데이터 구조 (ScriptableObject 아님)
[Serializable]
public class InventorySlot
{
    public ItemData itemData; // 슬롯에 있는 아이템 데이터 (없으면 null)
    public int quantity;      // 아이템 수량

    public InventorySlot()
    {
        itemData = null;
        quantity = 0;
    }

    public InventorySlot(ItemData data, int amount)
    {
        itemData = data;
        quantity = amount;
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        itemData = null;
        quantity = 0;
    }

    // 수량 추가 (최대 스택 고려)
    public int AddQuantity(int amount)
    {
        if (itemData == null || !itemData.isStackable) return amount; // 아이템 없거나 스택 불가

        int spaceAvailable = itemData.maxStackSize - quantity;
        int amountToAdd = Mathf.Min(amount, spaceAvailable);
        quantity += amountToAdd;
        return amount - amountToAdd; // 추가하고 남은 수량 반환
    }
} 