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
            float doubleDamageChance = 0f, attackTwiceChance = 0f, selfSpeedUpChance = 0f, selfSpeedUpAmount = 0f, selfSpeedUpDuration = 0f;
            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                switch (entry.passiveType)
                {
                    case PassiveType.DoubleDamageChance: doubleDamageChance = entry.passiveValue; break;
                    case PassiveType.AttackTwiceChance: attackTwiceChance = entry.passiveValue; break;
                    case PassiveType.SelfAttackSpeedUpChance: selfSpeedUpChance = entry.passiveValue; selfSpeedUpAmount = entry.passiveSecondValue; selfSpeedUpDuration = entry.passiveDuration; break;
                }
            }
            unit.ApplyPassiveBonus(totalDamageBonus, totalSpeedBonus, doubleDamageChance, attackTwiceChance, selfSpeedUpChance, selfSpeedUpAmount, selfSpeedUpDuration);
        }

        foreach (EnemyMove enemy in allEnemies)
        {
            if (enemy == null) continue;
            enemy.ApplySpeedPenalty(totalEnemySpeedDown);
        }

        UpdatePassiveUI(totalDamageBonus, totalSpeedBonus, totalEnemySpeedDown, totalEnemyDefenseDown);
    }

    void UpdatePassiveUI(float dmg, float spd, float enemySpd, float enemyDef)
    {
        if (passiveStatusText == null) return;
        if (dmg == 0f && spd == 0f && enemySpd == 0f && enemyDef == 0f) { passiveStatusText.text = ""; return; }
        string text = "[ Passive Status ]\n";
        if (dmg > 0f) text += $"ATK Damage +{dmg}%\n";
        if (spd > 0f) text += $"ATK Speed +{spd}%\n";
        if (enemySpd > 0f) text += $"Enemy Speed -{enemySpd}\n";
        if (enemyDef > 0f) text += $"Enemy Defense -{enemyDef}\n";
        passiveStatusText.text = text;
    }
}