using UnityEngine;

public enum AugmentType { AttackDamage, AttackSpeed, CoinPerKill }
public enum AugmentGrade { Silver, Gold, Prism }

[System.Serializable]
public class AugmentData
{
    public string augmentName;
    public AugmentType type;
    public AugmentGrade grade;
    public float value;
    public Sprite sprite;
}

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

    [Header("Augment Sprites (순서: 공격력실버/골드/프리즘, 공속실버/골드/프리즘, 코인실버/골드/프리즘)")]
    public Sprite[] augmentSprites = new Sprite[9];

    private float bonusAttackDamage = 0f;
    private float bonusAttackSpeed = 0f;
    private int bonusCoinPerKill = 0;

    public float BonusAttackDamage => bonusAttackDamage;
    public float BonusAttackSpeed => bonusAttackSpeed;
    public int BonusCoinPerKill => bonusCoinPerKill;

    private AugmentData[] allAugments;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        allAugments = new AugmentData[]
        {
            new AugmentData { augmentName = "공격력 강화 I", type = AugmentType.AttackDamage, grade = AugmentGrade.Silver, value = 10f },
            new AugmentData { augmentName = "공격력 강화 II", type = AugmentType.AttackDamage, grade = AugmentGrade.Gold, value = 20f },
            new AugmentData { augmentName = "공격력 강화 III", type = AugmentType.AttackDamage, grade = AugmentGrade.Prism, value = 35f },
            new AugmentData { augmentName = "공격속도 강화 I", type = AugmentType.AttackSpeed, grade = AugmentGrade.Silver, value = 10f },
            new AugmentData { augmentName = "공격속도 강화 II", type = AugmentType.AttackSpeed, grade = AugmentGrade.Gold, value = 20f },
            new AugmentData { augmentName = "공격속도 강화 III", type = AugmentType.AttackSpeed, grade = AugmentGrade.Prism, value = 35f },
            new AugmentData { augmentName = "코인 획득 I", type = AugmentType.CoinPerKill, grade = AugmentGrade.Silver, value = 2f },
            new AugmentData { augmentName = "코인 획득 II", type = AugmentType.CoinPerKill, grade = AugmentGrade.Gold, value = 4f },
            new AugmentData { augmentName = "코인 획득 III", type = AugmentType.CoinPerKill, grade = AugmentGrade.Prism, value = 7f },
        };

        for (int i = 0; i < allAugments.Length && i < augmentSprites.Length; i++)
            allAugments[i].sprite = augmentSprites[i];
    }

    public AugmentData[] GetRandomAugments()
    {
        AugmentGrade grade = GetRandomGrade();

        AugmentType[] types = new AugmentType[]
        {
            AugmentType.AttackDamage,
            AugmentType.AttackSpeed,
            AugmentType.CoinPerKill
        };

        AugmentData[] result = new AugmentData[3];
        for (int i = 0; i < 3; i++)
        {
            var pool = System.Array.FindAll(allAugments, a => a.type == types[i] && a.grade == grade);
            result[i] = pool[Random.Range(0, pool.Length)];
        }
        return result;
    }

    AugmentGrade GetRandomGrade()
    {
        float rand = Random.Range(0f, 100f);
        if (rand < 10f) return AugmentGrade.Prism;
        if (rand < 40f) return AugmentGrade.Gold;
        return AugmentGrade.Silver;
    }

    public void ApplyAugment(AugmentData data)
    {
        switch (data.type)
        {
            case AugmentType.AttackDamage: bonusAttackDamage += data.value; break;
            case AugmentType.AttackSpeed: bonusAttackSpeed += data.value; break;
            case AugmentType.CoinPerKill: bonusCoinPerKill += (int)data.value; break;
        }
        ApplyToUnits();
        ApplyToCoinManager();
    }

    void ApplyToUnits()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.ApplyAugmentBonus(bonusAttackDamage, bonusAttackSpeed);
    }

    void ApplyToCoinManager()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.augmentCoinBonus = bonusCoinPerKill;
    }

    public void ResetAugments()
    {
        bonusAttackDamage = 0f;
        bonusAttackSpeed = 0f;
        bonusCoinPerKill = 0;
    }
}