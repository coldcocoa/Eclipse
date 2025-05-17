using UnityEngine;
using UnityEngine.SceneManagement;
public class Go_Main : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoMain()
    {
        SceneManager.LoadScene("Loading_Main");
    }
}
