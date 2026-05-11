using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI")]
    public GameObject pausePanel;
    public GameObject blockerPanel;

    [Header("Sound Settings")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (blockerPanel != null) blockerPanel.SetActive(false);

        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : 1f;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        }
    }

    void OnBGMSliderChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.BGMVolume = value;
    }

    void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SFXVolume = value;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        if (blockerPanel != null) blockerPanel.SetActive(isPaused);
        SetPlayerInteraction(!isPaused);
    }

    public void SetWarningMode(bool isWarning)
    {
        SetPlayerInteraction(!isWarning);
        if (PlayerSpawner.Instance != null)
        {
            if (PlayerSpawner.Instance.spawnButton != null)
                PlayerSpawner.Instance.spawnButton.interactable = !isWarning;
            if (PlayerSpawner.Instance.specialSpawnButton != null)
                PlayerSpawner.Instance.specialSpawnButton.interactable = !isWarning;
        }
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

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
