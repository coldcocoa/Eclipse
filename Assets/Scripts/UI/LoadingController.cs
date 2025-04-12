using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class LoadingController : MonoBehaviour
{
    [SerializeField] private LoadingScreen loadingScreen;
    [SerializeField] private float minLoadingTime = 4.0f; // 최소 로딩 시간(초)
    [SerializeField] private float sliderSmoothTime = 0.5f; // 슬라이더 부드러운 이동 시간
    
    private float currentVisualProgress = 0f;
    private float targetProgress = 0f;
    
    private void Start()
    {
        if (loadingScreen == null)
        {
            loadingScreen = FindObjectOfType<LoadingScreen>();
            if (loadingScreen == null)
            {
                Debug.LogError("LoadingScreen을 찾을 수 없습니다!");
                return;
            }
        }
        
        // 씬 이름 확인
        if (string.IsNullOrEmpty(LoadingManager.sceneToLoad))
        {
            Debug.LogError("로드할 씬 이름이 설정되지 않았습니다!");
            return;
        }
        
        // 로딩 화면 표시
        loadingScreen.Show(LoadingManager.dungeonName);
        
        // 다음 씬 로드 시작
        StartCoroutine(LoadScene());
    }
    
    private IEnumerator LoadScene()
    {
        // 비동기 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(LoadingManager.sceneToLoad);
        asyncLoad.allowSceneActivation = false;
        
        float startTime = Time.time;
        float timeElapsed = 0;
        
        // 부드러운 시각적 로딩 효과를 위한 코루틴 시작
        StartCoroutine(UpdateVisualProgress());
        
        // 실제 로딩 진행
        while (asyncLoad.progress < 0.9f)
        {
            timeElapsed = Time.time - startTime;
            targetProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            yield return null;
        }
        
        // 로딩 완료, 목표 진행률을 100%로 설정
        targetProgress = 1.0f;
        
        // 최소 로딩 시간 보장
        float remainingTime = minLoadingTime - timeElapsed;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }
        
        // 시각적 진행 상태가 100%에 도달할 때까지 대기
        while (currentVisualProgress < 0.99f)
        {
            yield return null;
        }
        
        // 완벽히 100%로 설정
        loadingScreen.UpdateProgress(1.0f);
        yield return new WaitForSeconds(1.0f);
        
        // 씬 활성화 (던전으로 이동)
        asyncLoad.allowSceneActivation = true;
    }
    
    // 부드러운 시각적 진행 효과
    private IEnumerator UpdateVisualProgress()
    {
        while (currentVisualProgress < 1.0f)
        {
            // 현재 시각적 진행률을 목표 진행률로 부드럽게 이동
            currentVisualProgress = Mathf.Lerp(currentVisualProgress, targetProgress, 
                                              Time.deltaTime / sliderSmoothTime);
            
            // 마지막 진행률이 너무 느리게 증가하는 것 방지
            if (targetProgress == 1.0f && (targetProgress - currentVisualProgress) < 0.01f)
            {
                currentVisualProgress = 1.0f;
            }
            
            // 화면 업데이트
            loadingScreen.UpdateProgress(currentVisualProgress);
            
            yield return null;
        }
    }
} 