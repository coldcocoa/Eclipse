using UnityEngine;
using TMPro; // TextMeshPro 사용

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float moveSpeed = 2.0f; // 위로 떠오르는 속도
    [SerializeField] private float fadeDuration = 1.0f; // 사라지는 데 걸리는 시간
    [SerializeField] private float lifeTime = 1.0f; // 총 생존 시간 (fadeDuration과 같거나 약간 길게)

    private float fadeTimer;
    private Color startColor;
    private Transform cameraTransform;

    void Awake()
    {
        // TextMeshPro 컴포넌트 찾기
        if (damageText == null) damageText = GetComponentInChildren<TextMeshProUGUI>();
        if (damageText != null) startColor = damageText.color;
        else
        {
            Debug.LogError("Damage Text (TextMeshProUGUI) component not found!", this);
            Destroy(gameObject); // 컴포넌트 없으면 파괴
            return;
        }

        fadeTimer = fadeDuration;
        cameraTransform = Camera.main?.transform; // 메인 카메라 참조 (Null 체크)

        if (cameraTransform == null)
        {
             Debug.LogError("Main Camera not found!", this);
             // 카메라 없으면 빌보드 효과 불가
        }

        // lifeTime 후 자동 파괴 예약
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 위로 이동
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 페이드 아웃 처리
        if (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(fadeTimer / fadeDuration);
            damageText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        // 빌보드 효과 (카메라 바라보기) - 카메라가 있을 때만
        if (cameraTransform != null)
        {
             transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                              cameraTransform.rotation * Vector3.up);
        }
    }

    // 데미지 값 설정 함수
    public void SetDamage(float damage)
    {
        if (damageText != null)
        {
            damageText.text = Mathf.RoundToInt(damage).ToString(); // 정수로 표시
        }
    }
} 