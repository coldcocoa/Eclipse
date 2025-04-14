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
    
    private bool isReloading = false;
    
    private void Start()
    {
        // 초기 총알 설정
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
    }
    
    // 발사 기능 - IntegratedPlayerController에서 호출
    public bool Fire()
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
        
        // 발사 효과 처리
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
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
        Color targetColor = currentAmmo <= 5 ? Color.red : Color.white;
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