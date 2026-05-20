using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundData
{
    public int waveStart, waveEnd, stage, maxEnemyCount, coinsPerKill;
    public float baseHp, hpIncrement, spawnDelay, spawnDelayDecrement, enemySpeed, roundDuration, enemyDefense;
}

[System.Serializable]
public class BossData
{
    public int bossType, stage, bossWaveLevel, reward;
    public float hp, speed, defense;
    public bool forceDamageOne;
}

[System.Serializable]
public class UpgradeData
{
    public string upgradeType;
    public int costPerLevel;
    public float bonusPerLevel;
}

[System.Serializable]
public class GameSettingsData
{
    public int startingCoins = 100;
    public int spawnCost = 20;
    public int anvilCost = 3;
    public int specialSpawnCost = 3;
    public int specialSpawnMinTier = 2;
    public int specialSpawnMaxTier = 4;
    public int baseCharacterLimit = 35;
    public float hpScalePerStage = 2f;
    public float defenseScalePerStage = 1.5f;
    public float rewardScalePerStage = 1.5f;
    public int[] tierUnlockCosts = { 5, 10, 20, 40 };
    public float[] tierSpawnWeights = { 70f, 30f };
    public float[] specialTierSpawnWeights = { 70f, 25f, 5f };
}

[System.Serializable]
public class SpecialMonsterStageData
{
    public int killCount;
    public float hp;
    public float speed;
    public float lifetime;
    public int coinReward;
    public float cooldown;
    public float defense;
}

[System.Serializable]
public class SpawnCostData
{
    public int spawnCount;
    public int cost;
}

