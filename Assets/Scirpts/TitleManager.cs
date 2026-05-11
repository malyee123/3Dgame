using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI pressAnyKeyText;

    [Header("Blink Settings")]
    public float blinkInterval = 0.5f;

    private bool isLoading = false;
    private float blinkTimer = 0f;
    private bool inputReady = false;

    void Start()
    {
        StartCoroutine(EnableInputNextFrame());
    }

    System.Collections.IEnumerator EnableInputNextFrame()
    {
        yield return null;
        inputReady = true;
    }

    void Update()
    {
        if (isLoading || !inputReady) return;

        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            if (pressAnyKeyText != null)
                pressAnyKeyText.enabled = !pressAnyKeyText.enabled;
        }

        if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            isLoading = true;
            StartCoroutine(LoadSceneNextFrame());
        }
    }

    System.Collections.IEnumerator LoadSceneNextFrame()
    {
        yield return null;
        yield return null;
        SceneManager.LoadScene("LobbyScene");
    }
}
