#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpawnPoint))]
public class SpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 그리기
        DrawDefaultInspector();
        
        SpawnPoint spawnPoint = (SpawnPoint)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("디버깅 도구", EditorStyles.boldLabel);
        
        // 테스트 버튼
        if (GUILayout.Button("테스트 몬스터 스폰 (에디터)"))
        {
            // 에디터 모드에서 실행되는 함수 호출
            if (Application.isPlaying)
            {
                // spawnPoint.SpawnMonster(); // SpawnMonster가 public이어야 함
                Debug.Log("테스트 몬스터 스폰 요청됨");
            }
            else
            {
                Debug.LogWarning("플레이 모드에서만 테스트 가능합니다!");
            }
        }
    }
}

// 메뉴 아이템 추가
public class SpawnPointTool
{
    [MenuItem("Tools/Monster System/Create Spawn Point")]
    public static void CreateSpawnPoint()
    {
        // 새 게임 오브젝트 생성
        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.AddComponent<SpawnPoint>();
        
        // 씬 뷰 위치에 생성
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            spawnPoint.transform.position = sceneView.camera.transform.position + 
                                           sceneView.camera.transform.forward * 5f;
        }
        
        // 생성된 오브젝트 선택
        Selection.activeGameObject = spawnPoint;
        
        Debug.Log("새 스폰 포인트 생성됨");
    }
}
#endif 