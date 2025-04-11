using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// 던전 관련 UI만 담당하는 매니저로 이름 변경
public class DungeonUIManager : MonoBehaviour
{
    public static DungeonUIManager Instance { get; private set; }
    
    [Header("던전 UI")]
    [Tooltip("던전 입장 확인 UI 패널")]
    [SerializeField] private GameObject dungeonEntryUI;
    
    [Tooltip("던전 이름 표시 텍스트")]
    [SerializeField] private Text dungeonNameText;
    
    [Tooltip("던전 설명 표시 텍스트")]
    [SerializeField] private Text dungeonDescriptionText;
    
    [Header("난이도 버튼")]
    [Tooltip("쉬움 난이도 버튼")]
    [SerializeField] private Button easyButton;
    
    [Tooltip("보통 난이도 버튼")]
    [SerializeField] private Button normalButton;
    
    [Tooltip("어려움 난이도 버튼")]
    [SerializeField] private Button hardButton;
    
    [Header("알림 UI")]
    [Tooltip("던전 내 알림 표시 패널")]
    [SerializeField] private GameObject notificationPanel;
    
    [Tooltip("알림 메시지 텍스트")]
    [SerializeField] private Text notificationText;
    
    [Header("던전 시작/클리어 UI")]
    [Tooltip("던전 시작 시 표시할 패널")]
    [SerializeField] private GameObject dungeonStartPanel;
    
    [Tooltip("던전 클리어 시 표시할 패널")]
    [SerializeField] private GameObject dungeonClearPanel;
    
    [Tooltip("플레이어 사망 시 표시할 화면")]
    [SerializeField] private GameObject deathScreen;
    
    // 현재 선택된 던전 정보
    private DungeonData currentDungeonData;
    private DungeonDifficulty selectedDifficulty = DungeonDifficulty.Easy; // 기본값은 쉬움
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 던전 입장 UI 표시
    public void ShowDungeonEntryUI(DungeonData data)
    {
        if (dungeonEntryUI == null) {
            Debug.LogError("던전 입장 UI가 설정되지 않았습니다!");
            return;
        }
        
        currentDungeonData = data;
        dungeonEntryUI.SetActive(true);
        
        // UI 요소 설정
        if (dungeonNameText) dungeonNameText.text = data.dungeonName;
        if (dungeonDescriptionText) dungeonDescriptionText.text = data.description;
        
        // 난이도 버튼 이벤트 설정
        SetupDifficultyButtons();
        
        // 플레이어 컨트롤 일시 중지 및 마우스 커서 표시
        SetPlayerControlState(false);
    }
    
