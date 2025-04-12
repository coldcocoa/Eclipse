using UnityEngine;

public class OpenWorldInitializer : MonoBehaviour
{
    private void Start()
    {
        // 플레이어 위치 복원 (던전에서 돌아왔을 경우)
        if (LoadingManager.sceneToLoad == "OpenWorld" && LoadingManager.dungeonId != null)
        {
            // 던전 입구 위치로 플레이어 이동
            PlayerManager.Instance.TeleportPlayer(DungeonManager.Instance.GetEntrancePosition());
            
            // 로딩 정보 초기화
            LoadingManager.dungeonId = null;
        }
    }
} 