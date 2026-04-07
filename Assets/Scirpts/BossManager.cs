using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public PathManager pathManager;

    [Header("Spawn Position")]
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
        if (bossPrefab == null) { Debug.LogWarning("[BossManager] bossPrefab is missing."); return; }
        GameObject obj = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = data.speed; }
        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.isSpecial = true;
            enemyHealth.specialCoinReward = data.reward;
            enemyHealth.Init(data.hp);
        }
        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }
}