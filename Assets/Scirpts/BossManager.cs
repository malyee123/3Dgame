using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Prefabs")]
    public GameObject[] bossPrefabs;

    [Header("Settings")]
    public PathManager pathManager;
    public Vector2 spawnPosition = new Vector2(-6f, 3f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
    }

    public void TrySpawnBoss(int wave)
    {
        if (CSVLoader.Instance == null) return;
        BossData data = CSVLoader.Instance.GetBossData(wave);
        if (data == null) return;
        SpawnBoss(data);
    }

    void SpawnBoss(BossData data)
    {
        if (bossPrefabs == null || data.bossId >= bossPrefabs.Length)
        {
            Debug.LogWarning("[BossManager] bossPrefab not found for bossId: " + data.bossId);
            return;
        }
        GameObject prefab = bossPrefabs[data.bossId];
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = data.speed; }
        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.isSpecial = true;
            enemyHealth.specialCoinReward = data.reward;
            enemyHealth.Init(data.hp, data.defense);
        }
        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }
}