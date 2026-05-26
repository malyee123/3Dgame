using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AugmentType
{
    BasicTraining, EmergencyFund, LightStep, HuntersSense, SmallPouch,
    LuckyDay, ArmorBreaker, VeteranInstructor, Teamwork, InvestmentExpert,
    GiantSlayer, TheBigOne, Executioner, TwinsOfChaos,
    SharpBlade, IceShard, Recycle, SharpEye, Vulnerability,
    MergeAccel, PassiveResonance, Dominator, DoomArmy,
    SpeedBurst, WeakPoint, AllIn, FateDice, BouncingArrow
}

[System.Serializable]
public class AugmentData
{
    public string augmentName;
    public string description;
    public string summary;
    public AugmentType type;
    public Sprite sprite;
    public int requiredTier;
}

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

    [Header("Augment Sprites")]
    public Sprite[] augmentSprites = new Sprite[28];

    private bool hasBossSpecialCoinDouble  = false;
    private bool hasGiantSlayer            = false;
    private bool hasTheBigOne              = false;
    private bool hasExecutioner            = false;
    private bool hasTwinsOfChaos           = false;
    private bool hasLuckyDay               = false;
    private bool hasDoomArmy               = false;
    private bool hasSpeedBurst             = false;
    private bool hasWeakPoint              = false;
    private bool hasBouncingArrow          = false;
    private bool hasFateDice               = false;
    private float augmentDefenseDown       = 0f;
    private float passiveResonanceBonus    = 0f;
    private float dominatorBonusThreshold  = 70;
    private float dominatorBonusMult       = 2.0f;
    private float dominatorPenaltyThreshold= 50;
    private float dominatorPenaltyMult     = 0.5f;
    private float allInDamageBonus         = 0f;
    private bool  allInActive              = false;
    private float coinRewardMultiplier     = 1f;

    public bool  HasGiantSlayer            => hasGiantSlayer;
    public bool  HasTheBigOne              => hasTheBigOne;
    public bool  HasExecutioner            => hasExecutioner;
    public bool  HasTwinsOfChaos           => hasTwinsOfChaos;
    public bool  HasBossSpecialCoinDouble  => hasBossSpecialCoinDouble;
    public bool  HasLuckyDay               => hasLuckyDay;
    public bool  HasDoomArmy               => hasDoomArmy;
    public bool  HasSpeedBurst             => hasSpeedBurst;
    public bool  HasWeakPoint              => hasWeakPoint;
    public bool  HasBouncingArrow          => hasBouncingArrow;
    public bool  HasFateDice               => hasFateDice;
    public float BonusDefenseDown          => augmentDefenseDown;
    public float PassiveResonanceBonus     => passiveResonanceBonus;
    public float AllInDamageBonus          => allInDamageBonus;
    public float CoinRewardMultiplier      => coinRewardMultiplier;

    public float GetDominatorDamageMultiplier()
    {
        if (GameManager.Instance == null) return 1f;
        int ec = GameManager.Instance.GetCurrentEnemyCount();
        if (ec >= dominatorBonusThreshold)   return dominatorBonusMult;
        if (ec <= dominatorPenaltyThreshold) return dominatorPenaltyMult;
        return 1f;
    }

    private int freeSpawnCount = 0;
    public int  FreeSpawnCount => freeSpawnCount;
    public void UseFreeSpawn() { if (freeSpawnCount > 0) freeSpawnCount--; }

    private List<string>      activeSummaries  = new List<string>();
    private List<AugmentType> selectedAugments = new List<AugmentType>();
    private AugmentData[]     allAugments;

    private Coroutine speedBurstCoroutine;
    private Coroutine fateDiceCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        allAugments = new AugmentData[]
        {
            new AugmentData { augmentName="기초 훈련",            requiredTier=1, type=AugmentType.BasicTraining,     summary="1~2티어 공격력 +30% / +200골드",               description="일반, 고급 등급 유닛의 공격력이 30% 증가하고 즉시 200골드를 획득합니다." },
            new AugmentData { augmentName="비상금",               requiredTier=1, type=AugmentType.EmergencyFund,     summary="+600골드 / 특수코인 +6",                        description="즉시 600 골드 및 특수 코인 6개를 획득합니다." },
            new AugmentData { augmentName="가벼운 발걸음",         requiredTier=1, type=AugmentType.LightStep,         summary="공격속도 +15% / 사거리 +1",                     description="모든 유닛의 공격 속도가 15%, 사거리가 1 증가합니다." },
            new AugmentData { augmentName="현상금 사냥꾼",         requiredTier=1, type=AugmentType.HuntersSense,      summary="킬 시 5%확률 +10골드 / 소환비용 +20%",          description="적 처치 시 5% 확률로 10골드 획득. *소환 비용 20% 증가" },
            new AugmentData { augmentName="작은 주머니",           requiredTier=1, type=AugmentType.SmallPouch,        summary="소환비용 -15% / +300골드",                      description="소환 비용이 15% 감소하고 즉시 300 골드를 획득합니다." },
            new AugmentData { augmentName="운빨 좋은 날",          requiredTier=1, type=AugmentType.LuckyDay,          summary="고급이상 확률 1.5배 / 무료소환 3회",             description="유닛 소환 시 고급 이상 등급이 나올 확률이 1.5배 증가하고 무료 소환권을 3회 획득합니다." },
            new AugmentData { augmentName="방어구 파쇄기",         requiredTier=1, type=AugmentType.ArmorBreaker,      summary="적 방어력 -15% / 공격력 +5%",                   description="적의 방어력이 15% 하락하고 모든 아군 유닛의 공격력이 5% 증가합니다." },
            new AugmentData { augmentName="베테랑 교관",           requiredTier=1, type=AugmentType.VeteranInstructor, summary="업그레이드비용 -50%",                            description="유닛 업그레이드 비용이 50% 감소합니다." },
            new AugmentData { augmentName="백지장도 맞들면 낫다",   requiredTier=3, type=AugmentType.Teamwork,          summary="희귀유닛 5명 소환",                              description="희귀 등급 유닛을 랜덤으로 5명 소환합니다." },
            new AugmentData { augmentName="투자 전문가",           requiredTier=1, type=AugmentType.InvestmentExpert,  summary="특수코인 획득량 2배",                            description="보스 및 특수 몬스터 처치 시 특수 코인을 2배로 획득합니다." },
            new AugmentData { augmentName="거인 학살자",           requiredTier=1, type=AugmentType.GiantSlayer,       summary="보스/특수 피해 +100% / 일반 피해 -20%",          description="보스 및 특수 몬스터에게 입히는 피해량이 100% 증가합니다. *일반 몬스터 피해 20% 감소" },
            new AugmentData { augmentName="딱 대",                 requiredTier=5, type=AugmentType.TheBigOne,         summary="레전드 10% 확률 5배 / 공속 -15%",               description="레전드 유닛이 10% 확률로 5배의 피해를 입힙니다. *레전드 유닛 공격속도 15% 감소" },
            new AugmentData { augmentName="사형 집행인",           requiredTier=1, type=AugmentType.Executioner,       summary="보스 10% 처형 / 보스 체력 +20%",                description="보스 몬스터의 체력이 10% 이하가 되면 즉시 처형시킵니다. *보스 최대 체력 20% 증가" },
            new AugmentData { augmentName="두 사람은 문제아지만 최강", requiredTier=5, type=AugmentType.TwinsOfChaos,  summary="동일 레전드 2마리시 패시브 2배 / 배치 수 -2",   description="동일한 레전드 유닛을 2마리 이상 배치하면 패시브 발동 확률이 2배가 됩니다. *유닛 배치 수 -2" },
            new AugmentData { augmentName="날카로운 칼날",          requiredTier=1, type=AugmentType.SharpBlade,       summary="1~2티어 5% 확률 치명타(2배)",                   description="일반, 고급 등급 유닛의 공격이 5% 확률로 치명타(2배 피해)를 입힙니다." },
            new AugmentData { augmentName="얼음 조각",             requiredTier=1, type=AugmentType.IceShard,          summary="5% 확률 1초 둔화",                              description="모든 아군의 공격이 5% 확률로 적을 1초간 둔화시킵니다." },
            new AugmentData { augmentName="재활용",                requiredTier=1, type=AugmentType.Recycle,           summary="판매 환급 +20%",                                description="유닛 판매 시 환급 금액이 20% 증가합니다." },
            new AugmentData { augmentName="명사수의 눈",            requiredTier=1, type=AugmentType.SharpEye,          summary="1~2티어 사거리 +1",                             description="1, 2티어 유닛의 사거리가 1 증가합니다." },
            new AugmentData { augmentName="취약 노출",             requiredTier=1, type=AugmentType.Vulnerability,     summary="적 받는 피해 +20% / 공속 -10%",                 description="적이 받는 피해가 20% 증가합니다. *아군 공격속도 10% 감소" },
            new AugmentData { augmentName="합성 가속",             requiredTier=1, type=AugmentType.MergeAccel,        summary="합성 30% 확률 무료 / 1% 확률 2배 비용",         description="합성 시 30% 확률로 골드를 소모하지 않습니다. *1% 확률로 골드를 2배로 소모합니다." },
            new AugmentData { augmentName="패시브 공명",            requiredTier=2, type=AugmentType.PassiveResonance, summary="패시브 발동 확률 +20% / 공격력 -15%",            description="모든 아군 유닛의 패시브 발동 확률이 20% 증가합니다. *공격력 15% 감소" },
            new AugmentData { augmentName="전장의 지배자",          requiredTier=1, type=AugmentType.Dominator,        summary="적 70마리↑ 공격력 2배 / 50마리↓ 공격력 0.5배", description="화면에 적이 70마리 이상이면 아군 공격력 100% 증가. *적이 50마리 이하면 공격력 50% 감소" },
            new AugmentData { augmentName="종말의 군대",            requiredTier=5, type=AugmentType.DoomArmy,         summary="보스웨이브 레전드 1명 소환 / 일반웨이브 골드 -50%", description="보스 웨이브 시작 시 레전드 유닛 1명을 무작위로 소환합니다. *일반 웨이브 골드 보상 50% 감소" },
            new AugmentData { augmentName="순간 가속",             requiredTier=1, type=AugmentType.SpeedBurst,        summary="웨이브 후반 10초 공속 +200% / 전반 20초 공속 -30%", description="매 웨이브 후반 10초간 모든 아군의 공격속도가 200% 증가합니다. *웨이브 전반 20초동안 공속 30% 감소" },
            new AugmentData { augmentName="약점 간파",             requiredTier=1, type=AugmentType.WeakPoint,         summary="스킬 피해 +100% / 일반 데미지 -30%",            description="모든 적이 받는 스킬 피해 +100%. *스킬이 아닌 모든 데미지 30% 감소" },
            new AugmentData { augmentName="올인",                  requiredTier=1, type=AugmentType.AllIn,            summary="보유 골드 소모 → 공격력 증가 / 골드 획득 -50%", description="보유 골드를 모두 소모하여 100골드당 모든 아군 공격력 1% 증가합니다. *이후 골드 획득량 50% 감소" },
            new AugmentData { augmentName="운명의 주사위",          requiredTier=1, type=AugmentType.FateDice,         summary="매 웨이브 공격력/공속 0.5~2배 무작위",          description="매 웨이브 시작 시 모든 아군의 공격력 또는 공격속도가 0.5배~2배 사이로 무작위 변동합니다." },
            new AugmentData { augmentName="튕겨나가는 화살",        requiredTier=2, type=AugmentType.BouncingArrow,    summary="공격 인접 적 50% 피해 추가 튕김 / 사거리 -1",  description="일반 공격이 가까운 적 1명에게 추가로 튕깁니다 (50% 피해). *사거리 1 감소" },
        };

        for (int i = 0; i < allAugments.Length && i < augmentSprites.Length; i++)
            allAugments[i].sprite = augmentSprites[i];
    }

    public AugmentData[] GetRandomAugments()
    {
        int unlockedTier = UpgradeManager.Instance != null ? UpgradeManager.Instance.UnlockedTier : 1;
        List<AugmentData> pool = new List<AugmentData>();
        foreach (AugmentData data in allAugments)
            if (!selectedAugments.Contains(data.type) && data.requiredTier <= unlockedTier)
                pool.Add(data);

        if (pool.Count == 0) return new AugmentData[0];
        int count = Mathf.Min(3, pool.Count);
        List<AugmentData> result      = new List<AugmentData>();
        List<int>          usedIndices = new List<int>();
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
                ApplyToTierUnits(30f, 0f, 1, 2);
                CoinManager.Instance?.AddCoins(200);
                break;
            case AugmentType.EmergencyFund:
                CoinManager.Instance?.AddCoins(600);
                SpecialCoinManager.Instance?.AddSpecialCoins(6);
                break;
            case AugmentType.LightStep:
                ApplyToAllUnits(0f, 15f, 1f);
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
                augmentDefenseDown += 15f;
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
            case AugmentType.SharpBlade:
                ApplySharpBlade();
                break;
            case AugmentType.IceShard:
                ApplyIceShard();
                break;
            case AugmentType.Recycle:
                if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.sellPriceMultiplier += 0.2f;
                break;
            case AugmentType.SharpEye:
                ApplyToTierUnits(0f, 0f, 1, 2, 1f);
                break;
            case AugmentType.Vulnerability:
                augmentDefenseDown += 20f;
                ApplyToAllUnits(0f, -10f);
                break;
            case AugmentType.MergeAccel:
                if (MergeManager.Instance != null) MergeManager.Instance.mergeAccelActive = true;
                break;
            case AugmentType.PassiveResonance:
                passiveResonanceBonus = 20f;
                ApplyToAllUnits(-15f, 0f);
                break;
            case AugmentType.Dominator:
                break;
            case AugmentType.DoomArmy:
                hasDoomArmy = true;
                coinRewardMultiplier *= 0.5f;
                break;
            case AugmentType.SpeedBurst:
                hasSpeedBurst = true;
                if (speedBurstCoroutine != null) StopCoroutine(speedBurstCoroutine);
                speedBurstCoroutine = StartCoroutine(SpeedBurstRoutine());
                break;
            case AugmentType.WeakPoint:
                hasWeakPoint = true;
                ApplyNormalDamagePenaltyToAll(0.7f);
                RefreshAllUnits();
                break;
            case AugmentType.AllIn:
                ApplyAllIn();
                break;
            case AugmentType.FateDice:
                hasFateDice = true;
                if (fateDiceCoroutine != null) StopCoroutine(fateDiceCoroutine);
                fateDiceCoroutine = StartCoroutine(FateDiceRoutine());
                break;
            case AugmentType.BouncingArrow:
                hasBouncingArrow = true;
                ApplyToAllUnits(0f, 0f, -1f);
                break;
        }
        PassiveManager.Instance?.RecalculatePassives();
    }

    void ApplySharpBlade()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.characterData != null && unit.characterData.tier <= 2)
                unit.ApplySharpBlade(5f);
    }

    void ApplyIceShard()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyIceShard(5f, 1f);
    }

    void ApplyAllIn()
    {
        if (CoinManager.Instance == null) return;
        int coins = CoinManager.Instance.GetCoins();
        if (coins <= 0) return;
        allInDamageBonus = (coins / 100f);
        CoinManager.Instance.SpendCoins(coins);
        coinRewardMultiplier *= 0.5f;
        allInActive = true;
        ApplyToAllUnits(allInDamageBonus, 0f);
    }

    IEnumerator SpeedBurstRoutine()
    {
        while (true)
        {
            if (GameManager.Instance == null) { yield return new WaitForSeconds(1f); continue; }
            float roundDuration = GameManager.Instance.GetCurrentRoundDuration();
            float timeLeft      = GameManager.Instance.GetRoundTimeLeft();
            float elapsed       = roundDuration - timeLeft;

            bool inPenaltyZone = elapsed < 20f;
            bool inBonusZone   = timeLeft <= 10f;

            float target = inBonusZone ? 200f : inPenaltyZone ? -30f : 0f;
            SetSpeedBurstBonus(target);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private float currentSpeedBurstBonus = 0f;
    void SetSpeedBurstBonus(float newBonus)
    {
        if (Mathf.Approximately(currentSpeedBurstBonus, newBonus)) return;
        float delta = newBonus - currentSpeedBurstBonus;
        currentSpeedBurstBonus = newBonus;
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(0f, delta);
    }

    IEnumerator FateDiceRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (GameManager.Instance == null) continue;
            float timeLeft = GameManager.Instance.GetRoundTimeLeft();
            float prev     = GameManager.Instance.GetPrevRoundTimeLeft();
            bool newRound  = prev > 0f && timeLeft > prev + 1f;
            if (!newRound) continue;
            float roll = Random.Range(0.5f, 2.0f);
            bool damageDice = Random.value > 0.5f;
            PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
            foreach (PlayerAttack unit in allUnits)
                if (unit != null) unit.ResetFateDice();
            foreach (PlayerAttack unit in allUnits)
            {
                if (unit == null) continue;
                if (damageDice) unit.ApplyFateDiceDamage(roll);
                else            unit.ApplyFateDiceSpeed(roll);
            }
        }
    }

    void ApplyToAllUnits(float dmg, float spd, float range = 0f)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(dmg, spd, range);
    }

    void ApplyToTierUnits(float dmg, float spd, int minTier, int maxTier, float range = 0f)
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.characterData != null &&
                unit.characterData.tier >= minTier && unit.characterData.tier <= maxTier)
                unit.ApplyAugmentBonus(dmg, spd, range);
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
        int tier = unit.characterData.tier;

        if (selectedAugments.Contains(AugmentType.BasicTraining) && tier <= 2)
            unit.ApplyAugmentBonus(30f, 0f);
        if (selectedAugments.Contains(AugmentType.LightStep))
            unit.ApplyAugmentBonus(0f, 15f, 1f);
        if (selectedAugments.Contains(AugmentType.ArmorBreaker))
            unit.ApplyAugmentBonus(5f, 0f);
        if (selectedAugments.Contains(AugmentType.GiantSlayer))
            unit.ApplyAugmentBonus(0f, 0f, 0f, 0.8f);
        if (selectedAugments.Contains(AugmentType.TheBigOne) && tier >= 5)
            unit.ApplyAugmentBonus(0f, 0f, 0f, 1f, 15f);
        if (selectedAugments.Contains(AugmentType.SharpBlade) && tier <= 2)
            unit.ApplySharpBlade(5f);
        if (selectedAugments.Contains(AugmentType.IceShard))
            unit.ApplyIceShard(5f, 1f);
        if (selectedAugments.Contains(AugmentType.SharpEye) && tier <= 2)
            unit.ApplyAugmentBonus(0f, 0f, 1f);
        if (selectedAugments.Contains(AugmentType.Vulnerability))
            unit.ApplyAugmentBonus(0f, -10f);
        if (selectedAugments.Contains(AugmentType.PassiveResonance))
            unit.ApplyAugmentBonus(-15f, 0f);
        if (selectedAugments.Contains(AugmentType.WeakPoint))
            unit.ApplyAugmentBonus(0f, 0f, 0f, 0.7f);
        if (selectedAugments.Contains(AugmentType.AllIn) && allInDamageBonus > 0f)
            unit.ApplyAugmentBonus(allInDamageBonus, 0f);
        if (selectedAugments.Contains(AugmentType.BouncingArrow))
            unit.ApplyAugmentBonus(0f, 0f, -1f);
        if (selectedAugments.Contains(AugmentType.SpeedBurst) && !Mathf.Approximately(currentSpeedBurstBonus, 0f))
            unit.ApplyAugmentBonus(0f, currentSpeedBurstBonus);
    }

    public string GetActiveAugmentText()
    {
        if (activeSummaries.Count == 0) return "";
        return "[ 증강 ]\n" + string.Join("\n", activeSummaries);
    }

    public void OnBossWaveStart()
    {
        if (hasDoomArmy) PlayerSpawner.Instance?.SpawnLegendUnit();
    }

    public void OnNewRoundStart()
    {
        if (hasSpeedBurst)
        {
            if (speedBurstCoroutine != null) StopCoroutine(speedBurstCoroutine);
            speedBurstCoroutine = StartCoroutine(SpeedBurstRoutine());
        }
    }

    public void ResetAugments()
    {
        hasBossSpecialCoinDouble  = false;
        hasGiantSlayer            = false;
        hasTheBigOne              = false;
        hasExecutioner            = false;
        hasTwinsOfChaos           = false;
        hasLuckyDay               = false;
        hasDoomArmy               = false;
        hasSpeedBurst             = false;
        hasWeakPoint              = false;
        hasBouncingArrow          = false;
        hasFateDice               = false;
        augmentDefenseDown        = 0f;
        passiveResonanceBonus     = 0f;
        allInDamageBonus          = 0f;
        allInActive               = false;
        coinRewardMultiplier      = 1f;
        currentSpeedBurstBonus    = 0f;
        freeSpawnCount            = 0;
        activeSummaries.Clear();
        selectedAugments.Clear();
        if (speedBurstCoroutine != null) { StopCoroutine(speedBurstCoroutine); speedBurstCoroutine = null; }
        if (fateDiceCoroutine   != null) { StopCoroutine(fateDiceCoroutine);   fateDiceCoroutine   = null; }
        if (PlayerSpawner.Instance  != null) { PlayerSpawner.Instance.sellPriceMultiplier = 1f;  PlayerSpawner.Instance.luckyDayMultiplier = 1f; }
        if (CoinManager.Instance    != null) { CoinManager.Instance.spawnCostMultiplier   = 1f;  CoinManager.Instance.huntersSenseActive   = false; }
        if (MergeManager.Instance   != null) { MergeManager.Instance.upgradeCostMultiplier = 1f; MergeManager.Instance.mergeAccelActive    = false; }
        if (BossManager.Instance    != null) BossManager.Instance.bossHpMultiplier = 1f;
    }
}
