using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;

public class Loading_Main : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private Text loadingText;

    [Header("로딩 설정")]
    [SerializeField] private float loadingDuration = 4f; // 4초

    private void Start()
    {
        // 시작 시 로딩바 0으로 초기화
        if (loadingSlider != null)
            loadingSlider.value = 0f;
        if (loadingText != null)
            loadingText.text = "0%";

        // 트윈 시작
        DOTween.To(
            () => loadingSlider.value,
            x => {
                loadingSlider.value = x;
                if (loadingText != null)
                    loadingText.text = $"{Mathf.RoundToInt(x * 100)}%";
            },
            1f, // 목표값(100%)
            loadingDuration
        )
        .SetEase(Ease.Linear)
        .OnComplete(() => {
            // 100%가 되면 Test1 씬으로 이동
            SceneManager.LoadScene("Test1");
        });
    }

   
    
   
    
    
} 