using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over Settings")]
    public int maxEnemyCount = 200;

    [Header("Boss Wave Settings")]
    public int bossWaveInterval = 10;
    public float bossRoundDuration = 40f;

    [Header("UI")]
    public TextMeshProUGUI enemyCountText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI roundTimerText;
    public TextMeshProUGUI totalTimerText;
    public TextMeshProUGUI stageText;

    private int currentEnemyCount = 0;
    private int currentRound = 1;
    private int currentStage = 1;
    private float roundTimeLeft;
    private float totalElapsedTime = 0f;
    private bool isGameOver = false;
    private bool isBossWave = false;
    private bool isWarning = false;
    private EnemySpawner enemySpawner;

    private int anvilEnemyLimitBonus = 0;

    private int prevEnemyCount = -1;
    private int prevRound = -1;
    private int prevRoundTimeLeft = -1;
    private int prevTotalTimeSeconds = -1;
    private int prevStage = -1;
    private float prevRoundTimeLeftPublic = 0f;

    public bool IsWarning => isWarning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentStage = PlayerPrefs.GetInt("SelectedStage", 1);
        currentRound = 1;
        PlayerPrefs.Save();
    }

    void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        ApplyRoundData(currentRound);
        if (enemySpawner != null) enemySpawner.ApplyRoundSettings(currentRound);
        UpdateAllUI();
    }

    void Update()
    {
        if (isGameOver || isWarning) return;
        totalElapsedTime += Time.deltaTime;
        roundTimeLeft -= Time.deltaTime;
        UpdateUIIfChanged();
        if (roundTimeLeft <= 0f) NextRound();
    }

    public void SetWarning(bool warning) => isWarning = warning;
    public void ExtendRoundTime(float time) => roundTimeLeft = time;

    public void AddEnemyLimit(int amount)
    {
        anvilEnemyLimitBonus += amount;
        maxEnemyCount += amount;
        UpdateEnemyCountUI();
    }

    public void AddBossTime(float amount) => bossRoundDuration += amount;

    void ApplyRoundData(int round)
    {
        if (CSVLoader.Instance != null)
        {
            RoundData data = CSVLoader.Instance.GetRoundData(round, currentStage);
            if (data != null)
            {
                roundTimeLeft = data.roundDuration;
                maxEnemyCount = data.maxEnemyCount + anvilEnemyLimitBonus;
                return;
            }
        }
        roundTimeLeft = 60f;
        maxEnemyCount = 200 + anvilEnemyLimitBonus;
    }

    public void OnEnemySpawned() { currentEnemyCount++; UpdateEnemyCountUI(); if (currentEnemyCount >= maxEnemyCount) GameOver(); }
    public void OnEnemyDied() { currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1); UpdateEnemyCountUI(); }

    public void OnBossKilled()
    {
        if (!isBossWave) return;
        if (BossManager.Instance != null) BossManager.Instance.ClearBossRef();

        // [요구사항 1·2] 데모 체크 블록 제거됨.
        // 스테이지 클리어 판정만 수행. 데모 처리는 StageClear() 내부에서 스테이지 번호로 분기.
        int stageEndRound = CSVLoader.Instance != null ? CSVLoader.Instance.GetStageEndRound(currentStage) : 50;
        if (currentRound >= stageEndRound) { StageClear(); return; }

        if (AugmentUI.Instance != null)
            AugmentUI.Instance.ShowAugments();
        else
            roundTimeLeft = 0f;
    }

    public void OnAugmentSelected() => roundTimeLeft = 0f;

    // ─────────────────────────────────────────────────────────
    // [요구사항 1·2] StageClear — 스테이지별 분기 처리
    //
    //  Stage 1 클리어 : 2스테이지 즉시 해금 → 로비 이동 (데모 없음)
    //  Stage 2 클리어 : 데모 문구(DemoEndScene) 표시, 3스테이지 해금 없음
    //  기타 스테이지  : 다음 스테이지 해금 → 로비 이동
    // ─────────────────────────────────────────────────────────
    void StageClear()
    {
        isGameOver = true;

        PlayerPrefs.SetFloat("LastTotalTime", totalElapsedTime);
        PlayerPrefs.SetInt("LastRound", currentRound);

        if (currentStage == 2)
        {
            // Stage 2 클리어: 데모 문구 출력, Stage 3는 해금하지 않음
            if (PlayerPrefs.GetInt("DemoEndShown", 0) == 0)
            {
                PlayerPrefs.SetInt("DemoEndShown", 1);
                PlayerPrefs.Save();
                Time.timeScale = 1f;
                SceneLoader.GoTo("DemoEndScene");
            }
            else
            {
                // 이미 데모를 본 경우: 로비로 복귀 (Stage 3 해금 없음 유지)
                PlayerPrefs.Save();
                Time.timeScale = 1f;
                SceneLoader.GoTo("LobbyScene");
            }
            return;
        }

        // Stage 1 및 기타: 다음 스테이지 즉시 해금 후 로비 이동
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
        if (currentStage >= unlockedStage)
            PlayerPrefs.SetInt("UnlockedStage", currentStage + 1);

        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneLoader.GoTo("LobbyScene");
    }

    void NextRound()
    {
        if (isBossWave && BossManager.Instance != null && BossManager.Instance.IsBossAlive()) { GameOver(); return; }

        isBossWave = false;
        if (BossManager.Instance != null) BossManager.Instance.ClearBossRef();
        if (enemySpawner != null) enemySpawner.SetPaused(false);

        currentRound++;
        AugmentManager.Instance?.OnNewRoundStart();

        ApplyRoundData(currentRound);
        if (enemySpawner != null) enemySpawner.ApplyRoundSettings(currentRound);

        if (currentRound % bossWaveInterval == 0)
        {
            isBossWave = true;
            if (enemySpawner != null) enemySpawner.SetPaused(true);
            roundTimeLeft = bossRoundDuration;
            BossManager.Instance?.TrySpawnBoss();
            AugmentManager.Instance?.OnBossWaveStart();
        }
    }

    void GameOver()
    {
        isGameOver = true;
        PlayerPrefs.SetFloat("LastTotalTime", totalElapsedTime);
        PlayerPrefs.SetInt("LastRound", currentRound);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneLoader.GoTo("GameOverScene");
    }

    void UpdateAllUI()
    {
        UpdateEnemyCountUI();
        if (roundText != null) roundText.text = $"Round: {currentRound}";
        if (roundTimerText != null) roundTimerText.text = $"Time: {Mathf.CeilToInt(roundTimeLeft)}s";
        if (totalTimerText != null) totalTimerText.text = $"Total: {FormatTime(totalElapsedTime)}";
        if (stageText != null) stageText.text = $"Stage: {currentStage}";
    }

    void UpdateUIIfChanged()
    {
        prevRoundTimeLeftPublic = roundTimeLeft;
        int ceilTimeLeft = Mathf.CeilToInt(roundTimeLeft);
        if (currentRound != prevRound) { prevRound = currentRound; if (roundText != null) roundText.text = $"Round: {currentRound}"; }
        if (ceilTimeLeft != prevRoundTimeLeft) { prevRoundTimeLeft = ceilTimeLeft; if (roundTimerText != null) roundTimerText.text = $"Time: {ceilTimeLeft}s"; }
        int currentTotalSeconds = (int)totalElapsedTime;
        if (currentTotalSeconds != prevTotalTimeSeconds) { prevTotalTimeSeconds = currentTotalSeconds; if (totalTimerText != null) totalTimerText.text = $"Total: {FormatTime(totalElapsedTime)}"; }
        if (currentStage != prevStage) { prevStage = currentStage; if (stageText != null) stageText.text = $"Stage: {currentStage}"; }
    }

    void UpdateEnemyCountUI()
    {
        if (currentEnemyCount == prevEnemyCount) return;
        prevEnemyCount = currentEnemyCount;
        if (enemyCountText != null) enemyCountText.text = $"Enemies: {currentEnemyCount}/{maxEnemyCount}";
    }

    public float GetTotalTime() => totalElapsedTime;
    public int GetCurrentRound() => currentRound;
    public int GetCurrentStage() => currentStage;
    public int GetCurrentEnemyCount() => currentEnemyCount;
    public float GetRoundTimeLeft() => roundTimeLeft;
    public float GetPrevRoundTimeLeft() => prevRoundTimeLeftPublic;

    public float GetCurrentRoundDuration()
    {
        if (CSVLoader.Instance != null)
        {
            RoundData data = CSVLoader.Instance.GetRoundData(currentRound, currentStage);
            if (data != null) return data.roundDuration;
        }
        return 60f;
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}