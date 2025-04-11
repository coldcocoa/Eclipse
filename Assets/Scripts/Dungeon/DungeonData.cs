using System.Collections.Generic;
using UnityEngine;

public enum DungeonDifficulty
{
    Easy,    // 쉬움
    Normal,  // 보통
    Hard     // 어려움
}

[CreateAssetMenu(fileName = "NewDungeonData", menuName = "Dungeon/Dungeon Data")]
public class DungeonData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("던전 고유 ID (다른 던전과 중복되지 않아야 함)")]
    public string dungeonId;
    
    [Tooltip("던전 표시 이름")]
    public string dungeonName;
    
    [Tooltip("던전 상세 설명")]
    [TextArea(3, 5)]
    public string description;
    
    [Header("던전 설정")]
    [Tooltip("던전 씬 이름 (Build Settings에 등록된 씬 이름과 일치해야 함)")]
    public string sceneName;
    
    [Header("몬스터 구성")]
    [Tooltip("일반 몬스터 수")]
    public int normalMonsterCount = 5;
    
    [Tooltip("정예 몬스터 수")]
    public int eliteMonsterCount = 2;
    
    [Tooltip("보스 몬스터 존재 여부")]
    public bool hasBoss = true;
    
    [Header("보상 설정")]
    [Tooltip("던전 클리어 시 반드시 지급되는 보상 아이템")]
    public List<ItemData> guaranteedRewards;
    
    [Tooltip("던전 클리어 시 확률적으로 지급되는 보상 아이템")]
    public List<ItemDropChance> possibleRewards;
    
    // 난이도별 스케일 값 - 선택된 난이도에 따라 적용
    public float GetHPMultiplier(DungeonDifficulty difficulty)
    {
        switch(difficulty) {
            case DungeonDifficulty.Easy: return 1.0f;
            case DungeonDifficulty.Normal: return 1.5f;
            case DungeonDifficulty.Hard: return 2.0f;
            default: return 1.0f;
        }
    }
    
    public float GetDamageMultiplier(DungeonDifficulty difficulty)
    {
        switch(difficulty) {
            case DungeonDifficulty.Easy: return 1.0f;
            case DungeonDifficulty.Normal: return 1.3f;
            case DungeonDifficulty.Hard: return 1.8f;
            default: return 1.0f;
        }
    }
}

[System.Serializable]
public class ItemDropChance
{
    [Tooltip("드롭될 아이템")]
    public ItemData item;
    
    [Tooltip("드롭 확률 (0-100%)")]
    [Range(0, 100)]
    public float dropChance;
} 