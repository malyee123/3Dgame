using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI roundReachedText;
    public TextMeshProUGUI skillPointText;

    [Header("Skill Point Settings")]
    public float secondsPerPoint = 60f;

    void Start()
    {
        float totalTime = PlayerPrefs.GetFloat("LastTotalTime", 0f);
        int lastRound = PlayerPrefs.GetInt("LastRound", 1);
        int earnedPoints = Mathf.FloorToInt(totalTime / secondsPerPoint);
        int currentPoints = PlayerPrefs.GetInt("SkillPoints", 0);
        currentPoints += earnedPoints;
        PlayerPrefs.SetInt("SkillPoints", currentPoints);
        PlayerPrefs.Save();

        if (totalTimeText != null) totalTimeText.text = $"생존 시간: {FormatTime(totalTime)}";
        if (roundReachedText != null) roundReachedText.text = $"도달한 라운드: {lastRound}";
        if (skillPointText != null) skillPointText.text = $"획득한 스킬포인트: +{earnedPoints} (누적: {currentPoints})";
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LobbyScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}