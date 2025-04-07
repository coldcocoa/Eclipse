using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween 네임스페이스 추가

public class MonsterHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private float yOffset = 2.0f; // 몬스터 머리 위로 얼마나 띄울지
    [SerializeField] private float healthChangeDuration = 0.3f; // 체력 변경 애니메이션 시간

    private Transform monsterTransform;
    private Transform cameraTransform;
    private Monster_AI monsterAI;
    private Tween healthTween; // 현재 진행 중인 체력 트윈 참조

    // 몬스터 AI와 연결하고 초기 설정
    public void Setup(Monster_AI targetMonster)
    {
        monsterAI = targetMonster;
        if (monsterAI == null)
        {
            Debug.LogError("Target Monster is null in Setup!", this);
            Destroy(gameObject);
            return;
        }
        monsterTransform = targetMonster.transform;
        cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
             Debug.LogError("Main Camera not found!", this);
             Destroy(gameObject);
             return;
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
            if (healthSlider == null)
            {
                 Debug.LogError("Health Slider component not found!", this);
                 Destroy(gameObject);
                 return;
            }
        }

        // 초기 체력 설정 (애니메이션 없이 즉시 설정)
        SetInitialHealth();
    }

    // Update에서 직접 체력 업데이트 제거
    void Update()
    {
        // 몬스터가 파괴되었거나 설정 안됐으면 자신도 파괴
        if (monsterAI == null || monsterTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        // 몬스터가 죽으면 비활성화 또는 파괴 (이 로직은 유지)
        // (주의: 체력이 0으로 부드럽게 줄어드는 동안 파괴될 수 있으므로,
        //  TakeDamage에서 즉시 0으로 만들고 여기서 파괴하는 것이 더 나을 수 있음)
        // if (monsterAI.currentHp <= 0)
        // {
        //     Destroy(gameObject);
        // }
    }

    void LateUpdate()
    {
        // 몬스터나 카메라가 사라졌으면 실행 중지
        if (monsterTransform == null || cameraTransform == null) return;

        // 몬스터 머리 위 위치 계산
        transform.position = monsterTransform.position + Vector3.up * yOffset;

        // 항상 카메라를 바라보도록 회전 (빌보드 효과)
        transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                         cameraTransform.rotation * Vector3.up);
    }

    // 초기 체력 설정 (애니메이션 없음)
    private void SetInitialHealth()
    {
        if (monsterAI != null && healthSlider != null)
        {
            float initialValue = (monsterAI.maxHp > 0) ? (monsterAI.currentHp / monsterAI.maxHp) : 0f;
            healthSlider.value = initialValue;
        }
    }

    // --- 체력을 부드럽게 업데이트하는 함수 (Monster_AI에서 호출) ---
    public void UpdateHealthSmoothly(float currentHp, float maxHp)
    {
        if (healthSlider == null) return;

        // 목표 값 계산 (0과 1 사이)
        float targetValue = (maxHp > 0) ? Mathf.Clamp01(currentHp / maxHp) : 0f;

        // 기존 트윈이 있다면 중지 (새로운 값으로 즉시 시작하기 위해)
        healthTween?.Kill(); // 또는 healthSlider.DOKill();

        // DOTween을 사용하여 슬라이더 값 부드럽게 변경
        healthTween = healthSlider.DOValue(targetValue, healthChangeDuration)
                                .SetEase(Ease.OutQuad); // 부드러운 감속 효과 (원하는 Ease로 변경 가능)
                                //.OnComplete(() => healthTween = null); // 트윈 완료 시 참조 제거 (선택 사항)

        // 체력이 0 이하가 되면 즉시 파괴 (선택적이지만 권장)
        if (currentHp <= 0)
        {
            // 약간의 딜레이 후 파괴하여 체력 바가 0으로 가는 것을 보여줄 수 있음
            // Destroy(gameObject, healthChangeDuration + 0.1f);
            // 또는 즉시 파괴
             Destroy(gameObject);
        }
    }
    // ---------------------------------------------------------

    // 오브젝트 파괴 시 DOTween 정리 (메모리 누수 방지)
    void OnDestroy()
    {
        healthTween?.Kill();
    }
} 