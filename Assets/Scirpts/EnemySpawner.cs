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
    private float currentEnemySpeed = 2f;
    private Coroutine spawnCoroutine;

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (pathManager == null) { Debug.LogError("[EnemySpawner] PathManager not found."); return; }
        if (enemyPrefab == null) { Debug.LogError("[EnemySpawner] enemyPrefab is missing."); return; }
        if (CSVLoader.Instance != null)
        {
            baseEnemyHp = CSVLoader.Instance.baseEnemyHp;
            hpIncrement = CSVLoader.Instance.hpIncrement;
            baseSpawnDelay = CSVLoader.Instance.baseSpawnDelay;
            spawnDelayDecrement = CSVLoader.Instance.spawnDelayDecrement;
        }
        ApplyRoundSettings(1);
    }

    public void ApplyRoundSettings(int round)
    {
        if (CSVLoader.Instance != null)
        {
            RoundData data = CSVLoader.Instance.GetRoundData(round);
            if (data != null)
            {
                currentEnemyHp = data.enemyHp;
                currentSpawnDelay = data.enemySpawnDelay;
                currentEnemySpeed = data.enemySpeed;
            }
            else
            {
                currentSpawnDelay = Mathf.Max(0.2f, baseSpawnDelay - (spawnDelayDecrement * (round - 1)));
                currentEnemyHp = baseEnemyHp + (hpIncrement * (round - 1));
            }
        }
        else
        {
            currentSpawnDelay = Mathf.Max(0.2f, baseSpawnDelay - (spawnDelayDecrement * (round - 1)));
            currentEnemyHp = baseEnemyHp + (hpIncrement * (round - 1));
        }
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