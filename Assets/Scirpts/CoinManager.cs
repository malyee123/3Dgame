using UnityEngine;
using TMPro;

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
        currentCoins = startingCoins;
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateCoinUI();

        if (MergeManager.Instance != null)
            MergeManager.Instance.CheckMergeAvailable();
        Debug.Log($"[CoinManager] Coins: {currentCoins} (+{amount})");
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