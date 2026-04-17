using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Attack Damage Upgrade")]
    public int maxAttackDamageLevel = 5;
    public float attackDamagePercentPerLevel = 5f;
    public int[] attackDamageCosts;

    [Header("Attack Speed Upgrade")]
    public int maxAttackSpeedLevel = 5;
    public float attackSpeedPercentPerLevel = 5f;
    public int[] attackSpeedCosts;

    [Header("Coin Per Kill Upgrade")]
    public int maxCoinPerKillLevel = 5;
    public int coinPerKillBonusPerLevel = 2;
    public int[] coinPerKillCosts;

    [Header("Starting Coin Upgrade")]
    public int maxStartingCoinLevel = 5;
    public int startingCoinBonusPerLevel = 20;
    public int[] startingCoinCosts;

    [Header("Tier Unlock")]
    public int[] tierUnlockCosts;

    private const string KEY_ATK_DMG = "UpgradeAttackDamage";
    private const string KEY_ATK_SPD = "UpgradeAttackSpeed";
    private const string KEY_COIN_KILL = "UpgradeCoinPerKill";
    private const string KEY_START_COIN = "UpgradeStartingCoin";
    private const string KEY_TIER = "UnlockedTier";

    public int AttackDamageLevel => PlayerPrefs.GetInt(KEY_ATK_DMG, 0);
    public int AttackSpeedLevel => PlayerPrefs.GetInt(KEY_ATK_SPD, 0);
    public int CoinPerKillLevel => PlayerPrefs.GetInt(KEY_COIN_KILL, 0);
    public int StartingCoinLevel => PlayerPrefs.GetInt(KEY_START_COIN, 0);
    public int UnlockedTier => PlayerPrefs.GetInt(KEY_TIER, 1);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetSkillPoints() => PlayerPrefs.GetInt("SkillPoints", 0);

    public int GetCost(int[] costs, int currentLevel)
    {
        if (costs == null || costs.Length == 0) return currentLevel + 1;
        if (currentLevel >= costs.Length) return costs[costs.Length - 1];
        return costs[currentLevel];
    }

    bool SpendSkillPoints(int amount)
    {
        int current = GetSkillPoints();
        if (current < amount) return false;
        PlayerPrefs.SetInt("SkillPoints", current - amount);
        PlayerPrefs.Save();
        return true;
    }

    public bool UpgradeAttackDamage()
    {
        int level = AttackDamageLevel;
        if (level >= maxAttackDamageLevel) return false;
        int cost = GetCost(attackDamageCosts, level);
        if (!SpendSkillPoints(cost)) return false;
        PlayerPrefs.SetInt(KEY_ATK_DMG, level + 1);
        PlayerPrefs.Save();
        return true;
    }

    public bool UpgradeAttackSpeed()
    {
        int level = AttackSpeedLevel;
        if (level >= maxAttackSpeedLevel) return false;
        int cost = GetCost(attackSpeedCosts, level);
        if (!SpendSkillPoints(cost)) return false;
        PlayerPrefs.SetInt(KEY_ATK_SPD, level + 1);
        PlayerPrefs.Save();
        return true;
    }

    public bool UpgradeCoinPerKill()
    {
        int level = CoinPerKillLevel;
        if (level >= maxCoinPerKillLevel) return false;
        int cost = GetCost(coinPerKillCosts, level);
        if (!SpendSkillPoints(cost)) return false;
        PlayerPrefs.SetInt(KEY_COIN_KILL, level + 1);
        PlayerPrefs.Save();
        return true;
    }

    public bool UpgradeStartingCoin()
    {
        int level = StartingCoinLevel;
        if (level >= maxStartingCoinLevel) return false;
        int cost = GetCost(startingCoinCosts, level);
        if (!SpendSkillPoints(cost)) return false;
        PlayerPrefs.SetInt(KEY_START_COIN, level + 1);
        PlayerPrefs.Save();
        return true;
    }

    public bool UnlockNextTier()
    {
        int currentTier = UnlockedTier;
        if (tierUnlockCosts == null || currentTier >= tierUnlockCosts.Length) return false;
        int cost = tierUnlockCosts[currentTier - 1];
        if (!SpendSkillPoints(cost)) return false;
        PlayerPrefs.SetInt(KEY_TIER, currentTier + 1);
        PlayerPrefs.Save();
        return true;
    }

    public float GetAttackDamageMultiplier() => 1f + (AttackDamageLevel * attackDamagePercentPerLevel / 100f);
    public float GetAttackSpeedMultiplier() => Mathf.Max(0.1f, 1f - (AttackSpeedLevel * attackSpeedPercentPerLevel / 100f));
    public int GetCoinPerKillBonus() => CoinPerKillLevel * coinPerKillBonusPerLevel;
    public int GetStartingCoinBonus() => StartingCoinLevel * startingCoinBonusPerLevel;
}
