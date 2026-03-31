using UnityEngine;
using UnityEngine.UI;

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

    void Start()
    {
        currentHp = maxHp;
        if (hpSlider != null) { hpSlider.maxValue = maxHp; hpSlider.value = maxHp; }
        if (hpFillImage != null) hpFillImage.color = Color.green;
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
                CoinManager.Instance.AddCoins(CoinManager.Instance.coinsPerKill);
        }
        // Debug.Log($"[EnemyHealth] {gameObject.name} died. Special: {isSpecial}");
        Destroy(gameObject);
    }
}