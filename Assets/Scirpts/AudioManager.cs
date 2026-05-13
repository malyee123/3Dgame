using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    public AudioClip lobbyBGM;
    public AudioClip gameBGM;

    [Header("Attack SFX")]
    public AudioClip meleeSFX;
    public AudioClip bowSFX;
    public AudioClip magicSFX;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private const string KEY_BGM_VOL = "BGMVolume";
    private const string KEY_SFX_VOL = "SFXVolume";

    public float BGMVolume
    {
        get => PlayerPrefs.GetFloat(KEY_BGM_VOL, 1f);
        set { PlayerPrefs.SetFloat(KEY_BGM_VOL, value); PlayerPrefs.Save(); if (bgmSource != null) bgmSource.volume = value; }
    }

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
        set { PlayerPrefs.SetFloat(KEY_SFX_VOL, value); PlayerPrefs.Save(); if (sfxSource != null) sfxSource.volume = value; }
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

        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayBGMForScene(scene.name);

    void PlayBGMForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "TitleScene":
            case "LobbyScene":
            case "UpgradeScene":
            case "PassiveUpgradeScene":
            case "StageSelectScene":
            case "GameOverScene":
            case "DemoEndScene":
                PlayBGM(lobbyBGM);
                break;
            case "GameScene":
                PlayBGM(gameBGM != null ? gameBGM : lobbyBGM);
                break;
            default:
                StopBGM();
                break;
        }
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

    public void PlayAttackSFX(string unitTag)
    {
        AudioClip clip = unitTag switch
        {
            "Bow"   => bowSFX,
            "Magic" => magicSFX,
            _       => meleeSFX,
        };
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, SFXVolume);
    }
}
