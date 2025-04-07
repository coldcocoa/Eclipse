using UnityEngine;
using UnityEngine.UI;

public class MonsterHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private float yOffset = 2.0f; // 몬스터 머리 위로 얼마나 띄울지

    private Transform monsterTransform;
    private Transform cameraTransform;
    private Monster_AI monsterAI;

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
        cameraTransform = Camera.main?.transform; // 메인 카메라 참조 (Null 체크)

        if (cameraTransform == null)
        {
             Debug.LogError("Main Camera not found!", this);
             // 카메라가 없으면 빌보드 효과를 사용할 수 없으므로,
             // UI가 이상하게 보일 수 있습니다. 파괴하거나 다른 처리 필요.
             Destroy(gameObject);
             return;
        }

        // 슬라이더 참조 확인
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(); // 하위에서 찾아보기
            if (healthSlider == null)
            {
                 Debug.LogError("Health Slider component not found!", this);
                 Destroy(gameObject);
                 return;
            }
        }

        // 초기 체력 설정
        UpdateHealth();
    }

    void Update()
    {
        // 몬스터가 파괴되었거나 설정 안됐으면 자신도 파괴
        if (monsterAI == null || monsterTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        // 체력 업데이트
        UpdateHealth();

        // 몬스터가 죽으면 비활성화 (또는 파괴)
        if (monsterAI.currentHp <= 0)
        {
            // gameObject.SetActive(false); // 비활성화
            Destroy(gameObject); // 파괴 (더 깔끔할 수 있음)
        }
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

    // 슬라이더 값 업데이트
    private void UpdateHealth()
    {
        if (monsterAI != null && healthSlider != null)
        {
            // 체력을 0과 1 사이의 비율로 변환하여 슬라이더 값 설정
            // maxHp가 0이 되는 경우 방지
            if (monsterAI.maxHp > 0)
            {
                healthSlider.value = monsterAI.currentHp / monsterAI.maxHp;
            }
            else
            {
                healthSlider.value = 0; // maxHp가 0이면 체력 0으로 표시
            }
        }
    }
} 