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

    private GameObject currentBoss;

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

    public void TrySpawnBoss(int wave)
    {
        if (CSVLoader.Instance == null) return;
        BossData data = CSVLoader.Instance.GetBossData(wave);
        if (data == null) return;
        StartCoroutine(WarningThenSpawn(data));
    }

    IEnumerator WarningThenSpawn(BossData data)
    {
        // 타이머 + 스폰 + 적 이동 정지
        if (GameManager.Instance != null) GameManager.Instance.SetWarning(true);
        if (PauseManager.Instance != null) PauseManager.Instance.SetWarningMode(true);
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.SetPaused(true);
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove e in enemies) e.SetPaused(true);

        // 경고 이미지 깜빡임
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

        // 타이머 + 스폰 + 적 이동 재개
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
        int randomId = Random.Range(0, bossPrefabs.Length);
        GameObject prefab = bossPrefabs[randomId];
        if (prefab == null) return;

        currentBoss = Instantiate(prefab, spawnPosition, Quaternion.identity);

        EnemyMove enemyMove = currentBoss.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = data.speed; }

        EnemyHealth enemyHealth = currentBoss.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.isSpecial = true;
            enemyHealth.specialCoinReward = data.reward;
            enemyHealth.Init(data.hp, data.defense);
        }

        if (GameManager.Instance != null) GameManager.Instance.OnEnemySpawned();
    }

    public bool IsBossAlive() => currentBoss != null && currentBoss.activeSelf;
    public void ClearBossRef() { currentBoss = null; }
}