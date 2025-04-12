 using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 추가
using System.Collections.Generic; // List 사용

[RequireComponent(typeof(NavMeshAgent))] // NavMeshAgent 컴포넌트 강제
[RequireComponent(typeof(Animator))]    // Animator 컴포넌트 강제
public class Monster_AI : MonoBehaviour
{
    [Header("몬스터 스탯")]
    [SerializeField] public float maxHp = 30f; // public 또는 프로퍼티로 접근 가능해야 함
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float detectionRange = 15f; // 플레이어 감지 범위
    [SerializeField] private float attackRange = 2f;    // 공격 범위
    // [SerializeField] private float attackDamage = 10f; // 공격 데미지 (필요시 추가)
    [SerializeField] private float attackCooldown = 2f; // 공격 쿨다운

    [Header("탐색 및 이동 설정")]
    [SerializeField] private float patrolCycleTime = 5f; // 순찰(이동+탐색) 주기
    [SerializeField] private float searchDuration = 3f;  // 탐색 지속 시간

    [Header("드랍 설정")] // 드랍 테이블 필드 추가
    [SerializeField] private DropTable dropTable;
    [SerializeField] private GameObject lootableObjectPrefab; // 루팅 가능 오브젝트 프리팹

    // --- UI 관련 필드 추가 ---
    [Header("UI 설정")]
    [SerializeField] private GameObject healthBarPrefab; // 체력 바 UI 프리팹
    [SerializeField] private GameObject damageNumberPrefab; // 데미지 숫자 UI 프리팹
    [SerializeField] private Transform uiSpawnPoint; // UI 생성 위치 (옵션, 없으면 몬스터 루트 사용)

    private MonsterHealthUI healthBarInstance; // 생성된 체력 바 인스턴스 참조
    // ------------------------

    // 내부 변수
    public float currentHp; // public 유지 (MonsterHealthUI에서 접근)
    private Transform player; // 플레이어 Transform
    private NavMeshAgent agent;
    private Animator animator;
    private MonsterState currentState = MonsterState.Idle;
    private float lastActionTime = 0f; // 마지막 행동(이동/탐색 시작 또는 공격) 시간
    private float lastAttackTime = -999f; // 마지막 공격 시간 (쿨다운 계산용)
    private bool isDead = false;

    // 애니메이션 파라미터 이름 (상수)
    private const string ANIM_WALK_F = "Walk_F";
    private const string ANIM_WALK_B = "Walk_B"; // 필요시 사용
    private const string ANIM_ATTACK = "Attack";
    private const string ANIM_LOOK_FOR = "LookFor";
    private const string ANIM_HIT = "Hit";
    private const string ANIM_DIE = "Die";

    // 몬스터 상태 열거형
    private enum MonsterState
    {
        Idle,      // 대기 (다음 순찰 주기 기다림)
        Searching, // 주변 탐색 (LookFor 애니메이션)
        Chasing,   // 플레이어 추적 (Walk_F 애니메이션)
        Attacking, // 플레이어 공격 (Attack 애니메이션)
        Hit,       // 피격 (Hit 애니메이션)
        Dead       // 죽음 (Die 애니메이션)
    }

    // 스폰 시스템 연동을 위한 추가 필드
    private SpawnPoint parentSpawnPoint; // 소환된 스폰 포인트 참조
    private float hpMultiplier = 1.0f;
    private float damageMultiplier = 1.0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform; // Null 조건 연산자 사용

        if (player == null)
        {
            Debug.LogWarning("플레이어를 찾을 수 없습니다. Player 태그를 확인하세요.", this);
        }

        currentHp = maxHp; // 시작 시 체력 초기화
        agent.speed = moveSpeed; // NavMeshAgent 속도 설정
        lastActionTime = -patrolCycleTime; // 게임 시작 시 바로 행동 시작하도록

