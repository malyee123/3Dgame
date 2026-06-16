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
    private float currentRoundDuration = 60f;
    private float totalElapsedTime = 0f;
    private bool isGameOver = false;
    private bool isBossWave = false;
    private bool isWarning = false;
    private EnemySpawner enemySpawner;

    private int anvilEnemyLimitBonus = 0;

    private int prevEnemyCount = -1;
    private int prevMaxEnemyCount = -1;
    private int prevRound = -1;
    private int prevRoundTimeLeft = -1;
    private float prevRoundTimeLeftPublic = 0f;
    private int prevTotalTimeSeconds = -1;
    private int prevStage = -1;

    public bool IsWarning => isWarning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentStage = PlayerPrefs.GetInt("SelectedStage", 1);
        currentRound = 1;
    }

    void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        ApplyRoundData(currentRound);
        if (enemySpawner != null) enemySpawner.ApplyRoundSettings(currentRound);
        UpdateAllUI();
        TutorialManager.Instance?.TryShowGameTutorial();
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
        prevMaxEnemyCount = -1;
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
                currentRoundDuration = data.roundDuration;
                maxEnemyCount = data.maxEnemyCount + anvilEnemyLimitBonus;
                return;
            }
        }
        roundTimeLeft = 60f;
        currentRoundDuration = 60f;
        maxEnemyCount = 200 + anvilEnemyLimitBonus;
    }

    public void OnEnemySpawned() { currentEnemyCount++; UpdateEnemyCountUI(); if (currentEnemyCount >= maxEnemyCount) GameOver(); }
    public void OnEnemyDied() { currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1); UpdateEnemyCountUI(); }

    public void OnBossKilled()
    {
        if (!isBossWave) return;
        if (BossManager.Instance != null) BossManager.Instance.ClearBossRef();

        int stageEndRound = CSVLoader.Instance != null
            ? CSVLoader.Instance.GetStageEndRound(currentStage)
            : 50;

        if (currentRound >= stageEndRound) { StageClear(); return; }

        if (AugmentUI.Instance != null)
            AugmentUI.Instance.ShowAugments();
        else
            roundTimeLeft = 0f;
    }

    public void OnAugmentSelected() => roundTimeLeft = 0f;

    // ══════════════════════════════════════════════════════
    //  스테이지 클리어
    //  Stage 1, 2        → StageClearScene (다음 스테이지 CSV 데이터가 있어야 다음 스테이지로 진행 가능)
    //  Stage 3 (최초)     → DemoEndScene   (데모 종료 안내, 다음 스테이지 해금 안 함)
    //  Stage 3 (재클리어) → StageClearScene (다음 스테이지 버튼 비활성 상태로 표시)
    // ══════════════════════════════════════════════════════
    private const int DEMO_END_STAGE = 3;

    void StageClear()
    {
        isGameOver = true;

        // 다음 스테이지 해금
        // - 데모 마지막 스테이지(DEMO_END_STAGE)까지만 허용
        // - 그리고 다음 스테이지의 CSV 데이터(rounds.csv 등)가 실제로 존재할 때만 허용
        //   (기획 데이터가 아직 없는 스테이지로 진입해 깨진 화면이 뜨는 것을 방지)
        int maxAvailableStage = CSVLoader.Instance != null ? CSVLoader.Instance.GetMaxStage() : currentStage;
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
        if (currentStage < DEMO_END_STAGE && currentStage < maxAvailableStage && currentStage >= unlockedStage)
            PlayerPrefs.SetInt("UnlockedStage", currentStage + 1);

        PlayerPrefs.SetFloat("LastTotalTime", totalElapsedTime);
        PlayerPrefs.SetInt("LastRound", currentRound);
        PlayerPrefs.SetInt("ClearedStage", currentStage);
        PlayerPrefs.Save();
        Time.timeScale = 1f;

        if (currentStage == DEMO_END_STAGE)
        {
            if (PlayerPrefs.GetInt("DemoEndShown", 0) == 0)
            {
                PlayerPrefs.SetInt("DemoEndShown", 1);
                PlayerPrefs.Save();
                SceneLoader.GoTo("DemoEndScene");
            }
            else
            {
                SceneLoader.GoTo("StageClearScene");
            }
            return;
        }

        SceneLoader.GoTo("StageClearScene");
    }

    void NextRound()
    {
        if (isBossWave && BossManager.Instance != null && BossManager.Instance.IsBossAlive())
        { GameOver(); return; }

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
            currentRoundDuration = bossRoundDuration;
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
        int totalSec = (int)totalElapsedTime;
        if (totalSec != prevTotalTimeSeconds) { prevTotalTimeSeconds = totalSec; if (totalTimerText != null) totalTimerText.text = $"Total: {FormatTime(totalElapsedTime)}"; }
        if (currentStage != prevStage) { prevStage = currentStage; if (stageText != null) stageText.text = $"Stage: {currentStage}"; }
    }

    void UpdateEnemyCountUI()
    {
        if (currentEnemyCount == prevEnemyCount &&
            maxEnemyCount == prevMaxEnemyCount) return;

        prevEnemyCount = currentEnemyCount;
        prevMaxEnemyCount = maxEnemyCount;

        if (enemyCountText == null) return;

        enemyCountText.text = $"Enemies: {currentEnemyCount}/{maxEnemyCount}";
        enemyCountText.color = currentEnemyCount >= maxEnemyCount * 0.8f
            ? Color.red
            : Color.black;
    }

    public float GetTotalTime() => totalElapsedTime;
    public int GetCurrentRound() => currentRound;
    public int GetCurrentStage() => currentStage;
    public float GetRoundTimeLeft() => roundTimeLeft;
    public float GetPrevRoundTimeLeft() => prevRoundTimeLeftPublic;

    public int GetCurrentEnemyCount() => currentEnemyCount;

    public float GetCurrentRoundDuration() => isBossWave ? bossRoundDuration : currentRoundDuration;

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}