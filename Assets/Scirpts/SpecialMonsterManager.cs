using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpecialMonsterManager : MonoBehaviour
{
    public static SpecialMonsterManager Instance { get; private set; }

    [Header("Special Monster Settings")]
    public GameObject specialMonsterPrefab;
    public float specialMonsterHp = 500f;
    public float specialMonsterSpeed = 1.5f;
    public float specialMonsterLifetime = 15f;
    public int specialCoinReward = 3;

    [Header("Notice Settings")]
    public float spawnInterval = 20f;

    [Header("UI")]
    public GameObject spawnButtonObject;
    public Button spawnButton;

    [Header("Spawn Position")]
    public Vector2 spawnPosition = new Vector2(-6f, 3f);

    [Header("Path Settings")]
    public PathManager pathManager;

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
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (spawnButtonObject != null) spawnButtonObject.SetActive(false);
        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveAllListeners();
            spawnButton.onClick.AddListener(OnSpawnButtonClicked);
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
        GameObject obj = Instantiate(specialMonsterPrefab, spawnPosition, Quaternion.identity);
        EnemyMove enemyMove = obj.GetComponent<EnemyMove>();
        if (enemyMove != null) { enemyMove.SetPathManager(pathManager); enemyMove.speed = specialMonsterSpeed; }
        EnemyHealth enemyHealth = obj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.isSpecial = true;
            enemyHealth.specialCoinReward = specialCoinReward;
            enemyHealth.Init(specialMonsterHp);
        }
        GameManager.Instance?.OnEnemySpawned();
        StartCoroutine(DespawnAfterTime(obj, enemyHealth, specialMonsterLifetime));
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