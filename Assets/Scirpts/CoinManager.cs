using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI coinText;

    [HideInInspector] public int spawnCost = 20;
    [HideInInspector] public float spawnCostMultiplier = 1f;
    [HideInInspector] public bool huntersSenseActive = false;
    [HideInInspector] public int augmentCoinBonus = 0;
    [HideInInspector] public int coinsPerKill = 10;

    private int startingCoins = 100;
    private int currentCoins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (CSVLoader.Instance?.GameSettings != null)
        {
            startingCoins = CSVLoader.Instance.GameSettings.startingCoins;
            spawnCost = CSVLoader.Instance.GameSettings.spawnCost;
        }
        int bonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetStartingCoinBonus() : 0;
        currentCoins = startingCoins + bonus;
        UpdateCoinUI();
    }

    public int GetActualSpawnCost() => Mathf.Max(1, Mathf.RoundToInt(spawnCost * spawnCostMultiplier));

    public void AddCoins(int amount, bool applyKillBonus = false)
    {
        int bonus = (applyKillBonus && UpgradeManager.Instance != null) ? UpgradeManager.Instance.GetCoinPerKillBonus() : 0;
        int augBonus = applyKillBonus ? augmentCoinBonus : 0;
        int huntersBonus = (applyKillBonus && huntersSenseActive && Random.Range(0f, 100f) < 5f) ? 10 : 0;
        currentCoins += amount + bonus + augBonus + huntersBonus;
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
