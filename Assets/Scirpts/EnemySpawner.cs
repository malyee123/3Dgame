using System.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public float baseSpawnDelay = 1f;
    public float baseEnemyHp = 50f;

    [Header("Round Scaling")]
    public float spawnDelayDecrement = 0.1f;
    public float hpIncrement = 20f;

    [Header("Path Settings")]
    public PathManager pathManager;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2 spawnPosition = new Vector2(-6f, 3f);

    private float currentSpawnDelay;
    private float currentEnemyHp;
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
        currentSpawnDelay = Mathf.Max(0.2f, baseSpawnDelay - (spawnDelayDecrement * (round - 1)));
        currentEnemyHp = baseEnemyHp + (hpIncrement * (round - 1));
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
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
        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null) enemyHealth.maxHp = currentEnemyHp;
        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }
}