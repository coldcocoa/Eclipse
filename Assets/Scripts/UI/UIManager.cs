using UnityEngine;
using UnityEngine.UI; // Slider, Text 등 사용
using System.Collections; // Coroutine 사용
using TMPro; // TextMeshPro 사용 시 추가

// UI 요소 관리 및 상호작용 피드백 처리 (씬에 하나 존재)
public class UIManager : MonoBehaviour
{
    [Header("루팅 UI")]
    [SerializeField] private GameObject lootingProgressPanel; // 루팅 진행 슬라이더 포함 패널
    [SerializeField] private Slider lootingSlider;

    [Header("메시지 UI")]
    [SerializeField] private GameObject messagePanel; // 메시지 표시 패널
    [SerializeField] private TextMeshProUGUI messageText; // 메시지 텍스트 (TMP)
    [SerializeField] private float messageDuration = 2f;

    [Header("인벤토리 UI")]
    [SerializeField] private GameObject inventoryPanel; // 인벤토리 전체 패널
    [SerializeField] private InventoryUI inventoryUI; // InventoryUI 스크립트 참조

    [Header("상호작용 UI")]
    [SerializeField] private GameObject interactionPromptPanel; // "E키 눌러 상호작용" 텍스트 등

    // --- 골드 UI 추가 ---
    [Header("플레이어 정보")]
    [SerializeField] private TextMeshProUGUI goldText; // 골드 표시 텍스트 (TMP)
    // [SerializeField] private Text goldText; // 일반 Text 사용 시
    // --------------------

    // 싱글톤
    public static UIManager Instance { get; private set; }

    private Coroutine lootingCoroutine = null;
    private Coroutine messageCoroutine = null;
    private LootableObject currentLootTarget = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 초기 UI 상태 설정
        lootingProgressPanel?.SetActive(false);
        messagePanel?.SetActive(false);
        inventoryPanel?.SetActive(false);
        interactionPromptPanel?.SetActive(false);

        // 게임 시작 시 초기 골드 UI 업데이트
        if (PlayerWallet.Instance != null)
        {
            UpdateGoldUI(PlayerWallet.Instance.currentGold);
        }
        else
        {
             // PlayerWallet이 아직 준비되지 않았을 수 있으므로, 약간의 딜레이 후 시도하거나
             // PlayerWallet의 Awake에서 UIManager를 호출하는 방식을 고려할 수 있습니다.
             // 여기서는 간단히 초기값 0으로 설정하거나, 오류 메시지를 표시할 수 있습니다.
             UpdateGoldUI(0); // 또는 오류 처리
             Debug.LogWarning("UIManager.Start: PlayerWallet.Instance is null. Initial gold UI might be incorrect.");
        }
    }

    private void Update()
    {
        // 인벤토리 열기/닫기 ('I' 키)
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventoryPanel();
        }
    }

    // --- 루팅 관련 ---
    public void StartLootingProgress(LootableObject target, float duration)
    {
        if (lootingCoroutine != null)
        {
            StopCoroutine(lootingCoroutine); // 이전 코루틴 중지
        }
        currentLootTarget = target;
        lootingCoroutine = StartCoroutine(LootingProgressCoroutine(duration));
    }

    public void StopLootingProgress()
    {
        if (lootingCoroutine != null)
        {
            StopCoroutine(lootingCoroutine);
            lootingCoroutine = null;
        }
        lootingProgressPanel?.SetActive(false);
        currentLootTarget = null; // 타겟 초기화
        // PlayerInteraction 상태 업데이트 알림
        FindObjectOfType<PlayerInteraction>()?.NotifyLootingFinished(false); // 취소 시 성공 false
    }

    private IEnumerator LootingProgressCoroutine(float duration)
    {
        lootingProgressPanel?.SetActive(true);
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (lootingSlider != null)
            {
                lootingSlider.value = timer / duration;
            }
            yield return null; // 다음 프레임까지 대기
        }

        // 루팅 완료
        lootingProgressPanel?.SetActive(false);
        lootingCoroutine = null;
        currentLootTarget?.CompleteLoot(); // LootableObject에게 완료 알림
        // PlayerInteraction 상태 업데이트 알림
        FindObjectOfType<PlayerInteraction>()?.NotifyLootingFinished(true); // 완료 시 성공 true
        currentLootTarget = null; // 타겟 초기화
    }

    // --- 메시지 관련 ---
    public void ShowMessage(string message)
    {
        if (messageText == null) return;

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        messageCoroutine = StartCoroutine(ShowMessageCoroutine(message));
    }

    private IEnumerator ShowMessageCoroutine(string message)
    {
        messageText.text = message;
        messagePanel?.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        messagePanel?.SetActive(false);
        messageCoroutine = null;
    }

    // --- 인벤토리 관련 ---
    public void ToggleInventoryPanel()
    {
        bool isActive = !inventoryPanel.activeSelf;
        inventoryPanel?.SetActive(isActive);
        // 인벤토리 열 때 UI 갱신 (선택 사항)
        if (isActive && inventoryUI != null)
        {
            inventoryUI.UpdateUI();
        }
        // 인벤토리 열 때 게임 일시정지 등 추가 로직 가능
        // Time.timeScale = isActive ? 0f : 1f;
        // Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        // Cursor.visible = isActive;
    }

    // --- 상호작용 프롬프트 ---
    public void ShowInteractionPrompt(bool show)
    {
        interactionPromptPanel?.SetActive(show);
    }

    // --- 골드 UI 업데이트 함수 추가 ---
    public void UpdateGoldUI(int goldAmount)
    {
        if (goldText != null)
        {
            goldText.text = $"골드: {goldAmount}"; // 원하는 형식으로 표시
        }
        else
        {
            Debug.LogWarning("UIManager에 골드 텍스트가 할당되지 않았습니다.");
        }
    }
    // ---------------------------------
} 