[System.Serializable]
public class AnvilRangeData
{
    public AnvilType type;
    public int stage;
    public float min;
    public float max;
}

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader Instance { get; private set; }

    [Header("CSV Files")]
    public TextAsset characterCSV, passiveCSV, roundCSV, bossCSV, upgradeCSV;
    public TextAsset gameSettingsCSV;
    public TextAsset specialMonsterCSV;
    public TextAsset anvilSettingsCSV;
    public TextAsset spawnCostCSV;

    [Header("Character Data List")]
    public CharacterData[] characterDataList;

    public List<RoundData> roundDataList = new List<RoundData>();
    public List<BossData> bossDataList = new List<BossData>();
    public List<UpgradeData> upgradeDataList = new List<UpgradeData>();
    public List<AnvilRangeData>  anvilRangeList     = new List<AnvilRangeData>();
    public List<SpawnCostData>   spawnCostDataList  = new List<SpawnCostData>();
    public List<SpecialMonsterStageData> specialMonsterDataList = new List<SpecialMonsterStageData>();

    public GameSettingsData GameSettings { get; private set; } = new GameSettingsData();

    private Dictionary<string, CharacterData> characterDataMap = new Dictionary<string, CharacterData>();
    private Dictionary<string, UpgradeData> upgradeDataMap = new Dictionary<string, UpgradeData>();

    static int ParseInt(string s, int fallback = 0) { return int.TryParse(s, out int v) ? v : fallback; }
    static float ParseFloat(string s, float fallback = 0f) { return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CharacterData[] loaded = Resources.LoadAll<CharacterData>("CharacterData");
        if (loaded != null && loaded.Length > 0) characterDataList = loaded;
        foreach (CharacterData data in characterDataList)
            if (data != null) characterDataMap[data.characterName] = data;
        LoadCharacterStats();
        LoadPassiveStats();
        LoadRoundStats();
        LoadBossStats();
        LoadUpgradeStats();
        LoadGameSettings();
        LoadSpecialMonsterStats();
        LoadAnvilSettings();
        LoadSpawnCostData();
    }

    void LoadCharacterStats()
    {
        if (characterCSV == null) return;
        string[] lines = characterCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 9) continue;
            CharacterData data = FindCharacterData(col[0].Trim());
            if (data == null) continue;
            data.tier = ParseInt(col[1].Trim(), data.tier);
            data.unitTag = col[2].Trim();
            data.attackDamage = ParseFloat(col[3].Trim(), data.attackDamage);
            data.attackSpeed = ParseFloat(col[4].Trim(), data.attackSpeed);
            data.attackRange = ParseFloat(col[5].Trim(), data.attackRange);
            data.upgradeCost = ParseInt(col[6].Trim(), data.upgradeCost);
            data.sellPrice = ParseInt(col[7].Trim(), data.sellPrice);
            data.attackHitDelay = ParseFloat(col[8].Trim(), data.attackHitDelay);
            if (col.Length >= 10 && !string.IsNullOrWhiteSpace(col[9].Trim()))
                data.attackType = col[9].Trim();
        }
    }

    void LoadPassiveStats()
    {
        if (passiveCSV == null) return;
        foreach (CharacterData data in characterDataList)
            if (data != null) data.passives.Clear();
        string[] lines = passiveCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 5) continue;
            CharacterData data = FindCharacterData(col[0].Trim());
            if (data == null) continue;
            PassiveEntry entry = new PassiveEntry();
            System.Enum.TryParse(col[1].Trim(), out entry.passiveType);
            entry.passiveValue = ParseFloat(col[2].Trim());
            entry.passiveSecondValue = ParseFloat(col[3].Trim());
            entry.passiveDuration = ParseFloat(col[4].Trim());
            data.passives.Add(entry);
        }
    }

    void LoadRoundStats()
    {
        if (roundCSV == null) return;
        roundDataList.Clear();
        string[] lines = roundCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 12) continue;
            roundDataList.Add(new RoundData
            {
                waveStart = ParseInt(col[0].Trim()),
                waveEnd = ParseInt(col[1].Trim()),
                stage = ParseInt(col[2].Trim(), 1),
                baseHp = ParseFloat(col[3].Trim(), 100f),
                hpIncrement = ParseFloat(col[4].Trim()),
                spawnDelay = ParseFloat(col[5].Trim(), 1f),
                spawnDelayDecrement = ParseFloat(col[6].Trim()),
                enemySpeed = ParseFloat(col[7].Trim(), 1f),
                maxEnemyCount = ParseInt(col[8].Trim(), 100),
                roundDuration = ParseFloat(col[9].Trim(), 60f),
                coinsPerKill = ParseInt(col[10].Trim(), 10),
                enemyDefense = ParseFloat(col[11].Trim())
            });
        }
    }

    void LoadBossStats()
    {
        if (bossCSV == null) return;
        bossDataList.Clear();
        string[] lines = bossCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 8) continue;
            bossDataList.Add(new BossData
            {
                bossType = ParseInt(col[0].Trim()),
                stage = ParseInt(col[1].Trim(), 1),
                bossWaveLevel = ParseInt(col[2].Trim(), 1),
                hp = ParseFloat(col[3].Trim(), 1000f),
                speed = ParseFloat(col[4].Trim(), 1f),
                defense = ParseFloat(col[5].Trim()),
                reward = ParseInt(col[6].Trim(), 1),
                forceDamageOne = col[7].Trim() == "1"
            });
        }
    }

    void LoadUpgradeStats()
    {
        if (upgradeCSV == null) return;
        upgradeDataList.Clear();
        upgradeDataMap.Clear();
        string[] lines = upgradeCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 3) continue;
            UpgradeData data = new UpgradeData
            {
                upgradeType = col[0].Trim(),
                costPerLevel = ParseInt(col[1].Trim(), 1),
                bonusPerLevel = ParseFloat(col[2].Trim(), 1f)
            };
            upgradeDataList.Add(data);
            upgradeDataMap[data.upgradeType] = data;
        }
    }

    void LoadGameSettings()
    {
        GameSettings = new GameSettingsData();
        if (gameSettingsCSV == null) return;
        Dictionary<string, string> kvMap = new Dictionary<string, string>();
        string[] lines = gameSettingsCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 2) continue;
            kvMap[col[0].Trim()] = col[1].Trim();
        }
        if (kvMap.TryGetValue("startingCoins", out string v)) GameSettings.startingCoins = ParseInt(v, GameSettings.startingCoins);
        if (kvMap.TryGetValue("spawnCost", out v)) GameSettings.spawnCost = ParseInt(v, GameSettings.spawnCost);
        if (kvMap.TryGetValue("anvilCost", out v)) GameSettings.anvilCost = ParseInt(v, GameSettings.anvilCost);
        if (kvMap.TryGetValue("specialSpawnCost", out v)) GameSettings.specialSpawnCost = ParseInt(v, GameSettings.specialSpawnCost);
        if (kvMap.TryGetValue("specialSpawnMinTier", out v)) GameSettings.specialSpawnMinTier = ParseInt(v, GameSettings.specialSpawnMinTier);
        if (kvMap.TryGetValue("specialSpawnMaxTier", out v)) GameSettings.specialSpawnMaxTier = ParseInt(v, GameSettings.specialSpawnMaxTier);
        if (kvMap.TryGetValue("baseCharacterLimit", out v)) GameSettings.baseCharacterLimit = ParseInt(v, GameSettings.baseCharacterLimit);
        if (kvMap.TryGetValue("hpScalePerStage", out v)) GameSettings.hpScalePerStage = ParseFloat(v, GameSettings.hpScalePerStage);
        if (kvMap.TryGetValue("defenseScalePerStage", out v)) GameSettings.defenseScalePerStage = ParseFloat(v, GameSettings.defenseScalePerStage);
        if (kvMap.TryGetValue("rewardScalePerStage", out v)) GameSettings.rewardScalePerStage = ParseFloat(v, GameSettings.rewardScalePerStage);
        List<int> tierUnlockList = new List<int>();
        for (int i = 1; i <= 10; i++)
        {
            if (kvMap.TryGetValue($"tierUnlockCost_{i}", out v)) tierUnlockList.Add(ParseInt(v));
            else break;
        }
        if (tierUnlockList.Count > 0) GameSettings.tierUnlockCosts = tierUnlockList.ToArray();
        List<float> tierWeightList = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            if (kvMap.TryGetValue($"tierSpawnWeight_{i}", out v)) tierWeightList.Add(ParseFloat(v));
            else break;
        }
        if (tierWeightList.Count > 0) GameSettings.tierSpawnWeights = tierWeightList.ToArray();
        List<float> specialWeightList = new List<float>();
        for (int i = 1; i <= 10; i++)
        {
            if (kvMap.TryGetValue($"specialTierSpawnWeight_{i}", out v)) specialWeightList.Add(ParseFloat(v));
            else break;
        }
        if (specialWeightList.Count > 0) GameSettings.specialTierSpawnWeights = specialWeightList.ToArray();
    }

    void LoadSpecialMonsterStats()
    {
        specialMonsterDataList.Clear();
        if (specialMonsterCSV == null) return;
        string[] lines = specialMonsterCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 6) continue;
            specialMonsterDataList.Add(new SpecialMonsterStageData
            {
                killCount = ParseInt(col[0].Trim()),
                hp = ParseFloat(col[1].Trim(), 500f),
                speed = ParseFloat(col[2].Trim(), 1.5f),
                lifetime = ParseFloat(col[3].Trim(), 20f),
                coinReward = ParseInt(col[4].Trim(), 3),
                cooldown = ParseFloat(col[5].Trim(), 30f),
                defense = col.Length >= 7 ? ParseFloat(col[6].Trim(), 0f) : 0f
            });
        }
    }

    void LoadAnvilSettings()
    {
        anvilRangeList.Clear();
        if (anvilSettingsCSV == null) return;
        string[] lines = anvilSettingsCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 4) continue;
            if (!System.Enum.TryParse(col[0].Trim(), out AnvilType type)) continue;
            anvilRangeList.Add(new AnvilRangeData
            {
                type = type,
                stage = ParseInt(col[1].Trim(), 1),
                min = ParseFloat(col[2].Trim()),
                max = ParseFloat(col[3].Trim(), 10f)
            });
        }
    }

    public SpecialMonsterStageData GetSpecialMonsterData(int killCount)
    {
        SpecialMonsterStageData best = null;
        int bestKill = -1;
        foreach (SpecialMonsterStageData data in specialMonsterDataList)
        {
            if (data.killCount <= killCount && data.killCount > bestKill)
            {
                bestKill = data.killCount;
                best = data;
            }
        }
        if (best == null && specialMonsterDataList.Count > 0)
            best = specialMonsterDataList[0];
        return best;
    }

    void LoadSpawnCostData()
    {
        spawnCostDataList.Clear();
        if (spawnCostCSV == null) return;
        string[] lines = spawnCostCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 2) continue;
            spawnCostDataList.Add(new SpawnCostData
            {
                spawnCount = ParseInt(col[0].Trim()),
                cost       = ParseInt(col[1].Trim(), 20)
            });
        }
    }

    public int GetSpawnCost(int spawnCount)
    {
        int result = GameSettings?.spawnCost ?? 20;
        foreach (SpawnCostData data in spawnCostDataList)
            if (spawnCount >= data.spawnCount) result = data.cost;
        return result;
    }

    public AnvilRangeData GetAnvilRange(AnvilType type, int stage)
    {
        AnvilRangeData best = null;
        int bestStage = 0;
        foreach (AnvilRangeData data in anvilRangeList)
        {
            if (data.type != type) continue;
            if (data.stage <= stage && data.stage > bestStage)
            {
                bestStage = data.stage;
                best = data;
            }
        }
        return best;
    }

    public UpgradeData GetUpgradeData(string upgradeType)
    {
        upgradeDataMap.TryGetValue(upgradeType, out UpgradeData data);
        return data;
    }

    public RoundData GetRoundData(int round, int stage)
    {
        foreach (RoundData data in roundDataList)
            if (data.stage == stage && round >= data.waveStart && round <= data.waveEnd) return data;
        RoundData fallback = null;
        int maxStage = 0;
        foreach (RoundData data in roundDataList)
            if (data.stage > maxStage) { maxStage = data.stage; fallback = data; }
        return fallback;
    }

    public int GetMaxStage()
    {
        int max = 1;
        foreach (RoundData rd in roundDataList)
            if (rd.stage > max) max = rd.stage;
        return max;
    }

    public int GetStageEndRound(int stage)
    {
        int maxWave = 0;
        foreach (RoundData rd in roundDataList)
            if (rd.stage == stage && rd.waveEnd > maxWave) maxWave = rd.waveEnd;
        return maxWave == 0 ? 50 : maxWave;
    }

    public BossData GetBossDataByTypeStageLevel(int bossType, int stage, int waveLevel)
    {
        foreach (BossData data in bossDataList)
            if (data.bossType == bossType && data.stage == stage && data.bossWaveLevel == waveLevel) return data;
        return null;
    }

    public BossData GetMaxStageBossData(int bossType, int waveLevel)
    {
        BossData maxData = null;
        int maxStage = 0;
        foreach (BossData data in bossDataList)
        {
            if (data.bossType == bossType && data.bossWaveLevel == waveLevel && data.stage > maxStage)
            {
                maxStage = data.stage;
                maxData = data;
            }
        }
        return maxData;
    }

    CharacterData FindCharacterData(string name)
    {
        if (characterDataMap.TryGetValue(name, out CharacterData data)) return data;
        return null;
    }
}