using UnityEngine;
using UnityEngine.UI;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("Merge Settings")]
    public int mergeCost = 150;

    [Header("UI")]
    public Button mergeButton;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[MergeManager] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        if (mergeButton != null)
            mergeButton.interactable = false;
    }

    public void CheckMergeAvailable()
    {
        if (mergeButton == null) return;

        bool hasMergeReady = FindMergeReadyPlayer() != null;
        bool hasEnoughCoins = CoinManager.Instance != null &&
                              CoinManager.Instance.GetCoins() >= mergeCost;

        mergeButton.interactable = hasMergeReady && hasEnoughCoins;
    }

    PlayerAttack FindMergeReadyPlayer()
    {
        PlayerAttack[] players = FindObjectsOfType<PlayerAttack>();
        foreach (PlayerAttack player in players)
        {
            if (player.MergeCount >= 2)
                return player;
        }
        return null;
    }

    public void ExecuteMerge()
    {
        PlayerAttack target = FindMergeReadyPlayer();
        if (target == null) { Debug.Log("[MergeManager] No merge ready player!"); return; }
        if (!CoinManager.Instance.SpendCoins(mergeCost)) return;

        target.ForceUpgrade();
        CheckMergeAvailable();
    }
}