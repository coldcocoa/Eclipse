using UnityEngine;
using UnityEngine.SceneManagement;

public class FootstepManager : MonoBehaviour
{
    [Header("발자국 소리 세트")]
    [SerializeField] private AudioClip[] forestWalkFootsteps; // 숲 걷기 소리
    [SerializeField] private AudioClip[] forestRunFootsteps;  // 숲 뛰기 소리
    [SerializeField] private AudioClip[] dungeonWalkFootsteps; // 던전 걷기 소리
    [SerializeField] private AudioClip[] dungeonRunFootsteps;  // 던전 뛰기 소리
    
    [Header("씬 식별자")]
    [SerializeField] private string[] dungeonSceneNames; // 던전 씬 이름 목록
    
    [Header("애니메이션 설정")]
    [SerializeField] private float runThreshold = 0.8f; // 달리기로 간주할 Speed 값 (0.5: 걷기, 1.0: 달리기)
    
    [Header("오디오 설정")]
    [SerializeField] private float footstepVolume = 0.7f; // 발소리 볼륨
    [SerializeField] private AudioSource audioSource; // 오디오 소스 (없으면 자동 생성)
    
    // 런타임 참조
    private Animator playerAnimator;
    private AudioClip[] currentWalkFootsteps; // 현재 걷기 소리
    private AudioClip[] currentRunFootsteps;  // 현재 뛰기 소리
    private bool isInDungeon;
    
    private void Awake()
    {
        // 애니메이터 참조 찾기
        playerAnimator = GetComponentInParent<IntegratedPlayerController>()?.GetComponentInChildren<Animator>();
        
        // 찾지 못했다면 다른 방법으로 시도
        if (playerAnimator == null)
        {
            // 부모에서 찾기
            playerAnimator = GetComponentInParent<Animator>();
            
            // 그래도 없다면 전체 계층에서 찾기
            if (playerAnimator == null)
            {
                Transform root = transform.root;
                playerAnimator = root.GetComponentInChildren<Animator>();
            }
        }
        
        if (playerAnimator == null)
        {
            Debug.LogError("FootstepManager: Animator를 찾을 수 없습니다!");
        }
        
        // 오디오 소스 설정
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f; // 3D 소리
                audioSource.volume = footstepVolume;
                audioSource.playOnAwake = false;
            }
        }
        
        // 초기 발자국 소리 세트 설정
        UpdateFootstepSet();
        
        // 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // 이벤트 해제 (메모리 누수 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 변경 시 발자국 소리 세트 업데이트
        UpdateFootstepSet();
    }
    
    private void UpdateFootstepSet()
    {
        // 현재 씬이 던전인지 필드인지 확인
        string currentSceneName = SceneManager.GetActiveScene().name;
        isInDungeon = false;
        
        foreach (string dungeonName in dungeonSceneNames)
        {
            if (currentSceneName.Contains(dungeonName))
            {
                isInDungeon = true;
                break;
            }
        }
        
        // 환경에 맞는 발자국 소리 세트 선택
        currentWalkFootsteps = isInDungeon ? dungeonWalkFootsteps : forestWalkFootsteps;
        currentRunFootsteps = isInDungeon ? dungeonRunFootsteps : forestRunFootsteps;
    }
    
    // 플레이어 애니메이션에서 호출할 발자국 소리 재생 메서드
    public void PlayFootstep()
    {
        // 발소리 재생할 소리 세트 결정
        AudioClip[] selectedFootsteps = GetAppropriateFootstepSounds();
        if (selectedFootsteps == null || selectedFootsteps.Length == 0)
        {
            Debug.LogWarning("FootstepManager: 발소리 클립이 없습니다!");
            return;
        }
        
        // 랜덤 발자국 소리 선택
        AudioClip footstep = selectedFootsteps[Random.Range(0, selectedFootsteps.Length)];
        
        // 발자국 소리 재생
        if (footstep != null)
        {
            audioSource.clip = footstep;
            audioSource.volume = footstepVolume;
            audioSource.Play();
        }
    }
    
    // 현재 상태에 적합한 발소리 결정
    private AudioClip[] GetAppropriateFootstepSounds()
    {
        bool isRunning = false;
        
        // 애니메이터 파라미터 기준으로 걷기/뛰기 판단
        if (playerAnimator != null)
        {
            float speedParam = playerAnimator.GetFloat("Speed");
            isRunning = speedParam >= runThreshold;
        }
        
        // 환경과 이동 속도에 따라 발소리 선택
        return isRunning ? currentRunFootsteps : currentWalkFootsteps;
    }
} 