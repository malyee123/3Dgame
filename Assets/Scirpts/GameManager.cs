using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over Settings")]
    public int maxEnemyCount = 200;

    [Header("Round Settings")]
    public float roundDuration = 60f;

    [Header("Boss Wave Settings")]
    public int bossWaveInterval = 3;
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

    private int prevEnemyCount = -1;
    private int prevRound = -1;
    private int prevRoundTimeLeft = -1;
    private int prevTotalTimeSeconds = -1;
    private int prevStage = -1;

    public bool IsWarning => isWarning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        ApplyRoundData(currentRound);
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

    void ApplyRoundData(int round)
    {
        if (CSVLoader.Instance != null)
        {
            RoundData data = CSVLoader.Instance.GetRoundData(round);
            if (data != null)
            {
                roundDuration = data.roundDuration;
                maxEnemyCount = data.maxEnemyCount;
                currentStage = data.stage;
            }
        }
        roundTimeLeft = roundDuration;
    }

    int GetDisplayRound()
    {
        if (CSVLoader.Instance == null) return currentRound;
        RoundData data = CSVLoader.Instance.GetRoundData(currentRound);
        if (data == null) return currentRound;
             
        // 현재 스테이지의 첫 번째 구간 waveStart 찾기
        int stageWaveStart = data.waveStart;
        foreach (RoundData rd in CSVLoader.Instance.roundDataList)
        {
            if (rd.stage == data.stage && rd.waveStart < stageWaveStart)
                stageWaveStart = rd.waveStart;
        }

        return currentRound - stageWaveStart + 1;
    }

    public void OnEnemySpawned() { currentEnemyCount++; UpdateEnemyCountUI(); if (currentEnemyCount >= maxEnemyCount) GameOver(); }
    public void OnEnemyDied() { currentEnemyCount--; UpdateEnemyCountUI(); }

    public void OnBossKilled()
    {
        if (!isBossWave) return;
        if (BossManager.Instance != null) BossManager.Instance.ClearBossRef();
        roundTimeLeft = 0f;
    }

    void UpdateUIIfChanged()
    {
        int ceilTimeLeft = Mathf.CeilToInt(roundTimeLeft);
        int displayRound = GetDisplayRound();

        if (currentRound != prevRound) { prevRound = currentRound; if (roundText != null) roundText.text = $"Round: {displayRound}"; }
        if (ceilTimeLeft != prevRoundTimeLeft) { prevRoundTimeLeft = ceilTimeLeft; if (roundTimerText != null) roundTimerText.text = $"Time: {ceilTimeLeft}s"; }
        int currentTotalSeconds = (int)totalElapsedTime;
        if (currentTotalSeconds != prevTotalTimeSeconds) { prevTotalTimeSeconds = currentTotalSeconds; if (totalTimerText != null) totalTimerText.text = $"Total: {FormatTime(totalElapsedTime)}"; }
        if (currentStage != prevStage) { prevStage = currentStage; if (stageText != null) stageText.text = $"Stage: {currentStage}"; }
    }

    void NextRound()
    {
        if (isBossWave && BossManager.Instance != null && BossManager.Instance.IsBossAlive())
        {
            GameOver();
            return;
        }

        isBossWave = false;
        if (BossManager.Instance != null) BossManager.Instance.ClearBossRef();
        if (enemySpawner != null) enemySpawner.SetPaused(false);

        currentRound++;
        ApplyRoundData(currentRound);
        if (enemySpawner != null) enemySpawner.ApplyRoundSettings(currentRound);

        if (currentRound % bossWaveInterval == 0)
        {
            isBossWave = true;
            if (enemySpawner != null) enemySpawner.SetPaused(true);
            BossManager.Instance?.TrySpawnBoss();
        }
    }

    void GameOver()
    {
        isGameOver = true;
        PlayerPrefs.SetFloat("LastTotalTime", totalElapsedTime);
        PlayerPrefs.SetInt("LastRound", currentRound);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOverScene");
    }

    void UpdateAllUI()
    {
        UpdateEnemyCountUI();
        if (roundText != null) roundText.text = $"Round: {GetDisplayRound()}";
        if (roundTimerText != null) roundTimerText.text = $"Time: {Mathf.CeilToInt(roundTimeLeft)}s";
        if (totalTimerText != null) totalTimerText.text = $"Total: {FormatTime(totalElapsedTime)}";
        if (stageText != null) stageText.text = $"Stage: {currentStage}";
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

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}