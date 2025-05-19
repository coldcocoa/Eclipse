using UnityEngine;

public class MainScenePlayerRespawner : MonoBehaviour
{
    [Header("리스폰 위치")]
    public Transform respawnPoint; // Inspector에서 위치 지정

    public void Point_Player()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null && respawnPoint != null)
        {
            // CharacterController가 있다면 잠시 비활성화
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;

            if (cc != null) cc.enabled = true;

            Debug.Log("[Respawner] 플레이어 위치 이동 완료");
        }
        else
        {
            Debug.LogWarning("Player 또는 respawnPoint를 찾을 수 없습니다.");
        }
    }
} 