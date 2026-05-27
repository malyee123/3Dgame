using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    private const string KEY_ATK_DMG = "UpgradeAttackDamage";
    private const string KEY_ATK_SPD = "UpgradeAttackSpeed";
    private const string KEY_COIN_KILL = "UpgradeCoinPerKill";
    private const string KEY_START_COIN = "UpgradeStartingCoin";
    private const string KEY_TIER = "UnlockedTier";
    private const string KEY_TIER1_PASSIVE = "Tier1PassiveLevel";
    private const string KEY_TIER2_PASSIVE = "Tier2PassiveLevel";
    private const string KEY_TIER3_PASSIVE = "Tier3PassiveLevel";
    private const string KEY_TIER4_PASSIVE = "Tier4PassiveLevel";
    private const string KEY_TIER5_PASSIVE = "Tier5PassiveLevel";
    private const string KEY_CHAR_LIMIT = "UpgradeCharacterLimit";

    private const int maxCharacterLimitLevel = 10;
    private static readonly int[] defaultTierUnlockCosts = { 5, 10, 20, 40 };

    public int AttackDamageLevel => PlayerPrefs.GetInt(KEY_ATK_DMG, 0);
    public int AttackSpeedLevel => PlayerPrefs.GetInt(KEY_ATK_SPD, 0);
    public int CoinPerKillLevel => PlayerPrefs.GetInt(KEY_COIN_KILL, 0);
    public int StartingCoinLevel => PlayerPrefs.GetInt(KEY_START_COIN, 0);
    public int UnlockedTier => PlayerPrefs.GetInt(KEY_TIER, 1);
    public int CharacterLimitLevel => PlayerPrefs.GetInt(KEY_CHAR_LIMIT, 0);
    public int Tier1PassiveLevel => PlayerPrefs.GetInt(KEY_TIER1_PASSIVE, 0);
    public int Tier2PassiveLevel => PlayerPrefs.GetInt(KEY_TIER2_PASSIVE, 0);
    public int Tier3PassiveLevel => PlayerPrefs.GetInt(KEY_TIER3_PASSIVE, 0);
    public int Tier4PassiveLevel => PlayerPrefs.GetInt(KEY_TIER4_PASSIVE, 0);
    public int Tier5PassiveLevel => PlayerPrefs.GetInt(KEY_TIER5_PASSIVE, 0);

    public int[] TierUnlockCosts
    {
        get
        {
            if (CSVLoader.Instance == null) return defaultTierUnlockCosts;
            int[] result = new int[4];
            for (int i = 0; i < 4; i++)
            {
                string key = $"TierUnlock{i + 2}";
                UpgradeData data = CSVLoader.Instance.GetUpgradeData(key);
                result[i] = data != null ? data.costPerLevel : defaultTierUnlockCosts[i];
            }
            return result;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayerPrefs.DeleteKey(KEY_ATK_SPD);
        PlayerPrefs.Save();
    }

    public int GetMaxUpgradeLevel() => PlayerPrefs.GetInt("UnlockedStage", 1) * 10;

    public int GetSkillPoints() => PlayerPrefs.GetInt("SkillPoints", 0);

    bool SpendSkillPoints(int amount)
    {
        int current = GetSkillPoints();
        if (current < amount) return false;
        PlayerPrefs.SetInt("SkillPoints", current - amount);
        PlayerPrefs.Save();
        return true;
    }

    int GetCostFromCSV(string upgradeType, int currentLevel)
    {
        if (CSVLoader.Instance == null) return currentLevel + 1;
        UpgradeData data = CSVLoader.Instance.GetUpgradeData(upgradeType);
        if (data == null) return currentLevel + 1;
        return data.costPerLevel * (currentLevel + 1);
    }

    float GetBonusFromCSV(string upgradeType)
    {
        if (CSVLoader.Instance == null) return 0f;
        UpgradeData data = CSVLoader.Instance.GetUpgradeData(upgradeType);
        return data != null ? data.bonusPerLevel : 0f;
    }

    public int GetUpgradeCost(string upgradeType, int currentLevel) => GetCostFromCSV(upgradeType, currentLevel);

    public bool UpgradeAttackDamage()
    {
        int level = AttackDamageLevel;
        if (level >= GetMaxUpgradeLevel()) return false;
        if (!SpendSkillPoints(GetCostFromCSV("AttackDamage", level))) return false;
        PlayerPrefs.SetInt(KEY_ATK_DMG, level + 1); PlayerPrefs.Save(); return true;
    }

    public bool UpgradeAttackSpeed()
    {
        int level = AttackSpeedLevel;
        if (level >= GetMaxUpgradeLevel()) return false;
        if (!SpendSkillPoints(GetCostFromCSV("AttackSpeed", level))) return false;
        PlayerPrefs.SetInt(KEY_ATK_SPD, level + 1); PlayerPrefs.Save(); return true;
    }

    public bool UpgradeCoinPerKill()
    {
        int level = CoinPerKillLevel;
        if (level >= GetMaxUpgradeLevel()) return false;
        if (!SpendSkillPoints(GetCostFromCSV("CoinPerKill", level))) return false;
        PlayerPrefs.SetInt(KEY_COIN_KILL, level + 1); PlayerPrefs.Save(); return true;
    }

    public bool UpgradeStartingCoin()
    {
        int level = StartingCoinLevel;
        if (level >= GetMaxUpgradeLevel()) return false;
        if (!SpendSkillPoints(GetCostFromCSV("StartingCoin", level))) return false;
        PlayerPrefs.SetInt(KEY_START_COIN, level + 1); PlayerPrefs.Save(); return true;
    }

    public bool UpgradeCharacterLimit()
    {
        int level = CharacterLimitLevel;
        if (level >= maxCharacterLimitLevel) return false;
        if (!SpendSkillPoints(GetCostFromCSV("CharacterLimit", level))) return false;
        PlayerPrefs.SetInt(KEY_CHAR_LIMIT, level + 1); PlayerPrefs.Save(); return true;
    }

    public int GetCharacterLimitBonus() => CharacterLimitLevel;

    public bool UpgradeTierPassive(int tier)
    {
        if (UnlockedTier < tier) return false;
        string key = GetTierPassiveKey(tier);
        int level = PlayerPrefs.GetInt(key, 0);
        if (!SpendSkillPoints(GetCostFromCSV($"Tier{tier}Passive", level))) return false;
        PlayerPrefs.SetInt(key, level + 1); PlayerPrefs.Save(); return true;
    }

    public int GetTierPassiveLevel(int tier) => PlayerPrefs.GetInt(GetTierPassiveKey(tier), 0);
    public float GetTierPassiveBonus(int tier) => GetTierPassiveLevel(tier) * GetBonusFromCSV($"Tier{tier}Passive");
    public int GetTierPassiveCost(int tier) => GetCostFromCSV($"Tier{tier}Passive", GetTierPassiveLevel(tier));

    string GetTierPassiveKey(int tier)
    {
        switch (tier)
        {
            case 1: return KEY_TIER1_PASSIVE;
            case 2: return KEY_TIER2_PASSIVE;
            case 3: return KEY_TIER3_PASSIVE;
            case 4: return KEY_TIER4_PASSIVE;
            case 5: return KEY_TIER5_PASSIVE;
            default: return KEY_TIER1_PASSIVE;
        }
    }

    public bool UnlockNextTier()
    {
        int currentTier = UnlockedTier;
        int[] costs = TierUnlockCosts;
        if (costs == null || currentTier > costs.Length) return false;
        int cost = costs[currentTier - 1];
        if (!SpendSkillPoints(cost)) return false;
        PlayerPrefs.SetInt(KEY_TIER, currentTier + 1); PlayerPrefs.Save(); return true;
    }

    public float GetAttackDamageMultiplier() => 1f + (AttackDamageLevel * GetBonusFromCSV("AttackDamage") / 100f);
    public float GetAttackSpeedMultiplier() => 1f + (AttackSpeedLevel * GetBonusFromCSV("AttackSpeed") / 100f);
    public int GetCoinPerKillBonus() => (int)(CoinPerKillLevel * GetBonusFromCSV("CoinPerKill"));
    public int GetStartingCoinBonus() => (int)(StartingCoinLevel * GetBonusFromCSV("StartingCoin"));
    public int GetCharacterLimitUpgradeCost() => GetCostFromCSV("CharacterLimit", CharacterLimitLevel);
    public bool IsCharacterLimitMaxed() => CharacterLimitLevel >= maxCharacterLimitLevel;
}
