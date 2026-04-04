using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHp = 50f;

    [Header("HP Bar Settings")]
    public Slider hpSlider;
    public Image hpFillImage;

    [Header("Special Monster Settings")]
    public bool isSpecial = false;
    public int specialCoinReward = 0;

    private float currentHp;
    private bool isDead = false;

    public void Init(float hp)
    {
        maxHp = hp;
        currentHp = hp;
        isDead = false;
        if (hpSlider != null) { hpSlider.maxValue = hp; hpSlider.value = hp; }
        if (hpFillImage != null) hpFillImage.color = Color.green;
    }

    void Start()
    {
        if (currentHp <= 0f) Init(maxHp);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHp -= damage;
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpFillImage != null) hpFillImage.color = Color.Lerp(Color.red, Color.green, currentHp / maxHp);
        if (currentHp <= 0) Die();
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
        Destroy(gameObject);
    }
}