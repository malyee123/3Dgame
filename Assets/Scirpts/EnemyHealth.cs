using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHp = 50f;
    public float defense = 0f;

    [Header("HP Bar Settings")]
    public Slider hpSlider;
    public Image hpFillImage;

    [Header("Special Monster Settings")]
    public bool isSpecial = false;
    public int specialCoinReward = 0;

    private float currentHp;
    private bool isDead = false;
    private PlayerAttack lastAttacker = null;
    public GameObject damageTextPrefab;

    public void Init(float hp, float def = 0f)
    {
        maxHp = hp;
        currentHp = hp;
        defense = def;
        isDead = false;
        lastAttacker = null;
        if (hpSlider != null) { hpSlider.maxValue = hp; hpSlider.value = hp; }
        if (hpFillImage != null) hpFillImage.color = Color.green;
    }

    void Start()
    {
        if (currentHp <= 0f) Init(maxHp, defense);
    }

    public void TakeDamage(float damage, PlayerAttack attacker = null)
    {
        if (isDead) return;
        if (attacker != null) lastAttacker = attacker;
        float actualDamage = Mathf.Max(1f, damage - defense);
        currentHp -= actualDamage;
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpFillImage != null) hpFillImage.color = Color.Lerp(Color.red, Color.green, currentHp / maxHp);
        ShowDamageText(actualDamage);
        if (currentHp <= 0) Die();
    }

    void ShowDamageText(float damage)
    {
        if (damageTextPrefab == null) return;
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject obj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        DamageText dt = obj.GetComponent<DamageText>();
        if (dt != null) dt.Init(damage, transform);
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
                {
                    int coinAmount = (int)entry.passiveSecondValue;
                    if (SpecialCoinManager.Instance != null)
                        SpecialCoinManager.Instance.AddSpecialCoins(coinAmount);
                }
            }
        }
        Destroy(gameObject);
    }
}