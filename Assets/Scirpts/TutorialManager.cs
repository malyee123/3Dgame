using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string KEY_LOBBY_SHOWN = "TutorialLobbyShown";
    private const string KEY_GAME_SHOWN = "TutorialGameShown";
    private const string KEY_BOSS_SHOWN = "TutorialBossShown";

    public const int STEP_LOBBY = 1;
    public const int STEP_GAME = 2;
    public const int STEP_BOSS = 3;

    [System.Serializable]
    public class IconEntry
    {
        public string key;
        public Sprite sprite;
    }

    [Header("아이콘 매핑 (tutorial.csv의 icon 컬럼과 key가 일치해야 함)")]
    [SerializeField] private IconEntry[] iconEntries;

    [Header("한글 폰트 (비워두면 TMP 기본 폰트 사용)")]
    [SerializeField] private TMP_FontAsset koreanFont;

    private Dictionary<string, Sprite> iconMap = new Dictionary<string, Sprite>();

    private TutorialUI activeUI;
    private bool gamePaused;
    private float savedTimeScale = 1f;
    private string pendingPrefsKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildIconMap();
    }

    private void BuildIconMap()
    {
        iconMap.Clear();
        if (iconEntries == null) return;

        for (int i = 0; i < iconEntries.Length; i++)
        {
            IconEntry entry = iconEntries[i];
            if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
            if (!iconMap.ContainsKey(entry.key))
            {
                iconMap.Add(entry.key, entry.sprite);
            }
        }
    }

    public void TryShowLobbyTutorial()
    {
        if (PlayerPrefs.GetInt(KEY_LOBBY_SHOWN, 0) == 1) return;
        ShowTutorial(STEP_LOBBY, false, KEY_LOBBY_SHOWN);
    }

    public void TryShowGameTutorial()
    {
        if (PlayerPrefs.GetInt(KEY_GAME_SHOWN, 0) == 1) return;
        StartCoroutine(ShowGameTutorialDelayed());
    }

    private IEnumerator ShowGameTutorialDelayed()
    {
        yield return null;
        ShowTutorial(STEP_GAME, true, KEY_GAME_SHOWN);
    }

    public void TryShowBossTutorial(int stage, int wave)
    {
        if (stage != 1 || wave != 10) return;
        if (PlayerPrefs.GetInt(KEY_BOSS_SHOWN, 0) == 1) return;
        ShowTutorial(STEP_BOSS, true, KEY_BOSS_SHOWN);
    }

    public void ShowTutorialForce(int step)
    {
        ShowTutorial(step, pauseGame: step != STEP_LOBBY, prefsKeyToSet: null);
    }

    public void ResetAllTutorialFlags()
    {
        PlayerPrefs.DeleteKey(KEY_LOBBY_SHOWN);
        PlayerPrefs.DeleteKey(KEY_GAME_SHOWN);
        PlayerPrefs.DeleteKey(KEY_BOSS_SHOWN);
    }

    private void ShowTutorial(int step, bool pauseGame, string prefsKeyToSet)
    {
        if (activeUI != null) return;
        if (CSVLoader.Instance == null) return;

        var pages = CSVLoader.Instance.GetTutorialPages(step);
        if (pages == null || pages.Count == 0)
        {
            if (!string.IsNullOrEmpty(prefsKeyToSet))
                PlayerPrefs.SetInt(prefsKeyToSet, 1);
            return;
        }

        if (pauseGame)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            gamePaused = true;
        }

        TMP_FontAsset fontToUse = koreanFont;
        if (fontToUse == null)
        {
            TextMeshProUGUI[] tmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (TextMeshProUGUI tmp in tmps)
            {
                if (tmp.font != null)
                {
                    fontToUse = tmp.font;   
                    break;
                }
            }
        }

        pendingPrefsKey = prefsKeyToSet;
        activeUI = TutorialUI.Show(pages, iconMap, fontToUse, OnTutorialClosed);
    }

    private void OnTutorialClosed()
    {
        if (gamePaused)
        {
            Time.timeScale = savedTimeScale;
            gamePaused = false;
        }

        if (!string.IsNullOrEmpty(pendingPrefsKey))
        {
            PlayerPrefs.SetInt(pendingPrefsKey, 1);
            pendingPrefsKey = null;
        }

        activeUI = null;
    }
}
