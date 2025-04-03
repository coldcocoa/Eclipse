using UnityEngine;
using UnityEngine.UI; // Slider, Text 등 사용
using System.Collections; // Coroutine 사용

// UI 요소 관리 및 상호작용 피드백 처리 (씬에 하나 존재)
public class UIManager : MonoBehaviour
{
    [Header("루팅 UI")]
    [SerializeField] private GameObject lootingProgressPanel; // 루팅 진행 슬라이더 포함 패널
    [SerializeField] private Slider lootingSlider;

    [Header("메시지 UI")]
    [SerializeField] private Text messageText; // "인벤토리 가득 참" 등 메시지 표시
    [SerializeField] private float messageDuration = 2f;

    [Header("인벤토리 UI")]
    [SerializeField] private GameObject inventoryPanel; // 인벤토리 전체 패널
    [SerializeField] private InventoryUI inventoryUI; // InventoryUI 스크립트 참조

    [Header("상호작용 UI")]
    [SerializeField] private GameObject interactionPromptPanel; // "E키 눌러 상호작용" 텍스트 등

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
        messageText?.gameObject.SetActive(false);
        inventoryPanel?.SetActive(false);
        interactionPromptPanel?.SetActive(false);
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
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        messageText.gameObject.SetActive(false);
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
} 