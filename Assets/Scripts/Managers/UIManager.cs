  using UnityEngine;
using UnityEngine.UI; // Slider, Text 등 사용
using System.Collections; // Coroutine 사용
using TMPro; // TextMeshPro 사용 시 추가
using System.Collections.Generic; // List 사용 위해 추가
using System.Text; // StringBuilder 사용 위해 추가

// UI 요소 관리 및 상호작용 피드백 처리 (씬에 하나 존재)
public class UIManager : MonoBehaviour
{
    [Header("루팅 UI")]
    [SerializeField] private GameObject lootingProgressPanel; // 루팅 진행 슬라이더 포함 패널
    [SerializeField] private Slider lootingSlider;

    [Header("메시지 UI")]
    [SerializeField] private GameObject messagePanel; // 단기 메시지 표시 패널
    [SerializeField] private TextMeshProUGUI messageText; // 단기 메시지 텍스트
    [SerializeField] private float messageDuration = 3f; // 표시 시간 (조금 늘리는 것이 좋을 수 있음)
    [SerializeField] private int maxRecentMessages = 5; // 단기 메시지 패널에 표시할 최대 줄 수
    // --- 메시지 기록 UI 추가 ---
    [SerializeField] private GameObject chatHistoryPanel; // 메시지 기록 패널
    [SerializeField] private TextMeshProUGUI chatHistoryText; // 메시지 기록 텍스트
    [SerializeField] private int maxHistoryLines = 10; // 최대 기록 줄 수
    // --------------------------

    [Header("인벤토리 UI")]
    [SerializeField] private GameObject inventoryPanel; // 인벤토리 전체 패널
    [SerializeField] private InventoryUI inventoryUI; // InventoryUI 스크립트 참조

    [Header("상호작용 UI")]
    [SerializeField] private GameObject interactionPromptPanel; // "E키 눌러 상호작용" 텍스트 등

    // --- 골드 UI 추가 ---
    [Header("플레이어 정보")]
    [SerializeField] private TextMeshProUGUI goldText; // 골드 표시 텍스트 (TMP)
    // [SerializeField] private Text goldText; // 일반 Text 사용 시
    // --- 스태미나 슬라이더 참조 추가 ---
    [SerializeField] private Slider staminaSlider;
    // --------------------------------

    // 싱글톤
    public static UIManager Instance { get; private set; }

    private Coroutine lootingCoroutine = null;
    private Coroutine messageCoroutine = null; // 단기 메시지 타이머 코루틴
    private LootableObject currentLootTarget = null;

    // --- 메시지 기록 관련 변수 추가 ---
    private List<string> messageHistory = new List<string>(); // 전체 기록용
    private List<string> recentMessages = new List<string>(); // 단기 패널 표시용
    private StringBuilder sb = new StringBuilder(); // 기록 패널 업데이트용
    // --------------------------------

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
        messagePanel?.SetActive(false); // 시작 시 단기 메시지 패널 숨김
        inventoryPanel?.SetActive(false);
        interactionPromptPanel?.SetActive(false);
        chatHistoryPanel?.SetActive(false);
        UpdateChatHistoryUI();

