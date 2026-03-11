using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("Merge Settings")]
    [SerializeField] private string unitType = "Default";
    [SerializeField] private int unitLevel = 1;
    [SerializeField] private float damageIncreasePerMerge = 1.5f;

    [Header("Player Info")]
    public int spawnIndex = -1;

    private float cooldownTimer;
    private EnemyMove currentTarget;
    [SerializeField] private Animator anim;

    public string UnitType => unitType;
    public int UnitLevel => unitLevel;

    void Awake()
    {
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }
    }

    void Start()
    {
        cooldownTimer = attackCooldown;
        Debug.Log($"[Player {spawnIndex}] Spawned at {transform.position}");

        if (anim == null)
        {
            Debug.LogWarning($"[Player {spawnIndex}] Animator reference is missing. Attack animation will not play.");
        }
    }

    void Update()
    {
        cooldownTimer += Time.deltaTime;

        if (cooldownTimer < attackCooldown)
        {
            return;
        }

        AttackWithLockedTarget();
    }

    public bool CanMergeWith(PlayerAttack other)
    {
        if (other == null || other == this)
        {
            return false;
        }

        return other.UnitType == unitType;
    }

    public void MergeFrom(PlayerAttack consumedUnit)
    {
        if (!CanMergeWith(consumedUnit))
        {
            return;
        }

        unitLevel += 1;
        attackDamage *= damageIncreasePerMerge;
        attackRange += 0.2f;
        Debug.Log($"[Player {spawnIndex}] Merged with same type. New level: {unitLevel}, damage: {attackDamage}");
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
        cooldownTimer = 0f;

        if (anim != null)
        {
            anim.SetTrigger("2_Attack");
            Debug.Log("애니메이션 실행됨!");
        }
        else
        {
            Debug.LogError("Player 자식에게서 Animator를 찾을 수 없습니다!");
        }
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
