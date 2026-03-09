using UnityEngine;

/// <summary>
/// 스페이스바 입력 시 가장 가까운 Enemy를 공격하는 스크립트
/// Player 프리팹에 붙여서 사용
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    // ───────────────────────────────────────────
    // Inspector에서 조절 가능한 공격 설정
    // ───────────────────────────────────────────

    [Header("공격 설정")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("플레이어 정보")]
    // 몇 번 스폰 포인트에서 생성됐는지 저장 (PlayerSpawner에서 주입)
    public int spawnIndex = -1;


    // ───────────────────────────────────────────
    // 내부에서만 사용하는 변수
    // ───────────────────────────────────────────
    private float cooldownTimer;


    // ───────────────────────────────────────────
    // 게임 시작 시 딱 한 번 실행
    // ───────────────────────────────────────────
    void Start()
    {
        cooldownTimer = attackCooldown;

        // 생성 위치 로그 출력
        Debug.Log($"[Player {spawnIndex}번] 위치: {transform.position} 에 생성됨!");
    }


    // ───────────────────────────────────────────
    // 매 프레임 실행 → 입력 감지 & 쿨다운 처리
    // ───────────────────────────────────────────
    void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && cooldownTimer >= attackCooldown)
        {
            cooldownTimer = 0f;
            AttackNearestEnemy();
        }
    }


    // ───────────────────────────────────────────
    // 사거리 안에서 가장 가까운 Enemy를 찾아 공격하는 함수
    // ───────────────────────────────────────────
    void AttackNearestEnemy()
    {
        EnemyMove[] enemies = FindObjectsOfType<EnemyMove>();

        EnemyMove nearestEnemy = null;
        float minDistance = Mathf.Infinity;

        foreach (EnemyMove enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance <= attackRange && distance < minDistance)
            {
                minDistance = distance;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
        {
            EnemyHealth health = nearestEnemy.GetComponent<EnemyHealth>();

            if (health != null)
            {
                health.TakeDamage(attackDamage);

                // 몇 번 플레이어가 어디서 어느 적을 공격했는지 로그
                Debug.Log($"[Player {spawnIndex}번 | 위치: {transform.position}] " +
                          $"→ {nearestEnemy.gameObject.name} 공격! " +
                          $"거리: {minDistance:F2} / 데미지: {attackDamage}");
            }
        }
        else
        {
            Debug.Log($"[Player {spawnIndex}번 | 위치: {transform.position}] 사거리 안에 Enemy 없음");
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}