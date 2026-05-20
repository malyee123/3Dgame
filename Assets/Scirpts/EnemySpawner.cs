using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;

    [Header("Path Settings")]
    public PathManager pathManager;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2 spawnPosition = new Vector2(-6f, 3f);

    private float currentSpawnDelay;
    private float currentEnemyHp;
    private float currentEnemySpeed;
    private float currentEnemyDefense = 0f;
    private Coroutine spawnCoroutine;
    private bool isPaused = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (pathManager == null) return;
        if (enemyPrefab == null) return;
    }

    public void SetPaused(bool paused) => isPaused = paused;

    public void ApplyRoundSettings(int round)
    {
        int stage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        RoundData data = CSVLoader.Instance != null ? CSVLoader.Instance.GetRoundData(round, stage) : null;
        if (data != null)
        {
            int offsetInRange = round - data.waveStart;
            currentEnemyHp      = data.baseHp + data.hpIncrement * offsetInRange;
            currentSpawnDelay   = Mathf.Max(0.1f, data.spawnDelay - data.spawnDelayDecrement * offsetInRange);
            currentEnemySpeed   = data.enemySpeed;
            currentEnemyDefense = data.enemyDefense;
            if (CoinManager.Instance != null) CoinManager.Instance.coinsPerKill = data.coinsPerKill;
        }
        else
        {
            currentEnemyHp      = 50f;
            currentSpawnDelay   = 1f;
            currentEnemySpeed   = 2f;
            currentEnemyDefense = 0f;
        }
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(currentSpawnDelay);
        while (true)
        {
            if (!isPaused) SpawnEnemy();
            yield return new WaitForSeconds(currentSpawnDelay);
        }
    }

    void SpawnEnemy()
    {
        Vector2 offsetPos = spawnPosition;
        offsetPos.x += Random.Range(-0.3f, 0.3f);
        GameObject obj = Instantiate(enemyPrefab, offsetPos, Quaternion.identity);

        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null)
        {
            enemyMove.SetPathManager(pathManager);
            enemyMove.speed = currentEnemySpeed;
            float speedDown = PassiveManager.Instance != null ? PassiveManager.Instance.GetTotalEnemySpeedDown() : 0f;
            enemyMove.ApplySpeedPenalty(speedDown);
        }

        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.Init(currentEnemyHp, currentEnemyDefense);
            float passiveDefenseDown  = PassiveManager.Instance  != null ? PassiveManager.Instance.GetTotalEnemyDefenseDown()  : 0f;
            float anvilDefenseDown   = AnvilManager.Instance   != null ? AnvilManager.Instance.BonusDefenseDown              : 0f;
            float augmentDefenseDown = AugmentManager.Instance != null ? AugmentManager.Instance.BonusDefenseDown             : 0f;
            enemyHealth.ApplyDefenseDownPercent(passiveDefenseDown + anvilDefenseDown + augmentDefenseDown);
        }

        GameManager.Instance?.OnEnemySpawned();
    }
}
