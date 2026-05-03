using UnityEngine;
using UnityEngine.UI;

public class SpeedManager : MonoBehaviour
{
    public static SpeedManager Instance { get; private set; }

    [Header("UI")]
    public Button speed1xButton;
    public Button speed2xButton;

    public float CurrentSpeed { get; private set; } = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (speed1xButton != null) { speed1xButton.onClick.RemoveAllListeners(); speed1xButton.onClick.AddListener(() => SetSpeed(1f)); }
        if (speed2xButton != null) { speed2xButton.onClick.RemoveAllListeners(); speed2xButton.onClick.AddListener(() => SetSpeed(2f)); }
        SetSpeed(1f);
    }

    void SetSpeed(float speed)
    {
        CurrentSpeed = speed;
        Time.timeScale = speed;
    }
}