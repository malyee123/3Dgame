using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundData
{
    public int stage;
    public float roundDuration;
    public int maxEnemyCount;
    public float enemyHp;
    public float enemySpawnDelay;
    public float enemySpeed;
    public int roundsPerStage;
}

public class CSVLoader : MonoBehaviour
{
    public static CSVLoader Instance { get; private set; }

    [Header("CSV Files")]
    public TextAsset characterCSV;
    public TextAsset passiveCSV;
    public TextAsset enemyCSV;
    public TextAsset roundCSV;

    [Header("Character Data List")]
    public CharacterData[] characterDataList;

    [Header("Enemy Base Stats")]
    public float baseEnemyHp = 50f;
    public float hpIncrement = 20f;
    public float baseSpawnDelay = 1.0f;
    public float spawnDelayDecrement = 0.1f;

    public List<RoundData> roundDataList = new List<RoundData>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadCharacterStats();
        LoadPassiveStats();
        LoadEnemyStats();
        LoadRoundStats();
    }

    void LoadCharacterStats()
    {
        if (characterCSV == null) { Debug.LogWarning("[CSVLoader] characters.csv 없음"); return; }
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
        Debug.Log("[CSVLoader] 캐릭터 스탯 로드 완료");
    }

    void LoadPassiveStats()
    {
        if (passiveCSV == null) { Debug.LogWarning("[CSVLoader] passives.csv 없음"); return; }
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
        Debug.Log("[CSVLoader] 패시브 스탯 로드 완료");
    }

    void LoadEnemyStats()
    {
        if (enemyCSV == null) { Debug.LogWarning("[CSVLoader] enemies.csv 없음"); return; }
        string[] lines = enemyCSV.text.Split('\n');
        if (lines.Length < 2) return;
        string[] col = lines[1].Trim().Split(',');
        if (col.Length < 4) return;
        baseEnemyHp = float.Parse(col[0].Trim());
        hpIncrement = float.Parse(col[1].Trim());
        baseSpawnDelay = float.Parse(col[2].Trim());
        spawnDelayDecrement = float.Parse(col[3].Trim());
        Debug.Log("[CSVLoader] 적 스탯 로드 완료");
    }

    void LoadRoundStats()
    {
        if (roundCSV == null) { Debug.LogWarning("[CSVLoader] rounds.csv 없음"); return; }
        roundDataList.Clear();
        string[] lines = roundCSV.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] col = line.Split(',');
            if (col.Length < 7) continue;
            RoundData data = new RoundData();
            data.stage = int.Parse(col[0].Trim());
            data.roundDuration = float.Parse(col[1].Trim());
            data.maxEnemyCount = int.Parse(col[2].Trim());
            data.enemyHp = float.Parse(col[3].Trim());
            data.enemySpawnDelay = float.Parse(col[4].Trim());
            data.enemySpeed = float.Parse(col[5].Trim());
            data.roundsPerStage = int.Parse(col[6].Trim());
            roundDataList.Add(data);
        }
        Debug.Log("[CSVLoader] 라운드 스탯 로드 완료");
    }

    public int GetStage(int round)
    {
        if (roundDataList.Count == 0) return 1;
        int accumulated = 0;
        for (int i = 0; i < roundDataList.Count; i++)
        {
            accumulated += roundDataList[i].roundsPerStage;
            if (round <= accumulated) return roundDataList[i].stage;
        }
        RoundData last = roundDataList[roundDataList.Count - 1];
        int extra = round - accumulated;
        return last.stage + (extra / last.roundsPerStage) + 1;
    }

    public RoundData GetRoundData(int round)
    {
        if (roundDataList.Count == 0) return null;
        int accumulated = 0;
        for (int i = 0; i < roundDataList.Count; i++)
        {
            accumulated += roundDataList[i].roundsPerStage;
            if (round <= accumulated) return roundDataList[i];
        }
        return roundDataList[roundDataList.Count - 1];
    }

    CharacterData FindCharacterData(string characterName)
    {
        foreach (CharacterData data in characterDataList)
            if (data != null && data.characterName == characterName) return data;
        Debug.LogWarning($"[CSVLoader] '{characterName}' CharacterData 못 찾음");
        return null;
    }
}