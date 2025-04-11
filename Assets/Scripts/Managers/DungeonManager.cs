using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }
    
    [Tooltip("게임에서 사용 가능한 모든 던전 데이터 목록")]
    [SerializeField] private List<DungeonData> availableDungeons;
    
    // Dictionary는 인스펙터에 표시되지 않으므로 주석 생략
    private Dictionary<string, DungeonData> dungeonLookup = new Dictionary<string, DungeonData>();
    
    // 현재 활성 던전
    private DungeonData currentDungeon;
    
    // 던전 상태 추적 변수들
    private Vector3 entrancePosition; // 입장 위치
    private Vector3 respawnPosition;  // 리스폰 위치
    private float dungeonStartTime;   // 던전 시작 시간
    
    // 리스폰 지점 목록
    private Dictionary<string, Vector3> respawnPoints = new Dictionary<string, Vector3>();
    
    [Tooltip("던전에서 획득한 아이템 목록 (자동 추적)")]
    private List<ItemData> collectedItems = new List<ItemData>();
    
    [Tooltip("던전에서 획득한 골드 (자동 추적)")]
    private int collectedGold = 0;
    
    [Tooltip("씬 전환 시 사용할 로딩 화면")]
    [SerializeField] private LoadingScreen loadingScreen;
    
    // 필드 추가
    private DungeonDifficulty currentDifficulty = DungeonDifficulty.Easy;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 던전 데이터 캐싱
            foreach (DungeonData dungeon in availableDungeons)
            {
                dungeonLookup[dungeon.dungeonId] = dungeon;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 던전 데이터 조회
    public DungeonData GetDungeonData(string dungeonId)
    {
        if (dungeonLookup.TryGetValue(dungeonId, out DungeonData data))
            return data;
            
        Debug.LogWarning($"던전 ID '{dungeonId}' 데이터를 찾을 수 없습니다.");
        return null;
    }
    
    // 던전 입장 요청
    public void EnterDungeon(string dungeonId, DungeonDifficulty difficulty)
    {
        DungeonData data = GetDungeonData(dungeonId);
        if (data == null) return;
        
        // 현재 위치 저장
        entrancePosition = PlayerManager.Instance.GetPlayerPosition();
        
        // 시작 시간 기록
        dungeonStartTime = Time.time;
        
        // 던전 데이터 저장
        currentDungeon = data;
        
        // 선택한 난이도 저장
        currentDifficulty = difficulty;
        
        // 플레이어 상태 초기화 (체력, 스태미나 100%)
        PlayerManager.Instance.ResetPlayerStatus();
        
        // 던전 씬 로드
        StartCoroutine(LoadDungeonScene(data.sceneName));
    }
    
    // 비동기 씬 로드
    private IEnumerator LoadDungeonScene(string sceneName)
    {
        // 로딩 UI 표시
        loadingScreen.Show(currentDungeon.dungeonName);
        
        // 수집 아이템 초기화
        collectedItems.Clear();
        collectedGold = 0;
        
        // 씬 비동기 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        float timeElapsed = 0;
        while (asyncLoad.progress < 0.9f)
        {
            timeElapsed += Time.deltaTime;
            // 로딩 진행률 업데이트 (실제 진행률과 시각적 효과 혼합)
            float visualProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingScreen.UpdateProgress(visualProgress);
            yield return null;
        }
        
        // 로딩이 너무 빨리 끝나는 경우 최소 1초는 로딩 화면 표시
        if (timeElapsed < 1f)
        {
            yield return new WaitForSeconds(1f - timeElapsed);
        }
        
        // 로딩 완료
        loadingScreen.UpdateProgress(1.0f);
        yield return new WaitForSeconds(0.5f);
        
        // 씬 활성화
        asyncLoad.allowSceneActivation = true;
        
        // 씬 전환 완료 대기
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 던전 초기화
        InitializeDungeon();
        
        // 로딩 UI 숨기기
        loadingScreen.Hide();
    }
    
    // 던전 초기화
    private void InitializeDungeon()
    {
        // 초기 리스폰 지점 설정
        GameObject startPoint = GameObject.FindGameObjectWithTag("DungeonStart");
        if (startPoint != null)
        {
            respawnPosition = startPoint.transform.position;
        }
        respawnPoints.Clear();
        
        // 던전 시작 메시지 - 선택된 난이도 전달
        DungeonUIManager.Instance.ShowDungeonStartMessage(currentDungeon.dungeonName, currentDifficulty);
    }
    
    // 리스폰 지점 설정
    public void SetRespawnPoint(string pointId, Vector3 position)
    {
        respawnPosition = position;
        respawnPoints[pointId] = position;
    }
    
    // 플레이어 사망 처리
    public void OnPlayerDeath()
    {
        // 리스폰
        StartCoroutine(RespawnPlayer());
    }
    
    // 플레이어 리스폰
    private IEnumerator RespawnPlayer()
    {
        // 사망 UI 표시
        DungeonUIManager.Instance.ShowDeathScreen();
        
        yield return new WaitForSeconds(2.0f);
        
        // 플레이어 리스폰
        PlayerManager.Instance.RespawnPlayer(respawnPosition);
        
        // 사망 UI 숨기기
        DungeonUIManager.Instance.HideDeathScreen();
    }
    
    // 아이템 획득 기록
    public void RecordItemCollected(ItemData item)
    {
        collectedItems.Add(item);
    }
    
    // 골드 획득 기록
    public void RecordGoldCollected(int amount)
    {
        collectedGold += amount;
    }
    
    // 던전 클리어
    public void CompleteDungeon()
    {
        // 클리어 시간 계산
        float clearTime = Time.time - dungeonStartTime;
        
        // 클리어 UI 표시 (클리어 시간, 획득 아이템, 골드)
        DungeonUIManager.Instance.ShowDungeonClearUI(clearTime, collectedItems, collectedGold);
        
        // 보상 지급
        GiveRewards();
    }
    
    // 던전 퇴장
    public void ExitDungeon()
    {
        // 오픈 월드로 이동
        StartCoroutine(ReturnToOpenWorld());
    }
    
    // 오픈 월드로 복귀
    private IEnumerator ReturnToOpenWorld()
    {
        // 로딩 UI 표시
        loadingScreen.Show("오픈 월드로 복귀");
        
        // 메인 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("OpenWorld");
        asyncLoad.allowSceneActivation = false;
        
        while (asyncLoad.progress < 0.9f)
        {
            loadingScreen.UpdateProgress(asyncLoad.progress / 0.9f);
            yield return null;
        }
        
        // 로딩 완료
        loadingScreen.UpdateProgress(1.0f);
        yield return new WaitForSeconds(0.5f);
        
        asyncLoad.allowSceneActivation = true;
        
        // 씬 전환 완료 대기
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 플레이어 위치 복원
        PlayerManager.Instance.TeleportPlayer(entrancePosition);
        
        // 로딩 UI 숨기기
        loadingScreen.Hide();
    }
    
    // 보상 지급
    private void GiveRewards()
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            // 보장된 보상 지급
            foreach (ItemData item in currentDungeon.guaranteedRewards)
            {
                inventory.AddItem(item, 1);
            }
            
            // 확률적 보상 지급
            foreach (ItemDropChance itemChance in currentDungeon.possibleRewards)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= itemChance.dropChance)
                {
                    inventory.AddItem(itemChance.item, 1);
                }
            }
        }
    }
} 