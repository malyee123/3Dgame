using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [System.Serializable]
    public class TransitionConfig
    {
        [Tooltip("출발 씬 이름 (예: LobbyScene)")]
        public string fromScene;
        [Tooltip("도착 씬 이름 (예: PassiveUpgradeScene)")]
        public string toScene;
        [Tooltip("이 전환에서 표시할 패널 오브젝트")]
        public GameObject panel;
        [Tooltip("최소 대기 시간 (초)")]
        public float delay = 1f;
    }

    [Header("씬별 전환 설정 (7개)")]
    [Tooltip(
        "Element 0: LobbyScene       → PassiveUpgradeScene\n" +
        "Element 1: StageSelectScene → GameScene\n" +
        "Element 2: LobbyScene       → UpgradeScene\n" +
        "Element 3: LobbyScene       → CompendiumScene\n" +
        "Element 4: GameScene        → GameOverScene\n" +
        "Element 5: GameScene        → DemoEndScene\n" +
        "Element 6: GameScene        → StageClearScene\n"
        )]

    public TransitionConfig[] transitionConfigs;

    // defaultPanel / defaultDelay 제거
    // 목록에 없는 전환은 패널 없이 즉시 전환

    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        HideAllPanels();
    }

    public void LoadScene(string toSceneName)
    {
        if (isTransitioning) return;
        string fromSceneName = SceneManager.GetActiveScene().name;
        TransitionConfig config = FindConfig(fromSceneName, toSceneName);

        if (config != null)
        {
            // 등록된 전환 → 패널 + 딜레이
            StartCoroutine(TransitionRoutine(toSceneName, config));
        }
        else
        {
            // 등록되지 않은 전환 → 즉시 이동 (패널 없음)
            SceneManager.LoadScene(toSceneName);
        }
    }

    IEnumerator TransitionRoutine(string toScene, TransitionConfig config)
    {
        isTransitioning = true;

        if (config.panel != null) config.panel.SetActive(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(toScene);
        op.allowSceneActivation = false;

        yield return new WaitForSecondsRealtime(config.delay);

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;

        yield return new WaitForSecondsRealtime(0.05f);

        if (config.panel != null) config.panel.SetActive(false);
        isTransitioning = false;
    }

    TransitionConfig FindConfig(string from, string to)
    {
        if (transitionConfigs == null || transitionConfigs.Length == 0) return null;
        foreach (var cfg in transitionConfigs)
        {
            if (cfg == null) continue;
            bool fromMatch = string.IsNullOrEmpty(cfg.fromScene) || cfg.fromScene == from;
            bool toMatch = string.IsNullOrEmpty(cfg.toScene) || cfg.toScene == to;
            if (fromMatch && toMatch) return cfg;
        }
        return null;
    }

    void HideAllPanels()
    {
        if (transitionConfigs == null) return;
        foreach (var cfg in transitionConfigs)
            if (cfg?.panel != null) cfg.panel.SetActive(false);
    }
}