using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI")]
    public Canvas transitionCanvas;
    public Image backgroundPanel;
    public TextMeshProUGUI loadingText;

    [Header("Settings")]
    public float minDelaySeconds = 1f;

    private bool isTransitioning = false;

    private static readonly string[] loadingMessages =
    {
        "로딩중..."
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
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;
        ShowPanel();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        yield return new WaitForSecondsRealtime(minDelaySeconds);

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;

        yield return new WaitForSecondsRealtime(0.05f);
        HidePanel();
        isTransitioning = false;
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