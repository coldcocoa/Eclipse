using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class Skeleton_AI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject hpBarPrefab;
    [SerializeField] private Transform hpBarPosition;
    [SerializeField] private AudioSource audioSource;
    
    [Header("오디오")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("시각 효과")]
    [SerializeField] private GameObject attackVFX;
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private GameObject deathVFX;
    
    [Header("기본 속성")]
    [SerializeField] private int maxHealth = 100; // 슬라임보다 2배 높은 체력
    [SerializeField] private int attackDamage = 15; // 슬라임보다 2배 높은 데미지
    [SerializeField] private float attackRange = 1.5f; // 슬라임보다 넓은 공격 범위
    [SerializeField] private float detectionRange = 5f; // 탐지 범위 5미터
    [SerializeField] private float attackCooldown = 1f; // 공격 쿨다운 1초
    [SerializeField] private float rotationSpeed = 180f; // 초당 180도 회전 (1초에 완전히 돌 수 있음)
    [SerializeField] private float targetLostTimeout = 1f; // 플레이어 시야에서 사라진 후 1초간 추적
    
    // 애니메이터 파라미터 해시값 (성능 최적화)
    private int speedHHash;
    private int speedVHash;
    private int attackHash;
    private int hitHash;
    private int dieHash;
    
    // 상태 변수
    private int currentHealth;
    private bool isDead = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private Transform target;
    private GameObject hpBarInstance;
    private Vector3 lastKnownTargetPosition;
    private float targetLostTimer = 0f;
    private bool hasTarget = false;
    private SpawnPoint parentSpawnPoint;
    
    // 난이도 관련 멀티플라이어
    private float hpMultiplier = 1.0f;
    private float damageMultiplier = 1.0f;
    
    // 이벤트 (MonsterDrop 등에서 사용)
    public event Action OnMonsterDeath;
    
    private void Awake()
    {
        // 컴포넌트 참조 초기화
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // 해시값 초기화 (성능 최적화)
        speedHHash = Animator.StringToHash("speedh");
        speedVHash = Animator.StringToHash("speedv");
        attackHash = Animator.StringToHash("Attack1h1");
        hitHash = Animator.StringToHash("Hit1");
        dieHash = Animator.StringToHash("Die");
        
        // 상태 초기화
        currentHealth = maxHealth;
        agent.stoppingDistance = attackRange * 0.8f; // 공격 범위보다 약간 짧게 설정
    }
    
    private void Start()
    {
        // HP 바 생성
        if (hpBarPrefab != null && hpBarPosition != null)
        {
            hpBarInstance = Instantiate(hpBarPrefab, hpBarPosition.position, Quaternion.identity, transform);
            hpBarInstance.transform.localPosition = Vector3.up * 2.0f; // 머리 위에 표시
        }
        
        // 플레이어 참조 (타겟) 설정
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // NavMesh 확인
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"스켈레톤 '{gameObject.name}'이 NavMesh 위에 없습니다! 움직이지 못할 수 있습니다.");
        }
        
        // 난이도 설정
        parentSpawnPoint = GetComponentInParent<SpawnPoint>();
        if (parentSpawnPoint != null)
        {
            SetDifficultyMultipliers(1);
        }
    }
    
    private void Update()
    {
        if (isDead) return;
        
        UpdateTargeting();
        UpdateMovement();
        UpdateAnimation();
        UpdateAttack();
    }
    
    private void UpdateTargeting()
    {
        if (target == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // 타겟이 감지 범위 내에 있는 경우
        if (distanceToTarget <= detectionRange)
        {
            hasTarget = true;
            targetLostTimer = 0f;
            lastKnownTargetPosition = target.position;
        }
        // 타겟이 감지 범위를 벗어난 경우
        else if (hasTarget)
        {
            targetLostTimer += Time.deltaTime;
            if (targetLostTimer >= targetLostTimeout)
            {
                hasTarget = false;
            }
        }
    }
    
    private void UpdateMovement()
    {
        // 기존 체크에 추가로 NavMeshAgent 활성화 여부와 NavMesh 위 존재 여부 확인
        if (!hasTarget || isAttacking || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        
        // 타겟을 향해 이동
        agent.SetDestination(lastKnownTargetPosition);
        
        // 타겟 방향으로 회전
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // Y축 회전 제한
            
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;
        
        // 이동 애니메이션 업데이트 (블렌드 트리용)
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        float normalizedSpeedH = Mathf.Clamp(localVelocity.x / agent.speed, -1f, 1f);
        float normalizedSpeedV = Mathf.Clamp(localVelocity.z / agent.speed, -1f, 1f);
        
        animator.SetFloat(speedHHash, normalizedSpeedH, 0.2f, Time.deltaTime);
        animator.SetFloat(speedVHash, normalizedSpeedV, 0.2f, Time.deltaTime);
    }
    
    private void UpdateAttack()
    {
        if (target == null || !hasTarget || isAttacking) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // 공격 범위 내에 타겟이 있고, 쿨다운이 끝났을 때
        if (distanceToTarget <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackCoroutine());
        }
    }
    
    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        
        // 공격 애니메이션 재생
        animator.SetTrigger(attackHash);
        
        // 공격 사운드 재생
        if (audioSource != null && attackSound != null)
        {
            audioSource.clip = attackSound;
            audioSource.Play();
        }
        
        // NavMeshAgent 정지
        agent.isStopped = true;
        
        // 데미지 입히는 타이밍까지 대기 (0.17초)
        yield return new WaitForSeconds(0.17f);
        
        // 공격 범위 내에 있는지 다시 체크
        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            // 공격 이펙트 생성
            if (attackVFX != null)
            {
                Instantiate(attackVFX, target.position, Quaternion.identity);
            }
            
            // 임시로 데미지만 로그로 출력
            int calculatedDamage = Mathf.RoundToInt(attackDamage * damageMultiplier);
            Debug.Log($"스켈레톤이 플레이어에게 {calculatedDamage} 대미지를 입혔습니다.");
            
            // 나중에 실제 구현으로 대체
            // target.GetComponent<실제클래스>().TakeDamage(calculatedDamage);
        }
        
        // 공격 애니메이션 종료까지 대기 (추가 시간)
        yield return new WaitForSeconds(0.8f); // 공격 애니메이션 총 재생 시간 약 1초 가정
        
        // 상태 초기화
        isAttacking = false;
        agent.isStopped = false;
        lastAttackTime = Time.time;
    }
    
    // 외부에서 호출될 수 있는 데미지 처리 메서드
    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // HP 바 업데이트
        UpdateHealthBar();
        
        // 피격 효과
        if (hitVFX != null)
        {
            Instantiate(hitVFX, hitPoint, Quaternion.identity);
        }
        
        // 피격 사운드
        if (audioSource != null && hitSound != null)
        {
            audioSource.clip = hitSound;
            audioSource.Play();
        }
        
        // 피격 애니메이션
        animator.SetTrigger(hitHash);
        
        // 사망 처리
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // 사망 애니메이션
        animator.SetTrigger(dieHash);
        
        // 사망 사운드
        if (audioSource != null && deathSound != null)
        {
            audioSource.clip = deathSound;
            audioSource.Play();
        }
        
        // 사망 이펙트
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position + Vector3.up, Quaternion.identity);
        }
        
        // NavMesh 비활성화
        agent.enabled = false;
        
        // 콜라이더 비활성화 (선택적)
        var colliders = GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        
        // HP 바 제거
        if (hpBarInstance != null)
        {
            Destroy(hpBarInstance);
        }
        
        // 사망 이벤트 발생 (MonsterDrop 등에서 사용)
        OnMonsterDeath?.Invoke();
        
        // SpawnPoint에 사망 알림
        if (parentSpawnPoint != null)
        {
            parentSpawnPoint.OnMonsterDeath(gameObject);
        }
        
        // Destroy 대신 코루틴으로 지연 후 비활성화
        StartCoroutine(DelayedDeactivation(3f));
    }
    
    // 지연 후 비활성화 코루틴 추가
    private IEnumerator DelayedDeactivation(float delay)
    {
        // 지정된 시간 대기
        yield return new WaitForSeconds(delay);
        
        // 이미 풀에 반환되었을 수 있으므로 추가 체크는 하지 않음
        // 몬스터 매니저와 스폰 포인트가 이미 처리함
    }
    
    private void UpdateHealthBar()
    {
        if (hpBarInstance != null)
        {
            var healthBar = hpBarInstance.GetComponent<HealthBarUI>();
            if (healthBar != null)
            {
                healthBar.UpdateHealthBar((float)currentHealth / (maxHealth * hpMultiplier));
            }
        }
    }
    
    // 난이도 설정 메서드
    public void SetDifficultyMultipliers(int difficultyLevel)
    {
        // 난이도에 따른 체력 및 데미지 증가
        hpMultiplier = 1.0f + (difficultyLevel - 1) * 0.2f;
        damageMultiplier = 1.0f + (difficultyLevel - 1) * 0.1f;
        
        // 체력 재설정
        int adjustedMaxHealth = Mathf.RoundToInt(maxHealth * hpMultiplier);
        currentHealth = adjustedMaxHealth;
        
        Debug.Log($"스켈레톤 난이도 설정: {difficultyLevel}, HP: {adjustedMaxHealth}, 데미지: {attackDamage * damageMultiplier}");
    }
    
    // 스폰 포인트 설정 (풀링 시스템용)
    public void SetSpawnPoint(SpawnPoint spawnPoint)
    {
        this.parentSpawnPoint = spawnPoint;
    }
    
    // 발소리 재생 (애니메이션 이벤트에서 호출)
    public void PlayFootstepSound()
    {
        if (audioSource == null || footstepSounds == null || footstepSounds.Length == 0) return;
        
        AudioClip randomFootstep = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];
        if (randomFootstep != null)
        {
            audioSource.PlayOneShot(randomFootstep);
        }
    }
    
    // 리셋 메서드 (풀링 시스템에서 재사용 시 호출)
    public void ResetSkeleton()
    {
        isDead = false;
        isAttacking = false;
        currentHealth = Mathf.RoundToInt(maxHealth * hpMultiplier);
        
        // NavMesh 재활성화
        agent.enabled = true;
        
        // 콜라이더 재활성화
        var colliders = GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
        
        // HP 바 재생성
        if (hpBarPrefab != null && hpBarPosition != null && hpBarInstance == null)
        {
            hpBarInstance = Instantiate(hpBarPrefab, hpBarPosition.position, Quaternion.identity, transform);
            hpBarInstance.transform.localPosition = Vector3.up * 2.0f;
            UpdateHealthBar();
        }
    }
} 