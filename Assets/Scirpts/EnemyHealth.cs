using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public float maxHp = 50f;

    [Header("HP Bar Settings")]
    public Slider hpSlider;
    public Image hpFillImage;

    private float currentHp;

    void Start()
    {
        currentHp = maxHp;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
        }

        if (hpFillImage != null)
        {
            hpFillImage.color = Color.green;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;

        if (hpSlider != null)
        {
            hpSlider.value = currentHp;
        }

        if (hpFillImage != null)
        {
            float ratio = currentHp / maxHp;
            hpFillImage.color = Color.Lerp(Color.red, Color.green, ratio);
        }

        Debug.Log($"[EnemyHealth] Hit. Remaining HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} died.");
        Destroy(gameObject);
    }
}
