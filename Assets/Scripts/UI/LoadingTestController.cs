using UnityEngine;
using System.Collections;

public class LoadingTestController : MonoBehaviour
{
    [SerializeField] private LoadingScreen loadingScreen;
    [SerializeField] private float testDuration = 5.0f; // 테스트 로딩 총 시간 (초)
    [SerializeField] private float sliderSmoothTime = 0.5f; // 슬라이더 부드러운 이동 시간
    [SerializeField] private string testDungeonName = "테스트 던전";
    
    private float currentVisualProgress = 0f;
    private float targetProgress = 0f;
    
    private void Start()
    {
        // LoadingController 비활성화 (테스트 중에는 실제 로딩 컨트롤러를 사용하지 않음)
        LoadingController realController = FindObjectOfType<LoadingController>();
        if (realController != null && realController.gameObject != gameObject)
        {
            realController.enabled = false;
        }
        
        // LoadingScreen 참조 찾기
        if (loadingScreen == null)
        {
            loadingScreen = FindObjectOfType<LoadingScreen>();
        }
        
        if (loadingScreen != null)
        {
            // 테스트 시작
            StartCoroutine(SimulateLoading());
        }
        else
        {
            Debug.LogError("LoadingScreen을 찾을 수 없습니다!");
        }
    }
    
    private IEnumerator SimulateLoading()
    {
        // 로딩 화면 표시
        loadingScreen.Show(testDungeonName);
        
        float startTime = Time.time;
        float elapsed = 0f;
        
        // 부드러운 시각적 로딩 효과를 위한 코루틴 시작
        StartCoroutine(UpdateVisualProgress());
        
        // 단계별 로딩 시뮬레이션 (더 자연스러운 로딩 효과)
        float[] loadingStages = new float[] { 0.1f, 0.25f, 0.4f, 0.6f, 0.75f, 0.9f, 1.0f };
        float stageTime = testDuration / loadingStages.Length;
        
        foreach (float stage in loadingStages)
        {
            // 각 단계별 대기
            float stageWait = stageTime * Random.Range(0.8f, 1.2f); // 약간의 무작위성
            yield return new WaitForSeconds(stageWait);
            
            // 목표 진행률 업데이트
            targetProgress = stage;
        }
        
        // 시각적 진행 상태가 100%에 도달할 때까지 대기
        while (currentVisualProgress < 0.99f)
        {
            yield return null;
        }
        
        // 완벽히 100%로 설정
        loadingScreen.UpdateProgress(1.0f);
        
        yield return new WaitForSeconds(2.0f);
        
        // 테스트 종료 후에는 숨기지 않음 (결과 확인용)
        Debug.Log("로딩 테스트 완료!");
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