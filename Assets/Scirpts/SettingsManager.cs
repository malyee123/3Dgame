using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Panel")]
    public GameObject settingsPanel;

    [Header("Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Buttons")]
    public Button openButton;
    public Button closeButton;
    public Button quitButton;

    private bool isOpen = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(v => { if (AudioManager.Instance != null) AudioManager.Instance.BGMVolume = v; });
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(v => { if (AudioManager.Instance != null) AudioManager.Instance.SFXVolume = v; });
        }
        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(OpenSettings);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseSettings);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen) CloseSettings();
            else OpenSettings();
        }
    }

    public void OpenSettings()
    {
        isOpen = true;
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (bgmSlider != null && AudioManager.Instance != null)
            bgmSlider.value = AudioManager.Instance.BGMVolume;
        if (sfxSlider != null && AudioManager.Instance != null)
            sfxSlider.value = AudioManager.Instance.SFXVolume;
    }

    public void CloseSettings()
    {
        isOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void QuitGame() => Application.Quit();
}
