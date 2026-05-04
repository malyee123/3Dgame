using System.Collections.Generic;
using UnityEngine;

public enum AugmentType
{
    BasicTraining, EmergencyFund, LightStep, HuntersSense, SmallPouch,
    LuckyDay, ArmorBreaker, VeteranInstructor, Teamwork, InvestmentExpert,
    GiantSlayer, TheBigOne, Executioner, TwinsOfChaos
}

[System.Serializable]
public class AugmentData
{
    public string augmentName;
    public string description;
    public string summary;
    public AugmentType type;
    public Sprite sprite;
}

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

    [Header("Augment Sprites (순서: 기초훈련/비상금/가벼운발걸음/현상금사냥꾼/작은주머니/운빨좋은날/방어구파쇄기/베테랑교관/백지장도/투자전문가/거인학살자/딱대/사형집행인/두사람최강)")]
    public Sprite[] augmentSprites = new Sprite[14];

    private bool hasBossSpecialCoinDouble = false;
    private bool hasGiantSlayer = false;
    private bool hasTheBigOne = false;
    private bool hasExecutioner = false;
    private bool hasTwinsOfChaos = false;
    private bool hasLuckyDay = false;

    public bool HasGiantSlayer => hasGiantSlayer;
    public bool HasTheBigOne => hasTheBigOne;
    public bool HasExecutioner => hasExecutioner;
    public bool HasTwinsOfChaos => hasTwinsOfChaos;
    public bool HasBossSpecialCoinDouble => hasBossSpecialCoinDouble;
    public bool HasLuckyDay => hasLuckyDay;

    private int freeSpawnCount = 0;
    public int FreeSpawnCount => freeSpawnCount;
    public void UseFreeSpawn() { if (freeSpawnCount > 0) freeSpawnCount--; }

    private List<string> activeSummaries = new List<string>();
    private List<AugmentType> selectedAugments = new List<AugmentType>();

    private AugmentData[] allAugments;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        allAugments = new AugmentData[]
        {
            new AugmentData { augmentName = "기초 훈련", description = "일반, 고급 등급 유닛의 공격력이 30% 증가하고 즉시 200골드를 획득합니다.", summary = "1~2티어 공격력 +30% / 즉시 +200골드", type = AugmentType.BasicTraining },
            new AugmentData { augmentName = "비상금", description = "즉시 600 골드 및 특수 코인 6개를 획득합니다.", summary = "즉시 +600골드 / 특수코인 +6", type = AugmentType.EmergencyFund },
            new AugmentData { augmentName = "가벼운 발걸음", description = "모든 유닛의 공격 속도가 15%, 사거리가 1 증가합니다.", summary = "공격속도 +15% / 사거리 +1", type = AugmentType.LightStep },
            new AugmentData { augmentName = "현상금 사냥꾼", description = "적 처치 시 5% 확률로 10골드 획득\n*소환 비용 20% 증가", summary = "킬 시 5%확률 +10골드 / 소환비용 +20%", type = AugmentType.HuntersSense },
            new AugmentData { augmentName = "작은 주머니", description = "소환 비용이 15% 감소하고 즉시 300 골드를 획득합니다.", summary = "소환비용 -15% / 즉시 +300골드", type = AugmentType.SmallPouch },
            new AugmentData { augmentName = "운빨 좋은 날", description = "유닛 소환 시 고급 이상 등급이 나올 확률이 1.5배 증가하고 무료 소환권을 3회 획득합니다.", summary = "고급이상 확률 1.5배 / 무료소환 3회", type = AugmentType.LuckyDay },
            new AugmentData { augmentName = "방어구 파쇄기", description = "적의 방어력이 15% 하락하고 모든 아군 유닛의 공격력이 5% 증가합니다.", summary = "적 방어력 -15% / 공격력 +5%", type = AugmentType.ArmorBreaker },
            new AugmentData { augmentName = "베테랑 교관", description = "유닛 업그레이드 비용이 50% 감소합니다.", summary = "업그레이드비용 -50%", type = AugmentType.VeteranInstructor },
            new AugmentData { augmentName = "백지장도 맞들면 낫다", description = "희귀 등급 유닛을 랜덤으로 5명 소환합니다.", summary = "희귀유닛 5명 소환", type = AugmentType.Teamwork },
            new AugmentData { augmentName = "투자 전문가", description = "보스 및 특수 몬스터 처치 시 특수 코인을 2배로 획득합니다.", summary = "특수코인 획득량 2배", type = AugmentType.InvestmentExpert },
            new AugmentData { augmentName = "거인 학살자", description = "보스 및 특수 몬스터에게 입히는 피해량이 100% 증가합니다.\n*일반 몬스터 피해 20% 감소", summary = "보스/특수 피해 +100% / 일반 피해 -20%", type = AugmentType.GiantSlayer },
            new AugmentData { augmentName = "딱 대", description = "레전드 유닛이 10% 확률로 5배의 피해를 입힙니다.\n*레전드 유닛 공격속도 15% 감소", summary = "레전드 10% 확률 5배 / 공속 -15%", type = AugmentType.TheBigOne },
            new AugmentData { augmentName = "사형 집행인", description = "보스 몬스터의 체력이 10% 이하가 되면 즉시 처형시킵니다.\n*보스 최대 체력 20% 증가", summary = "보스 10% 처형 / 보스 체력 +20%", type = AugmentType.Executioner },
            new AugmentData { augmentName = "두 사람은 문제아지만 최강", description = "동일한 레전드 유닛을 2마리 이상 배치하면, 해당 유닛들의 특수 능력 발동 확률이 2배가 됩니다.\n*유닛 배치 수 -2", summary = "동일 레전드 2마리시 패시브 2배 / 배치 수 -2", type = AugmentType.TwinsOfChaos },
        };

        for (int i = 0; i < allAugments.Length && i < augmentSprites.Length; i++)
            allAugments[i].sprite = augmentSprites[i];
    }

    public AugmentData[] GetRandomAugments()
    {
        List<AugmentData> pool = new List<AugmentData>();
        foreach (AugmentData data in allAugments)
            if (!selectedAugments.Contains(data.type))
                pool.Add(data);

        if (pool.Count == 0) return new AugmentData[0];

        int count = Mathf.Min(3, pool.Count);
        List<AugmentData> result = new List<AugmentData>();
        List<int> usedIndices = new List<int>();
        while (result.Count < count)
        {
            int idx = Random.Range(0, pool.Count);
            if (usedIndices.Contains(idx)) continue;
            usedIndices.Add(idx);
            result.Add(pool[idx]);
        }
        return result.ToArray();
    }

    public void ApplyAugment(AugmentData data)
    {
        selectedAugments.Add(data.type);
        activeSummaries.Add(data.summary);

        switch (data.type)
        {
            case AugmentType.BasicTraining:
                ApplyBasicTraining();
                CoinManager.Instance?.AddCoins(200);
                break;
            case AugmentType.EmergencyFund:
                CoinManager.Instance?.AddCoins(600);
                SpecialCoinManager.Instance?.AddSpecialCoins(6);
                break;
            case AugmentType.LightStep:
                ApplyRangeAndSpeedToAll(1f, 15f);
                break;
            case AugmentType.HuntersSense:
                if (CoinManager.Instance != null) CoinManager.Instance.spawnCostMultiplier = Mathf.Min(10f, CoinManager.Instance.spawnCostMultiplier + 0.2f);
                if (CoinManager.Instance != null) CoinManager.Instance.huntersSenseActive = true;
                break;
            case AugmentType.SmallPouch:
                if (CoinManager.Instance != null) CoinManager.Instance.spawnCostMultiplier = Mathf.Max(0.1f, CoinManager.Instance.spawnCostMultiplier - 0.15f);
                CoinManager.Instance?.AddCoins(300);
                break;
            case AugmentType.LuckyDay:
                hasLuckyDay = true;
                if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.luckyDayMultiplier = 1.5f;
                freeSpawnCount += 3;
                break;
            case AugmentType.ArmorBreaker:
                if (EnemySpawner.Instance != null) EnemySpawner.Instance.armorBreakerMultiplier = Mathf.Max(0f, EnemySpawner.Instance.armorBreakerMultiplier - 0.15f);
                ApplyArmorBreakerToExisting();
                ApplyToAllUnits(5f, 0f);
                break;
            case AugmentType.VeteranInstructor:
                if (MergeManager.Instance != null) MergeManager.Instance.upgradeCostMultiplier = 0.5f;
                break;
            case AugmentType.Teamwork:
                PlayerSpawner.Instance?.SpawnRareUnits(5);
                break;
            case AugmentType.InvestmentExpert:
                hasBossSpecialCoinDouble = true;
                break;
            case AugmentType.GiantSlayer:
                hasGiantSlayer = true;
                ApplyNormalDamagePenaltyToAll(0.8f);
                RefreshAllUnits();
                break;
            case AugmentType.TheBigOne:
                hasTheBigOne = true;
                ApplyLegendSpeedPenalty(15f);
                RefreshAllUnits();
                break;
            case AugmentType.Executioner:
                hasExecutioner = true;
                ApplyBossHpBoost(1.2f);
                break;
            case AugmentType.TwinsOfChaos:
                hasTwinsOfChaos = true;
                if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.AddCharacterLimit(-2);
                RefreshAllUnits();
                break;
        }
        PassiveManager.Instance?.RecalculatePassives();
    }

    void ApplyBasicTraining()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.characterData != null && unit.characterData.tier <= 2)
                unit.ApplyAugmentBonus(30f, 0f);
    }

    void ApplyRangeAndSpeedToAll(float rangeBonus, float speedBonus)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(0f, speedBonus, rangeBonus);
    }

    void ApplyToAllUnits(float damageBonus, float speedBonus)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(damageBonus, speedBonus);
    }

    void ApplyNormalDamagePenaltyToAll(float penalty)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(0f, 0f, 0f, penalty);
    }

    void ApplyLegendSpeedPenalty(float speedPenalty)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.characterData != null && unit.characterData.tier >= 5)
                unit.ApplyAugmentBonus(0f, 0f, 0f, 1f, speedPenalty);
    }

    void ApplyArmorBreakerToExisting()
    {
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null) eh.ApplyArmorBreaker(0.15f);
    }

    void ApplyBossHpBoost(float multiplier)
    {
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null && eh.isBoss) eh.BoostMaxHp(multiplier);
        if (BossManager.Instance != null) BossManager.Instance.bossHpMultiplier *= multiplier;
    }

    void RefreshAllUnits()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.RefreshAugmentState();
    }


    public void ApplyAugmentBonusToUnit(PlayerAttack unit)
    {
        if (unit == null || unit.characterData == null) return;

        if (selectedAugments.Contains(AugmentType.BasicTraining) && unit.characterData.tier <= 2)
            unit.ApplyAugmentBonus(30f, 0f);
        if (selectedAugments.Contains(AugmentType.LightStep))
            unit.ApplyAugmentBonus(0f, 15f, 1f);
        if (selectedAugments.Contains(AugmentType.ArmorBreaker))
            unit.ApplyAugmentBonus(5f, 0f);
        if (selectedAugments.Contains(AugmentType.GiantSlayer))
            unit.ApplyAugmentBonus(0f, 0f, 0f, 0.8f);
        if (selectedAugments.Contains(AugmentType.TheBigOne) && unit.characterData.tier >= 5)
            unit.ApplyAugmentBonus(0f, 0f, 0f, 1f, 15f);
    }
    public string GetActiveAugmentText()
    {
        if (activeSummaries.Count == 0) return "";
        return "[ 증강 ]\n" + string.Join("\n", activeSummaries);
    }

    public void ResetAugments()
    {
        hasBossSpecialCoinDouble = false;
        hasGiantSlayer = false;
        hasTheBigOne = false;
        hasExecutioner = false;
        hasTwinsOfChaos = false;
        hasLuckyDay = false;
        freeSpawnCount = 0;
        activeSummaries.Clear();
        selectedAugments.Clear();
        if (PlayerSpawner.Instance != null) { PlayerSpawner.Instance.sellPriceMultiplier = 1f; PlayerSpawner.Instance.luckyDayMultiplier = 1f; }
        if (CoinManager.Instance != null) { CoinManager.Instance.spawnCostMultiplier = 1f; CoinManager.Instance.huntersSenseActive = false; }
        if (MergeManager.Instance != null) MergeManager.Instance.upgradeCostMultiplier = 1f;
        if (EnemySpawner.Instance != null) EnemySpawner.Instance.armorBreakerMultiplier = 1f;
        if (BossManager.Instance != null) BossManager.Instance.bossHpMultiplier = 1f;
    }
}