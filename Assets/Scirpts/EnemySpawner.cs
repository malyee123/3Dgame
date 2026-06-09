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

    // ─────────────────────────────────────────────────────────
    // [요구사항 3] WaveEnemySet — 스테이지 + 웨이브 범위 기반
    //
    //  stageNumber : 적용할 스테이지 번호 (1, 2, 3...)
    //  fromWave    : 적용 시작 웨이브 (포함)
    //  toWave      : 적용 종료 웨이브 (포함)
    //
    //  예시 설정:
    //    [0] stage=1 from=1  to=30 → 1스테이지 1~30웨이브
    //    [1] stage=1 from=31 to=50 → 1스테이지 31~50웨이브
    //    [2] stage=2 from=1  to=10 → 2스테이지 1~10웨이브
    //    [3] stage=2 from=11 to=50 → 2스테이지 11~50웨이브
    // ─────────────────────────────────────────────────────────
    [System.Serializable]
    public class WaveEnemySet
    {
        [Tooltip("적용할 스테이지 번호 (1, 2, 3...)")]
        public int stageNumber;
        [Tooltip("적용 시작 웨이브 (포함)")]
        public int fromWave;
        [Tooltip("적용 종료 웨이브 (포함)")]
        public int toWave;
        [Tooltip("적용할 RuntimeAnimatorController (null이면 staticSprite 사용)")]
        public RuntimeAnimatorController animatorController;
        [Tooltip("애니메이션 없을 때 사용할 고정 스프라이트 (animatorController가 null일 때만 적용)")]
        public Sprite staticSprite;
    }

    [Header("스테이지·웨이브 구간별 적 외형 설정")]
    public WaveEnemySet[] waveEnemySets;

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
    public void SetSpawnPosition(Vector2 pos) => spawnPosition = pos;

    // ─────────────────────────────────────────────────────────
    // [요구사항 3] ApplyWaveEnemy — 현재 스테이지·웨이브가
    //  stageNumber == currentStage && fromWave <= round <= toWave
    //  조건을 만족하는 첫 번째 항목 적용
    // ─────────────────────────────────────────────────────────
    void ApplyWaveEnemy(GameObject obj)
    {
        if (waveEnemySets == null || waveEnemySets.Length == 0 || obj == null) return;

        int stage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        int round = GameManager.Instance != null ? GameManager.Instance.GetCurrentRound() : 1;

        WaveEnemySet target = null;
        foreach (WaveEnemySet set in waveEnemySets)
        {
            if (set.stageNumber == stage && round >= set.fromWave && round <= set.toWave)
            {
                target = set;
                break; // 조건에 맞는 첫 번째 항목만 적용
            }
        }

        if (target == null) return;

        EnemyHealth eh = obj.GetComponent<EnemyHealth>();
        if (eh != null) eh.ApplyWaveEnemy(target.animatorController, target.staticSprite);
    }

    public void ApplyRoundSettings(int round)
    {
        int stage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
        RoundData data = CSVLoader.Instance != null ? CSVLoader.Instance.GetRoundData(round, stage) : null;
        if (data != null)
        {
            int offsetInRange = round - data.waveStart;
            currentEnemyHp = data.baseHp + data.hpIncrement * offsetInRange;
            currentSpawnDelay = Mathf.Max(0.1f, data.spawnDelay - data.spawnDelayDecrement * offsetInRange);
            currentEnemySpeed = data.enemySpeed;
            currentEnemyDefense = data.enemyDefense;
            if (CoinManager.Instance != null) CoinManager.Instance.coinsPerKill = data.coinsPerKill;
        }
        else
        {
            currentEnemyHp = 50f;
            currentSpawnDelay = 1f;
            currentEnemySpeed = 2f;
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
        try { ApplyWaveEnemy(obj); } catch (System.Exception) { }

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
            float passiveDefenseDown = PassiveManager.Instance != null ? PassiveManager.Instance.GetTotalEnemyDefenseDown() : 0f;
            float anvilDefenseDown = AnvilManager.Instance != null ? AnvilManager.Instance.BonusDefenseDown : 0f;
            float augmentDefenseDown = AugmentManager.Instance != null ? AugmentManager.Instance.BonusDefenseDown : 0f;
            enemyHealth.ApplyDefenseDownPercent(passiveDefenseDown + anvilDefenseDown + augmentDefenseDown);
        }

        GameManager.Instance?.OnEnemySpawned();
    }
}