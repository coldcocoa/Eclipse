using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 추가

[RequireComponent(typeof(NavMeshAgent))] // NavMeshAgent 컴포넌트 강제
[RequireComponent(typeof(Animator))]    // Animator 컴포넌트 강제
public class Monster_AI : MonoBehaviour
{
    [Header("몬스터 스탯")]
    [SerializeField] private float maxHp = 30f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float detectionRange = 15f; // 플레이어 감지 범위
    [SerializeField] private float attackRange = 2f;    // 공격 범위
    // [SerializeField] private float attackDamage = 10f; // 공격 데미지 (필요시 추가)
    [SerializeField] private float attackCooldown = 2f; // 공격 쿨다운

    [Header("탐색 및 이동 설정")]
    [SerializeField] private float patrolCycleTime = 5f; // 순찰(이동+탐색) 주기
    [SerializeField] private float searchDuration = 3f;  // 탐색 지속 시간

    // 내부 변수
    private float currentHp;
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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 플레이어 찾기 (태그가 "Player"라고 가정)
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("플레이어를 찾을 수 없습니다. 'Player' 태그를 확인하세요.", this);
            enabled = false; // 플레이어 없으면 AI 비활성화
        }
    }

    void Start()
    {
        currentHp = maxHp;
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f; // 공격 범위보다 약간 앞에서 멈추도록 설정
        lastActionTime = -patrolCycleTime; // 시작 시 바로 순찰 시작하도록
    }

    void Update()
    {
        if (isDead || player == null) return; // 죽었거나 플레이어가 없으면 업데이트 중지

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
                agent.enabled = false; // 네비게이션 중지
                // 추가적인 죽음 처리 (콜라이더 비활성화 등)
                Destroy(gameObject, 5f); // 5초 후 오브젝트 제거 (예시)
                break;
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
        Debug.Log($"{gameObject.name} HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            currentHp = 0;
            TransitionToState(MonsterState.Dead);
        }
        else
        {
            // 죽지 않았으면 Hit 상태로 전환
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
}
