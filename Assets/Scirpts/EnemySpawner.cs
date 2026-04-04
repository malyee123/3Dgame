using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;

    [Header("Path Settings")]
    public PathManager pathManager;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2 spawnPosition = new Vector2(-6f, 3f);

    private float currentSpawnDelay;
    private float currentEnemyHp;
    private float currentEnemySpeed;
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
        RoundData data = CSVLoader.Instance != null ? CSVLoader.Instance.GetRoundData(round) : null;
        if (data != null)
        {
            // 구간 내 라운드 오프셋 계산 (waveStart부터 몇 번째 웨이브인지)
            int offsetInRange = round - data.waveStart;
            currentEnemyHp = data.baseHp + data.hpIncrement * offsetInRange;
            currentSpawnDelay = Mathf.Max(0.1f, data.spawnDelay - data.spawnDelayDecrement * offsetInRange);
            currentEnemySpeed = data.enemySpeed;
        }
        else
        {
            currentEnemyHp = 50f;
            currentSpawnDelay = 1f;
            currentEnemySpeed = 2f;
        }
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true) { SpawnEnemy(); yield return new WaitForSeconds(currentSpawnDelay); }
    }

    void SpawnEnemy()
    {
        Vector2 offsetPos = spawnPosition;
        offsetPos.x += Random.Range(-0.3f, 0.3f);
        GameObject obj = Instantiate(enemyPrefab, offsetPos, Quaternion.identity);
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = currentEnemySpeed; }
        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null) enemyHealth.Init(currentEnemyHp);
        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }
}