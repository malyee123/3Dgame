using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpecialMonsterManager : MonoBehaviour
{
    public static SpecialMonsterManager Instance { get; private set; }

    [Header("Special Monster Settings")]
    public GameObject specialMonsterPrefab;

    [Header("UI")]
    public GameObject spawnButtonObject;
    public Button spawnButton;

    [Header("Spawn Position")]
    public Vector2 spawnPosition = new Vector2(-6f, 3f);

    [Header("Path Settings")]
    public PathManager pathManager;

    private float spawnInterval = 20f;
    private float intervalTimer = 0f;
    private bool isButtonActive = false;
    private Coroutine buttonCoroutine = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        RefreshSettings();
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (spawnButtonObject != null) spawnButtonObject.SetActive(false);
        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveAllListeners();
            spawnButton.onClick.AddListener(OnSpawnButtonClicked);
        }
    }

    void RefreshSettings()
    {
        int stage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        if (CSVLoader.Instance != null)
        {
            SpecialMonsterStageData data = CSVLoader.Instance.GetSpecialMonsterData(stage);
            if (data != null) spawnInterval = data.spawnInterval;
        }
    }

    void Update()
    {
        if (isButtonActive) return;
        intervalTimer += Time.deltaTime;
        if (intervalTimer >= spawnInterval)
        {
            intervalTimer = 0f;
            buttonCoroutine = StartCoroutine(ShowButtonRoutine());
        }
    }

    IEnumerator ShowButtonRoutine()
    {
        isButtonActive = true;
        if (spawnButtonObject != null && (RecipeBook.Instance == null || !RecipeBook.Instance.IsPanelOpen))
            spawnButtonObject.SetActive(true);
        yield return null;
    }

    void OnSpawnButtonClicked()
    {
        if (buttonCoroutine != null) { StopCoroutine(buttonCoroutine); buttonCoroutine = null; }
        if (spawnButtonObject != null) spawnButtonObject.SetActive(false);
        isButtonActive = false;
        SpawnSpecialMonster();
    }

    void SpawnSpecialMonster()
    {
        if (specialMonsterPrefab == null || pathManager == null) return;

        int stage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        SpecialMonsterStageData settings = CSVLoader.Instance?.GetSpecialMonsterData(stage);

        float hp = settings?.hp ?? 500f;
        float speed = settings?.speed ?? 1.5f;
        float lifetime = settings?.lifetime ?? 15f;
        int coinReward = settings?.coinReward ?? 3;
        float defense = settings?.defense ?? 0f;

        GameObject obj = Instantiate(specialMonsterPrefab, spawnPosition, Quaternion.identity);
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = speed; }
        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.isSpecial = true;
            enemyHealth.specialCoinReward = coinReward;
            enemyHealth.Init(hp, defense);
        }
        GameManager.Instance?.OnEnemySpawned();
        StartCoroutine(DespawnAfterTime(obj, enemyHealth, lifetime));
    }

    IEnumerator DespawnAfterTime(GameObject obj, EnemyHealth health, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        if (obj != null)
        {
            if (health != null && health.gameObject.activeInHierarchy)
                GameManager.Instance?.OnEnemyDied();
            Destroy(obj);
        }
    }
}
