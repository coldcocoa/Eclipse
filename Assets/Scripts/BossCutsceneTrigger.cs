using UnityEngine;
using UnityEngine.Playables;

public class BossCutsceneTrigger : MonoBehaviour
{
    [SerializeField] private PlayableDirector timeline;
    private bool playerInRange = false;

    private void OnEnable()
    {
        if (timeline != null)
            timeline.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (timeline != null)
            timeline.stopped -= OnTimelineStopped;
    }

    private void OnTimelineStopped(PlayableDirector dir)
    {
        // 플레이어 조작 다시 활성화
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SetPlayerControlEnabled(true);

        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            // 타임라인 시작 시 플레이어 조작 비활성화
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.SetPlayerControlEnabled(false);

            timeline.Play();
            playerInRange = false; // 중복 재생 방지
        }
    }
} 