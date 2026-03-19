using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over Settings")]
    public int maxEnemyCount = 200;

    [Header("UI")]
    public TextMeshProUGUI enemyCountText;

    private int currentEnemyCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[GameManager] Duplicate instance found!");
        Instance = this;
    }

    
    public void OnEnemySpawned()
    {
        currentEnemyCount++;
        UpdateEnemyCountUI();

        if (currentEnemyCount >= maxEnemyCount)
            GameOver();
    }

    
    public void OnEnemyDied()
    {
        currentEnemyCount--;
        UpdateEnemyCountUI();
    }

    void UpdateEnemyCountUI()
    {
        if (enemyCountText != null)
            enemyCountText.text = $"Enemies: {currentEnemyCount}/{maxEnemyCount}";
    }

    void GameOver()
    {
        Debug.Log("[GameManager] Game Over!");
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOverScene");
    }
}