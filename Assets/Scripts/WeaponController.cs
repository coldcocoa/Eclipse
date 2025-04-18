using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WeaponController : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private float reloadTime = 2.0f;
    
    [Header("UI 참조")]
    [SerializeField] private Text ammoText;
    
    [Header("효과")]
    [SerializeField] private ParticleSystem muzzleFlash;
    
    [Header("총알 이펙트")]
    [SerializeField] private Transform muzzlePoint; // 총구 위치
    [SerializeField] private GameObject bulletTrailPrefab; // 총알 궤적 프리팹
    [SerializeField] private float bulletSpeed = 100f; // 총알 속도 (미터/초)
    [SerializeField] private float trailLifetime = 0.5f; // 궤적 지속 시간
    
    [Header("히트 이펙트")]
    [SerializeField] private GameObject hitEffectPrefab; // 몬스터 타격 시 표시할 이펙트
    [SerializeField] private float hitEffectLifetime = 1f; // 히트 이펙트 지속 시간
    
    private bool isReloading = false;
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Start()
    {
        // 초기 총알 설정
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
    }
    
    // 발사 기능 - IntegratedPlayerController에서 호출
    public bool Fire(Vector3 hitPosition, Vector3 hitNormal, bool didHit, string hitTag = "", string hitMaterial = "Default")
    {
        // 장전 중이거나 총알이 없으면 발사 불가
        if (isReloading || currentAmmo <= 0)
        {
            // 빈 총 소리 재생
            if (currentAmmo <= 0)
            {
                try 
                {
                    AudioManager.Instance.PlaySFX("EmptyGun", transform.position);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("빈 총 사운드 재생 실패: " + e.Message);
                }
            }
            return false;
        }
        
        // 총알 감소
        currentAmmo--;
        
        // 1. 총구 이펙트 재생
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // 발사 사운드 재생
        try 
        {
            AudioManager.Instance.PlaySFX("Fire", transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("발사 사운드 재생 실패: " + e.Message);
        }
        
        // UI 업데이트
        UpdateAmmoUI();
        
        return true;
    }
    
    // 재장전 시작 - IntegratedPlayerController에서 호출
    public bool StartReload()
    {
        // 이미 장전 중이거나 총알이 가득 차면 무시
        if (isReloading || currentAmmo >= maxAmmo)
        {
            return false;
        }
        
        StartCoroutine(ReloadRoutine());
        return true;
    }
    
    // 재장전 코루틴
    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        
        // 장전 사운드 재생
        try 
        {
            AudioManager.Instance.PlaySFX("Reload", transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("장전 사운드 재생 실패: " + e.Message);
        }
        
        // 장전 중 UI 효과
        if (ammoText != null)
        {
            ammoText.DOColor(Color.yellow, 0.3f);
            ammoText.transform.DOShakePosition(0.5f, 5, 10, 90, false);
        }
        
        // 장전 시간 대기
        yield return new WaitForSeconds(reloadTime);
        
        // 탄약 리필
        currentAmmo = maxAmmo;
        
        // 장전 완료 UI 효과
        if (ammoText != null)
        {
            ammoText.DOColor(Color.white, 0.3f);
            ammoText.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.3f);
        }
        
        // UI 업데이트
        UpdateAmmoUI();
        
        isReloading = false;
    }
    
    // UI 업데이트
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        
        // DOTween을 사용한 텍스트 애니메이션
        ammoText.transform.DOKill(); // 기존 애니메이션 중단
        ammoText.DOKill(); // 텍스트 컴포넌트의 애니메이션 중단
        
        // 텍스트 내용 업데이트
        ammoText.text = $"{currentAmmo}";
        
        // 텍스트 색상 업데이트 (애니메이션 효과로)
        Color targetColor = (currentAmmo < 10) ? Color.red : Color.white;
        ammoText.DOColor(targetColor, 0.3f);
        
        // 텍스트 크기 펄스 효과
        ammoText.transform.DOScale(1.2f, 0.1f).OnComplete(() => {
            ammoText.transform.DOScale(1f, 0.1f);
        });
        
        // 총알이 적으면 추가 효과 (약간의 흔들림)
        if (currentAmmo < 5)
        {
            ammoText.transform.DOShakePosition(0.3f, 3f, 10, 90, false);
        }
    }
    
    // 현재 재장전 중인지 확인
    public bool IsReloading()
    {
        return isReloading;
    }
    
    // 현재 총알 수 확인
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public bool TryShoot(Camera playerCamera, float weaponRange)
    {
        // 장전 중이거나 총알이 없으면 발사 불가
        if (isReloading || currentAmmo <= 0)
        {
            // 빈 총 소리 재생
            if (currentAmmo <= 0)
            {
                try 
                {
                    AudioManager.Instance.PlaySFX("EmptyGun", transform.position);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("빈 총 사운드 재생 실패: " + e.Message);
                }
            }
            return false;
        }
        
        // 카메라 중앙에서 레이캐스팅
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool didHit = Physics.Raycast(ray, out hit, weaponRange);
        Vector3 targetPoint = didHit ? hit.point : ray.GetPoint(weaponRange);
        
        // 총알 감소
        currentAmmo--;
        
        // 총구 이펙트 재생
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // 새로 추가: 총구에서 총알 궤적 발사
        FireBulletTrail(targetPoint);
        
        // 발사 사운드 재생
        try 
        {
            AudioManager.Instance.PlaySFX("Fire", transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("발사 사운드 재생 실패: " + e.Message);
        }
        
        // UI 업데이트
        UpdateAmmoUI();
        
        // 몬스터 히트 처리
        if (didHit)
        {
            // 몬스터 히트 처리를 위해 새 메서드 호출
            bool hitMonster = HandleMonsterHit(hit);
            
            // 몬스터가 아니라면 일반 표면 히트 이펙트 생성
            if (!hitMonster)
            {
                int layer = hit.collider.gameObject.layer;
                if (layer == LayerMask.NameToLayer("Ground") || 
                    layer == LayerMask.NameToLayer("Wall"))
                {
                    SpawnHitEffect(hit.point, hit.normal);
                }
            }
        }
        
        return true;
    }
    
    // 새로운 메서드: 총알 궤적 생성
    private void FireBulletTrail(Vector3 targetPoint)
    {
        if (bulletTrailPrefab == null) return;
        
        Vector3 startPoint = muzzlePoint != null ? muzzlePoint.position : transform.position;
        Vector3 direction = (targetPoint - startPoint).normalized;
        
        // 1. 낙뢰 효과 상위 컨테이너 생성
        GameObject container = new GameObject("BulletTrailContainer");
        container.transform.position = startPoint;
        container.transform.rotation = Quaternion.LookRotation(direction);
        
        // 2. 컨테이너의 자식으로 파티클 생성
        GameObject bulletTrail = Instantiate(bulletTrailPrefab, Vector3.zero, Quaternion.Euler(90, 0, 0), container.transform);
        bulletTrail.transform.localPosition = Vector3.zero; // 컨테이너 중심에 위치
        
        // 3. 파티클 시스템 최대 거리 설정 (낙뢰 길이)
        ParticleSystem ps = bulletTrail.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // 필요에 따라 파티클 시스템 속성 조정
            var main = ps.main;
            main.startLifetime = Vector3.Distance(startPoint, targetPoint) / bulletSpeed;
            
            // 필요하다면 추가 조정
            var shape = ps.shape;
            shape.scale = new Vector3(0.1f, 0.1f, Vector3.Distance(startPoint, targetPoint));
        }
        
        // 4. 컨테이너를 전진시키는 대신 파티클 효과 위치 고정
        float lifetime = Vector3.Distance(startPoint, targetPoint) / bulletSpeed + 0.5f;
        Destroy(container, lifetime);
    }

    private bool HandleMonsterHit(RaycastHit hit)
    {
        // 몬스터에 맞았는지 확인
        Monster_AI monsterAI = hit.collider?.GetComponentInParent<Monster_AI>();
        if (monsterAI != null)
        {
            // 데미지 처리
            monsterAI.TakeDamage(30);
            
            // 히트 이펙트 생성
            SpawnHitEffect(hit.point, hit.normal);
            return true;
        }

        // 스켈레톤에 맞았는지 확인
        Skeleton_AI skeletonAI = hit.collider?.GetComponentInParent<Skeleton_AI>();
        if (skeletonAI != null)
        {
            // 데미지 처리
            skeletonAI.TakeDamage(30, hit.point);
            
            // 히트 이펙트 생성
            SpawnHitEffect(hit.point, hit.normal);
            return true;
        }
        
        return false;
    }

    // 히트 이펙트 생성 메서드
    private void SpawnHitEffect(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (hitEffectPrefab == null) return;
        
        // 히트 지점에 이펙트 생성 (법선 방향으로 약간 오프셋)
        Vector3 effectPosition = hitPosition + hitNormal * 0.01f; // 약간 표면 위에 생성
        
        // 표면 법선 방향으로 회전
        Quaternion rotation = Quaternion.LookRotation(hitNormal);
        
        // 이펙트 생성
        GameObject hitEffect = Instantiate(hitEffectPrefab, effectPosition, rotation);
        
        // 지정된 시간 후 자동 제거
        Destroy(hitEffect, hitEffectLifetime);
        
        // 효과음 재생 (선택 사항)
        try
        {
            AudioManager.Instance.PlaySFX("BulletImpact", hitPosition);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("히트 효과음 재생 실패: " + e.Message);
        }
    }
} 