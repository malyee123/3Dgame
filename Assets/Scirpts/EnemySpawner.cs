using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public int baseEnemyCount = 10;
    public float baseSpawnDelay = 1f;
    public float baseEnemyHp = 50f;

    [Header("Round Scaling")]
    public int enemyCountPerRound = 3;
    public float spawnDelayDecrement = 0.1f;
    public float hpIncrement = 20f;

    [Header("Path Settings")]
    public PathManager pathManager;

    private int currentEnemyCount;
    private float currentSpawnDelay;
    private float currentEnemyHp;
    private int spawnCount = 0;
    private Vector2 spawnPosition = new Vector2(-8.5f, 4.5f);
    private Coroutine spawnCoroutine;

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (pathManager == null) { Debug.LogError("[EnemySpawner] PathManager not found."); return; }
        if (enemyPrefab == null) { Debug.LogError("[EnemySpawner] enemyPrefab is missing."); return; }
        ApplyRoundSettings(1);
    }

    public void ApplyRoundSettings(int round)
    {
        currentEnemyCount = baseEnemyCount + (enemyCountPerRound * (round - 1));
        currentSpawnDelay = Mathf.Max(0.2f, baseSpawnDelay - (spawnDelayDecrement * (round - 1)));
        currentEnemyHp = baseEnemyHp + (hpIncrement * (round - 1));
        spawnCount = 0;

        Debug.Log($"[EnemySpawner] Round {round} - Count: {currentEnemyCount} / Delay: {currentSpawnDelay} / HP: {currentEnemyHp}");

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(1f);
        while (spawnCount < currentEnemyCount)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(currentSpawnDelay);
        }
    }

    void SpawnEnemy()
    {
        GameObject obj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) enemyMove.SetPathManager(pathManager);
        else Debug.LogWarning("[EnemySpawner] Spawned enemy is missing EnemyMove component.");

        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null) enemyHealth.maxHp = currentEnemyHp;

        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
        spawnCount++;
    }
}