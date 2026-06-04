using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI")]
    public Canvas     transitionCanvas;
    public Image      backgroundPanel;
    public TextMeshProUGUI loadingText;

    [Header("Settings")]
    public float delaySeconds = 1f;

    private static string[] loadingMessages = new string[]
    {
        "로딩중...",
        "잠시만 기다려주세요...",
        "준비중..."
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        ShowPanel();
        yield return new WaitForSecondsRealtime(delaySeconds);
        SceneManager.LoadScene(sceneName);
        yield return null;
        HidePanel();
    }

    void ShowPanel()
    {
        if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(true);
        if (loadingText != null)
            loadingText.text = loadingMessages[Random.Range(0, loadingMessages.Length)];
    }

    void HidePanel()
    {
        if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(false);
    }
}
