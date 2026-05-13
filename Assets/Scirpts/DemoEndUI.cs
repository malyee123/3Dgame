using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DemoEndUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI messageText;
    //public TextMeshProUGUI totalTimeText;
    public Button lobbyButton;
    public Button quitButton;

    void Start()
    {
        if (messageText != null)
            messageText.text = "데모 버전 플레이 해주셔서 감사합니다";

        //float totalTime = PlayerPrefs.GetFloat("LastTotalTime", 0f);
        //if (totalTimeText != null)
        //    totalTimeText.text = $"생존 시간: {FormatTime(totalTime)}";

        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(GoToLobby);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
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