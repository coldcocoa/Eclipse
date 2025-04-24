using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가

public class KillCount_Dungeon : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("킬 카운트를 표시할 TextMeshPro UI 요소")]
    [SerializeField] private TextMeshProUGUI killCountText_Slime;
    [SerializeField] private TextMeshProUGUI killCountText_Skeleton;
    [SerializeField] private TextMeshProUGUI killCountText_Reaper;
   
    // 각 몬스터 타입별 카운터 변수 추가
    private int slimeKillCount = 0;
    private int skeletonKillCount = 0;
    private int reaperKillCount = 0;

    public void Start()
    {
        killCountText_Slime = GameObject.Find("Notification_Count_Slime").GetComponent<TextMeshProUGUI>();
        killCountText_Skeleton = GameObject.Find("Notification_Count_Skeleton").GetComponent<TextMeshProUGUI>();
        killCountText_Reaper = GameObject.Find("Notification_Count_Reaper").GetComponent<TextMeshProUGUI>();
        
        // 초기값 설정
        UpdateAllUI();
    }
    
    public void UpdateKillCountUI_Slime()
    {
        slimeKillCount++;
        if (killCountText_Slime != null)
            killCountText_Slime.text = slimeKillCount.ToString();
    }

    public void UpdateKillCountUI_Skeleton()
    {
        skeletonKillCount++;
        if (killCountText_Skeleton != null)
            killCountText_Skeleton.text = skeletonKillCount.ToString();
    }
    
    public void UpdateKillCountUI_Reaper()
    {
        reaperKillCount++;
        if (killCountText_Reaper != null)
            killCountText_Reaper.text = reaperKillCount.ToString();
    }
    
    private void UpdateAllUI()
    {
        if (killCountText_Slime != null) killCountText_Slime.text = slimeKillCount.ToString();
        if (killCountText_Skeleton != null) killCountText_Skeleton.text = skeletonKillCount.ToString();
        if (killCountText_Reaper != null) killCountText_Reaper.text = reaperKillCount.ToString();
    }
} 