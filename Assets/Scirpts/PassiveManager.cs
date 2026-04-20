using UnityEngine;
using TMPro;

public class PassiveManager : MonoBehaviour
{
    public static PassiveManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI passiveStatusText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RecalculatePassives()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        EnemyMove[] allEnemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);

        float totalDamageBonus = 0f, totalSpeedBonus = 0f, totalEnemySpeedDown = 0f, totalEnemyDefenseDown = 0f;

        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit.characterData == null) continue;
            int tier = unit.characterData.tier;
            float tierBonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetTierPassiveBonus(tier) : 0f;

            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                float value = entry.passiveValue + (entry.passiveValue * tierBonus / 100f);
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

            float doubleChance = 0f, twiceChance = 0f;
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

            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                float val = entry.passiveValue + (entry.passiveValue * tierBonus / 100f);
                float val2 = entry.passiveSecondValue + (entry.passiveSecondValue * tierBonus / 100f);
                switch (entry.passiveType)
                {
                    case PassiveType.DoubleDamageChance: doubleChance = val; break;
                    case PassiveType.AttackTwiceChance: twiceChance = val; break;
                    case PassiveType.SelfAttackSpeedUpChance: selfSpeedChance = val; selfSpeedAmount = val2; selfSpeedDuration = entry.passiveDuration; break;
                    case PassiveType.SelfAttackDamageUpChance: selfDamageChance = val; selfDamageAmount = val2; selfDamageDuration = entry.passiveDuration; break;
                    case PassiveType.StunChance: stunChance = val; stunDuration = entry.passiveDuration; break;
                    case PassiveType.ExecuteChance: executeChance = val; executeHpThreshold = entry.passiveSecondValue; executeBossDamagePercent = entry.passiveDuration; break;
                    case PassiveType.BuffNearbyAllyAttackSpeed: buffAllyChance = val; buffAllyAmount = val2; buffAllyDuration = entry.passiveDuration; break;
                    case PassiveType.AoeStunEveryNHits: aoeStunEveryN = entry.passiveValue; aoeStunRange = entry.passiveSecondValue; aoeStunDuration = entry.passiveDuration; break;
                    case PassiveType.BossDamageDouble: bossDamageDouble = true; break;
                    case PassiveType.AreaSpeedDownChance: areaSpeedDownChance = val; areaSpeedDownAmount = val2; areaSpeedDownDuration = entry.passiveDuration; break;
                    case PassiveType.MagicMissileChance: magicMissileChance = val; magicMissileDamagePercent = val2; break;
                    case PassiveType.SlamChance: slamChance = val; slamDamagePercent = val2; slamRange = entry.passiveDuration; break;
                }
            }
            unit.ApplyPassiveBonus(totalDamageBonus, totalSpeedBonus, doubleChance, twiceChance,
                selfSpeedChance, selfSpeedAmount, selfSpeedDuration,
                selfDamageChance, selfDamageAmount, selfDamageDuration,
                stunChance, stunDuration,
                executeChance, executeHpThreshold, executeBossDamagePercent,
                buffAllyChance, buffAllyAmount, buffAllyDuration,
                aoeStunEveryN, aoeStunRange, aoeStunDuration,
                bossDamageDouble,
                areaSpeedDownChance, areaSpeedDownAmount, areaSpeedDownDuration,
                magicMissileChance, magicMissileDamagePercent,
                slamChance, slamDamagePercent, slamRange);
        }

        foreach (EnemyMove enemy in allEnemies)
            if (enemy != null) enemy.ApplySpeedPenalty(totalEnemySpeedDown);

        EnemyHealth[] allEnemyHealths = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemyHealths)
            if (eh != null) eh.ApplyDefenseDown(totalEnemyDefenseDown);

        UpdatePassiveUI(totalDamageBonus, totalSpeedBonus, totalEnemySpeedDown, totalEnemyDefenseDown);
    }

    void UpdatePassiveUI(float dmg, float spd, float enemySpd, float enemyDef)
    {
        if (passiveStatusText == null) return;
        if (dmg == 0f && spd == 0f && enemySpd == 0f && enemyDef == 0f) { passiveStatusText.text = ""; return; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[ Passive Status ]\n");
        if (dmg > 0f) sb.Append($"ATK Damage +{dmg}%\n");
        if (spd > 0f) sb.Append($"ATK Speed +{spd}%\n");
        if (enemySpd > 0f) sb.Append($"Enemy Speed -{enemySpd}\n");
        if (enemyDef > 0f) sb.Append($"Enemy Defense -{enemyDef}\n");
        passiveStatusText.text = sb.ToString();
    }
}