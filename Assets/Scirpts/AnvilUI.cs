using UnityEngine;
using TMPro;

public class AnvilUI : MonoBehaviour
{
    public static AnvilUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject anvilPanel;

    [Header("Cards")]
    public AnvilCard[] cards = new AnvilCard[3];

    [Header("Active Anvils")]
    public TextMeshProUGUI activeAnvilText;

    private System.Collections.Generic.List<string> activeSummaries = new System.Collections.Generic.List<string>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (anvilPanel != null) anvilPanel.SetActive(false);
        UpdateActiveAnvilText();
    }

    public void ShowAnvils()
    {
        if (AnvilManager.Instance == null) return;
        AnvilData[] anvils = AnvilManager.Instance.GetRandomAnvils();

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i < anvils.Length)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(anvils[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        if (anvilPanel != null) anvilPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnSelectAnvil(AnvilData data)
    {
        AnvilManager.Instance?.ApplyAnvil(data);
        activeSummaries.Add(data.summary);
        if (anvilPanel != null) anvilPanel.SetActive(false);
        // 선택 전 배속으로 복원
        Time.timeScale = SpeedManager.Instance != null ? SpeedManager.Instance.CurrentSpeed : 1f;
        UpdateActiveAnvilText();
    }

    void UpdateActiveAnvilText()
    {
        if (activeAnvilText == null) return;
        if (activeSummaries.Count == 0) { activeAnvilText.text = ""; return; }
        activeAnvilText.text = "[ 모루 ]\n" + string.Join("\n", activeSummaries);
    }

    public void ResetAnvilUI()
    {
        activeSummaries.Clear();
        UpdateActiveAnvilText();
    }
}