    // 난이도 버튼 설정
    private void SetupDifficultyButtons()
    {
        // 쉬움 난이도 버튼
        if (easyButton != null)
        {
            easyButton.onClick.RemoveAllListeners();
            easyButton.onClick.AddListener(() => {
                selectedDifficulty = DungeonDifficulty.Easy;
                HighlightSelectedDifficultyButton();
            });
        }
        
        // 보통 난이도 버튼
        if (normalButton != null)
        {
            normalButton.onClick.RemoveAllListeners();
            normalButton.onClick.AddListener(() => {
                selectedDifficulty = DungeonDifficulty.Normal;
                HighlightSelectedDifficultyButton();
            });
        }
        
        // 어려움 난이도 버튼
        if (hardButton != null)
        {
            hardButton.onClick.RemoveAllListeners();
            hardButton.onClick.AddListener(() => {
                selectedDifficulty = DungeonDifficulty.Hard;
                HighlightSelectedDifficultyButton();
            });
        }
        
        // 입장 버튼 이벤트 설정
        Button enterButton = dungeonEntryUI.GetComponentInChildren<Button>();
        if (enterButton != null && enterButton != easyButton && enterButton != normalButton && enterButton != hardButton)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() => {
                EnterDungeonWithSelectedDifficulty();
            });
        }
        
        // 초기 선택 버튼 하이라이트
        selectedDifficulty = DungeonDifficulty.Easy; // 기본값으로 리셋
        HighlightSelectedDifficultyButton();
    }
    
    // 선택된 난이도 버튼 하이라이트
    private void HighlightSelectedDifficultyButton()
    {
        // 버튼 색상 또는 이미지 변경으로 선택 상태 표시
        ColorBlock colors;
        
        if (easyButton != null)
        {
            colors = easyButton.colors;
            colors.normalColor = (selectedDifficulty == DungeonDifficulty.Easy) 
                ? new Color(0.8f, 0.8f, 1f) : Color.white;
            easyButton.colors = colors;
        }
        
        if (normalButton != null)
        {
            colors = normalButton.colors;
            colors.normalColor = (selectedDifficulty == DungeonDifficulty.Normal)
                ? new Color(0.8f, 0.8f, 1f) : Color.white;
            normalButton.colors = colors;
        }
        
        if (hardButton != null)
        {
            colors = hardButton.colors;
            colors.normalColor = (selectedDifficulty == DungeonDifficulty.Hard)
                ? new Color(0.8f, 0.8f, 1f) : Color.white;
            hardButton.colors = colors;
        }
    }
    
    // 선택된 난이도로 던전 입장
    private void EnterDungeonWithSelectedDifficulty()
    {
        if (currentDungeonData != null)
        {
            // 던전 입장 (선택된 난이도 전달)
            DungeonManager.Instance.EnterDungeon(currentDungeonData.dungeonId, selectedDifficulty);
            CloseDungeonUI();
        }
    }
    
    // 던전 UI 닫기
    public void CloseDungeonUI()
    {
        if (dungeonEntryUI != null)
        {
            dungeonEntryUI.SetActive(false);
            
            // 플레이어 컨트롤 다시 활성화 및 마우스 커서 숨김
            SetPlayerControlState(true);
        }
    }
    
    // 알림 메시지 표시
    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;
        
        notificationText.text = message;
        notificationPanel.SetActive(true);
        
        // 3초 후 자동으로 닫기
        Invoke("HideNotification", 3f);
    }
    
    // 알림 패널 숨기기
    private void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    // 던전 시작 메시지
    public void ShowDungeonStartMessage(string dungeonName, DungeonDifficulty difficulty)
    {
        if (dungeonStartPanel == null) return;
        
        // 던전 시작 패널 설정 및 표시
        Text titleText = dungeonStartPanel.GetComponentInChildren<Text>();
        if (titleText != null)
        {
            string difficultyText = "";
            switch(difficulty) {
                case DungeonDifficulty.Easy: difficultyText = "(쉬움)"; break;
                case DungeonDifficulty.Normal: difficultyText = "(보통)"; break;
                case DungeonDifficulty.Hard: difficultyText = "(어려움)"; break;
            }
            titleText.text = $"{dungeonName} {difficultyText}";
        }
        
        dungeonStartPanel.SetActive(true);
        
        // 3초 후 자동으로 닫기
        Invoke("HideDungeonStartMessage", 3f);
    }
    
    private void HideDungeonStartMessage()
    {
        if (dungeonStartPanel != null)
        {
            dungeonStartPanel.SetActive(false);
        }
    }
    
    // 던전 클리어 UI
    public void ShowDungeonClearUI(float clearTime, List<ItemData> items, int goldAmount)
    {
        if (dungeonClearPanel == null) return;
        
        // 클리어 시간 형식 지정 (분:초)
        int minutes = Mathf.FloorToInt(clearTime / 60);
        int seconds = Mathf.FloorToInt(clearTime % 60);
        string timeText = string.Format("{0:00}:{1:00}", minutes, seconds);
        
        // 클리어 패널의 텍스트 요소들 (각 요소는 필요에 따라 찾아서 설정)
        Text[] texts = dungeonClearPanel.GetComponentsInChildren<Text>();
        foreach (Text text in texts)
        {
            if (text.name == "ClearTimeText")
                text.text = $"클리어 시간: {timeText}";
            else if (text.name == "ItemCountText")
                text.text = $"획득한 아이템: {items.Count}개";
            else if (text.name == "GoldText")
                text.text = $"획득한 골드: {goldAmount}G";
        }
        
        // 클리어 UI 표시
        dungeonClearPanel.SetActive(true);
        
        // 종료 버튼 이벤트 설정
        Button exitButton = dungeonClearPanel.GetComponentInChildren<Button>();
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => {
                dungeonClearPanel.SetActive(false);
                DungeonManager.Instance.ExitDungeon();
            });
        }
    }
    
    // 사망 화면 표시
    public void ShowDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }
    }
    
    // 사망 화면 숨기기
    public void HideDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
    }
    
    // 플레이어 컨트롤 상태 설정 메서드 추가
    private void SetPlayerControlState(bool enabled)
    {
        // 마우스 커서 상태 설정
        Cursor.visible = !enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        
        // PlayerManager를 통해 플레이어 컨트롤 설정 (옵션 1)
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.SetPlayerControlEnabled(enabled);
        }
        
        // 직접 플레이어 컨트롤러 참조 (옵션 2 - 필요시 사용)
        // IntegratedPlayerController playerController = FindObjectOfType<IntegratedPlayerController>();
        // if (playerController != null)
        // {
        //     playerController.enabled = enabled;
        // }
        
        // 카메라 컨트롤러 참조 (필요시 추가)
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = enabled;
        }
    }

    // Update 메서드 추가 - ESC 키로 UI 닫기
    private void Update()
    {
        // 던전 입장 패널이 열려있고 ESC 키가 눌리면 닫기
        if (dungeonEntryUI != null && dungeonEntryUI.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDungeonUI();
        }
    }
} 