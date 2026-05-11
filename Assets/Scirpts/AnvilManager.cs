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
    public AnvilType type;
    public float value;
    public Sprite sprite;
}

[System.Serializable]
public class AnvilSpriteSet
{
    public Sprite[] stageSprites;
}

public class AnvilManager : MonoBehaviour
{
    public static AnvilManager Instance { get; private set; }

    [Header("Anvil Sprites (인덱스: 0=AttackDamage, 1=AttackSpeed, 2=CharacterLimit, 3=EnemyLimit, 4=BossTime, 5=ArmorPenetration)")]
    public AnvilSpriteSet[] anvilSpriteSets = new AnvilSpriteSet[6];

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

    private int cachedStage = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 모루 UI 열릴 때 스테이지를 한 번 캐싱 → 풀 생성 동안 일관된 스테이지 사용
    public void CacheCurrentStage()
    {
        cachedStage = GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
    }

    float GetRangeValue(AnvilType type)
    {
        if (CSVLoader.Instance != null)
        {
            AnvilRangeData range = CSVLoader.Instance.GetAnvilRange(type, cachedStage);
            if (range != null)
            {
                if (type == AnvilType.CharacterLimit || type == AnvilType.EnemyLimit)
                    return Mathf.Round(Random.Range(range.min, range.max + 1));
                return Mathf.Round(Random.Range(range.min, range.max));
            }
        }
        return type == AnvilType.CharacterLimit ? Random.Range(1, 3) :
               type == AnvilType.EnemyLimit ? Random.Range(10, 31) :
               type == AnvilType.BossTime ? Mathf.Round(Random.Range(10f, 31f)) :
               Mathf.Round(Random.Range(5f, 15f));
    }

    Sprite GetSprite(AnvilType type)
    {
        int typeIndex = (int)type;
        if (anvilSpriteSets == null || typeIndex >= anvilSpriteSets.Length) return null;
        AnvilSpriteSet set = anvilSpriteSets[typeIndex];
        if (set == null || set.stageSprites == null || set.stageSprites.Length == 0) return null;
        int spriteIndex = Mathf.Clamp(cachedStage - 1, 0, set.stageSprites.Length - 1);
        return set.stageSprites[spriteIndex];
    }

    public AnvilData[] GetRandomAnvils()
    {
        List<AnvilData> pool = new List<AnvilData>
        {
            new AnvilData { type = AnvilType.AttackDamage,     value = GetRangeValue(AnvilType.AttackDamage),     sprite = GetSprite(AnvilType.AttackDamage) },
            new AnvilData { type = AnvilType.AttackSpeed,      value = GetRangeValue(AnvilType.AttackSpeed),      sprite = GetSprite(AnvilType.AttackSpeed) },
            new AnvilData { type = AnvilType.CharacterLimit,   value = GetRangeValue(AnvilType.CharacterLimit),   sprite = GetSprite(AnvilType.CharacterLimit) },
            new AnvilData { type = AnvilType.EnemyLimit,       value = GetRangeValue(AnvilType.EnemyLimit),       sprite = GetSprite(AnvilType.EnemyLimit) },
            new AnvilData { type = AnvilType.BossTime,         value = GetRangeValue(AnvilType.BossTime),         sprite = GetSprite(AnvilType.BossTime) },
            new AnvilData { type = AnvilType.ArmorPenetration, value = GetRangeValue(AnvilType.ArmorPenetration), sprite = GetSprite(AnvilType.ArmorPenetration) },
        };
        List<AnvilData> result = new List<AnvilData>();
        List<int> usedIndices = new List<int>();
        int count = Mathf.Min(3, pool.Count);
        while (result.Count < count)
        {
            int idx = Random.Range(0, pool.Count);
            if (usedIndices.Contains(idx)) continue;
            usedIndices.Add(idx);
            result.Add(pool[idx]);
        }
        return result.ToArray();
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
