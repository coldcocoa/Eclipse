using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class LoadingScreen : MonoBehaviour
{
    [Tooltip("전체 로딩 화면의 캔버스 그룹 (페이드 효과용)")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("로딩 진행도 표시 이미지")]
    [SerializeField] private Image loadingBar;
    [Tooltip("로딩 진행률 텍스트")]
    [SerializeField] private Text loadingText;
    [Tooltip("로딩 중인 던전 이름 표시")]
    [SerializeField] private Text dungeonNameText;
    [Tooltip("로딩 팁 표시 텍스트")]
    [SerializeField] private Text tipText;
    [Tooltip("로딩 화면에 표시할 팁 목록")]
    [SerializeField] private List<string> loadingTips;
    
    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        gameObject.SetActive(false);
    }
    
    public void Show(string dungeonName)
    {
        gameObject.SetActive(true);
        
        // 초기 상태 설정
        loadingBar.fillAmount = 0f;
        loadingText.text = "로딩 중... 0%";
        
        if (dungeonNameText != null)
        {
            dungeonNameText.text = dungeonName;
        }
        
        // 랜덤 팁 표시
        if (tipText != null && loadingTips.Count > 0)
        {
            tipText.text = loadingTips[Random.Range(0, loadingTips.Count)];
        }
        
        // 페이드 인 효과
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);
    }
    
    public void UpdateProgress(float progress)
    {
        // 진행률 표시 (0~1 사이)
        if (loadingBar != null)
        {
            // DOTween으로 부드러운 로딩바 업데이트
            loadingBar.DOFillAmount(progress, 0.3f);
        }
        
        if (loadingText != null)
        {
            loadingText.text = $"로딩 중... {Mathf.Round(progress * 100)}%";
        }
    }
    
    public void Hide()
    {
        // 페이드 아웃 효과
        canvasGroup.DOFade(0f, 0.5f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }
} 