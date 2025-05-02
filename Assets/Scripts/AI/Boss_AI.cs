using UnityEngine;
using System.Collections;

public class Boss_AI : MonoBehaviour
{
    [Header("기본 스탯")]
    [SerializeField] private int maxHealth = 1000;
    private int currentHealth;

    [Header("AI 설정")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float moveSpeed = 3f; // 이동 속도
    [SerializeField] private float rotationSpeed = 5f; // 회전 속도
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float detectionRange = 30f; // 플레이어 탐지 범위(미터)

    [Header("애니메이션")]
    [SerializeField] private Animator animator;
    private int speedHash;
    private int directionHash;
    private int deadHash;
    private int[] attackHashes = new int[6];

    [SerializeField] private int[] attackDamages = new int[6] { 50, 60, 80, 100, 120, 150 };

    private Transform player;

    private bool isDead = false;
    private bool isAttacking = false;
    private int lastAttackIndex = -1;
    private float attackTimer = 0f;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        speedHash = Animator.StringToHash("Speed");
        directionHash = Animator.StringToHash("Direction");
        deadHash = Animator.StringToHash("Dead");
        attackHashes[0] = Animator.StringToHash("Attack1");
        attackHashes[1] = Animator.StringToHash("Attack2");
        attackHashes[2] = Animator.StringToHash("Attack3");
        attackHashes[3] = Animator.StringToHash("Attack_Spell");
        attackHashes[4] = Animator.StringToHash("Attack_Combo1");
        attackHashes[5] = Animator.StringToHash("Attack_Combo2");
    }

    private void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (isDead) return;
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 탐지 범위 밖이면 대기
        if (distance > detectionRange)
        {
            SetMoveAnim(0f, 0f);
            return;
        }

        // 공격 쿨타임 관리
        if (isAttacking)
        {
            attackTimer += Time.deltaTime;
            SetMoveAnim(0f, 0f);
            if (attackTimer >= attackCooldown)
            {
                isAttacking = false;
                attackTimer = 0f;
            }
            return;
        }

        // 공격 조건
        if (distance <= attackRange)
        {
            TryAttack(distance);
            SetMoveAnim(0f, 0f);
            return;
        }

        // 이동 (NavMesh 없이)
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        // 회전 (플레이어 방향으로)
        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);

        // 애니메이션 파라미터 (원하는 대로 조정)
        SetMoveAnim(1f, 0f); // 예시: 앞으로 이동
    }

    private void SetMoveAnim(float speed, float direction)
    {
        animator.SetFloat(speedHash, speed);
        animator.SetFloat(directionHash, direction);
    }

    private void TryAttack(float distance)
    {
        // 공격 패턴 랜덤 선택(직전 패턴 제외)
        int attackIndex = GetRandomAttackIndex(distance);
        if (attackIndex == -1) return;

        animator.SetFloat(speedHash, 0f);
        animator.SetFloat(directionHash, 0f);
        animator.SetTrigger(attackHashes[attackIndex]);

        isAttacking = true;
        attackTimer = 0f;
        lastAttackIndex = attackIndex;
    }

    private int GetRandomAttackIndex(float distance)
    {
        System.Collections.Generic.List<int> candidates = new System.Collections.Generic.List<int>();
        for (int i = 0; i < 6; i++)
        {
            if (i == lastAttackIndex) continue;
            if (i == 3) // Attack_Spell(공격4)은 거리 무제한
            {
                candidates.Add(i);
            }
            else
            {
                if (distance <= attackRange)
                    candidates.Add(i);
            }
        }
        if (candidates.Count == 0) return -1;
        return candidates[Random.Range(0, candidates.Count)];
    }

    // 애니메이션 이벤트에서 호출할 함수들
    public void Attack1_Hit() { DealDamageToPlayer(0); }
    public void Attack2_Hit() { DealDamageToPlayer(1); }
    public void Attack3_Hit() { DealDamageToPlayer(2); }
    public void Attack4_Hit() { DealDamageToPlayer(3); }
    public void Attack5_Hit() { DealDamageToPlayer(4); }
    public void Attack6_Hit() { DealDamageToPlayer(5); }

    private void DealDamageToPlayer(int attackIndex)
    {
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);
        if (attackIndex != 3 && distance > attackRange) return;
        var playerController = player.GetComponent<IntegratedPlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(attackDamages[attackIndex]);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger(deadHash);
        SetMoveAnim(0f, 0f);
        // 사망 후 추가 처리

        // 3.5초 뒤에 오브젝트 비활성화
        StartCoroutine(DisableAfterDelay(3.5f));
    }

    private System.Collections.IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
