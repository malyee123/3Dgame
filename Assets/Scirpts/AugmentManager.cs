using System.Collections.Generic;
using UnityEngine;

public enum AugmentType
{
    BasicTraining, EmergencyFund, LightStep, HuntersSense, SmallPouch,
    LuckyDay, ArmorBreaker, VeteranInstructor, Teamwork, InvestmentExpert,
    GiantSlayer, TheBigOne, Executioner, TwinsOfChaos
}

public enum AugmentGrade { Silver, Gold, Platinum }

[System.Serializable]
public class AugmentData
{
    public string augmentName;
    public string description;
    public string summary;
    public AugmentType type;
    public AugmentGrade grade;
}

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

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

    private List<string> activeSummaries = new List<string>();
    private List<AugmentType> selectedAugments = new List<AugmentType>();

    private readonly AugmentData[] allAugments = new AugmentData[]
    {
        new AugmentData { augmentName = "기초 훈련", description = "일반, 고급 동급 유닛의 공격력이 20% 증가합니다.", summary = "공격력 +20% (1~2티어)", type = AugmentType.BasicTraining, grade = AugmentGrade.Silver },
        new AugmentData { augmentName = "비상금", description = "즉시 300 골드를 획득합니다.", summary = "즉시 골드 +300", type = AugmentType.EmergencyFund, grade = AugmentGrade.Silver },
        new AugmentData { augmentName = "가벼운 발걸음", description = "모든 유닛의 공격 속도가 10% 증가합니다.", summary = "공격속도 +10%", type = AugmentType.LightStep, grade = AugmentGrade.Silver },
        new AugmentData { augmentName = "사냥꾼의 감", description = "유닛 판매 시 얻는 골드가 10% 증가합니다.", summary = "판매금액 +10%", type = AugmentType.HuntersSense, grade = AugmentGrade.Silver },
        new AugmentData { augmentName = "작은 주머니", description = "소환 비용이 10% 감소합니다.", summary = "소환비용 -10%", type = AugmentType.SmallPouch, grade = AugmentGrade.Silver },
        new AugmentData { augmentName = "운빨 좋은 날", description = "유닛 소환 시 고급 이상 등급이 나올 확률이 1.5배 증가합니다.", summary = "고급이상 소환확률 1.5배", type = AugmentType.LuckyDay, grade = AugmentGrade.Gold },
        new AugmentData { augmentName = "방어구 파쇄기", description = "적의 방어력이 15% 감소합니다.", summary = "적 방어력 -15%", type = AugmentType.ArmorBreaker, grade = AugmentGrade.Gold },
        new AugmentData { augmentName = "베테랑 교관", description = "유닛 업그레이드 비용이 50% 감소합니다.", summary = "업그레이드비용 -50%", type = AugmentType.VeteranInstructor, grade = AugmentGrade.Gold },
        new AugmentData { augmentName = "백지장도 맞들면 낫다", description = "희귀 유닛을 5명 소환합니다.", summary = "희귀유닛 5명 소환", type = AugmentType.Teamwork, grade = AugmentGrade.Gold },
        new AugmentData { augmentName = "투자 전문가", description = "보스 및 특수 몬스터 처치 시 특수 코인을 2배로 획득합니다.", summary = "특수코인 획득량 2배", type = AugmentType.InvestmentExpert, grade = AugmentGrade.Gold },
        new AugmentData { augmentName = "거인 학살자", description = "모든 유닛이 보스와 특수 몬스터에게 입히는 피해량이 100% 증가합니다.", summary = "보스/특수 피해량 +100%", type = AugmentType.GiantSlayer, grade = AugmentGrade.Platinum },
        new AugmentData { augmentName = "딱 대", description = "레전드 유닛이 공격할 때마다 10% 확률로 해당 공격의 데미지가 5배로 적용됩니다.", summary = "레전드 10% 확률 5배 피해", type = AugmentType.TheBigOne, grade = AugmentGrade.Platinum },
        new AugmentData { augmentName = "사형 집행인", description = "보스 몬스터의 체력이 10% 미만이 되면 즉시 처형시킵니다.", summary = "보스 체력 10% 미만 처형", type = AugmentType.Executioner, grade = AugmentGrade.Platinum },
        new AugmentData { augmentName = "두 사람은 문제아지만 최강", description = "동일한 레전드 유닛을 2마리 이상 배치하면 해당 유닛의 특수 능력 발동 확률이 2배가 됩니다.", summary = "동일 레전드 2마리시 패시브 2배", type = AugmentType.TwinsOfChaos, grade = AugmentGrade.Platinum },
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    AugmentGrade GetRandomGrade()
    {
        int rand = Random.Range(0, 3);
        if (rand == 0) return AugmentGrade.Silver;
        if (rand == 1) return AugmentGrade.Gold;
        return AugmentGrade.Platinum;
    }

    public AugmentData[] GetRandomAugments()
    {
        AugmentGrade grade = GetRandomGrade();
        List<AugmentData> pool = new List<AugmentData>();
        foreach (AugmentData data in allAugments)
            if (data.grade == grade && !selectedAugments.Contains(data.type))
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
            case AugmentType.BasicTraining: ApplyBasicTraining(); break;
            case AugmentType.EmergencyFund: CoinManager.Instance?.AddCoins(300); break;
            case AugmentType.LightStep: ApplyToAllUnits(0f, 10f); break;
            case AugmentType.HuntersSense:
                if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.sellPriceMultiplier = 1.1f; break;
            case AugmentType.SmallPouch:
                if (CoinManager.Instance != null) CoinManager.Instance.spawnCostMultiplier = 0.9f; break;
            case AugmentType.LuckyDay:
                hasLuckyDay = true;
                if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.luckyDayMultiplier = 1.5f; break;
            case AugmentType.ArmorBreaker:
                if (EnemySpawner.Instance != null) EnemySpawner.Instance.armorBreakerMultiplier = 0.85f;
                ApplyArmorBreakerToExisting(); break;
            case AugmentType.VeteranInstructor:
                if (MergeManager.Instance != null) MergeManager.Instance.upgradeCostMultiplier = 0.5f; break;
            case AugmentType.Teamwork:
                PlayerSpawner.Instance?.SpawnRareUnits(5); break;
            case AugmentType.InvestmentExpert: hasBossSpecialCoinDouble = true; break;
            case AugmentType.GiantSlayer: hasGiantSlayer = true; RefreshAllUnits(); break;
            case AugmentType.TheBigOne: hasTheBigOne = true; RefreshAllUnits(); break;
            case AugmentType.Executioner: hasExecutioner = true; break;
            case AugmentType.TwinsOfChaos: hasTwinsOfChaos = true; RefreshAllUnits(); break;
        }
        PassiveManager.Instance?.RecalculatePassives();
    }

    // 1~2티어(일반/고급) 유닛에만 공격력 20% 적용
    void ApplyBasicTraining()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit.characterData == null) continue;
            if (unit.characterData.tier <= 2)
                unit.ApplyAugmentBonus(20f, 0f);
        }
    }

    void ApplyToAllUnits(float damageBonus, float speedBonus)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(damageBonus, speedBonus);
    }

    void ApplyArmorBreakerToExisting()
    {
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null) eh.ApplyArmorBreaker(0.15f);
    }

    void RefreshAllUnits()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.RefreshAugmentState();
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
        activeSummaries.Clear();
        selectedAugments.Clear();
        if (PlayerSpawner.Instance != null) { PlayerSpawner.Instance.sellPriceMultiplier = 1f; PlayerSpawner.Instance.luckyDayMultiplier = 1f; }
        if (CoinManager.Instance != null) CoinManager.Instance.spawnCostMultiplier = 1f;
        if (MergeManager.Instance != null) MergeManager.Instance.upgradeCostMultiplier = 1f;
        if (EnemySpawner.Instance != null) EnemySpawner.Instance.armorBreakerMultiplier = 1f;
    }
}