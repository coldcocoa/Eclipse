using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
        
        // 장전 시간 대기
        yield return new WaitForSeconds(reloadTime);
        
        // 탄약 리필
        currentAmmo = maxAmmo;
        
        // UI 업데이트
        UpdateAmmoUI();
        
        isReloading = false;
    }
    
    // UI 업데이트
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
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
} 