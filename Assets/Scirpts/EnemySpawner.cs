using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public int enemyCount = 10;
    public float spawnDelay = 1f;

    [Header("Path Settings")]
    public PathManager pathManager;

    private int spawnCount = 0;
    private Vector2 spawnPosition = new Vector2(-8.5f, 4.5f);

    void Start()
    {
        if (pathManager == null)
        {
            pathManager = FindFirstObjectByType<PathManager>();
        }

        if (pathManager == null)
        {
            Debug.LogError("[EnemySpawner] PathManager not found. Assign it in Inspector.");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyPrefab is missing. Assign a prefab in Inspector.");
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(1f);

        while (spawnCount < enemyCount)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnEnemy()
    {
        GameObject obj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();

        if (enemyMove != null)
        {
            enemyMove.SetPathManager(pathManager);
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Spawned enemy is missing EnemyMove component.");
        }

        spawnCount++;
    }
}
