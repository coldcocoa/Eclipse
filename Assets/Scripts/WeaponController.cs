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
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Light muzzleLight; // 발사 시 깜빡이는 라이트
    [SerializeField] private GameObject bulletTrailPrefab; // 총알 궤적 프리팹
    [SerializeField] private GameObject impactEffectPrefab; // 탄흔 효과 프리팹 
    [SerializeField] private GameObject[] impactDecalPrefabs; // 표면에 남는 탄흔 데칼 프리팹들
    [SerializeField] private float bulletTrailSpeed = 100f; // 총알 궤적 속도 (미터/초)
    
    [Header("사운드")]
    [SerializeField] private AudioClip[] impactSounds; // 다양한 표면 충돌 사운드
    
    private bool isReloading = false;
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 총구 빛이 없으면 생성
        if (muzzleLight == null && bulletSpawnPoint != null)
        {
            GameObject lightObj = new GameObject("MuzzleLight");
            lightObj.transform.SetParent(bulletSpawnPoint);
            lightObj.transform.localPosition = Vector3.zero;
            
            muzzleLight = lightObj.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = new Color(1f, 0.7f, 0.3f); // 주황빛 불꽃색
            muzzleLight.range = 3f;
            muzzleLight.intensity = 2f;
            muzzleLight.enabled = false; // 초기 비활성화
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
        PlayMuzzleEffects();
        
        // 2. 총알 궤적 효과 생성
        StartCoroutine(CreateBulletTrail(hitPosition, didHit));
        
        // 3. 히트 효과 생성 (명중 시)
        if (didHit)
        {
            CreateImpactEffect(hitPosition, hitNormal, hitTag, hitMaterial);
        }
        
        // 발사 사운드 재생 (Weapons 그룹의 Fire 사운드)
        try 
        {
            AudioManager.Instance.PlaySFX("Fire", bulletSpawnPoint != null ? bulletSpawnPoint.position : transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("발사 사운드 재생 실패: " + e.Message);
        }
        
        // UI 업데이트
        UpdateAmmoUI();
        
        return true;
    }
    
    // 총구 이펙트 재생 (플래시, 빛, 연기 등)
    private void PlayMuzzleEffects()
    {
        // 머즐 플래시 재생
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // 총구 빛 효과
        if (muzzleLight != null)
        {
            StartCoroutine(FlashMuzzleLight());
        }
    }
    
    // 총구 빛 효과 코루틴
    private IEnumerator FlashMuzzleLight()
    {
        muzzleLight.enabled = true;
        
        // 빠르게 페이드 아웃
        float duration = 0.05f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(2f, 0f, elapsed / duration);
            muzzleLight.intensity = intensity;
            yield return null;
        }
        
        muzzleLight.enabled = false;
    }
    
    // 총알 궤적 효과 생성
    private IEnumerator CreateBulletTrail(Vector3 targetPosition, bool didHit)
    {
        if (bulletTrailPrefab == null || bulletSpawnPoint == null) yield break;
        
        // 총알 궤적 오브젝트 생성
        GameObject bulletTrail = Instantiate(bulletTrailPrefab, bulletSpawnPoint.position, Quaternion.identity);
        
        // 시작점과 목표점 설정
        Vector3 startPosition = bulletSpawnPoint.position;
        
        // 궤적 이동 거리 계산
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / bulletTrailSpeed; // 속도에 따른 소요 시간
        
        // 총알 궤적을 시작점에서 목표점까지 빠르게 이동
        float elapsed = 0f;
        
        // 트레일 렌더러 참조 (있다면)
        TrailRenderer trail = bulletTrail.GetComponent<TrailRenderer>();
        
        while (elapsed < duration)
        {
            // 선형 보간으로 궤적 이동
            float t = elapsed / duration;
            bulletTrail.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 최종 위치 설정
        bulletTrail.transform.position = targetPosition;
        
        // 트레일 렌더러가 있다면 점차 사라지게 함
        if (trail != null)
        {
            trail.emitting = false;
            Destroy(bulletTrail, trail.time); // 트레일이 완전히 사라질 때까지 대기 후 제거
        }
        else
        {
            Destroy(bulletTrail, 0.2f); // 트레일이 없으면 짧은 시간 후 제거
        }
    }
    
    // 히트 이펙트 생성 (임팩트 파티클, 데칼, 소리 등)
    private void CreateImpactEffect(Vector3 position, Vector3 normal, string hitTag, string hitMaterial)
    {
        // 임팩트 이펙트 생성 (파티클)
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(impact, 2f); // 2초 후 제거
        }
        
        // 데칼 생성 (표면에 남는 탄흔)
        if (impactDecalPrefabs != null && impactDecalPrefabs.Length > 0)
        {
            // 표면 종류에 따라 다른 데칼 선택 가능 (hitTag/hitMaterial 기반)
            int decalIndex = 0; // 기본 데칼
            
            // 재질/태그에 따른 데칼 선택 로직 (예시)
            if (hitMaterial == "Metal") decalIndex = 1;
            else if (hitMaterial == "Wood") decalIndex = 2;
            else if (hitTag == "Monster") return; // 몬스터에는 데칼 생성 안 함
            
            // 배열 범위 내 인덱스로 조정
            decalIndex = Mathf.Clamp(decalIndex, 0, impactDecalPrefabs.Length - 1);
            
            // 데칼 생성 (약간 표면 앞으로 이동시켜 Z-fighting 방지)
            Vector3 decalPosition = position + normal * 0.01f;
            GameObject decal = Instantiate(impactDecalPrefabs[decalIndex], decalPosition, Quaternion.LookRotation(-normal));
            
            // 데칼 크기와 방향 랜덤화
            decal.transform.Rotate(0, 0, Random.Range(0, 360)); // Z축 기준 랜덤 회전
            float size = Random.Range(0.8f, 1.2f); // 약간의 크기 변형
            decal.transform.localScale = new Vector3(size, size, size);
            
            // 시간이 지남에 따라 데칼 페이드아웃
            StartCoroutine(FadeOutDecal(decal, 5f, 3f)); // 5초 후 3초간 페이드아웃
        }
        
        // 임팩트 사운드 재생
        if (impactSounds != null && impactSounds.Length > 0)
        {
            // 표면 종류에 따라 다른 사운드 선택 가능
            int soundIndex = 0;
            
            // 간단한 재질 기반 사운드 선택 로직
            if (hitMaterial == "Metal") soundIndex = 1;
            else if (hitMaterial == "Wood") soundIndex = 2;
            else if (hitTag == "Monster") soundIndex = 3;
            
            // 배열 범위 내로 조정
            soundIndex = Mathf.Clamp(soundIndex, 0, impactSounds.Length - 1);
            
            // 사운드 재생
            if (audioSource != null && impactSounds[soundIndex] != null)
            {
                audioSource.PlayOneShot(impactSounds[soundIndex], 0.7f);
            }
        }
    }
    
    // 데칼 페이드아웃 코루틴
    private IEnumerator FadeOutDecal(GameObject decal, float delay, float duration)
    {
        if (decal == null) yield break;
        
        // 초기 대기
        yield return new WaitForSeconds(delay);
        
        // 데칼의 렌더러 찾기
        Renderer renderer = decal.GetComponent<Renderer>();
        if (renderer == null) 
        {
            Destroy(decal);
            yield break;
        }
        
        // 알파값 서서히 감소
        Material material = renderer.material;
        Color startColor = material.color;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            Color newColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            material.color = newColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 완전히 투명해지면 제거
        Destroy(decal);
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
        
        // 장전 사운드 재생 (Weapons 그룹의 Reload 사운드)
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
        ammoText.transform.DOScale(1.2f, 0.1f).OnComplete(() => {
            ammoText.transform.DOScale(1f, 0.1f);
        });
        
        // 색상 변화 - 총알이 적으면 빨간색
        Color targetColor = currentAmmo <= 10 ? Color.red : Color.white;
        ammoText.DOColor(targetColor, 0.2f);
        
        // 텍스트 업데이트
        ammoText.text = $"{currentAmmo}";
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
    
    // 장전 중단 (필요시)
    public void CancelReload()
    {
        if (!isReloading) return;
        
        StopAllCoroutines();
        isReloading = false;
        
        if (ammoText != null)
        {
            ammoText.DOColor(Color.white, 0.1f);
        }
    }
} 