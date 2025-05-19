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
            // 씬 로드 후 플레이어 위치 이동 함수 등록
            SceneManager.sceneLoaded += OnTest1Loaded;
            SceneManager.LoadScene("Test1");
        });
    }

    // Test1 씬이 로드된 직후 호출되는 함수
    private void OnTest1Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Test1")
        {
            MainScenePlayerRespawner respawner = GameObject.FindObjectOfType<MainScenePlayerRespawner>();
            if (respawner != null)
            {
                respawner.Point_Player();
            }
        }
        // 이벤트 중복 방지
        SceneManager.sceneLoaded -= OnTest1Loaded;
    }
} 