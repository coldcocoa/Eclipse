using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }
    
    [Header("몬스터 프리팹")]
    [SerializeField] private GameObject slimePrefab; // 슬라임 프리팹
    
    [Header("풀링 설정")]
    [SerializeField] private int poolSize = 20; // 풀 크기
    [SerializeField] private Transform poolContainer; // 비활성 몬스터 보관 컨테이너 (에디터에서 할당 가능)
    
    // 몬스터 타입별 풀
    private List<GameObject> slimePool = new List<GameObject>();
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePool();
    }
    
    private void InitializePool()
    {
        // 풀 컨테이너가 없으면 생성
        if (poolContainer == null)
        {
            poolContainer = new GameObject("Monster Pool").transform;
            poolContainer.SetParent(transform);
        }
        
        // 슬라임 풀 초기화
        for (int i = 0; i < poolSize; i++)
        {
            if (slimePrefab == null)
            {
                Debug.LogError("슬라임 프리팹이 할당되지 않았습니다!");
                return;
            }
            
            GameObject monster = Instantiate(slimePrefab, Vector3.zero, Quaternion.identity, poolContainer);
            monster.name = $"Slime_{i}";
            monster.SetActive(false);
            slimePool.Add(monster);
        }
        
        Debug.Log($"몬스터 풀 초기화 완료: 슬라임 {poolSize}개");
    }
    
    /// <summary>
    /// 풀에서 비활성화된 몬스터를 가져옵니다.
    /// </summary>
    /// <param name="type">몬스터 타입</param>
    /// <returns>활성화할 몬스터 오브젝트</returns>
    public GameObject GetMonsterFromPool(MonsterType type)
    {
        // 현재는 슬라임만 있으므로 간단하게 구현
        List<GameObject> targetPool = GetPoolByType(type);
        
        foreach (var monster in targetPool)
        {
            if (!monster.activeInHierarchy)
            {
                return monster;
            }
        }
        
        // 풀에 여유가 없는 경우
        Debug.LogWarning($"몬스터 풀({type})이 부족합니다! 확장 필요.");
        return null;
    }
    
    /// <summary>
    /// 몬스터를 풀로 반환합니다.
    /// </summary>
    /// <param name="monster">반환할 몬스터 오브젝트</param>
    public void ReturnMonsterToPool(GameObject monster)
    {
        if (monster != null)
        {
            // 몬스터 상태 초기화
            monster.transform.SetParent(poolContainer);
            monster.transform.position = Vector3.zero;
            monster.SetActive(false);
        }
    }
    
    // 몬스터 타입에 따른 풀 반환
    private List<GameObject> GetPoolByType(MonsterType type)
    {
        switch (type)
        {
            case MonsterType.Slime:
                return slimePool;
            default:
                Debug.LogError($"지원하지 않는 몬스터 타입: {type}");
                return slimePool; // 기본값으로 슬라임 반환
        }
    }
}

// 몬스터 타입 열거형
public enum MonsterType
{
    Slime,
    // 추후 추가될 몬스터 타입들
} 