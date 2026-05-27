using UnityEngine;
using TMPro;

public class AugmentUI : MonoBehaviour
{
    public static AugmentUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject augmentPanel;

    [Header("Cards")]
    public AugmentCard[] cards = new AugmentCard[3];

    [Header("Active Augments")]
    public TextMeshProUGUI activeAugmentText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (augmentPanel != null) augmentPanel.SetActive(false);
        UpdateActiveAugmentText();
    }

    public void ShowAugments()
    {
        if (AugmentManager.Instance == null) return;
        AugmentData[] augments = AugmentManager.Instance.GetRandomAugments();

        if (augments.Length == 0)
        {
            GameManager.Instance?.OnAugmentSelected();
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i < augments.Length)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(augments[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        if (augmentPanel != null) augmentPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnSelectAugment(AugmentData data)
    {
        AugmentManager.Instance?.ApplyAugment(data);
        if (augmentPanel != null) augmentPanel.SetActive(false);
        Time.timeScale = SpeedManager.Instance != null ? SpeedManager.Instance.CurrentSpeed : 1f;
        PassiveManager.Instance?.RecalculatePassives();
        GameManager.Instance?.OnAugmentSelected();
        UpdateActiveAugmentText();
    }

    public void OnSelectAugmentDebug(AugmentData data)
    {
        AugmentManager.Instance?.ApplyAugment(data);
        if (augmentPanel != null) augmentPanel.SetActive(false);
        Time.timeScale = SpeedManager.Instance != null ? SpeedManager.Instance.CurrentSpeed : 1f;
        PassiveManager.Instance?.RecalculatePassives();
        UpdateActiveAugmentText();
    }

    public void ShowAugmentsDebug()
    {
        if (AugmentManager.Instance == null) return;
        AugmentData[] augments = AugmentManager.Instance.GetRandomAugments();
        if (augments.Length == 0) return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i < augments.Length)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].SetupDebug(augments[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
        if (augmentPanel != null) augmentPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void UpdateActiveAugmentText()
    {
        if (activeAugmentText == null) return;
        activeAugmentText.text = AugmentManager.Instance != null
            ? AugmentManager.Instance.GetActiveAugmentText()
            : "";
    }
}