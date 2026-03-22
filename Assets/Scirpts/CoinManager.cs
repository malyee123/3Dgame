using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Coin Settings")]
    public int startingCoins = 100;
    public int coinsPerKill = 10;
    public int spawnCost = 20;

    [Header("UI")]
    public TextMeshProUGUI coinText;

    private int currentCoins;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[CoinManager] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        float multiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetStartingCoinMultiplier() : 1f;
        currentCoins = Mathf.RoundToInt(startingCoins * multiplier);
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        float multiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetCoinPerKillMultiplier() : 1f;
        int finalAmount = Mathf.RoundToInt(amount * multiplier);
        currentCoins += finalAmount;
        UpdateCoinUI();
        if (MergeManager.Instance != null) MergeManager.Instance.CheckMergeAvailable();
        Debug.Log($"[CoinManager] Coins: {currentCoins} (+{finalAmount})");
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins < amount) { Debug.Log("[CoinManager] Not enough coins!"); return false; }
        currentCoins -= amount;
        UpdateCoinUI();

        if (MergeManager.Instance != null)
            MergeManager.Instance.CheckMergeAvailable();
        Debug.Log($"[CoinManager] Coins: {currentCoins} (-{amount})");
        return true;
    }

    public int GetCoins() => currentCoins;

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = $"Coins: {currentCoins}";
    }
}