        // --- 체력 바 생성 ---
        if (healthBarPrefab != null)
        {
            // UI 생성 위치 결정 (uiSpawnPoint 없으면 몬스터 루트 사용)
            Transform spawnParent = (uiSpawnPoint != null) ? uiSpawnPoint : transform;
            // World Space UI는 보통 특정 부모 없이 생성하거나, UI용 Canvas 아래에 생성합니다.
            // 여기서는 일단 부모 없이 생성합니다.
            GameObject healthBarGO = Instantiate(healthBarPrefab, spawnParent.position, Quaternion.identity);
            healthBarInstance = healthBarGO.GetComponent<MonsterHealthUI>();
            if (healthBarInstance != null)
            {
                healthBarInstance.Setup(this); // MonsterHealthUI에 몬스터 정보 전달
            }
            else
            {
                 Debug.LogError("Health Bar Prefab에 MonsterHealthUI 스크립트가 없습니다.", this);
                 Destroy(healthBarGO);
            }
        }
        // ------------------
    }

    void Start()
    {
        agent.stoppingDistance = attackRange * 0.8f; // 공격 범위보다 약간 앞에서 멈추도록 설정
    }

    void Update()
    {
        if (isDead || player == null) return; // 죽었거나 플레이어가 없으면 업데이트 중지

        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(10f); // 예시 데미지
        }

        // 상태에 따른 로직 실행
        switch (currentState)
        {
            case MonsterState.Idle:
                UpdateIdleState();
                break;
            case MonsterState.Searching:
                UpdateSearchingState();
                break;
            case MonsterState.Chasing:
                UpdateChasingState();
                break;
            case MonsterState.Attacking:
                UpdateAttackingState();
                break;
            case MonsterState.Hit:
                // Hit 애니메이션은 상태 전환 시 한 번만 재생하고 Chasing 등으로 돌아감
                // 여기서는 특별한 Update 로직 불필요 (애니메이션 끝날 때 이벤트로 처리 권장)
                break;
            case MonsterState.Dead:
                // 죽음 상태 처리 (이미 isDead 플래그로 Update 초반에 걸러짐)
                break;
        }
    }

    // --- 상태별 업데이트 함수 ---

    private void UpdateIdleState()
    {
        // 순찰 주기 확인
        if (Time.time >= lastActionTime + patrolCycleTime)
        {
            TransitionToState(MonsterState.Searching);
        }
        // Idle 상태에서도 플레이어 감지 시 즉시 추적
        else if (IsPlayerInDetectionRange())
        {
            TransitionToState(MonsterState.Chasing);
        }
    }

    private void UpdateSearchingState()
    {
        // 탐색 시간 확인
        if (Time.time >= lastActionTime + searchDuration)
        {
            TransitionToState(MonsterState.Idle); // 탐색 끝나면 Idle로 복귀
        }
        // 탐색 중 플레이어 감지 시 추적
        else if (IsPlayerInDetectionRange())
        {
            TransitionToState(MonsterState.Chasing);
        }
    }

    private void UpdateChasingState()
    {
        // 플레이어가 감지 범위를 벗어나면 Idle로 복귀
        if (!IsPlayerInDetectionRange())
        {
            TransitionToState(MonsterState.Idle);
            return;
        }

        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 공격 범위 내에 들어오면 공격 상태로 전환
        if (distanceToPlayer <= attackRange)
        {
            TransitionToState(MonsterState.Attacking);
        }
        else
        {
            // 플레이어 위치로 계속 이동
            agent.SetDestination(player.position);
            // 이동 중임을 보장하기 위해 애니메이션 설정 (이미 Chasing 상태 진입 시 설정되었을 수 있음)
            SetAnimationBool(ANIM_WALK_F, true);
        }
    }

    private void UpdateAttackingState()
    {
        // 공격 쿨다운 확인
        if (Time.time < lastAttackTime + attackCooldown)
        {
            // 쿨다운 중이면 대기 (Idle 애니메이션 또는 특정 대기 애니메이션 재생 가능)
            // 여기서는 공격 애니메이션이 끝난 후 자동으로 Idle로 돌아간다고 가정
            // 만약 공격 애니메이션 후 바로 다음 행동을 해야 한다면, 애니메이션 이벤트 사용 권장
            return;
        }

        // 플레이어가 공격 범위를 벗어나면 추적 상태로 전환
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > attackRange)
        {
            TransitionToState(MonsterState.Chasing);
            return;
        }

        // 공격 실행
        // 플레이어를 바라보도록 회전
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // 수평 회전만
        if (directionToPlayer != Vector3.zero)
        {
             transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        SetAnimationBool(ANIM_ATTACK, true); // 공격 애니메이션 재생
        lastAttackTime = Time.time; // 마지막 공격 시간 기록

        // TODO: 실제 플레이어에게 데미지 주는 로직 추가 (예: player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);)
        // Debug.Log("몬스터 공격!");
    }

    // --- 상태 전환 함수 ---

    private void TransitionToState(MonsterState nextState)
    {
        if (currentState == nextState || isDead) return; // 같은 상태거나 죽었으면 전환 안 함

        // 이전 상태 마무리 (필요시)
        // 예: agent.isStopped = false;

        currentState = nextState;
        lastActionTime = Time.time; // 상태 전환 시간 기록 (Idle, Searching 주기 계산용)

        // 모든 애니메이션 파라미터 초기화
        ResetAllAnimationBools();

        // NavMeshAgent 정지/재개 설정
        agent.isStopped = (nextState == MonsterState.Attacking || nextState == MonsterState.Searching || nextState == MonsterState.Idle || nextState == MonsterState.Hit || nextState == MonsterState.Dead);


        // 새 상태에 따른 애니메이션 및 설정 적용
        switch (nextState)
        {
            case MonsterState.Idle:
                // 특별한 애니메이션 없음 (기본 Idle)
                break;
            case MonsterState.Searching:
                SetAnimationBool(ANIM_LOOK_FOR, true);
                break;
            case MonsterState.Chasing:
                SetAnimationBool(ANIM_WALK_F, true);
                agent.SetDestination(player.position); // 즉시 추적 시작
                break;
            case MonsterState.Attacking:
                // UpdateAttackingState에서 Attack 애니메이션 처리
                break;
            case MonsterState.Hit:
                SetAnimationBool(ANIM_HIT, true);
                // Hit 애니메이션이 끝난 후 이전 상태(주로 Chasing)로 돌아가는 로직 필요
                // -> 애니메이션 이벤트 또는 코루틴 사용 권장
                // 임시: 짧은 시간 후 Chasing으로 강제 전환 (플레이어가 근처에 있다면)
                Invoke(nameof(RecoverFromHit), 0.5f); // 0.5초 후 회복 시도
                break;
            case MonsterState.Dead:
                SetAnimationBool(ANIM_DIE, true);
                isDead = true;
                if (agent.isOnNavMesh) // NavMesh 위에 있을 때만 비활성화 시도
                {
                    agent.isStopped = true; // 이동 중지
                    agent.enabled = false; // 네비게이션 비활성화
                }
                //GetComponent<Collider>().enabled = false; // 충돌 비활성화 (선택 사항)

                // --- 드랍 처리 로직 추가 ---
                ProcessDeathDrops();
                // ------------------------

                Destroy(gameObject, 5f); // 5초 후 오브젝트 제거 (Die 애니메이션 시간 고려)

                // --- 체력 바 비활성화 ---
                if (healthBarInstance != null)
                {
                    Destroy(healthBarInstance.gameObject);
                }
                // ------------------------

                // SpawnPoint에 사망 알림 (풀링 시스템용)
                if (parentSpawnPoint != null)
                {
                    parentSpawnPoint.OnMonsterDeath(gameObject);
                }

                break;
        }
    }

    // --- 사망 시 드랍 처리 함수 추가 ---
    private void ProcessDeathDrops()
    {
        if (dropTable == null)
        {
            Debug.LogError($"{gameObject.name}: DropTable이 설정되지 않았습니다.");
            return;
        }

        // 1. 드랍 테이블에서 골드 및 아이템 목록 계산
        dropTable.GetDrops(out int goldAmount, out List<KeyValuePair<ItemData, int>> droppedItems);

        // 2. 골드 지급 (PlayerWallet 참조 필요)
        PlayerWallet playerWallet = PlayerWallet.Instance; // 싱글톤 사용 예시
        if (playerWallet != null && goldAmount > 0)
        {
            playerWallet.AddGold(goldAmount);
            Debug.Log($"{gameObject.name}: 골드 {goldAmount} 드랍 (즉시 지급)");
        }

        // 3. 아이템 드랍 (LootableObject 생성)
        if (droppedItems != null && droppedItems.Count > 0)
        {
            if (lootableObjectPrefab != null)
            {
                // LootableObject 생성 위치 (몬스터 위치 또는 약간 위)
                Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
                GameObject lootGO = Instantiate(lootableObjectPrefab, spawnPosition, Quaternion.identity);
                LootableObject lootable = lootGO.GetComponent<LootableObject>();

                if (lootable != null)
                {
                    lootable.Initialize(droppedItems); // 계산된 아이템 목록 전달
                    Debug.Log($"{gameObject.name}: {droppedItems.Count} 종류의 아이템을 포함한 LootableObject 생성");
                }
                else
                {
                    Debug.LogError($"LootableObject 프리팹에 LootableObject 스크립트가 없습니다: {lootableObjectPrefab.name}");
                    Destroy(lootGO); // 스크립트 없으면 생성된 오브젝트 제거
                }
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: LootableObject 프리팹이 설정되지 않았습니다.");
            }
        }
        else
        {
            Debug.Log($"{gameObject.name}: 드랍할 아이템이 없습니다.");
        }
    }

    // --- 헬퍼 함수 ---

    private bool IsPlayerInDetectionRange()
    {
        return Vector3.Distance(transform.position, player.position) <= detectionRange;
    }

    // 모든 애니메이션 Bool 파라미터를 false로 설정
    private void ResetAllAnimationBools()
    {
        SetAnimationBool(ANIM_WALK_F, false);
        SetAnimationBool(ANIM_WALK_B, false);
        SetAnimationBool(ANIM_ATTACK, false);
        SetAnimationBool(ANIM_LOOK_FOR, false);
        SetAnimationBool(ANIM_HIT, false);
        SetAnimationBool(ANIM_DIE, false);
        // 필요시 기본 Idle 애니메이션 파라미터 true 설정
    }

    // 특정 애니메이션 Bool 파라미터 설정 (존재 여부 확인 포함)
    private void SetAnimationBool(string paramName, bool value)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(paramName, value);
                    return; // 파라미터 찾아서 설정했으면 종료
                }
            }
            // Debug.LogWarning($"Animator에 '{paramName}' Bool 파라미터가 없습니다.", this);
        }
    }

    // 피격 후 상태 회복 (임시)
    private void RecoverFromHit()
    {
        if (isDead) return;

        // 플레이어가 여전히 감지 범위 내에 있다면 추적 상태로
        if (IsPlayerInDetectionRange())
        {
            TransitionToState(MonsterState.Chasing);
        }
        else // 아니면 Idle 상태로
        {
            TransitionToState(MonsterState.Idle);
        }
    }


    // --- 외부 호출 함수 ---

    // 데미지를 받는 함수
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;
        // 체력이 0 미만으로 내려가지 않도록 제한
        currentHp = Mathf.Max(currentHp, 0f);

        Debug.Log($"{gameObject.name} HP: {currentHp}/{maxHp}");

        // --- 체력 바 부드럽게 업데이트 호출 ---
        healthBarInstance?.UpdateHealthSmoothly(currentHp, maxHp);
        // ------------------------------------

        // --- 데미지 숫자 생성 ---
        if (damageNumberPrefab != null)
        {
            Transform spawnTransform = (uiSpawnPoint != null) ? uiSpawnPoint : transform;
            Vector3 spawnPosition = spawnTransform.position + Vector3.up * 1.0f;

            GameObject damageNumGO = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);
            DamageNumber damageNumScript = damageNumGO.GetComponent<DamageNumber>();
            if (damageNumScript != null)
            {
                damageNumScript.SetDamage(damage);
            }
            else
            {
                 Debug.LogError("Damage Number Prefab에 DamageNumber 스크립트가 없습니다.", this);
                 Destroy(damageNumGO);
            }
        }
        // ------------------------

        if (currentHp <= 0)
        {
            // isDead 플래그는 Die 상태 전환 시 설정하는 것이 더 적합할 수 있음
            // isDead = true;
            TransitionToState(MonsterState.Dead);
        }
        else
        {
            TransitionToState(MonsterState.Hit);
        }
    }

    // Gizmos를 사용하여 범위 시각화 (Scene 뷰에서만 보임)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    /// <summary>
    /// 몬스터 초기화 함수 (SpawnPoint에서 호출)
    /// </summary>
    /// <param name="hpMult">체력 배율</param>
    /// <param name="damageMult">공격력 배율</param>
    /// <param name="spawnPoint">스폰 포인트 참조</param>
    public void Initialize(float hpMult, float damageMult, SpawnPoint spawnPoint)
    {
        // 스폰 포인트 참조 저장
        parentSpawnPoint = spawnPoint;
        
        // 스탯 배율 저장
        hpMultiplier = hpMult;
        damageMultiplier = damageMult;
        
        // HP 스케일링
        maxHp *= hpMultiplier;
        currentHp = maxHp;
        
        // 공격력 스케일링 (주석 처리된 attackDamage 사용 중이라면 주석 해제)
        // attackDamage *= damageMultiplier;
        
        // 상태 초기 설정은 Awake/Start에서 이미 처리됨
        
        Debug.Log($"{gameObject.name} 초기화: HP {maxHp}");
    }

    /// <summary>
    /// 몬스터 상태를 리셋합니다 (오브젝트 풀링 재사용 시 호출)
    /// </summary>
    public void ResetMonster()
    {
        // 체력 초기화
        currentHp = maxHp;
        
        // 죽음 상태 초기화
        isDead = false;
        
        // AI 기능 복원
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
        
        // 충돌체 재활성화 (필요시)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        
        // 애니메이션 초기화
        ResetAllAnimationBools();
        
        // 상태 초기화
        currentState = MonsterState.Idle;
        lastActionTime = Time.time - patrolCycleTime; // 즉시 행동 시작하도록
        
        // 체력바 재생성 필요시
        if (healthBarInstance == null && healthBarPrefab != null)
        {
            Transform spawnParent = (uiSpawnPoint != null) ? uiSpawnPoint : transform;
            GameObject healthBarGO = Instantiate(healthBarPrefab, spawnParent.position, Quaternion.identity);
            healthBarInstance = healthBarGO.GetComponent<MonsterHealthUI>();
            if (healthBarInstance != null)
            {
                healthBarInstance.Setup(this);
            }
        }
    }
}
