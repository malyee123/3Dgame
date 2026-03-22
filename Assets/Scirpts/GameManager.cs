using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over Settings")]
    public int maxEnemyCount = 200;

    [Header("Round Settings")]
    public float roundDuration = 60f;

    [Header("UI")]
    public TextMeshProUGUI enemyCountText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI roundTimerText;
    public TextMeshProUGUI totalTimerText;

    private int currentEnemyCount = 0;
    private int currentRound = 1;
    private float roundTimeLeft;
    private float totalElapsedTime = 0f;
    private bool isGameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[GameManager] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        roundTimeLeft = roundDuration;
        UpdateAllUI();
    }

    void Update()
    {
        if (isGameOver) return;
        totalElapsedTime += Time.deltaTime;
        roundTimeLeft -= Time.deltaTime;
        UpdateAllUI();
        if (roundTimeLeft <= 0f) NextRound();
    }

    public void OnEnemySpawned()
    {
        currentEnemyCount++;
        UpdateEnemyCountUI();
        if (currentEnemyCount >= maxEnemyCount) GameOver();
    }

    public void OnEnemyDied()
    {
        currentEnemyCount--;
        UpdateEnemyCountUI();
    }

    void NextRound()
    {
        currentRound++;
        roundTimeLeft = roundDuration;
        Debug.Log($"[GameManager] Round {currentRound} started!");
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.ApplyRoundSettings(currentRound);
    }

    void GameOver()
    {
        isGameOver = true;
        PlayerPrefs.SetFloat("LastTotalTime", totalElapsedTime);
        PlayerPrefs.SetInt("LastRound", currentRound);
        PlayerPrefs.Save();
        Debug.Log($"[GameManager] Game Over! Round: {currentRound} / Total: {FormatTime(totalElapsedTime)}");
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOverScene");
    }

    void UpdateAllUI()
    {
        UpdateEnemyCountUI();
        if (roundText != null) roundText.text = $"Round: {currentRound}";
        if (roundTimerText != null) roundTimerText.text = $"Time: {Mathf.CeilToInt(roundTimeLeft)}s";
        if (totalTimerText != null) totalTimerText.text = $"Total: {FormatTime(totalElapsedTime)}";
    }

    void UpdateEnemyCountUI()
    {
        if (enemyCountText != null)
            enemyCountText.text = $"Enemies: {currentEnemyCount}/{maxEnemyCount}";
    }

    public float GetTotalTime() => totalElapsedTime;
    public int GetCurrentRound() => currentRound;

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}