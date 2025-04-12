using UnityEngine;

public class DungeonInitializer : MonoBehaviour
{
    private void Start()
    {
        // 시작 위치 찾기
        GameObject startPoint = GameObject.FindGameObjectWithTag("DungeonStart");
        if (startPoint != null && DungeonManager.Instance != null)
        {
            DungeonManager.Instance.SetRespawnPoint("start", startPoint.transform.position);
            Debug.Log("던전 시작 위치 설정: " + startPoint.transform.position);
            
            // 플레이어를 시작 위치로 즉시 이동
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.TeleportPlayer(startPoint.transform.position);
                Debug.Log("플레이어를 시작 위치로 이동시킴");
            }
            else
            {
                Debug.LogError("PlayerManager를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("DungeonStart 태그가 있는 오브젝트를 찾을 수 없거나 DungeonManager가 없습니다!");
        }
        
        // 던전 시작 메시지 표시
        if (DungeonUIManager.Instance != null && !string.IsNullOrEmpty(LoadingManager.dungeonName))
        {
            DungeonUIManager.Instance.ShowDungeonStartMessage(
                LoadingManager.dungeonName, 
                LoadingManager.difficulty
            );
        }
    }
} 