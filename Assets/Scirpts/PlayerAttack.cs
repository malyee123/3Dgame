using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Character Data")]
    public CharacterData characterData;

    [Header("Player Info")]
    public int spawnIndex = -1;
    public string unitTag = "";
    public bool isLeader = false;

    private float cooldownTimer;
    private EnemyMove currentTarget;

    [SerializeField] private float appliedDamage;
    [SerializeField] private float appliedCooldown;
    [SerializeField] private float passiveDamageBonus = 0f;
    [SerializeField] private float passiveSpeedBonus = 0f;
    [SerializeField] private float doubleDamageChance = 0f;
    [SerializeField] private float attackTwiceChance = 0f;
    [SerializeField] private float selfSpeedUpChance = 0f;
    [SerializeField] private float selfSpeedUpAmount = 0f;
    [SerializeField] private float selfSpeedUpDuration = 0f;

    private bool isSelfSpeedBoosted = false;
    private bool isDragging = false;
    private SPUM_Prefabs spumPrefabs;

    public string UnitType => characterData != null ? characterData.characterName : "";

    void Start()
    {
        if (characterData == null) { Debug.LogError($"[Player {spawnIndex}] CharacterData is missing!"); enabled = false; return; }
        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
        spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        if (spumPrefabs != null && spumPrefabs.OverrideController == null)
            spumPrefabs.OverrideControllerInit();
    }

    void ApplyUpgradeStats()
    {
        float damageMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetAttackDamageMultiplier() : 1f;
        float speedMultiplier = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetAttackSpeedMultiplier() : 1f;
        appliedDamage = characterData.attackDamage * damageMultiplier * (1f + passiveDamageBonus / 100f);
        appliedCooldown = Mathf.Max(0.1f, characterData.attackCooldown * speedMultiplier * (1f - passiveSpeedBonus / 100f));
    }

    public void ApplyPassiveBonus(float damageBonus, float speedBonus, float doubleChance, float twiceChance, float selfSpeedChance, float selfSpeedAmount, float selfSpeedDuration)
    {
        passiveDamageBonus = damageBonus;
        passiveSpeedBonus = speedBonus;
        doubleDamageChance = doubleChance;
        attackTwiceChance = twiceChance;
        selfSpeedUpChance = selfSpeedChance;
        selfSpeedUpAmount = selfSpeedAmount;
        selfSpeedUpDuration = selfSpeedDuration;
        ApplyUpgradeStats();
    }

    public void SetDragging(bool dragging) { isDragging = dragging; }

    void Update()
    {
        if (isDragging || !isLeader) return;
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < appliedCooldown) return;
        AttackWithLockedTarget();
    }

    public bool CanMergeWith(PlayerAttack other)
    {
        if (other == null || other == this) return false;
        return other.characterData == characterData;
    }

    public void ApplyCharacterData(CharacterData newData)
    {
        if (newData == null) return;
        characterData = newData;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = characterData.characterColor;
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        if (characterData.characterPrefab != null)
        {
            GameObject visual = Instantiate(characterData.characterPrefab, transform);
            visual.transform.localPosition = Vector3.zero;
        }
        passiveDamageBonus = passiveSpeedBonus = doubleDamageChance = attackTwiceChance = selfSpeedUpChance = selfSpeedUpAmount = selfSpeedUpDuration = 0f;
        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
        StartCoroutine(InitSpumAfterFrame());
    }

    System.Collections.IEnumerator InitSpumAfterFrame()
    {
        yield return null;
        spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        if (spumPrefabs != null && spumPrefabs.OverrideController == null)
            spumPrefabs.OverrideControllerInit();
    }

    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
            currentTarget = FindBackmostEnemyInRange();
        if (currentTarget == null) return;

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null) { currentTarget = null; return; }

        if (spumPrefabs != null && spumPrefabs.OverrideController != null)
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);

        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit == this || unit.spawnIndex != spawnIndex) continue;
            SPUM_Prefabs mateSpum = unit.GetComponentInChildren<SPUM_Prefabs>();
            if (mateSpum != null && mateSpum.OverrideController != null)
                mateSpum.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);
        }

        float finalDamage = appliedDamage;
        if (doubleDamageChance > 0f && Random.Range(0f, 100f) < doubleDamageChance) finalDamage *= 2f;

        StartCoroutine(DealDamageWithDelay(currentTarget, finalDamage, false));

        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
            StartCoroutine(SecondAttackAnimRoutine());

        if (selfSpeedUpChance > 0f && !isSelfSpeedBoosted && Random.Range(0f, 100f) < selfSpeedUpChance)
            StartCoroutine(SelfSpeedBoostRoutine());

        cooldownTimer = 0f;
    }

    System.Collections.IEnumerator DealDamageWithDelay(EnemyMove target, float damage, bool isTwice)
    {
        yield return new WaitForSeconds(0.3f);
        if (target == null) yield break;
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health == null) yield break;
        health.TakeDamage(damage, this);
        if (isTwice) yield break;
        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
            StartCoroutine(DealDamageWithDelay(target, appliedDamage, true));
    }

    System.Collections.IEnumerator SecondAttackAnimRoutine()
    {
        yield return new WaitForSeconds(appliedCooldown * 0.5f);
        if (spumPrefabs != null && spumPrefabs.OverrideController != null)
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit == this || unit.spawnIndex != spawnIndex) continue;
            SPUM_Prefabs mateSpum = unit.GetComponentInChildren<SPUM_Prefabs>();
            if (mateSpum != null && mateSpum.OverrideController != null)
                mateSpum.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);
        }
    }

    System.Collections.IEnumerator SelfSpeedBoostRoutine()
    {
        isSelfSpeedBoosted = true;
        appliedCooldown = Mathf.Max(0.1f, appliedCooldown * (1f - selfSpeedUpAmount / 100f));
        yield return new WaitForSeconds(selfSpeedUpDuration);
        ApplyUpgradeStats();
        isSelfSpeedBoosted = false;
    }

    EnemyMove FindBackmostEnemyInRange()
    {
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        EnemyMove backmostEnemy = null;
        EnemyMove bossTarget = null;
        float smallestProgress = Mathf.Infinity;
        float fallbackNearestDistance = Mathf.Infinity;

        foreach (EnemyMove enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > characterData.attackRange) continue;
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null && health.isSpecial)
            {
                bossTarget = enemy;
                continue;
            }
            float progress = enemy.GetPathProgress();
            if (progress < smallestProgress || (Mathf.Approximately(progress, smallestProgress) && distance < fallbackNearestDistance))
            {
                smallestProgress = progress;
                fallbackNearestDistance = distance;
                backmostEnemy = enemy;
            }
        }
        return bossTarget != null ? bossTarget : backmostEnemy;
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