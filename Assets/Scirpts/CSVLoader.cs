using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundData
{
    public int waveStart;
    public int waveEnd;
    public int stage;
    public float baseHp;
    public float hpIncrement;
    public float spawnDelay;
    public float spawnDelayDecrement;
    public float enemySpeed;
    public int maxEnemyCount;
    public float roundDuration;
    public int coinsPerKill;
}

[System.Serializable]
public class BossData
{
    public int bossWave;
    public float hp;
    public float speed;
    public int reward;
}

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader Instance { get; private set; }

    [Header("CSV Files")]
    public TextAsset characterCSV;
    public TextAsset passiveCSV;
    public TextAsset roundCSV;
    public TextAsset bossCSV;

    [Header("Character Data List")]
    public CharacterData[] characterDataList;

    public List<RoundData> roundDataList = new List<RoundData>();
    public List<BossData> bossDataList = new List<BossData>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadCharacterStats();
        LoadPassiveStats();
        LoadRoundStats();
        LoadBossStats();
    }

    void LoadCharacterStats()
    {
        if (characterCSV == null) { Debug.LogWarning("[CSVLoader] characters.csv not found"); return; }
        string[] lines = characterCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 8) continue;
            string characterName = col[0].Trim();
            CharacterData data = FindCharacterData(characterName);
            if (data == null) continue;
            data.tier = int.Parse(col[1].Trim());
            data.unitTag = col[2].Trim();
            data.attackDamage = float.Parse(col[3].Trim());
            data.attackCooldown = float.Parse(col[4].Trim());
            data.attackRange = float.Parse(col[5].Trim());
            data.upgradeCost = int.Parse(col[6].Trim());
            data.sellPrice = int.Parse(col[7].Trim());
        }
        Debug.Log("[CSVLoader] Character stats loaded");
    }

    void LoadPassiveStats()
    {
        if (passiveCSV == null) { Debug.LogWarning("[CSVLoader] passives.csv not found"); return; }
        foreach (CharacterData data in characterDataList)
            if (data != null) data.passives.Clear();
        string[] lines = passiveCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 5) continue;
            string characterName = col[0].Trim();
            CharacterData data = FindCharacterData(characterName);
            if (data == null) continue;
            PassiveEntry entry = new PassiveEntry();
            System.Enum.TryParse(col[1].Trim(), out entry.passiveType);
            entry.passiveValue = float.Parse(col[2].Trim());
            entry.passiveSecondValue = float.Parse(col[3].Trim());
            entry.passiveDuration = float.Parse(col[4].Trim());
            data.passives.Add(entry);
        }
        Debug.Log("[CSVLoader] Passive stats loaded");
    }

    void LoadRoundStats()
    {
        if (roundCSV == null) { Debug.LogWarning("[CSVLoader] rounds.csv not found"); return; }
        roundDataList.Clear();
        string[] lines = roundCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 11) continue;
            RoundData data = new RoundData();
            data.waveStart = int.Parse(col[0].Trim());
            data.waveEnd = int.Parse(col[1].Trim());
            data.stage = int.Parse(col[2].Trim());
            data.baseHp = float.Parse(col[3].Trim());
            data.hpIncrement = float.Parse(col[4].Trim());
            data.spawnDelay = float.Parse(col[5].Trim());
            data.spawnDelayDecrement = float.Parse(col[6].Trim());
            data.enemySpeed = float.Parse(col[7].Trim());
            data.maxEnemyCount = int.Parse(col[8].Trim());
            data.roundDuration = float.Parse(col[9].Trim());
            data.coinsPerKill = int.Parse(col[10].Trim());
            roundDataList.Add(data);
        }
        Debug.Log("[CSVLoader] Round stats loaded");
    }
    void LoadBossStats()
    {
        if (bossCSV == null) { Debug.LogWarning("[CSVLoader] boss.csv not found"); return; }
        bossDataList.Clear();
        string[] lines = bossCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 4) continue;
            BossData data = new BossData();
            data.bossWave = int.Parse(col[0].Trim());
            data.hp = float.Parse(col[1].Trim());
            data.speed = float.Parse(col[2].Trim());
            data.reward = int.Parse(col[3].Trim());
            bossDataList.Add(data);
        }
        Debug.Log("[CSVLoader] Boss stats loaded");
    }

    public RoundData GetRoundData(int round)
    {
        foreach (RoundData data in roundDataList)
            if (round >= data.waveStart && round <= data.waveEnd) return data;
        return roundDataList.Count > 0 ? roundDataList[roundDataList.Count - 1] : null;
    }

    public BossData GetBossData(int wave)
    {
        foreach (BossData data in bossDataList)
            if (data.bossWave == wave) return data;
        return null;
    }

    CharacterData FindCharacterData(string characterName)
    {
        foreach (CharacterData data in characterDataList)
            if (data != null && data.characterName == characterName) return data;
        Debug.LogWarning($"[CSVLoader] '{characterName}' CharacterData not found");
        return null;
    }
}