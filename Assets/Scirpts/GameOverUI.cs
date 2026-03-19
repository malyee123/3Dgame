using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
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
}