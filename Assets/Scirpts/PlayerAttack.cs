using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Character Data")]
    public CharacterData characterData;

    [Header("Player Info")]
    public int spawnIndex = -1;
    public string unitTag = "";

    private float cooldownTimer;
    private EnemyMove currentTarget;
    private int mergeCount = 0;

    public string UnitType => characterData != null ? characterData.characterName : "";
    public int MergeCount => mergeCount;

    void Start()
    {
        if (characterData == null) { Debug.LogError($"[Player {spawnIndex}] CharacterData is missing!"); enabled = false; return; }
        cooldownTimer = characterData.attackCooldown;
    }

    void Update()
    {
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < characterData.attackCooldown) return;
        AttackWithLockedTarget();
    }

    public bool CanMergeWith(PlayerAttack other)
    {
        if (other == null || other == this) return false;
        return other.characterData == characterData;
    }

    public bool TryMerge(PlayerAttack consumedUnit)
    {
        if (!CanMergeWith(consumedUnit)) return false;

        mergeCount++;
        Debug.Log($"[Player {spawnIndex}] Merge progress: {mergeCount}/2");

        if (MergeManager.Instance != null)
            MergeManager.Instance.CheckMergeAvailable();

        return false;
    }

    public void ForceUpgrade()
    {
        Debug.LogWarning($"[Player {spawnIndex}] ForceUpgrade() is deprecated. Use PlayerSpawner merge flow.");
    }


    public void ApplyCharacterData(CharacterData newData)
    {
        if (newData == null) return;

        characterData = newData;
        mergeCount = 0;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = characterData.characterColor;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        if (characterData.characterPrefab != null)
        {
            GameObject visual = Instantiate(characterData.characterPrefab, transform);
            visual.transform.localPosition = Vector3.zero;
        }

        cooldownTimer = characterData.attackCooldown;

    }

    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
            currentTarget = FindBackmostEnemyInRange();

        if (currentTarget == null) return;

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null) { currentTarget = null; return; }

        health.TakeDamage(characterData.attackDamage);
        cooldownTimer = 0f;
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
            if (distance > characterData.attackRange) continue;

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


    void OnMouseDown()
    {
        if (MergeManager.Instance != null)
            MergeManager.Instance.SelectUnit(this);
    }

    bool IsTargetInRange(EnemyMove enemy)
    {
        if (enemy == null) return false;
        return Vector2.Distance(transform.position, enemy.transform.position) <= characterData.attackRange;
    }

    void OnDrawGizmosSelected()
    {
        if (characterData == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, characterData.attackRange);
    }
}
