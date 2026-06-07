using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI")]
    public Canvas          transitionCanvas;
    public Image           backgroundPanel;
    public TextMeshProUGUI loadingText;

    [Header("Settings")]
    public float minDelaySeconds = 0.2f;

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

        // 비동기 로딩 시작 — 씬 활성화 보류
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 최소 딜레이 무조건 강제 대기 (timeScale 무관)
        yield return new WaitForSecondsRealtime(minDelaySeconds);

        // 로드 완료 대기 (progress 0.9 = 씬 준비 완료)
        while (op.progress < 0.9f)
            yield return null;

        // 씬 활성화
        op.allowSceneActivation = true;

        // 새 씬 프레임 대기 후 패널 숨김
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
