using System.Collections;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private MonsterType monsterType = MonsterType.Slime;
    [SerializeField] private int maxMonsters = 3; // 이 스폰 포인트의 최대 몬스터 수
    [SerializeField] private float respawnDelay = 30f; // 리스폰 딜레이 (초)
    
    [Header("난이도 설정")]
    [Range(1, 10)]
    [SerializeField] private int difficultyLevel = 1; // 난이도 (1-10)
    [SerializeField] private float hpMultiplier = 1.0f; // HP 배수
    [SerializeField] private float damageMultiplier = 1.0f; // 데미지 배수
    
    [Header("활동 영역")]
    [SerializeField] private float activityRadius = 10f; // 활동 반경
    [SerializeField] private bool showActivityArea = true; // 에디터에서 활동 영역 표시
    [SerializeField] private LayerMask monsterLayer; // 몬스터 레이어 (콜라이더 충돌용)
    
    // 런타임 변수
    private int currentMonsterCount = 0; // 현재 활성화된 몬스터 수
    private GameObject activityBoundary; // 활동 영역 경계 오브젝트
    
    private void Start()
    {
        // MonsterManager 참조 확인
        if (MonsterManager.Instance == null)
        {
            Debug.LogError("MonsterManager가 존재하지 않습니다!");
            return;
        }
        
        // 활동 영역 경계 생성
        CreateActivityBoundary();
        
        // 초기 몬스터 스폰
        StartCoroutine(InitialSpawn());
    }
    
    // 초기 몬스터 생성 (약간의 시간차를 두고 생성하여 동시 스폰으로 인한 부하 방지)
    private IEnumerator InitialSpawn()
    {
        // 잠시 대기 (다른 초기화가 완료되도록)
        yield return new WaitForSeconds(1f);
        
        for (int i = 0; i < maxMonsters; i++)
        {
            SpawnMonster();
            yield return new WaitForSeconds(0.2f); // 약간의 간격으로 생성
        }
    }
    
    // 활동 영역 경계 생성
    private void CreateActivityBoundary()
    {
        // 이미 존재하면 삭제
        if (activityBoundary != null)
        {
            Destroy(activityBoundary);
        }
        
        // 새로운 경계 오브젝트 생성
        activityBoundary = new GameObject($"ActivityBoundary_{gameObject.name}");
        activityBoundary.transform.position = transform.position;
        activityBoundary.transform.SetParent(transform);
        
        // 물리적 콜라이더 추가 (보이지 않는 벽)
        SphereCollider boundaryCollider = activityBoundary.AddComponent<SphereCollider>();
        boundaryCollider.radius = activityRadius;
        boundaryCollider.isTrigger = false; // 물리적 충돌 허용
        
        // 경계는 몬스터만 막고 플레이어는 통과하도록 설정
        activityBoundary.layer = LayerMask.NameToLayer("MonsterBoundary");
        Physics.IgnoreLayerCollision(
            LayerMask.NameToLayer("MonsterBoundary"),
            LayerMask.NameToLayer("Player"),
            true
        );
        
        // 콜라이더는 보이지 않도록 (렌더러 없음)
        Debug.Log($"몬스터 활동 경계 생성: 반경 {activityRadius}m");
    }
    
    // 몬스터 스폰 처리
    private void SpawnMonster()
    {
        // 최대치 확인
        if (currentMonsterCount >= maxMonsters)
        {
            return;
        }
        
        // 몬스터 매니저에서 몬스터 가져오기
        GameObject monster = MonsterManager.Instance.GetMonsterFromPool(monsterType);
        if (monster == null)
        {
            Debug.LogWarning("스폰 실패: 사용 가능한 몬스터가 없습니다.");
            return;
        }
        
        // 스폰 위치 설정 (약간의 랜덤성 추가)
        Vector3 spawnPosition = transform.position;
        spawnPosition += new Vector3(
            Random.Range(-2f, 2f),
            0f,
            Random.Range(-2f, 2f)
        );
        
        // 몬스터 위치 및 회전 설정
        monster.transform.position = spawnPosition;
        monster.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        
        // 몬스터 능력치 설정 (난이도에 따른 스케일링)
        Monster_AI monsterAI = monster.GetComponent<Monster_AI>();
        if (monsterAI != null)
        {
            monsterAI.Initialize(
                hpMultiplier * difficultyLevel, 
                damageMultiplier * difficultyLevel,
                this
            );
        }
        else
        {
            Debug.LogError("Monster_AI 컴포넌트를 찾을 수 없습니다.");
        }
        
        // 몬스터 활성화
        monster.SetActive(true);
        currentMonsterCount++;
        
        // 몬스터 레이어 확인 및 설정
        if (monster.layer == LayerMask.NameToLayer("MonsterBoundary"))
        {
            Debug.LogWarning($"몬스터 {monster.name}이 MonsterBoundary 레이어로 잘못 설정되었습니다. 기본 레이어로 변경합니다.");
            monster.layer = 0; // 기본 레이어
        }
        
        // 콜라이더 확인
        Collider monsterCollider = monster.GetComponent<Collider>();
        if (monsterCollider == null)
        {
            Debug.LogError($"몬스터 {monster.name}에 콜라이더가 없습니다!");
        }
        else if (!monsterCollider.enabled)
        {
            Debug.LogWarning($"몬스터 {monster.name}의 콜라이더가 비활성화되어 있습니다. 활성화합니다.");
            monsterCollider.enabled = true;
        }
        
        Debug.Log($"몬스터 스폰: {monster.name} (현재 {currentMonsterCount}/{maxMonsters})");
    }
    
    // 몬스터 사망 처리 (Monster_AI에서 호출)
    public void OnMonsterDeath(GameObject monster)
    {
        if (currentMonsterCount > 0)
            currentMonsterCount--;
        
        Debug.Log($"몬스터 사망: {monster.name} (남은 수 {currentMonsterCount}/{maxMonsters})");
        
        // 몬스터를 풀로 반환
        MonsterManager.Instance.ReturnMonsterToPool(monster);
        
        // 리스폰 타이머 시작
        StartCoroutine(RespawnTimer());
    }
    
    // 리스폰 타이머
    private IEnumerator RespawnTimer()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnMonster();
    }
    
    // 스폰 포인트의 현재 몬스터 수 반환
    public int GetCurrentMonsterCount()
    {
        return currentMonsterCount;
    }
    
    // 에디터에서 활동 영역 시각화 (기즈모)
    private void OnDrawGizmos()
    {
        if (!showActivityArea) return;
        
        // 활동 영역 표시
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
        Gizmos.DrawSphere(transform.position, activityRadius);
        
        // 스폰 포인트 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 난이도 정보 표시
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
            $"난이도: {difficultyLevel} (HP x{hpMultiplier * difficultyLevel:F1}, DMG x{damageMultiplier * difficultyLevel:F1})");
        #endif
    }
} 