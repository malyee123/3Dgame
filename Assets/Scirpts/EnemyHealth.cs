using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHp = 50f;
    public float defense = 0f;

    [Header("HP Bar")]
    public Slider hpSlider;
    public Image hpFillImage;

    [Header("Special Monster")]
    public bool isSpecial = false;
    public bool isBoss = false;
    public bool forceDamageOne = false;
    public int specialCoinReward = 0;

    [Header("Damage Text")]
    public GameObject damageTextPrefab;

    private float currentHp;
    private float defenseDownPercent = 0f;
    private bool isDead = false;
    private PlayerAttack lastAttacker = null;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    public void Init(float hp, float def = 0f)
    {
        maxHp = hp; currentHp = hp; defense = def; defenseDownPercent = 0f; isDead = false; lastAttacker = null;
        if (hpSlider != null) { hpSlider.maxValue = hp; hpSlider.value = hp; }
        if (hpFillImage != null) hpFillImage.color = Color.green;
    }

    public void InitAsSpecialMonster(float hp, int reward, float def = 0f)
    {
        isSpecial = true;
        isBoss = false;
        specialCoinReward = reward;
        Init(hp, def);
    }

    void Start() { if (currentHp <= 0f) Init(maxHp, defense); }

    public void ApplyDefenseDownPercent(float percent) => defenseDownPercent = Mathf.Clamp(percent, 0f, 100f);
    public void BoostMaxHp(float multiplier) { maxHp *= multiplier; currentHp *= multiplier; if (hpSlider != null) { hpSlider.maxValue = maxHp; hpSlider.value = currentHp; } }

    public void TakeDamage(float damage, PlayerAttack attacker = null, bool isSkillDamage = false)
    {
        if (isDead) return;
        if (attacker != null) lastAttacker = attacker;

        float actualDamage;
        if (forceDamageOne)
            actualDamage = 1f;
        else
        {
            float effectiveDefense = Mathf.Max(0f, defense * (1f - defenseDownPercent / 100f));
            actualDamage = Mathf.Max(1f, damage - effectiveDefense);
        }

        currentHp -= actualDamage;
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpFillImage != null) hpFillImage.color = Color.Lerp(Color.red, Color.green, currentHp / maxHp);
        ShowDamageText(actualDamage, isSkillDamage);

        if (isBoss && AugmentManager.Instance != null && AugmentManager.Instance.HasExecutioner)
            if (currentHp / maxHp < 0.1f) { ExecuteKill(); return; }

        if (currentHp <= 0f) Die();
    }

    public void ExecuteKill()
    {
        if (isDead) return;
        currentHp = 0f;
        if (hpSlider != null) hpSlider.value = 0f;
        Die();
    }

    public void TakePercentDamage(float percent, PlayerAttack attacker = null) => TakeDamage(maxHp * (percent / 100f), attacker);

    public void ShowDamageText(float damage, bool isSkillDamage = false)
    {
        if (damageTextPrefab == null) return;
        GameObject obj = Instantiate(damageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt == null) return;
        if (isSkillDamage)
            dt.Init(damage, transform, 6f, 0.8f, Color.red);
        else
            dt.Init(damage, transform, isSpecial ? 6f : 3f, isSpecial ? 1f : 0.5f);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        GameManager.Instance?.OnEnemyDied();

        if (isSpecial)
        {
            if (SpecialCoinManager.Instance != null)
            {
                int reward = specialCoinReward;
                if (AugmentManager.Instance != null && AugmentManager.Instance.HasBossSpecialCoinDouble)
                    reward *= 2;
                SpecialCoinManager.Instance.AddSpecialCoins(reward);
            }
            if (isBoss)
                GameManager.Instance?.OnBossKilled();
            else
                SpecialMonsterManager.Instance?.OnSpecialMonsterKilled();
        }
        else
        {
            CoinManager.Instance?.AddCoins(CoinManager.Instance.coinsPerKill, true);
        }

        if (lastAttacker?.characterData != null)
        {
            foreach (PassiveEntry entry in lastAttacker.characterData.passives)
            {
                if (entry.passiveType != PassiveType.SpecialCoinOnKillChance) continue;
                if (Random.Range(0f, 100f) < entry.passiveValue)
                    SpecialCoinManager.Instance?.AddSpecialCoins((int)entry.passiveSecondValue);
            }
        }

        Destroy(gameObject);
    }
}
