using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class MonsterDrop : MonoBehaviour
{
    [SerializeField] private MonsterData monsterData;
    [SerializeField] private GameObject lootPrefab; // LootableObject 프리팹
    [SerializeField] private GameObject goldEffectPrefab; // 직접 에디터에서 할당할 수 있도록 변경
    
    // 이벤트 콜백 저장용 변수 추가
    private Action monsterDeathCallback;
    
    private void Awake()
    {
        // Monster_AI 또는 Skeleton_AI 컴포넌트 참조 찾기 및 이벤트 등록
        SetupDeathCallback();
    }
    
    private void SetupDeathCallback()
    {
        // 먼저 Monster_AI 시도
        Monster_AI monsterAI = GetComponent<Monster_AI>();
        if (monsterAI != null)
        {
            monsterDeathCallback = HandleMonsterDeath;
            monsterAI.OnMonsterDeath += monsterDeathCallback;
            return;
        }
        
        // 실패하면 Skeleton_AI 시도
        Skeleton_AI skeletonAI = GetComponent<Skeleton_AI>();
        if (skeletonAI != null)
        {
            monsterDeathCallback = HandleMonsterDeath;
            skeletonAI.OnMonsterDeath += monsterDeathCallback;
            return;
        }
        
        // 둘 다 못 찾으면 경고
        Debug.LogWarning("MonsterDrop 컴포넌트에 필요한 Monster_AI 또는 Skeleton_AI 컴포넌트를 찾을 수 없습니다!");
    }
    
    private void OnDisable()
    {
        // 이벤트 해제 로직도 수정
        if (monsterDeathCallback != null)
        {
            Monster_AI monsterAI = GetComponent<Monster_AI>();
            if (monsterAI != null)
            {
                monsterAI.OnMonsterDeath -= monsterDeathCallback;
                return;
            }
            
            Skeleton_AI skeletonAI = GetComponent<Skeleton_AI>();
            if (skeletonAI != null)
            {
                skeletonAI.OnMonsterDeath -= monsterDeathCallback;
            }
        }
    }
    
    public void HandleMonsterDeath()
    {
        if (monsterData == null) return;
        
        // 1. 골드 자동 획득
        DropGold();
        
        // 2. 아이템 드롭
        DropItems();
    }
    
    private void DropGold()
    {
        if (monsterData == null) return;
        
        int goldAmount = Random.Range(monsterData.minGold, monsterData.maxGold + 1);
        if (goldAmount > 0)
        {
            // 골드 이펙트 생성 (null 체크 추가)
            if (goldEffectPrefab != null) 
            {
                GameObject goldEffect = Instantiate(goldEffectPrefab, 
                                                   transform.position + Vector3.up * 0.5f, 
                                                   Quaternion.identity);
                Destroy(goldEffect, 2f);
            }
            else
            {
                Debug.LogWarning("골드 이펙트 프리팹이 할당되지 않았습니다. 이펙트 없이 골드만 지급합니다.");
            }
            
            // 실제 골드 추가
            Debug.Log($"{monsterData.monsterName}에서 {goldAmount} 골드를 획득했습니다.");
            
            // 골드 시스템에 추가 (InventorySystem.Instance 사용)
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddGold(goldAmount);
            }
            else
            {
                Debug.LogError("InventorySystem을 찾을 수 없습니다!");
            }
        }
    }
    
    private void DropItems()
    {
        if (monsterData == null || monsterData.dropItems.Count == 0) return;
        
        List<KeyValuePair<ItemData, int>> droppedItems = new List<KeyValuePair<ItemData, int>>();
        
        foreach (ItemDropData dropData in monsterData.dropItems)
        {
            // 보스 전용 아이템은 보스가 아니면 건너뜀
            // MonsterType.Boss 대신 MonsterType.Reaper 사용 (또는 구현된 다른 보스 타입)
            if (dropData.bossOnly && monsterData.monsterType != MonsterType.Reaper)
                continue;
                
            float roll = Random.Range(0f, 100f);
            if (roll <= dropData.dropChance)
            {
                int amount = Random.Range(dropData.minAmount, dropData.maxAmount + 1);
                droppedItems.Add(new KeyValuePair<ItemData, int>(dropData.item, amount));
            }
        }
        
        if (droppedItems.Count > 0)
        {
            // 루팅 아이템 오브젝트 생성
            CreateLootableObject(droppedItems);
        }
    }
    
    private void CreateLootableObject(List<KeyValuePair<ItemData, int>> items)
    {
        // LootableObject 프리팹을 가져옴
        if (lootPrefab == null)
        {
            // 더 이상 Resources.Load 사용하지 않음
            Debug.LogError("LootableObject 프리팹이 할당되지 않았습니다! Inspector에서 직접 할당해주세요.");
            return;
        }
        
        // 몬스터 위치 근처에 약간의 랜덤성을 추가하여 생성
        Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
        dropPosition += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
        
        GameObject lootObject = Instantiate(lootPrefab, dropPosition, Quaternion.identity);
        LootableObject lootComponent = lootObject.GetComponent<LootableObject>();
        
        if (lootComponent != null)
        {
            lootComponent.Initialize(items);
        }
    }
} 