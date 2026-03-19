using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    public GameObject pausePanel;

    private bool isPaused = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[PauseManager] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }


    public void TogglePause()
    {
        isPaused = !isPaused;

        
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        Debug.Log($"[PauseManager] {(isPaused ? "Paused" : "Resumed")}");
    }

    
    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LobbyScene");
    }
}