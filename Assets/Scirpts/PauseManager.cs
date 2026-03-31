using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    public GameObject pausePanel;
    public GameObject blockerPanel;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[PauseManager] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (blockerPanel != null) blockerPanel.SetActive(false);
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        if (blockerPanel != null) blockerPanel.SetActive(isPaused);
        SetPlayerInteraction(!isPaused);
        // Debug.Log($"[PauseManager] {(isPaused ? "Paused" : "Resumed")}");
    }

    void SetPlayerInteraction(bool enabled)
    {
        PlayerDragMerge[] dragMerges = FindObjectsByType<PlayerDragMerge>(FindObjectsSortMode.None);
        foreach (PlayerDragMerge drag in dragMerges) drag.enabled = enabled;
    }

    public void GoToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LobbyScene");
    }
}