using UnityEngine;

// 간단한 플레이어 골드 관리 스크립트 (플레이어 또는 GameManager에 부착)
public class PlayerWallet : MonoBehaviour
{
    public int currentGold { get; private set; } = 0;

    // 싱글톤 또는 다른 방식으로 접근 가능하게 만들 수 있음
    public static PlayerWallet Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬 전환 시 유지 필요하면 사용
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 시작 시 UI 업데이트 추가 (선택 사항) ---
    // UIManager보다 먼저 Awake가 실행될 수 있으므로 Start에서 호출하는 것이 더 안전할 수 있음
    // void Start()
    // {
    //     // UIManager가 준비되었는지 확인 후 초기 UI 업데이트 요청
    //     if (UIManager.Instance != null)
    //     {
    //         UIManager.Instance.UpdateGoldUI(currentGold);
    //     }
    // }
    // ------------------------------------------

    public void AddGold(int amount)
    {
        if (amount > 0)
        {
            currentGold += amount;
            Debug.Log($"골드 획득: +{amount} / 현재 골드: {currentGold}");
            // --- UI 업데이트 호출 추가 ---
            UIManager.Instance?.UpdateGoldUI(currentGold);
            UIManager.Instance?.ShowMessage($"{amount} 골드 획득");
            // --------------------------
        }
    }

    public bool UseGold(int amount)
    {
        if (amount > 0 && currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log($"골드 사용: -{amount} / 현재 골드: {currentGold}");
            // --- UI 업데이트 호출 추가 ---
            UIManager.Instance?.UpdateGoldUI(currentGold);
            // --------------------------
            return true;
        }
        return false;
    }
} 