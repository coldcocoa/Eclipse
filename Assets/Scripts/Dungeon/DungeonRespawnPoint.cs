using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DungeonRespawnPoint : MonoBehaviour
{
    [Tooltip("리스폰 지점 고유 ID (던전 내에서 구분용)")]
    [SerializeField] private string respawnId;
    
    [Tooltip("플레이어와 상호작용 가능한 거리 (미터)")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Tooltip("플레이어가 가까이 왔을 때 표시할 UI 요소")]
    [SerializeField] private GameObject interactionPrompt;
    
    [Tooltip("상호작용 안내 텍스트")]
    [SerializeField] private Text promptText;
    
    [Tooltip("체크포인트 활성화 시 보여줄 시각 효과")]
    [SerializeField] private GameObject checkpointEffect;
    
    [Tooltip("체크포인트 활성화 시 재생할 효과음")]
    [SerializeField] private AudioClip activationSound;
    
    [Tooltip("이미 활성화된 체크포인트인지 여부")]
    private bool isActivated = false;
    
    private bool playerInRange = false;
    
    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        if (promptText != null)
        {
            promptText.text = "F키를 눌러 리스폰 지점 설정";
        }
        
        if (checkpointEffect != null)
        {
            checkpointEffect.SetActive(false);
        }
        
        // 상호작용 콜라이더 설정
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
        }
        
        trigger.radius = interactionDistance;
        trigger.isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            playerInRange = true;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
    
    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F) && !isActivated)
        {
            ActivateCheckpoint();
        }
    }
    
    private void ActivateCheckpoint()
    {
        isActivated = true;
        
        // 리스폰 포인트 설정
        DungeonManager.Instance.SetRespawnPoint(respawnId, transform.position);
        
        // UI 프롬프트 숨기기
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // 시각 효과
        if (checkpointEffect != null)
        {
            checkpointEffect.SetActive(true);
            
            // DOTween 효과 (크기 증가 후 감소)
            Transform effectTransform = checkpointEffect.transform;
            effectTransform.localScale = Vector3.zero;
            effectTransform.DOScale(Vector3.one * 1.5f, 0.5f).SetEase(Ease.OutBack).OnComplete(() => {
                effectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutSine);
            });
        }
        
        // 소리 효과
        if (activationSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.clip = activationSound;
            audioSource.Play();
        }
        
        // 알림 메시지
        DungeonUIManager.Instance.ShowNotification("리스폰 지점이 설정되었습니다.");
    }
} 