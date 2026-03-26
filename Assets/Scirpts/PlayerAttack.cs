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
    [SerializeField] private float appliedDamage;
    [SerializeField] private float appliedCooldown;

    public string UnitType => characterData != null ? characterData.characterName : "";
    public int MergeCount => mergeCount;

    void Start()
    {
        if (characterData == null) { Debug.LogError($"[Player {spawnIndex}] CharacterData is missing!"); enabled = false; return; }
        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
    }

    void ApplyUpgradeStats()
    {
        float damageMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetAttackDamageMultiplier() : 1f;
        float speedMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetAttackSpeedMultiplier() : 1f;
        appliedDamage = characterData.attackDamage * damageMultiplier;
        appliedCooldown = characterData.attackCooldown * speedMultiplier;
        if (appliedCooldown <= 0f) appliedCooldown = 0.1f;
    }

    void Update()
    {
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < appliedCooldown) return;
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
        if (MergeManager.Instance != null) MergeManager.Instance.CheckMergeAvailable();
        return false;
    }

    public void ForceUpgrade() { }

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

        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
    }

    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
            currentTarget = FindBackmostEnemyInRange();

        if (currentTarget == null) return;

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null) { currentTarget = null; return; }

        health.TakeDamage(appliedDamage);
        cooldownTimer = 0f;
    }

    EnemyMove FindBackmostEnemyInRange()
    {
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
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
        if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
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