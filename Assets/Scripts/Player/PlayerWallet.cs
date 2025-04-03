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

    public void AddGold(int amount)
    {
        if (amount > 0)
        {
            currentGold += amount;
            Debug.Log($"골드 획득: +{amount} / 현재 골드: {currentGold}");
            // 여기에 골드 UI 업데이트 로직 호출 추가
            // UIManager.Instance.UpdateGoldUI(currentGold);
        }
    }

    public bool UseGold(int amount)
    {
        if (amount > 0 && currentGold >= amount)
        {
            currentGold -= amount;
            Debug.Log($"골드 사용: -{amount} / 현재 골드: {currentGold}");
            // 여기에 골드 UI 업데이트 로직 호출 추가
            // UIManager.Instance.UpdateGoldUI(currentGold);
            return true;
        }
        return false;
    }
} 