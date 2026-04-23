using UnityEngine;

public class AugmentUI : MonoBehaviour
{
    public static AugmentUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject augmentPanel;

    [Header("Cards")]
    public AugmentCard[] cards = new AugmentCard[3];

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (augmentPanel != null) augmentPanel.SetActive(false);
    }

    public void ShowAugments()
    {
        if (AugmentManager.Instance == null) return;
        AugmentData[] augments = AugmentManager.Instance.GetRandomAugments();
        for (int i = 0; i < 3; i++)
            if (cards[i] != null) cards[i].Setup(augments[i], this);

        if (augmentPanel != null) augmentPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnSelectAugment(AugmentData data)
    {
        AugmentManager.Instance?.ApplyAugment(data);
        if (augmentPanel != null) augmentPanel.SetActive(false);
        Time.timeScale = 1f;
        PassiveManager.Instance?.RecalculatePassives();
        GameManager.Instance?.OnAugmentSelected();
    }
}