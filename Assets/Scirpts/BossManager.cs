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

    // boss.csv: bossWave, bossId, hp, speed, reward, defense
    // bossId로 bossPrefabs 배열에서 프리팹 선택
    // 웨이브마다 다른 외형 + 다른 능력치의 보스 소환 가능

    void SpawnBoss(BossData data)
    {
        // bossId로 3가지 보스 중 하나 선택
        if (bossPrefabs == null || data.bossId >= bossPrefabs.Length) return;
        GameObject prefab = bossPrefabs[data.bossId];
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // CSV에서 읽어온 이동속도 적용
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = data.speed; }

        // CSV에서 읽어온 체력/방어력/보상 적용
        // isSpecial = true → 처치 시 일반 코인 대신 특수 코인 지급
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