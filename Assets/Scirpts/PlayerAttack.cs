using UnityEngine;

/// <summary>
/// ������ �ֱ�� ���� ����� Enemy�� �����ϴ� ��ũ��Ʈ
/// Player �����տ� �ٿ��� ���
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    // ��������������������������������������������������������������������������������������
    // Inspector���� ���� ������ ���� ����
    // ��������������������������������������������������������������������������������������

    [Header("���� ����")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("�÷��̾� ����")]
    // �� �� ���� ����Ʈ���� �����ƴ��� ���� (PlayerSpawner���� ����)
    public int spawnIndex = -1;


    // ��������������������������������������������������������������������������������������
    // ���ο����� ����ϴ� ����
    // ��������������������������������������������������������������������������������������
    private float cooldownTimer;
    private EnemyMove currentTarget;


    // ��������������������������������������������������������������������������������������
    // ���� ���� �� �� �� �� ����
    // ��������������������������������������������������������������������������������������
    void Start()
    {
        cooldownTimer = attackCooldown;

        // ���� ��ġ �α� ���
        Debug.Log($"[Player {spawnIndex}��] ��ġ: {transform.position} �� ������!");
    }


    // ��������������������������������������������������������������������������������������
    // �� ������ ���� �� ��ٿ� ó��
    // ��������������������������������������������������������������������������������������
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

    // ��������������������������������������������������������������������������������������
    // ��Ÿ� �ȿ��� ���� ����� Enemy�� ã�� �����ϴ� �Լ�
    // ��������������������������������������������������������������������������������������
    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
        {
            currentTarget = FindNearestEnemyInRange();
        }

        if (currentTarget == null)
        {
            Debug.Log($"[Player {spawnIndex}�� | ��ġ: {transform.position}] ��Ÿ� �ȿ� Enemy ����");
            return;
        }

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();

        if (health == null)
        {
            currentTarget = null;
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
        health.TakeDamage(attackDamage);

        Debug.Log($"[Player {spawnIndex}�� | ��ġ: {transform.position}] " +
                  $"�� {currentTarget.gameObject.name} ����! " +
                  $"�Ÿ�: {distance:F2} / ������: {attackDamage}");
    }


    EnemyMove FindNearestEnemyInRange()
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

        return nearestEnemy;
    }


    bool IsTargetInRange(EnemyMove enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, enemy.transform.position) <= attackRange;
    }

        return Vector2.Distance(transform.position, enemy.transform.position) <= attackRange;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
