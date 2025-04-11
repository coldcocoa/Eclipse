using UnityEngine;
using UnityEngine.UI;

public class DungeonEntrance : MonoBehaviour
{
    [Tooltip("던전 구분을 위한 고유 ID - DungeonData의 dungeonId와 일치해야 함")]
    [SerializeField] private string dungeonId;
    
    [Tooltip("플레이어와 상호작용 가능한 거리 (미터)")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Tooltip("플레이어가 가까이 왔을 때 표시할 UI 요소")]
    [SerializeField] private GameObject interactionPrompt;
    
    [Tooltip("상호작용 안내 텍스트")]
    [SerializeField] private Text promptText;
    
    private bool playerInRange = false;
    
    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        if (promptText != null)
        {
            promptText.text = "E키를 눌러 던전 정보 보기";
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
        if (other.CompareTag("Player"))
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
            
            // 열려있는 던전 UI가 있다면 닫기
            DungeonUIManager.Instance.CloseDungeonUI();
        }
    }
    
    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ShowDungeonUI();
        }
    }
    
    private void ShowDungeonUI()
    {
        DungeonData data = DungeonManager.Instance.GetDungeonData(dungeonId);
        if (data != null)
        {
            DungeonUIManager.Instance.ShowDungeonEntryUI(data);
        }
        else
        {
            Debug.LogError($"던전 ID {dungeonId}에 해당하는 데이터를 찾을 수 없습니다.");
        }
    }
} 