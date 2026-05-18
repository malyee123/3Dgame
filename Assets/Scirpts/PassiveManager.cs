using UnityEngine;
using TMPro;

public class PassiveManager : MonoBehaviour
{
    public static PassiveManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI passiveStatusText;

    private float totalEnemySpeedDown = 0f;
    private float totalEnemyDefenseDown = 0f;

    public float GetTotalEnemySpeedDown() => totalEnemySpeedDown;
    public float GetTotalEnemyDefenseDown() => totalEnemyDefenseDown;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RecalculatePassives()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);

        float totalDamageBonus = 0f, totalSpeedBonus = 0f;
        totalEnemySpeedDown = 0f;
        totalEnemyDefenseDown = 0f;

        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit.characterData == null) continue;
            int tier = unit.characterData.tier;
            float tierBonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTierPassiveBonus(tier) : 0f;

            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                float value = entry.passiveValue + tierBonus;
                switch (entry.passiveType)
                {
                    case PassiveType.AllAttackDamageUp: totalDamageBonus += value; break;
                    case PassiveType.AllAttackSpeedUp: totalSpeedBonus += value; break;
                    case PassiveType.AllEnemySpeedDown: totalEnemySpeedDown += value; break;
                    case PassiveType.AllEnemyDefenseDown: totalEnemyDefenseDown += value; break;
                }
            }
        }

        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit.characterData == null) continue;
            int tier = unit.characterData.tier;
            float tierBonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTierPassiveBonus(tier) : 0f;

            float doubleChance = 0f, doubleMultiplier = 2f;
            float twiceChance = 0f, twiceCount = 2f;
            float selfSpeedChance = 0f, selfSpeedAmount = 0f, selfSpeedDuration = 0f;
            float selfDamageChance = 0f, selfDamageAmount = 0f, selfDamageDuration = 0f;
            float stunChance = 0f, stunDuration = 0f;
            float executeChance = 0f, executeHpThreshold = 0f, executeBossDamagePercent = 0f;
            float buffAllyChance = 0f, buffAllyAmount = 0f, buffAllyDuration = 0f;
            float aoeStunEveryN = 0f, aoeStunRange = 0f, aoeStunDuration = 0f;
            bool bossDamageDouble = false;
            float areaSpeedDownChance = 0f, areaSpeedDownAmount = 0f, areaSpeedDownDuration = 0f;
            float magicMissileChance = 0f, magicMissileDamagePercent = 0f;
            float slamChance = 0f, slamDamagePercent = 0f, slamRange = 0f;
            float manaSkillDamage = 0f, manaSkillDuration = 0f, manaSkillInterval = 0f;

            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                float val = entry.passiveValue + tierBonus;
                switch (entry.passiveType)
                {
                    case PassiveType.DoubleDamageChance: doubleChance = val; doubleMultiplier = entry.passiveSecondValue > 0f ? entry.passiveSecondValue : 2f; break;
                    case PassiveType.AttackTwiceChance: twiceChance = val; twiceCount = entry.passiveSecondValue > 0f ? entry.passiveSecondValue : 2f; break;
                    case PassiveType.SelfAttackSpeedUpChance: selfSpeedChance = val; selfSpeedAmount = entry.passiveSecondValue; selfSpeedDuration = entry.passiveDuration; break;
                    case PassiveType.SelfAttackDamageUpChance: selfDamageChance = val; selfDamageAmount = entry.passiveSecondValue; selfDamageDuration = entry.passiveDuration; break;
                    case PassiveType.StunChance: stunChance = val; stunDuration = entry.passiveDuration; break;
                    case PassiveType.ExecuteChance: executeChance = val; executeHpThreshold = entry.passiveSecondValue; executeBossDamagePercent = entry.passiveDuration; break;
                    case PassiveType.BuffNearbyAllyAttackSpeed: buffAllyChance = val; buffAllyAmount = entry.passiveSecondValue; buffAllyDuration = entry.passiveDuration; break;
                    case PassiveType.AoeStunEveryNHits: aoeStunEveryN = entry.passiveValue; aoeStunRange = entry.passiveSecondValue; aoeStunDuration = entry.passiveDuration; break;
                    case PassiveType.BossDamageDouble: bossDamageDouble = true; break;
                    case PassiveType.AreaSpeedDownChance: areaSpeedDownChance = val; areaSpeedDownAmount = entry.passiveSecondValue; areaSpeedDownDuration = entry.passiveDuration; break;
                    case PassiveType.MagicMissileChance: magicMissileChance = val; magicMissileDamagePercent = entry.passiveSecondValue; break;
                    case PassiveType.SlamChance: slamChance = val; slamDamagePercent = entry.passiveSecondValue; slamRange = entry.passiveDuration; break;
                    case PassiveType.ManaSkill: manaSkillDamage = entry.passiveValue; manaSkillDuration = entry.passiveSecondValue; manaSkillInterval = entry.passiveDuration; break;
                }
            }

            unit.ApplyPassiveBonus(totalDamageBonus, totalSpeedBonus,
                doubleChance, doubleMultiplier,
                twiceChance, twiceCount,
                selfSpeedChance, selfSpeedAmount, selfSpeedDuration,
                selfDamageChance, selfDamageAmount, selfDamageDuration,
                stunChance, stunDuration,
                executeChance, executeHpThreshold, executeBossDamagePercent,
                buffAllyChance, buffAllyAmount, buffAllyDuration,
                aoeStunEveryN, aoeStunRange, aoeStunDuration,
                bossDamageDouble,
                areaSpeedDownChance, areaSpeedDownAmount, areaSpeedDownDuration,
                magicMissileChance, magicMissileDamagePercent,
                slamChance, slamDamagePercent, slamRange,
                manaSkillDamage, manaSkillDuration, manaSkillInterval);
        }

        EnemyMove[] allEnemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove enemy in allEnemies)
            if (enemy != null) enemy.ApplySpeedPenalty(totalEnemySpeedDown);

        float anvilDefenseDown = AnvilManager.Instance != null ? AnvilManager.Instance.BonusDefenseDown : 0f;
        float augmentDefenseDown = AugmentManager.Instance != null ? AugmentManager.Instance.BonusDefenseDown : 0f;
        float combinedDefenseDown = totalEnemyDefenseDown + anvilDefenseDown + augmentDefenseDown;

        EnemyHealth[] allEnemyHealths = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemyHealths)
            if (eh != null) eh.ApplyDefenseDownPercent(combinedDefenseDown);

        UpdatePassiveUI(totalDamageBonus, totalSpeedBonus, totalEnemySpeedDown, totalEnemyDefenseDown);
    }

    void UpdatePassiveUI(float dmg, float spd, float enemySpd, float enemyDef)
    {
        if (passiveStatusText == null) return;
        float anvilDefenseDown = AnvilManager.Instance != null ? AnvilManager.Instance.BonusDefenseDown : 0f;
        float augmentDefenseDown = AugmentManager.Instance != null ? AugmentManager.Instance.BonusDefenseDown : 0f;
        float totalDefDisplay = enemyDef + anvilDefenseDown + augmentDefenseDown;
        if (dmg == 0f && spd == 0f && enemySpd == 0f && totalDefDisplay == 0f) { passiveStatusText.text = ""; return; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[ 패시브 현황 ]\n");
        if (dmg > 0f) sb.Append($"공격력 +{dmg}%\n");
        if (spd > 0f) sb.Append($"공격속도 +{spd}%\n");
        if (enemySpd > 0f) sb.Append($"적 이동속도 -{enemySpd}\n");
        if (totalDefDisplay > 0f) sb.Append($"적 방어력 -{totalDefDisplay}%\n");
        passiveStatusText.text = sb.ToString().TrimEnd();
    }
}