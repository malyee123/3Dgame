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

    [HideInInspector] public float bossHpMultiplier = 1f;

    private float hpScalePerStage = 2f;
    private float defenseScalePerStage = 1.5f;
    private float rewardScalePerStage = 1.5f;

    private GameObject currentBoss;
    private int bossWaveCount = 0;
    private int lastStage = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (CSVLoader.Instance?.GameSettings != null)
        {
            hpScalePerStage = CSVLoader.Instance.GameSettings.hpScalePerStage;
            defenseScalePerStage = CSVLoader.Instance.GameSettings.defenseScalePerStage;
            rewardScalePerStage = CSVLoader.Instance.GameSettings.rewardScalePerStage;
        }
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (warningImage != null) warningImage.SetActive(false);
    }

    public void TrySpawnBoss()
    {
        if (CSVLoader.Instance == null) return;
        int currentStage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        if (currentStage != lastStage) { bossWaveCount = 0; lastStage = currentStage; }
        bossWaveCount++;
        int waveLevel = Mathf.Clamp(bossWaveCount, 1, 5);
        int randomType = Random.Range(0, 3);
        BossData data = CSVLoader.Instance.GetBossDataByTypeStageLevel(randomType, currentStage, waveLevel);
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
        int extraStages = currentStage - baseData.stage;
        return new BossData
        {
            bossType = baseData.bossType,
            stage = currentStage,
            bossWaveLevel = baseData.bossWaveLevel,
            hp = baseData.hp * Mathf.Pow(hpScalePerStage, extraStages),
            speed = baseData.speed,
            reward = Mathf.RoundToInt(baseData.reward * Mathf.Pow(rewardScalePerStage, extraStages)),
            defense = baseData.defense * Mathf.Pow(defenseScalePerStage, extraStages),
            forceDamageOne = baseData.forceDamageOne
        };
    }

    IEnumerator WarningThenSpawn(BossData data)
    {
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
            enemyHealth.isBoss = true;
            enemyHealth.forceDamageOne = data.forceDamageOne;
            enemyHealth.specialCoinReward = data.reward;
            enemyHealth.Init(data.hp * bossHpMultiplier, data.defense);
        }
        if (GameManager.Instance != null)
            GameManager.Instance.ExtendRoundTime(GameManager.Instance.bossRoundDuration);
        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }

    public bool IsBossAlive() => currentBoss != null && currentBoss.activeSelf;
    public void ClearBossRef() { currentBoss = null; }
}
