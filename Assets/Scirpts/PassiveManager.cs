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

        float totalDamageBonus = 0f;
        float totalSpeedBonus = 0f;
        float totalEnemySpeedDown = 0f;
        float totalEnemyDefenseDown = 0f;

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

            float doubleDamageChance = 0f;
            float attackTwiceChance = 0f;
            float selfSpeedUpChance = 0f;
            float selfSpeedUpAmount = 0f;
            float selfSpeedUpDuration = 0f;

            foreach (PassiveEntry entry in unit.characterData.passives)
            {
                switch (entry.passiveType)
                {
                    case PassiveType.DoubleDamageChance: doubleDamageChance = entry.passiveValue; break;
                    case PassiveType.AttackTwiceChance: attackTwiceChance = entry.passiveValue; break;
                    case PassiveType.SelfAttackSpeedUpChance:
                        selfSpeedUpChance = entry.passiveValue;
                        selfSpeedUpAmount = entry.passiveSecondValue;
                        selfSpeedUpDuration = entry.passiveDuration;
                        break;
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

        string text = "[ 패시브 현황 ]\n";

        if (dmg > 0f) text += $"아군 공격력 +{dmg}%\n";
        if (spd > 0f) text += $"아군 공속 +{spd}%\n";
        if (enemySpd > 0f) text += $"적 이동속도 -{enemySpd}\n";
        if (enemyDef > 0f) text += $"적 방어력 -{enemyDef}\n";

        if (dmg == 0f && spd == 0f && enemySpd == 0f && enemyDef == 0f)
            text = "";

        passiveStatusText.text = text;
    }
}