        // --- 스태미나 슬라이더 초기화 ---
        staminaSlider?.gameObject.SetActive(true); // 슬라이더 활성화 (필요에 따라 false)
        // 초기 값 설정 (보통 가득 찬 상태로 시작)
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f; // 비율로 표시하므로 0~1 범위 사용
            staminaSlider.value = 1f;    // 가득 찬 상태
        }
        // -----------------------------

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

        // 메시지 기록 패널 토글 키 (예: L 키)
        if (Input.GetKeyDown(KeyCode.L))
        {
             ToggleChatHistoryPanel();
        }

        // --- 엔터 키 입력 처리 수정 ---
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // 채팅 기록 패널이 활성화되어 있는지 확인
            bool isHistoryPanelActive = chatHistoryPanel != null && chatHistoryPanel.activeSelf;

            if (isHistoryPanelActive)
            {
                // 기록 패널이 켜져 있으면 -> 끄기
                chatHistoryPanel.SetActive(false);
                // 단기 메시지 패널은 건드리지 않거나, 필요시 여기서 켤 수 있음
                 messagePanel.SetActive(true); // <- 이 줄은 제거하거나 주석 처리 (요구사항에 따라)
            }
            else
            {
                // 기록 패널이 꺼져 있으면 -> 켜고, 단기 메시지 패널 끄기
                // 1. 단기 메시지 패널 끄기
                if (messagePanel != null)
                {
                    // 진행 중인 메시지 타이머 코루틴 중지
                    if (messageCoroutine != null)
                    {
                        StopCoroutine(messageCoroutine);
                        messageCoroutine = null;
                    }
                    messagePanel.SetActive(false);
                }

                // 2. 채팅 기록 패널 켜기
                if (chatHistoryPanel != null)
                {
                    chatHistoryPanel.SetActive(true);
                    // StartCoroutine(ScrollChatHistoryToBottom());
                }
            }
        }
        // ---------------------------
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
        // 1. 전체 기록에 추가
        AddMessageToHistory(message);

        // 2. 단기 메시지 패널 업데이트 및 표시
        if (messageText != null && messagePanel != null)
        {
            // 최근 메시지 리스트 업데이트
            recentMessages.Add(message);
            // 최대 줄 수 초과 시 가장 오래된 메시지 제거
            while (recentMessages.Count > maxRecentMessages)
            {
                recentMessages.RemoveAt(0);
            }

            // Text 컴포넌트 업데이트 (리스트 내용을 줄바꿈으로 연결)
            messageText.text = string.Join("\n", recentMessages);

            // 기존 타이머 코루틴 중지 및 재시작
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
            }
            // 기록 패널이 꺼져 있을 때만 단기 메시지 패널 표시 (선택 사항)
            if (chatHistoryPanel == null || !chatHistoryPanel.activeSelf)
            {
                 messageCoroutine = StartCoroutine(DisplayRecentMessagesTimer());
            }
        }
    }

    // 단기 메시지 패널 표시 타이머 코루틴 (이름 변경 및 로직 수정)
    private IEnumerator DisplayRecentMessagesTimer()
    {
        messagePanel.SetActive(true); // 패널 켜기
        yield return new WaitForSeconds(messageDuration); // 설정된 시간만큼 대기
        // 코루틴이 정상 종료될 때만 패널 끄기 (엔터키 등으로 중간에 꺼졌을 수 있음)
        if (messageCoroutine != null)
        {
            messagePanel.SetActive(false);
            messageCoroutine = null;
        }
    }

    public void AddMessageToHistory(string message)
    {
        if (messageHistory.Count >= maxHistoryLines)
        {
            messageHistory.RemoveAt(0); // 가장 오래된 메시지 제거
        }
        messageHistory.Add(message);
        UpdateChatHistoryUI(); // 기록 패널 UI 갱신
    }

    private void UpdateChatHistoryUI()
    {
        if (chatHistoryText == null) return;
        sb.Clear();
        foreach (string msg in messageHistory)
        {
            sb.AppendLine(msg); // 한 줄씩 추가
        }
        chatHistoryText.text = sb.ToString();
    }

    public void ToggleChatHistoryPanel()
    {
        if (chatHistoryPanel == null) return;
        chatHistoryPanel.SetActive(!chatHistoryPanel.activeSelf);
        // 선택적: 기록 패널 켤 때 단기 메시지 패널 끄기
        if (chatHistoryPanel.activeSelf && messagePanel != null)
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }
            messagePanel.SetActive(false);
        }
    }

    // 스크롤 뷰 맨 아래로 내리는 코루틴 (ScrollRect 컴포넌트 필요)
    // private ScrollRect chatHistoryScrollRect; // Inspector에서 연결 필요
    // private System.Collections.IEnumerator ScrollChatHistoryToBottom()
    // {
    //     // UI 업데이트 기다리기 (프레임 끝 또는 약간의 시간)
    //     yield return null;
    //     // 또는 yield return new WaitForEndOfFrame();
    //     if (chatHistoryScrollRect != null && chatHistoryPanel.activeSelf) // 패널 활성화 상태일 때만
    //     {
    //         // 레이아웃 업데이트 강제 실행 (필요할 수 있음)
    //         // LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryScrollRect.content);
    //         chatHistoryScrollRect.verticalNormalizedPosition = 0f;
    //     }
    // }

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
    public IEnumerator ShowInteractionPrompt(float duration_InteractionPrompt)
    {
        interactionPromptPanel?.SetActive(true);
        yield return new WaitForSeconds(duration_InteractionPrompt); // 설정된 시간만큼 대기
        // 코루틴이 정상 종료될 때만 패널 끄기 (엔터키 등으로 중간에 꺼졌을 수 있음)
        interactionPromptPanel.SetActive(false);
    }
    

    // --- 스태미나 UI 업데이트 함수 추가 ---
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        if (staminaSlider == null)
        {
            // Debug.LogWarning("Stamina Slider is not assigned in UIManager.");
            return;
        }

        if (maxStamina <= 0) // 0으로 나누기 방지
        {
            staminaSlider.value = 0f;
        }
        else
        {
            // 현재 스태미나 비율 계산 (0과 1 사이)
            float staminaRatio = Mathf.Clamp01(currentStamina / maxStamina);
            staminaSlider.value = staminaRatio;
        }
    }
    // ------------------------------------

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