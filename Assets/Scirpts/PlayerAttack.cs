using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("Player Info")]
    public int spawnIndex = -1;

    private float cooldownTimer;
    private EnemyMove currentTarget;
    [SerializeField] private Animator anim;

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

    void AttackWithLockedTarget()
    {
        // 1. 타겟이 범위 밖이면 새로 찾기
        if (!IsTargetInRange(currentTarget))
        {
            currentTarget = FindBackmostEnemyInRange();
        }

        // 2. 타겟이 없으면 여기서 중단 (쿨타임 리셋 하지 않음)
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

        // --- [중요] 실제 공격이 일어나는 지점 ---

        // 1. 데미지 입히기
        health.TakeDamage(attackDamage);
        cooldownTimer = 0f;

        // 2. 애니메이터 확인 및 강제 재생
        if (anim != null)
        {
            // 트리거 대신 애니메이션 이름을 직접 불러서 강제 재생합니다.
            // 세 번째 인자 0f는 애니메이션의 처음부터 재생하라는 뜻입니다.
            anim.SetTrigger("2_Attack");
            Debug.Log("애니메이션 실행됨!");
        }
        else
        {
            // 만약 애니메이터를 못 찾았다면 콘솔에 에러를 띄웁니다.
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
