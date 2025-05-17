using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PortalActivator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject promptUI; // "F키를 눌러 입장" 등

    private bool playerInRange = false;

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
            SceneManager.LoadScene("Loading_Main");
        }
    }
} 