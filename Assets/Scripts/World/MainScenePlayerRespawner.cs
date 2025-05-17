using UnityEngine;

public class MainScenePlayerRespawner : MonoBehaviour
{
    [Header("리스폰 위치")]
    public Transform respawnPoint; // Inspector에서 위치 지정

    private void Start()
    {
        GameObject player = GameObject.Find("Skeleton_Player");
        if (player != null && respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;
            player.transform.rotation = respawnPoint.rotation;
        }
        else
        {
            Debug.LogWarning("Skeleton_Player 또는 respawnPoint를 찾을 수 없습니다.");
        }
    }
} 