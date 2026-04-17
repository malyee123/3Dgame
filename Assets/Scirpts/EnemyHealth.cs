using UnityEngine;
using UnityEngine.UI;

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
    private float defenseDown = 0f;
    private bool isDead = false;
    private PlayerAttack lastAttacker = null;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;

    public void Init(float hp, float def = 0f)
    {
        maxHp = hp; currentHp = hp; defense = def; defenseDown = 0f; isDead = false; lastAttacker = null;
        if (hpSlider != null) { hpSlider.maxValue = hp; hpSlider.value = hp; }
        if (hpFillImage != null) hpFillImage.color = Color.green;
    }

    void Start() { if (currentHp <= 0f) Init(maxHp, defense); }

    public void ApplyDefenseDown(float amount) => defenseDown = amount;

    public void TakeDamage(float damage, PlayerAttack attacker = null)
    {
        if (isDead) return;
        if (attacker != null) lastAttacker = attacker;

        float actualDamage;
        if (forceDamageOne)
            actualDamage = 1f;
        else
        {
            float effectiveDefense = Mathf.Max(0f, defense - defenseDown);
            actualDamage = Mathf.Max(1f, damage - effectiveDefense);
        }

        currentHp -= actualDamage;
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpFillImage != null) hpFillImage.color = Color.Lerp(Color.red, Color.green, currentHp / maxHp);
        ShowDamageText(actualDamage);
        if (currentHp <= 0f) Die();
    }

    public void ExecuteKill()
    {
        if (isDead) return;
        currentHp = 0f;
        if (hpSlider != null) hpSlider.value = 0f;
        Die();
    }

    public void TakePercentDamage(float percent, PlayerAttack attacker = null)
    {
        if (isDead) return;
        TakeDamage(maxHp * (percent / 100f), attacker);
    }

    public void ShowDamageText(float damage)
    {
        if (damageTextPrefab == null) return;
        GameObject obj = Instantiate(damageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Init(damage, transform, isSpecial ? 6f : 3f, isSpecial ? 1f : 0.5f);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (GameManager.Instance != null) GameManager.Instance.OnEnemyDied();

        if (isSpecial)
        {
            if (SpecialCoinManager.Instance != null)
                SpecialCoinManager.Instance.AddSpecialCoins(specialCoinReward);
            if (isBoss && GameManager.Instance != null)
                GameManager.Instance.OnBossKilled();
        }
        else
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoins(CoinManager.Instance.coinsPerKill, true);
        }

        if (lastAttacker != null && lastAttacker.characterData != null)
        {
            foreach (PassiveEntry entry in lastAttacker.characterData.passives)
            {
                if (entry.passiveType != PassiveType.SpecialCoinOnKillChance) continue;
                if (Random.Range(0f, 100f) < entry.passiveValue)
                    if (SpecialCoinManager.Instance != null)
                        SpecialCoinManager.Instance.AddSpecialCoins((int)entry.passiveSecondValue);
            }
        }

        Destroy(gameObject);
    }
}
