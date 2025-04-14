using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "Game/Monster Data")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterDataId;
    public string monsterName;
    public MonsterType monsterType;
    
    [Header("드롭 정보")]
    public int minGold;
    public int maxGold;
    public List<ItemDropData> dropItems;
}

[System.Serializable]
public class ItemDropData
{
    public ItemData item;
    [Range(0, 100)]
    public float dropChance; // 드롭 확률 (0-100%)
    public int minAmount = 1; // 최소 드롭 개수
    public int maxAmount = 1; // 최대 드롭 개수
    public bool bossOnly = false; // 보스 전용 드롭인지 여부
} 