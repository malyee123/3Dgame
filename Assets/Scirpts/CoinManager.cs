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
    public int augmentCoinBonus = 0;
    public float spawnCostMultiplier = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        int bonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetStartingCoinBonus() : 0;
        currentCoins = startingCoins + bonus;
        UpdateCoinUI();
    }

    public int GetActualSpawnCost() => Mathf.Max(1, Mathf.RoundToInt(spawnCost * spawnCostMultiplier));

    public void AddCoins(int amount, bool applyKillBonus = false)
    {
        int bonus = (applyKillBonus && UpgradeManager.Instance != null) ? UpgradeManager.Instance.GetCoinPerKillBonus() : 0;
        int augBonus = applyKillBonus ? augmentCoinBonus : 0;
        currentCoins += amount + bonus + augBonus;
        UpdateCoinUI();
        MergeManager.Instance?.CheckMergeAvailable();
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins < amount) return false;
        currentCoins -= amount;
        UpdateCoinUI();
        MergeManager.Instance?.CheckMergeAvailable();
        return true;
    }

    public int GetCoins() => currentCoins;

    void UpdateCoinUI()
    {
        if (coinText != null) coinText.text = $"Coins: {currentCoins}";
    }
}
