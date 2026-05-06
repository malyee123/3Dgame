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

    void Update()
    {
        if (isLoading) return;

        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            if (pressAnyKeyText != null)
                pressAnyKeyText.enabled = !pressAnyKeyText.enabled;
        }

        if (Input.anyKeyDown || Input.touchCount > 0)
        {
            isLoading = true;
            SceneManager.LoadScene("LobbyScene");
        }
    }
}