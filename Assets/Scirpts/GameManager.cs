using System;
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
    private int prevMaxEnemyCount = -1;   // maxEnemyCount 변동 감지용
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
        prevMaxEnemyCount = -1;   // 강제 갱신
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
    //  Stage 1 → StageClearScene (클리어 보상 표시)
    //  Stage 2 → DemoEndScene   (기존 동작 유지)
    //  기타    → StageClearScene
    // ══════════════════════════════════════════════════════
    void StageClear()
    {
        isGameOver = true;

        // 다음 스테이지 즉시 해금
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
        if (currentStage >= unlockedStage)
            PlayerPrefs.SetInt("UnlockedStage", currentStage + 1);

        PlayerPrefs.SetFloat("LastTotalTime", totalElapsedTime);
        PlayerPrefs.SetInt("LastRound", currentRound);
        PlayerPrefs.SetInt("ClearedStage", currentStage);   // StageClearUI에서 사용
        PlayerPrefs.Save();
        Time.timeScale = 1f;

        if (currentStage == 2)
        {
            // 2스테이지 클리어 → 데모 종료 씬 (최초 1회)
            if (PlayerPrefs.GetInt("DemoEndShown", 0) == 0)
            {
                PlayerPrefs.SetInt("DemoEndShown", 1);
                PlayerPrefs.Save();
                SceneLoader.GoTo("DemoEndScene");
            }
            else
            {
                SceneLoader.GoTo("LobbyScene");
            }
            return;
        }

        // 1스테이지 (및 기타) → 클리어 씬으로 이동
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

    // ── Enemy Count UI — 80% 이상 시 빨간색 ─────────────────
    // maxEnemyCount 에는 증강/모루 보너스가 이미 포함되어 있음
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
            : Color.white;
    }

    public float GetTotalTime() => totalElapsedTime;
    public int GetCurrentRound() => currentRound;
    public int GetCurrentStage() => currentStage;
    public float GetRoundTimeLeft() => roundTimeLeft;              // ← 추가
    public float GetPrevRoundTimeLeft() => prevRoundTimeLeftPublic;    // ← 추가

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    internal int GetCurrentEnemyCount()
    {
        throw new NotImplementedException();
    }

    internal float GetCurrentRoundDuration()
    {
        throw new NotImplementedException();
    }
}