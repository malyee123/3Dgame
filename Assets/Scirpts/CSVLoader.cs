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

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader Instance { get; private set; }

    [Header("CSV Files")]
    public TextAsset characterCSV, passiveCSV, roundCSV, bossCSV, upgradeCSV;

    [Header("Character Data List")]
    public CharacterData[] characterDataList;

    public List<RoundData> roundDataList = new List<RoundData>();
    public List<BossData> bossDataList = new List<BossData>();
    public List<UpgradeData> upgradeDataList = new List<UpgradeData>();

    private Dictionary<string, CharacterData> characterDataMap = new Dictionary<string, CharacterData>();
    private Dictionary<string, UpgradeData> upgradeDataMap = new Dictionary<string, UpgradeData>();

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
    }

    void LoadCharacterStats()
    {
        if (characterCSV == null) { return; }
        string[] lines = characterCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 9) continue;
            CharacterData data = FindCharacterData(col[0].Trim());
            if (data == null) continue;
            data.tier = int.Parse(col[1].Trim());
            data.unitTag = col[2].Trim();
            data.attackDamage = float.Parse(col[3].Trim());
            data.attackSpeed = float.Parse(col[4].Trim());
            data.attackRange = float.Parse(col[5].Trim());
            data.upgradeCost = int.Parse(col[6].Trim());
            data.sellPrice = int.Parse(col[7].Trim());
            data.attackHitDelay = float.Parse(col[8].Trim());
        }
    }

    void LoadPassiveStats()
    {
        if (passiveCSV == null) { return; }
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
            entry.passiveValue = float.Parse(col[2].Trim());
            entry.passiveSecondValue = float.Parse(col[3].Trim());
            entry.passiveDuration = float.Parse(col[4].Trim());
            data.passives.Add(entry);
        }
    }

    void LoadRoundStats()
    {
        if (roundCSV == null) { return; }
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
                waveStart = int.Parse(col[0].Trim()),
                waveEnd = int.Parse(col[1].Trim()),
                stage = int.Parse(col[2].Trim()),
                baseHp = float.Parse(col[3].Trim()),
                hpIncrement = float.Parse(col[4].Trim()),
                spawnDelay = float.Parse(col[5].Trim()),
                spawnDelayDecrement = float.Parse(col[6].Trim()),
                enemySpeed = float.Parse(col[7].Trim()),
                maxEnemyCount = int.Parse(col[8].Trim()),
                roundDuration = float.Parse(col[9].Trim()),
                coinsPerKill = int.Parse(col[10].Trim()),
                enemyDefense = float.Parse(col[11].Trim())
            });
        }
    }

    void LoadBossStats()
    {
        if (bossCSV == null) { return; }
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
                bossType = int.Parse(col[0].Trim()),
                stage = int.Parse(col[1].Trim()),
                bossWaveLevel = int.Parse(col[2].Trim()),
                hp = float.Parse(col[3].Trim()),
                speed = float.Parse(col[4].Trim()),
                reward = int.Parse(col[5].Trim()),
                defense = float.Parse(col[6].Trim()),
                forceDamageOne = int.Parse(col[7].Trim()) == 1
            });
        }
    }

    void LoadUpgradeStats()
    {
        if (upgradeCSV == null) { return; }
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
                costPerLevel = int.Parse(col[1].Trim()),
                bonusPerLevel = float.Parse(col[2].Trim())
            };
            upgradeDataList.Add(data);
            upgradeDataMap[data.upgradeType] = data;
        }
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