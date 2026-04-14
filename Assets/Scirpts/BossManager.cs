using System.Collections;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss Prefabs")]
    public GameObject[] bossPrefabs;

    [Header("Settings")]
    public PathManager pathManager;
    public Vector2 spawnPosition = new Vector2(-6f, 3f);

    [Header("Warning UI")]
    public GameObject warningImage;
    public float warningDuration = 2f;
    public float blinkInterval = 0.25f;

    [Header("Infinite Scaling")]
    public float hpScalePerStage = 2f;       // CSV 최고 스테이지 이후 스테이지당 체력 배율
    public float defenseScalePerStage = 1.5f; // 방어력 배율
    public float rewardScalePerStage = 1.5f;  // 보상 배율

    private GameObject currentBoss;
    private int bossWaveCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (warningImage != null) warningImage.SetActive(false);
    }

    public void TrySpawnBoss()
    {
        if (CSVLoader.Instance == null) return;
        bossWaveCount++;
        int waveLevel = Mathf.Clamp(bossWaveCount, 1, 5);
        int currentStage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        int randomType = Random.Range(0, 3);

        BossData data = CSVLoader.Instance.GetBossDataByTypeStageLevel(randomType, currentStage, waveLevel);

        // CSV에 해당 스테이지 데이터 없으면 최고 스테이지 기반으로 스케일링
        if (data == null)
        {
            BossData maxData = CSVLoader.Instance.GetMaxStageBossData(randomType, waveLevel);
            if (maxData == null) return;
            data = ScaleBossData(maxData, currentStage);
        }

        StartCoroutine(WarningThenSpawn(data));
    }

    BossData ScaleBossData(BossData baseData, int currentStage)
    {
        // CSV 최고 스테이지 이후 초과 스테이지 수 계산
        int extraStages = currentStage - baseData.stage;
        float multiplier = Mathf.Pow(hpScalePerStage, extraStages);
        float defMultiplier = Mathf.Pow(defenseScalePerStage, extraStages);
        float rewardMultiplier = Mathf.Pow(rewardScalePerStage, extraStages);

        return new BossData
        {
            bossType = baseData.bossType,
            stage = currentStage,
            bossWaveLevel = baseData.bossWaveLevel,
            hp = baseData.hp * multiplier,
            speed = baseData.speed,
            reward = Mathf.RoundToInt(baseData.reward * rewardMultiplier),
            defense = baseData.defense * defMultiplier,
            forceDamageOne = baseData.forceDamageOne
        };
    }

    IEnumerator WarningThenSpawn(BossData data)
    {
        if (GameManager.Instance != null) GameManager.Instance.SetWarning(true);
        if (PauseManager.Instance != null) PauseManager.Instance.SetWarningMode(true);
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.SetPaused(true);
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove e in enemies) e.SetPaused(true);

        if (warningImage != null)
        {
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                warningImage.SetActive(true);
                yield return new WaitForSeconds(blinkInterval);
                warningImage.SetActive(false);
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval * 2f;
            }
        }


        if (GameManager.Instance != null) GameManager.Instance.SetWarning(false);
        if (PauseManager.Instance != null) PauseManager.Instance.SetWarningMode(false);
        if (spawner != null) spawner.SetPaused(false);
        enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove e in enemies) e.SetPaused(false);

        SpawnBoss(data);
    }

    void SpawnBoss(BossData data)
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return;
        if (data.bossType >= bossPrefabs.Length) return;
        GameObject prefab = bossPrefabs[data.bossType];
        if (prefab == null) return;

        currentBoss = Instantiate(prefab, spawnPosition, Quaternion.identity);

        EnemyMove enemyMove = currentBoss.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = data.speed; }

        EnemyHealth enemyHealth = currentBoss.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.isSpecial = true;
            enemyHealth.forceDamageOne = data.forceDamageOne;
            enemyHealth.specialCoinReward = data.reward;
            enemyHealth.Init(data.hp, data.defense);
        }

        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }

    public bool IsBossAlive() => currentBoss != null && currentBoss.activeSelf;
    public void ClearBossRef() { currentBoss = null; }
}




