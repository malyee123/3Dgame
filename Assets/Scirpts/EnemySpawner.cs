using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy 프리팹을 일정 간격으로 생성하는 스크립트
/// 생성된 Enemy에게 PathManager를 직접 전달해서 이동 경로를 알려줌
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy 설정")]
    // 생성할 Enemy 프리팹 (Inspector에서 드래그해서 연결)
    public GameObject enemyPrefab;

    // 생성할 Enemy 총 개수
    public int enemyCount = 10;

    // Enemy 생성 간격 (초 단위, 예: 1이면 1초마다 생성)
    public float spawnDelay = 1f;


    [Header("경로 설정")]
    // EnemyMove에게 전달할 PathManager (Inspector에서 직접 연결 권장)
    // FindObjectOfType보다 직접 연결이 안전하고 성능도 좋음
    public PathManager pathManager;


    // 지금까지 생성된 Enemy 수 (내부에서만 사용)
    private int spawnCount = 0;

    // Enemy가 생성될 위치 (X=-7, Y=0 = 화면 왼쪽 끝)
    private Vector2 spawnPosition = new Vector2(-8.5f, 4.5f);


    void Start()
    {
        // Inspector에서 PathManager를 연결하지 않았으면 자동으로 찾아봄
        if (pathManager == null)
        {
            pathManager = FindFirstObjectByType<PathManager>();
        }

        // PathManager가 없으면 Enemy 생성 시작하지 않음
        if (pathManager == null)
        {
            Debug.LogError("[EnemySpawner] PathManager를 찾을 수 없습니다! Inspector에서 연결해주세요.");
            return;
        }

        // 프리팹이 연결되지 않았으면 오류 출력 후 종료
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab이 없습니다! Inspector에서 프리팹을 연결해주세요.");
            return;
        }

        // ── InvokeRepeating 대신 코루틴 사용 ──
        // InvokeRepeating은 함수 이름을 문자열로 넘기기 때문에
        // 오타가 있어도 컴파일 오류가 안 나고 런타임에서만 발견됨
        // 코루틴은 오타 시 컴파일 오류가 바로 나서 훨씬 안전함
        StartCoroutine(SpawnRoutine());
    }


    // Enemy를 일정 간격으로 생성하는 코루틴
    IEnumerator SpawnRoutine()
    {
        // 게임 시작 후 1초 뒤에 첫 Enemy 생성 (준비 시간)
        yield return new WaitForSeconds(1f);

        // enemyCount 개수만큼 생성될 때까지 반복
        while (spawnCount < enemyCount)
        {
            SpawnEnemy();
            // spawnDelay 초 기다린 후 다음 Enemy 생성
            yield return new WaitForSeconds(spawnDelay);
        }
    }


    // Enemy를 실제로 생성하고 PathManager를 전달하는 함수
    void SpawnEnemy()
    {
        // spawnPosition 위치에 Enemy 프리팹 생성
        // Quaternion.identity = 회전 없음 (기본 방향)
        GameObject obj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // 생성된 Enemy에서 EnemyMove 컴포넌트를 가져옴
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();

        if (enemyMove != null)
        {
            // PathManager를 직접 전달
            // FindObjectOfType 없이 바로 연결되므로 더 안전하고 성능도 좋음
            enemyMove.SetPathManager(pathManager);
        }
        else
        {
            // 프리팹에 EnemyMove 스크립트가 없으면 경고 출력
            Debug.LogWarning("[EnemySpawner] 생성된 Enemy에 EnemyMove 컴포넌트가 없습니다!");
        }

        // 생성된 Enemy 수 증가
        spawnCount++;
    }
}