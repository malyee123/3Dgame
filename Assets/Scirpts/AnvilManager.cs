using System.Collections.Generic;
using UnityEngine;

public enum AnvilType
{
    AttackDamage,
    AttackSpeed,
    CharacterLimit,
    EnemyLimit,
    BossTime,
    ArmorPenetration
}

[System.Serializable]
public class AnvilData
{
    public string anvilName;
    public string description;
    public string summary;
    public AnvilType type;
    public float value;
}

public class AnvilManager : MonoBehaviour
{
    public static AnvilManager Instance { get; private set; }

    private float bonusAttackDamage = 0f;
    private float bonusAttackSpeed = 0f;
    private int bonusCharacterLimit = 0;
    private int bonusEnemyLimit = 0;
    private float bonusBossTime = 0f;
    private float bonusArmorPenetration = 0f;

    public float BonusAttackDamage => bonusAttackDamage;
    public float BonusAttackSpeed => bonusAttackSpeed;
    public int BonusCharacterLimit => bonusCharacterLimit;
    public int BonusEnemyLimit => bonusEnemyLimit;
    public float BonusBossTime => bonusBossTime;
    public float BonusArmorPenetration => bonusArmorPenetration;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public AnvilData[] GetRandomAnvils()
    {
        List<AnvilData> allAnvils = GenerateAnvilPool();
        List<AnvilData> result = new List<AnvilData>();
        List<int> usedIndices = new List<int>();
        int count = Mathf.Min(3, allAnvils.Count);
        while (result.Count < count)
        {
            int idx = Random.Range(0, allAnvils.Count);
            if (usedIndices.Contains(idx)) continue;
            usedIndices.Add(idx);
            result.Add(allAnvils[idx]);
        }
        return result.ToArray();
    }

    List<AnvilData> GenerateAnvilPool()
    {
        List<AnvilData> pool = new List<AnvilData>();

        float dmg = Mathf.Round(Random.Range(5f, 15f));
        pool.Add(new AnvilData { anvilName = "강화된 무기", description = $"모든 유닛의 공격력이 {dmg}% 증가합니다.", summary = $"공격력 +{dmg}%", type = AnvilType.AttackDamage, value = dmg });

        float spd = Mathf.Round(Random.Range(50f, 50f));
        pool.Add(new AnvilData { anvilName = "신속의 부적", description = $"모든 유닛의 공격속도가 {spd}% 증가합니다.", summary = $"공격속도 +{spd}%", type = AnvilType.AttackSpeed, value = spd });

        int charLimit = Random.Range(1, 3);
        pool.Add(new AnvilData { anvilName = "지원 병력", description = $"최대 캐릭터 수가 {charLimit}마리 증가합니다.", summary = $"캐릭터 제한 +{charLimit}", type = AnvilType.CharacterLimit, value = charLimit });

        int enemyLimit = Random.Range(10, 31);
        pool.Add(new AnvilData { anvilName = "철벽 방어", description = $"게임오버 적 인원이 {enemyLimit} 증가합니다.", summary = $"적 인원 제한 +{enemyLimit}", type = AnvilType.EnemyLimit, value = enemyLimit });

        float bossTime = Mathf.Round(Random.Range(10f, 31f));
        pool.Add(new AnvilData { anvilName = "여유로운 전투", description = $"보스전 제한 시간이 {bossTime}초 증가합니다.", summary = $"보스전 시간 +{bossTime}초", type = AnvilType.BossTime, value = bossTime });

        float armor = Mathf.Round(Random.Range(5f, 15f));
        pool.Add(new AnvilData { anvilName = "방어구 파괴", description = $"적의 방어력이 {armor}% 감소합니다.", summary = $"방관 +{armor}%", type = AnvilType.ArmorPenetration, value = armor });

        return pool;
    }

    public void ApplyAnvil(AnvilData data)
    {
        switch (data.type)
        {
            case AnvilType.AttackDamage:
                bonusAttackDamage += data.value;
                ApplyToAllUnits(data.value, 0f);
                break;
            case AnvilType.AttackSpeed:
                bonusAttackSpeed += data.value;
                ApplyToAllUnits(0f, data.value);
                break;
            case AnvilType.CharacterLimit:
                bonusCharacterLimit += (int)data.value;
                if (PlayerSpawner.Instance != null)
                    PlayerSpawner.Instance.AddCharacterLimit((int)data.value);
                break;
            case AnvilType.EnemyLimit:
                bonusEnemyLimit += (int)data.value;
                if (GameManager.Instance != null)
                    GameManager.Instance.AddEnemyLimit((int)data.value);
                break;
            case AnvilType.BossTime:
                bonusBossTime += data.value;
                if (GameManager.Instance != null)
                    GameManager.Instance.AddBossTime(data.value);
                break;
            case AnvilType.ArmorPenetration:
                bonusArmorPenetration += data.value;
                ApplyArmorPenetrationToExisting();
                if (EnemySpawner.Instance != null)
                    EnemySpawner.Instance.armorBreakerMultiplier =
                        Mathf.Max(0f, EnemySpawner.Instance.armorBreakerMultiplier - data.value / 100f);
                break;
        }
        PassiveManager.Instance?.RecalculatePassives();
    }

    void ApplyToAllUnits(float dmg, float spd)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(dmg, spd);
    }

    void ApplyArmorPenetrationToExisting()
    {
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null) eh.ApplyArmorBreaker(bonusArmorPenetration / 100f);
    }

    public void ResetAnvils()
    {
        bonusAttackDamage = 0f;
        bonusAttackSpeed = 0f;
        bonusCharacterLimit = 0;
        bonusEnemyLimit = 0;
        bonusBossTime = 0f;
        bonusArmorPenetration = 0f;
    }
}