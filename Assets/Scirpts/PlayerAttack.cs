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
        appliedCooldown = characterData.attackCooldown * speedMultiplier * (1f - passiveSpeedBonus / 100f);
        if (appliedCooldown <= 0.1f) appliedCooldown = 0.1f;
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
        passiveDamageBonus = 0f;
        passiveSpeedBonus = 0f;
        doubleDamageChance = 0f;
        attackTwiceChance = 0f;
        selfSpeedUpChance = 0f;
        selfSpeedUpAmount = 0f;
        selfSpeedUpDuration = 0f;
        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
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
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, 0);

        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit == this || unit.spawnIndex != spawnIndex) continue;
            SPUM_Prefabs mateSpum = unit.GetComponentInChildren<SPUM_Prefabs>();
            if (mateSpum != null && mateSpum.OverrideController != null)
                mateSpum.PlayAnimation(PlayerState.ATTACK, 0);
        }

        float finalDamage = appliedDamage;
        if (doubleDamageChance > 0f && Random.Range(0f, 100f) < doubleDamageChance)
        {
            finalDamage *= 2f;
            Debug.Log($"[{characterData.characterName}] 2배 피해 발동! 데미지: {finalDamage}");
        }
        health.TakeDamage(finalDamage);

        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
        {
            health.TakeDamage(appliedDamage);
            Debug.Log($"[{characterData.characterName}] 2번 타격 발동! 데미지: {appliedDamage}");
        }

        if (selfSpeedUpChance > 0f && !isSelfSpeedBoosted && Random.Range(0f, 100f) < selfSpeedUpChance)
        {
            StartCoroutine(SelfSpeedBoostRoutine());
            Debug.Log($"[{characterData.characterName}] 공속 일시 증가 발동! 증가량: {selfSpeedUpAmount}% / 지속: {selfSpeedUpDuration}초");
        }

        cooldownTimer = 0f;
    }

    System.Collections.IEnumerator SelfSpeedBoostRoutine()
    {
        isSelfSpeedBoosted = true;
        appliedCooldown *= (1f - selfSpeedUpAmount / 100f);
        if (appliedCooldown <= 0.1f) appliedCooldown = 0.1f;
        yield return new WaitForSeconds(selfSpeedUpDuration);
        ApplyUpgradeStats();
        isSelfSpeedBoosted = false;
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
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        if (MergeManager.Instance != null) MergeManager.Instance.SelectUnit(this);
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