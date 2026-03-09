// EnemyHealth.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enemy의 HP를 관리하고 머리 위에 체력바를 표시하는 스크립트
/// Enemy 프리팹에 붙여서 사용
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    // ───────────────────────────────────────────
    // Inspector에서 연결할 항목들
    // ───────────────────────────────────────────

    [Header("HP 설정")]
    public float maxHp = 50f;

    [Header("체력바 설정")]
    // Enemy 프리팹 자식으로 만들 Canvas 안의 Slider 연결
    public Slider hpSlider;

    // 체력바 색상 (체력에 따라 초록 → 빨강으로 변함)
    public Image hpFillImage;


    // ───────────────────────────────────────────
    // 내부에서만 사용하는 변수
    // ───────────────────────────────────────────
    private float currentHp;


    // ───────────────────────────────────────────
    // 게임 시작 시 딱 한 번 실행
    // ───────────────────────────────────────────
    void Start()
    {
        currentHp = maxHp;

        // Slider 최대값을 maxHp로 설정
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = maxHp;
        }

        // 시작할 때 체력바 색상 초록으로 초기화
        if (hpFillImage != null)
        {
            hpFillImage.color = Color.green;
        }
    }


    /// <summary>
    /// 외부(PlayerAttack 등)에서 데미지를 전달할 때 호출하는 함수
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHp -= damage;

        // 체력바 수치 업데이트
        if (hpSlider != null)
        {
            hpSlider.value = currentHp;
        }

        // 체력 비율에 따라 색상 변경 (초록 → 노랑 → 빨강)
        if (hpFillImage != null)
        {
            float ratio = currentHp / maxHp; // 0.0 ~ 1.0
            hpFillImage.color = Color.Lerp(Color.red, Color.green, ratio);
        }

        Debug.Log($"[EnemyHealth] 피격! 남은 HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }


    void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} 사망!");
        Destroy(gameObject);
    }
}