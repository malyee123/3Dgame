using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ──────────────────────────────────────────────────────────
//  StageClearScene 의 빈 오브젝트에 부착
//  GameOverUI 와 동일한 구조 — 3버튼 (로비 / 재도전 / 다음스테이지)
// ──────────────────────────────────────────────────────────
public class StageClearUI : MonoBehaviour
{
    [Header("── 결과 텍스트 ──")]
    public TextMeshProUGUI stageClearedText;   // "1스테이지 클리어!"
    public TextMeshProUGUI totalTimeText;       // "클리어 시간: 15:32"
    public TextMeshProUGUI roundClearedText;   // "클리어 라운드: 50"
    public TextMeshProUGUI skillPointText;     // "획득 스킬포인트: +15 (누적: 30)"

    [Header("── 버튼 ──")]
    public Button lobbyButton;       // 로비로
    public Button restartButton;     // 재도전 (같은 스테이지)
    public Button nextStageButton;   // 다음 스테이지

    [Header("── 스킬포인트 설정 ──")]
    [Tooltip("GameOverUI 와 동일한 값으로 맞출 것")]
    public float secondsPerPoint = 60f;

    private int clearedStage;

    void Start()
    {
        float totalTime = PlayerPrefs.GetFloat("LastTotalTime", 0f);
        int lastRound = PlayerPrefs.GetInt("LastRound", 1);
        clearedStage = PlayerPrefs.GetInt("ClearedStage", 1);

        // ── 스킬포인트 지급 (GameOverUI 와 동일 방식) ──────
        int earnedPoints = Mathf.FloorToInt(totalTime / secondsPerPoint);
        int totalPoints = PlayerPrefs.GetInt("SkillPoints", 0) + earnedPoints;
        PlayerPrefs.SetInt("SkillPoints", totalPoints);
        PlayerPrefs.Save();

        // ── 텍스트 설정 ────────────────────────────────────
        if (stageClearedText != null)
            stageClearedText.text = $"{clearedStage}스테이지 클리어!";
        if (totalTimeText != null)
            totalTimeText.text = $"클리어 시간: {FormatTime(totalTime)}";
        if (roundClearedText != null)
            roundClearedText.text = $"클리어 라운드: {lastRound}";
        if (skillPointText != null)
            skillPointText.text = $"획득 스킬포인트: +{earnedPoints}  (누적: {totalPoints})";

        // ── 버튼 이벤트 ────────────────────────────────────
        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(GoToLobby);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartStage);
        }
        if (nextStageButton != null)
        {
            nextStageButton.onClick.RemoveAllListeners();
            nextStageButton.onClick.AddListener(GoToNextStage);

            // 다음 스테이지가 해금되어 있으면 활성, 아니면 비활성
            int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
            nextStageButton.interactable = unlockedStage > clearedStage;
        }
    }

    // ── 로비 이동 ────────────────────────────────────────
    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SceneLoader.GoTo("LobbyScene");
    }

    // ── 같은 스테이지 재도전 ──────────────────────────────
    public void RestartStage()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("SelectedStage", clearedStage);
        PlayerPrefs.Save();
        SceneLoader.GoTo("GameScene");
    }

    // ── 다음 스테이지 ────────────────────────────────────
    public void GoToNextStage()
    {
        int nextStage = clearedStage + 1;
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
        if (unlockedStage < nextStage) return;   // 혹시 모를 중복 클릭 방어

        Time.timeScale = 1f;
        PlayerPrefs.SetInt("SelectedStage", nextStage);
        PlayerPrefs.Save();
        SceneLoader.GoTo("GameScene");
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }
}