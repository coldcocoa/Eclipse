using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PortalActivator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject promptUI; // "F키를 눌러 입장" 등

    private bool playerInRange = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않게 설정
    }

    private void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptUI != null)
                promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptUI != null)
                promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            // 씬 로드 후 콜백 등록
            SceneManager.sceneLoaded += OnMainSceneLoaded;
            SceneManager.LoadScene("Loading_Main"); // 메인씬 이름에 맞게 수정
        }
    }

    private void OnMainSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Test1") // 메인 씬 이름으로 체크
        {
            MainScenePlayerRespawner respawner = FindObjectOfType<MainScenePlayerRespawner>();
            if (respawner != null)
            {
                respawner.Point_Player();
            }
        }
        SceneManager.sceneLoaded -= OnMainSceneLoaded;
    }
} 