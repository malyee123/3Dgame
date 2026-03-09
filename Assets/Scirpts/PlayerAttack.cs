using UnityEngine;

/// <summary>
/// 일정 주기로 적을 자동 공격하는 스크립트
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("공격 설정")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("플레이어 정보")]
    public int spawnIndex = -1;

    private float cooldownTimer;
    private EnemyMove currentTarget;

    void Start()
    {
        cooldownTimer = attackCooldown;
        Debug.Log($"[Player {spawnIndex}] Spawned at {transform.position}");
    }

    void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer < attackCooldown)
        {
            return;
        }

        cooldownTimer = 0f;
        AttackWithLockedTarget();
    }

    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
        {
            currentTarget = FindBackmostEnemyInRange();
        }

        if (currentTarget == null)
        {
            return;
        }

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null)
        {
            currentTarget = null;
            return;
        }

        health.TakeDamage(attackDamage);
    }

    EnemyMove FindBackmostEnemyInRange()
    {
        EnemyMove[] enemies = FindObjectsOfType<EnemyMove>();

        EnemyMove backmostEnemy = null;
        float smallestProgress = Mathf.Infinity;
        float fallbackNearestDistance = Mathf.Infinity;

        foreach (EnemyMove enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > attackRange)
            {
                continue;
            }

            float progress = enemy.GetPathProgress();

            if (progress < smallestProgress)
            {
                smallestProgress = progress;
                fallbackNearestDistance = distance;
                backmostEnemy = enemy;
            }
            else if (Mathf.Approximately(progress, smallestProgress) && distance < fallbackNearestDistance)
            {
                fallbackNearestDistance = distance;
                backmostEnemy = enemy;
            }
        }

        return backmostEnemy;
    }

    bool IsTargetInRange(EnemyMove enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, enemy.transform.position) <= attackRange;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
