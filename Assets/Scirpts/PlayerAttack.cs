using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public CharacterData characterData;
    public int spawnIndex = -1;
    public string unitTag = "";
    public bool isLeader = false;

    private float cooldownTimer;
    private EnemyMove currentTarget;

    [SerializeField] private float appliedDamage;
    [SerializeField] private float appliedCooldown;
    [SerializeField] private float passiveDamageBonus;
    [SerializeField] private float passiveSpeedBonus;
    [SerializeField] private float doubleDamageChance;
    [SerializeField] private float attackTwiceChance;
    [SerializeField] private float selfSpeedUpChance;
    [SerializeField] private float selfSpeedUpAmount;
    [SerializeField] private float selfSpeedUpDuration;
    [SerializeField] private float selfDamageUpChance;
    [SerializeField] private float selfDamageUpAmount;
    [SerializeField] private float selfDamageUpDuration;
    [SerializeField] private float stunChance;
    [SerializeField] private float stunDuration;
    [SerializeField] private float executeChance;
    [SerializeField] private float executeHpThreshold;
    [SerializeField] private float executeBossDamagePercent;
    [SerializeField] private float buffAllyChance;
    [SerializeField] private float buffAllyAmount;
    [SerializeField] private float buffAllyDuration;
    [SerializeField] private float aoeStunEveryN;
    [SerializeField] private float aoeStunRange;
    [SerializeField] private float aoeStunDuration;
    [SerializeField] private bool bossDamageDouble;
    [SerializeField] private float areaSpeedDownChance;
    [SerializeField] private float areaSpeedDownAmount;
    [SerializeField] private float areaSpeedDownDuration;
    [SerializeField] private float magicMissileChance;
    [SerializeField] private float magicMissileDamagePercent;
    [SerializeField] private float slamChance;
    [SerializeField] private float slamDamagePercent;
    [SerializeField] private float slamRange;
    [SerializeField] private float manaSkillDamage;
    [SerializeField] private float manaSkillDuration;
    [SerializeField] private float manaSkillInterval;

    private bool isSelfSpeedBoosted = false;
    private bool isSelfDamageBoosted = false;
    private bool isAllySpeedBoosted = false;
    private bool isDragging = false;
    private SPUM_Prefabs spumPrefabs;

    private int hitCounter = 0;
    private const int gridWidth = 5;

    private List<PlayerAttack> cachedSlotMates = new List<PlayerAttack>();
    private bool slotMatesDirty = true;

    public string UnitType => characterData != null ? characterData.characterName : "";

    void Start()
    {
        if (characterData == null) { Debug.LogError($"[PlayerAttack {spawnIndex}] CharacterData missing!"); enabled = false; return; }
        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
        spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        if (spumPrefabs != null && spumPrefabs.OverrideController == null)
            spumPrefabs.OverrideControllerInit();
    }

    void ApplyUpgradeStats()
    {
        float dmgMult = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetAttackDamageMultiplier() : 1f;
        float spdMult = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetAttackSpeedMultiplier() : 1f;
        appliedDamage = characterData.attackDamage * dmgMult * (1f + passiveDamageBonus / 100f);
        appliedCooldown = Mathf.Max(0.1f, characterData.attackCooldown * spdMult * (1f - passiveSpeedBonus / 100f));
    }

    public void ApplyPassiveBonus(float damageBonus, float speedBonus, float doubleChance, float twiceChance,
        float selfSpeedChance, float selfSpeedAmount, float selfSpeedDuration,
        float selfDamageChance, float selfDamageAmount, float selfDamageDuration,
        float stunChance, float stunDuration,
        float executeChance, float executeHpThreshold, float executeBossDamagePercent,
        float buffAllyChance, float buffAllyAmount, float buffAllyDuration,
        float aoeStunEveryN, float aoeStunRange, float aoeStunDuration,
        bool bossDamageDouble,
        float areaSpeedDownChance, float areaSpeedDownAmount, float areaSpeedDownDuration,
        float magicMissileChance, float magicMissileDamagePercent,
        float slamChance, float slamDamagePercent, float slamRange,
        float manaSkillDamage, float manaSkillDuration, float manaSkillInterval)
    {
        passiveDamageBonus = damageBonus;
        passiveSpeedBonus = speedBonus;
        doubleDamageChance = doubleChance;
        attackTwiceChance = twiceChance;
        selfSpeedUpChance = selfSpeedChance;
        selfSpeedUpAmount = selfSpeedAmount;
        selfSpeedUpDuration = selfSpeedDuration;
        selfDamageUpChance = selfDamageChance;
        selfDamageUpAmount = selfDamageAmount;
        selfDamageUpDuration = selfDamageDuration;
        this.stunChance = stunChance;
        this.stunDuration = stunDuration;
        this.executeChance = executeChance;
        this.executeHpThreshold = executeHpThreshold;
        this.executeBossDamagePercent = executeBossDamagePercent;
        this.buffAllyChance = buffAllyChance;
        this.buffAllyAmount = buffAllyAmount;
        this.buffAllyDuration = buffAllyDuration;
        this.aoeStunEveryN = aoeStunEveryN;
        this.aoeStunRange = aoeStunRange;
        this.aoeStunDuration = aoeStunDuration;
        this.bossDamageDouble = bossDamageDouble;
        this.areaSpeedDownChance = areaSpeedDownChance;
        this.areaSpeedDownAmount = areaSpeedDownAmount;
        this.areaSpeedDownDuration = areaSpeedDownDuration;
        this.magicMissileChance = magicMissileChance;
        this.magicMissileDamagePercent = magicMissileDamagePercent;
        this.slamChance = slamChance;
        this.slamDamagePercent = slamDamagePercent;
        this.slamRange = slamRange;
        this.manaSkillDamage = manaSkillDamage;
        this.manaSkillDuration = manaSkillDuration;
        this.manaSkillInterval = manaSkillInterval;
        ApplyUpgradeStats();
    }

    public void ReceiveAllySpeedBuff(float amount, float duration)
    {
        if (isAllySpeedBoosted) return;
        StartCoroutine(AllySpeedBuffRoutine(amount, duration));
    }

    IEnumerator AllySpeedBuffRoutine(float amount, float duration)
    {
        isAllySpeedBoosted = true;
        float original = appliedCooldown;
        appliedCooldown = Mathf.Max(0.1f, appliedCooldown * (1f - amount / 100f));
        yield return new WaitForSeconds(duration);
        appliedCooldown = original;
        isAllySpeedBoosted = false;
    }

    public void SetDragging(bool dragging) { isDragging = dragging; slotMatesDirty = true; }
    public void MarkSlotMatesDirty() => slotMatesDirty = true;

    void Update()
    {
        if (isDragging || !isLeader) return;
        if (GameManager.Instance != null && GameManager.Instance.IsWarning) return;
        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < appliedCooldown) return;
        AttackWithLockedTarget();
    }

    public void ApplyCharacterData(CharacterData newData)
    {
        if (newData == null) return;
        characterData = newData;
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        if (characterData.characterPrefab != null)
        {
            GameObject visual = Instantiate(characterData.characterPrefab, transform);
            visual.transform.localPosition = Vector3.zero;
        }
        passiveDamageBonus = passiveSpeedBonus = doubleDamageChance = attackTwiceChance =
            selfSpeedUpChance = selfSpeedUpAmount = selfSpeedUpDuration =
            selfDamageUpChance = selfDamageUpAmount = selfDamageUpDuration =
            stunChance = stunDuration =
            executeChance = executeHpThreshold = executeBossDamagePercent =
            buffAllyChance = buffAllyAmount = buffAllyDuration =
            aoeStunEveryN = aoeStunRange = aoeStunDuration =
            areaSpeedDownChance = areaSpeedDownAmount = areaSpeedDownDuration =
            magicMissileChance = magicMissileDamagePercent =
            slamChance = slamDamagePercent = slamRange =
            manaSkillDamage = manaSkillDuration = manaSkillInterval = 0f;
        bossDamageDouble = false;
        hitCounter = 0;
        ApplyUpgradeStats();
        cooldownTimer = appliedCooldown;
        slotMatesDirty = true;
        StartCoroutine(InitSpumAfterFrame());
    }

    IEnumerator InitSpumAfterFrame()
    {
        yield return null;
        spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        if (spumPrefabs != null && spumPrefabs.OverrideController == null)
            spumPrefabs.OverrideControllerInit();
    }

    void RefreshSlotMatesCache()
    {
        cachedSlotMates.Clear();
        PlayerAttack[] all = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in all)
        {
            if (unit == null || unit == this || unit.spawnIndex != spawnIndex) continue;
            cachedSlotMates.Add(unit);
        }
        slotMatesDirty = false;
    }

    void PlayAttackAnimAll()
    {
        if (spumPrefabs != null && spumPrefabs.OverrideController != null)
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);
        if (slotMatesDirty) RefreshSlotMatesCache();
        foreach (PlayerAttack mate in cachedSlotMates)
        {
            if (mate == null) continue;
            SPUM_Prefabs mateSpum = mate.GetComponentInChildren<SPUM_Prefabs>();
            if (mateSpum != null && mateSpum.OverrideController != null)
                mateSpum.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);
        }
    }

    int GetSlotUnitCount()
    {
        if (slotMatesDirty) RefreshSlotMatesCache();
        return cachedSlotMates.Count + 1;
    }

    void SpawnHitEffect(Vector3 position, Transform parent)
    {
        if (characterData.hitEffectPrefab == null) return;
        Vector3 effectPos = position + Vector3.up * characterData.hitEffectOffsetY;
        GameObject effect = Instantiate(characterData.hitEffectPrefab, effectPos, Quaternion.identity);
        if (parent != null) effect.transform.SetParent(parent);
        Destroy(effect, characterData.hitEffectDuration);
    }

    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
            currentTarget = FindBackmostEnemyInRange();
        if (currentTarget == null) return;

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null) { currentTarget = null; return; }

        PlayAttackAnimAll();

        int slotCount = GetSlotUnitCount();
        float finalDamage = appliedDamage * slotCount;
        if (doubleDamageChance > 0f && Random.Range(0f, 100f) < doubleDamageChance)
            finalDamage *= 2f;
        if (bossDamageDouble && health.isBoss)
            finalDamage *= 2f;

        bool slamFired = slamChance > 0f && Random.Range(0f, 100f) < slamChance;
        bool missileFired = magicMissileChance > 0f && Random.Range(0f, 100f) < magicMissileChance;

        if (!slamFired && !missileFired)
        {
            if (characterData.projectilePrefab != null)
                StartCoroutine(FireProjectileWithDelay(currentTarget, finalDamage));
            else
                StartCoroutine(DealDamageWithDelay(currentTarget, finalDamage, false));
        }

        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
            StartCoroutine(SecondAttackAnimRoutine());

        if (selfSpeedUpChance > 0f && !isSelfSpeedBoosted && Random.Range(0f, 100f) < selfSpeedUpChance)
            StartCoroutine(SelfSpeedBoostRoutine());

        if (selfDamageUpChance > 0f && !isSelfDamageBoosted && Random.Range(0f, 100f) < selfDamageUpChance)
            StartCoroutine(SelfDamageBoostRoutine());

        if (stunChance > 0f && Random.Range(0f, 100f) < stunChance)
            currentTarget.ApplyStun(stunDuration);

        if (executeChance > 0f && Random.Range(0f, 100f) < executeChance)
            StartCoroutine(ExecuteCheckRoutine(currentTarget));

        if (buffAllyChance > 0f && Random.Range(0f, 100f) < buffAllyChance)
            BuffNearbyAllies();

        if (aoeStunEveryN > 0f)
        {
            hitCounter++;
            if (hitCounter >= (int)aoeStunEveryN)
            {
                hitCounter = 0;
                StartCoroutine(AoeStunRoutine(currentTarget));
            }
        }

        if (areaSpeedDownChance > 0f && Random.Range(0f, 100f) < areaSpeedDownChance)
            StartCoroutine(AreaSpeedDownRoutine());

        if (slamFired) StartCoroutine(SlamRoutine(currentTarget));
        if (missileFired) StartCoroutine(MagicMissileRoutine());

        cooldownTimer = 0f;
    }

    IEnumerator FireProjectileWithDelay(EnemyMove target, float damage)
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        if (target == null) yield break;
        GameObject proj = Instantiate(characterData.projectilePrefab, transform.position, Quaternion.identity);
        Projectile projectile = proj.AddComponent<Projectile>();
        projectile.Init(target.transform, characterData.projectileSpeed, damage, this, characterData.hitEffectPrefab, characterData.hitEffectDuration, characterData.hitEffectOffsetY);
    }

    IEnumerator SlamRoutine(EnemyMove target)
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        if (target == null) yield break;
        Vector3 targetPos = target.transform.position;
        float damage = appliedDamage * (slamDamagePercent / 100f);
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
        {
            if (eh == null) continue;
            if (Vector2.Distance(eh.transform.position, targetPos) <= slamRange)
                eh.TakeDamage(damage, this);
        }
    }

    IEnumerator MagicMissileRoutine()
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        float damage = appliedDamage * (magicMissileDamagePercent / 100f);
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
        {
            if (eh == null) continue;
            eh.TakeDamage(damage, this);
        }
    }

    IEnumerator AreaSpeedDownRoutine()
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        EnemyMove[] allEnemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove enemy in allEnemies)
        {
            if (enemy == null) continue;
            if (Vector2.Distance(transform.position, enemy.transform.position) <= characterData.attackRange)
                enemy.ApplyTempSpeedPenalty(areaSpeedDownAmount, areaSpeedDownDuration);
        }
    }

    IEnumerator AoeStunRoutine(EnemyMove target)
    {
        Vector3 targetPos = target != null ? target.transform.position : transform.position;
        yield return new WaitForSeconds(characterData.attackHitDelay);
        EnemyMove[] allEnemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove enemy in allEnemies)
        {
            if (enemy == null || enemy == target) continue;
            if (Vector2.Distance(enemy.transform.position, targetPos) <= aoeStunRange)
            {
                enemy.ApplyStun(aoeStunDuration);
                EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
                if (eh != null) eh.TakeDamage(appliedDamage, this);
            }
        }
    }

    void BuffNearbyAllies()
    {
        if (spawnIndex < 0) return;
        int idx = spawnIndex;
        List<int> adjacentSlots = new List<int>();
        if (idx % gridWidth != 0) adjacentSlots.Add(idx - 1);
        if (idx % gridWidth != gridWidth - 1) adjacentSlots.Add(idx + 1);
        if (idx - gridWidth >= 0) adjacentSlots.Add(idx - gridWidth);
        if (idx + gridWidth < 25) adjacentSlots.Add(idx + gridWidth);
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (int slot in adjacentSlots)
            foreach (PlayerAttack unit in allUnits)
                if (unit != null && unit.spawnIndex == slot && unit.isLeader)
                    unit.ReceiveAllySpeedBuff(buffAllyAmount, buffAllyDuration);
    }

    IEnumerator ExecuteCheckRoutine(EnemyMove target)
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        if (target == null) yield break;
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health == null) yield break;
        if (health.isSpecial)
        {
            if (executeBossDamagePercent > 0f)
                health.TakePercentDamage(executeBossDamagePercent, this);
        }
        else
        {
            float hpPercent = health.CurrentHp / health.MaxHp * 100f;
            if (hpPercent < executeHpThreshold)
                health.ExecuteKill();
        }
    }

    IEnumerator DealDamageWithDelay(EnemyMove target, float damage, bool isTwice)
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        if (target == null) yield break;
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health == null) yield break;
        health.TakeDamage(damage, this);
        SpawnHitEffect(target.transform.position, target.transform);
        if (isTwice) yield break;
        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
            StartCoroutine(DealDamageWithDelay(target, appliedDamage, true));
    }

    IEnumerator SecondAttackAnimRoutine()
    {
        yield return new WaitForSeconds(appliedCooldown * 0.5f);
        PlayAttackAnimAll();
    }

    IEnumerator SelfSpeedBoostRoutine()
    {
        isSelfSpeedBoosted = true;
        float original = appliedCooldown;
        appliedCooldown = Mathf.Max(0.1f, appliedCooldown * (1f - selfSpeedUpAmount / 100f));
        yield return new WaitForSeconds(selfSpeedUpDuration);
        appliedCooldown = original;
        isSelfSpeedBoosted = false;
    }

    IEnumerator SelfDamageBoostRoutine()
    {
        isSelfDamageBoosted = true;
        float original = appliedDamage;
        appliedDamage = appliedDamage * (1f + selfDamageUpAmount / 100f);
        yield return new WaitForSeconds(selfDamageUpDuration);
        appliedDamage = original;
        isSelfDamageBoosted = false;
    }

    public void ActivateManaSkill()
    {
        if (characterData == null) return;
        StartCoroutine(ManaSkillRoutine());
    }

    IEnumerator ManaSkillRoutine()
    {
        yield return new WaitForSeconds(characterData.attackHitDelay);
        switch (characterData.characterName)
        {
            case "Tier5_1": StartCoroutine(ManaSkill_Tier5_1()); break;
            case "Tier5_2": StartCoroutine(ManaSkill_Tier5_2()); break;
            case "Tier5_3": StartCoroutine(ManaSkill_Tier5_3()); break;
            case "Tier5_4": StartCoroutine(ManaSkill_Tier5_4()); break;
        }
    }

    IEnumerator ManaSkill_Tier5_1()
    {
        float elapsed = 0f;
        float damage = appliedDamage * (manaSkillDamage / 100f);
        float interval = manaSkillInterval > 0f ? manaSkillInterval : 0.1f;
        while (elapsed < manaSkillDuration)
        {
            EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth eh in allEnemies)
                if (eh != null) eh.TakeDamage(damage, this);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    IEnumerator ManaSkill_Tier5_2()
    {
        float originalDamage = appliedDamage;
        float originalCooldown = appliedCooldown;
        appliedDamage = appliedDamage * (manaSkillDamage / 100f);
        appliedCooldown = Mathf.Max(0.1f, appliedCooldown * (200f / 100f));
        yield return new WaitForSeconds(manaSkillDuration);
        appliedDamage = originalDamage;
        appliedCooldown = originalCooldown;
    }

    IEnumerator ManaSkill_Tier5_3()
    {
        float damage = appliedDamage * (manaSkillDamage / 100f);
        EnemyHealth bossTarget = null;
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null && eh.isBoss) { bossTarget = eh; break; }
        if (bossTarget == null)
            foreach (EnemyHealth eh in allEnemies)
                if (eh != null) { bossTarget = eh; break; }
        if (bossTarget != null) bossTarget.TakeDamage(damage, this);
        yield break;
    }

    IEnumerator ManaSkill_Tier5_4()
    {
        float damage = appliedDamage * (manaSkillDamage / 100f);
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null) eh.TakeDamage(damage, this);
        yield break;
    }

    EnemyMove FindBackmostEnemyInRange()
    {
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        EnemyMove backmostEnemy = null;
        float smallestProgress = Mathf.Infinity;
        float fallbackNearest = Mathf.Infinity;
        foreach (EnemyMove enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > characterData.attackRange) continue;
            float progress = enemy.GetPathProgress();
            if (progress < smallestProgress || (Mathf.Approximately(progress, smallestProgress) && dist < fallbackNearest))
            {
                smallestProgress = progress;
                fallbackNearest = dist;
                backmostEnemy = enemy;
            }
        }
        return backmostEnemy;
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