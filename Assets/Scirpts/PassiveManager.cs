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
            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                switch (entry.passiveType)
                {
                    case PassiveType.AllAttackDamageUp: totalDamageBonus += entry.passiveValue; break;
                    case PassiveType.AllAttackSpeedUp: totalSpeedBonus += entry.passiveValue; break;
                    case PassiveType.AllEnemySpeedDown: totalEnemySpeedDown += entry.passiveValue; break;
                    case PassiveType.AllEnemyDefenseDown: totalEnemyDefenseDown += entry.passiveValue; break;
                }
            }
        }

        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit.characterData == null) continue;
            float doubleChance = 0f, twiceChance = 0f;
            float selfSpeedChance = 0f, selfSpeedAmount = 0f, selfSpeedDuration = 0f;
            float selfDamageChance = 0f, selfDamageAmount = 0f, selfDamageDuration = 0f;
            float stunChance = 0f, stunDuration = 0f;
            float executeChance = 0f, executeHpThreshold = 0f, executeBossDamagePercent = 0f;
            float buffAllyChance = 0f, buffAllyAmount = 0f, buffAllyDuration = 0f;
            float aoeStunEveryN = 0f, aoeStunRange = 0f, aoeStunDuration = 0f;

            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                switch (entry.passiveType)
                {
                    case PassiveType.DoubleDamageChance: doubleChance = entry.passiveValue; break;
                    case PassiveType.AttackTwiceChance: twiceChance = entry.passiveValue; break;
                    case PassiveType.SelfAttackSpeedUpChance: selfSpeedChance = entry.passiveValue; selfSpeedAmount = entry.passiveSecondValue; selfSpeedDuration = entry.passiveDuration; break;
                    case PassiveType.SelfAttackDamageUpChance: selfDamageChance = entry.passiveValue; selfDamageAmount = entry.passiveSecondValue; selfDamageDuration = entry.passiveDuration; break;
                    case PassiveType.StunChance: stunChance = entry.passiveValue; stunDuration = entry.passiveDuration; break;
                    case PassiveType.ExecuteChance: executeChance = entry.passiveValue; executeHpThreshold = entry.passiveSecondValue; executeBossDamagePercent = entry.passiveDuration; break;
                    case PassiveType.BuffNearbyAllyAttackSpeed: buffAllyChance = entry.passiveValue; buffAllyAmount = entry.passiveSecondValue; buffAllyDuration = entry.passiveDuration; break;
                    case PassiveType.AoeStunEveryNHits: aoeStunEveryN = entry.passiveValue; aoeStunRange = entry.passiveSecondValue; aoeStunDuration = entry.passiveDuration; break;
                }
            }
            unit.ApplyPassiveBonus(totalDamageBonus, totalSpeedBonus, doubleChance, twiceChance,
                selfSpeedChance, selfSpeedAmount, selfSpeedDuration,
                selfDamageChance, selfDamageAmount, selfDamageDuration,
                stunChance, stunDuration,
                executeChance, executeHpThreshold, executeBossDamagePercent,
                buffAllyChance, buffAllyAmount, buffAllyDuration,
                aoeStunEveryN, aoeStunRange, aoeStunDuration);
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