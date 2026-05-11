using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    public AudioClip lobbyBGM;

    [Header("SFX")]
    public AudioClip attackSFX;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private const string KEY_BGM_VOL = "BGMVolume";
    private const string KEY_SFX_VOL = "SFXVolume";

    public float BGMVolume
    {
        get => PlayerPrefs.GetFloat(KEY_BGM_VOL, 1f);
        set { PlayerPrefs.SetFloat(KEY_BGM_VOL, value); PlayerPrefs.Save(); bgmSource.volume = value; }
    }

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
        set { PlayerPrefs.SetFloat(KEY_SFX_VOL, value); PlayerPrefs.Save(); sfxSource.volume = value; }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = BGMVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = SFXVolume;
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "TitleScene" || scene.name == "LobbyScene" ||
            scene.name == "UpgradeScene" || scene.name == "PassiveUpgradeScene")
            PlayBGM(lobbyBGM);
        else
            StopBGM();
    }

    void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    void StopBGM()
    {
        if (bgmSource.isPlaying) bgmSource.Stop();
    }

    public void PlayAttackSFX()
    {
        if (attackSFX == null) return;
        sfxSource.PlayOneShot(attackSFX);
    }
}
