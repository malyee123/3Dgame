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
    [SerializeField] private float appliedRange;
    [SerializeField] private float appliedCooldown;
    [SerializeField] private float passiveDamageBonus;
    [SerializeField] private float passiveSpeedBonus;
    [SerializeField] private float doubleDamageChance;
    [SerializeField] private float doubleDamageMultiplier;
    [SerializeField] private float attackTwiceChance;
    [SerializeField] private float attackTwiceCount;
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

    public float appliedDamagePublic => appliedDamage;
    public float AppliedRange => appliedRange;
    public float manaSkillDamagePublic => manaSkillDamage;
    public float manaSkillDurationPublic => manaSkillDuration;
    public float manaSkillIntervalPublic => manaSkillInterval;

    private float augmentDamageBonus = 0f;
    private float augmentSpeedBonus = 0f;
    private float augmentRangeBonus = 0f;
    private float augmentNormalDamagePenalty = 1f;
    private float augmentSpeedPenalty = 0f;

    private bool isSelfSpeedBoosted = false;
    private bool isSelfDamageBoosted = false;
    private bool isAllySpeedBoosted = false;
    private bool isDragging = false;

    public float currentMana = 0f;
    private float manaChargeTimer = 0f;

    private SPUM_Prefabs spumPrefabs;
    private int hitCounter = 0;
    private const int gridWidth = 5;
    private List<PlayerAttack> cachedSlotMates = new List<PlayerAttack>();
    private bool slotMatesDirty = true;

    public string UnitType => characterData != null ? characterData.characterName : "";
    public EnemyMove GetCurrentTarget() => currentTarget;

    public bool IsManaFull() => characterData != null && characterData.maxMana > 0 && currentMana >= characterData.maxMana;
    public void ResetMana() { currentMana = 0f; manaChargeTimer = 0f; }

    void Start()
    {
        if (characterData == null) { enabled = false; return; }
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
        appliedDamage = characterData.attackDamage * dmgMult * (1f + passiveDamageBonus / 100f) * (1f + augmentDamageBonus / 100f) * augmentNormalDamagePenalty;
        float totalSpeedBonus = (passiveSpeedBonus + augmentSpeedBonus) / 100f;
        float totalSpeedPenalty = augmentSpeedPenalty / 100f;
        float totalSpeed = spdMult + totalSpeedBonus;
        appliedCooldown = Mathf.Max(0.1f, Mathf.Round(1f / characterData.attackSpeed / totalSpeed * (1f + totalSpeedPenalty) * 100f) / 100f);
        appliedRange = characterData.attackRange + augmentRangeBonus;
    }

    public void ApplyAugmentBonus(float damageBonus, float speedBonus, float rangeBonus = 0f, float normalDamagePenalty = 1f, float speedPenalty = 0f)
    {
        augmentDamageBonus += damageBonus;
        augmentSpeedBonus += speedBonus;
        augmentRangeBonus += rangeBonus;
        augmentNormalDamagePenalty *= normalDamagePenalty;
        augmentSpeedPenalty += speedPenalty;
        ApplyUpgradeStats();
    }

    public void RefreshAugmentState() => ApplyUpgradeStats();

    public void ApplyPassiveBonus(float damageBonus, float speedBonus,
        float doubleChance, float doubleMultiplier,
        float twiceChance, float twiceCount,
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
        doubleDamageMultiplier = doubleMultiplier > 0f ? doubleMultiplier : 2f;
        attackTwiceChance = twiceChance;
        attackTwiceCount = twiceCount > 0f ? twiceCount : 2f;
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

        if (characterData != null && characterData.maxMana > 0)
        {
            manaChargeTimer += Time.deltaTime;
            if (manaChargeTimer >= 1f)
            {
                manaChargeTimer = 0f;
                currentMana = Mathf.Min(currentMana + 1f, characterData.maxMana);
            }
        }

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
        passiveDamageBonus = passiveSpeedBonus = doubleDamageChance = doubleDamageMultiplier =
            attackTwiceChance = attackTwiceCount =
            selfSpeedUpChance = selfSpeedUpAmount = selfSpeedUpDuration =
            selfDamageUpChance = selfDamageUpAmount = selfDamageUpDuration =
            stunChance = stunDuration =
            executeChance = executeHpThreshold = executeBossDamagePercent =
            buffAllyChance = buffAllyAmount = buffAllyDuration =
            aoeStunEveryN = aoeStunRange = aoeStunDuration =
            areaSpeedDownChance = areaSpeedDownAmount = areaSpeedDownDuration =
            magicMissileChance = magicMissileDamagePercent =
            slamChance = slamDamagePercent = slamRange =
            manaSkillDamage = manaSkillDuration = manaSkillInterval =
            augmentDamageBonus = augmentSpeedBonus = augmentRangeBonus = augmentSpeedPenalty = 0f;
        augmentNormalDamagePenalty = 1f;
        bossDamageDouble = false;
        hitCounter = 0;
        currentMana = 0f;
        manaChargeTimer = 0f;
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
        if (!IsTargetInRange(currentTarget)) currentTarget = FindBackmostEnemyInRange();
        if (currentTarget == null) return;

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null) { currentTarget = null; return; }

        PlayAttackAnimAll();

        int slotCount = GetSlotUnitCount();
        float finalDamage = appliedDamage * slotCount;

        if (doubleDamageChance > 0f && Random.Range(0f, 100f) < doubleDamageChance)
            finalDamage *= doubleDamageMultiplier;
        if (bossDamageDouble && health.isBoss) finalDamage *= 2f;
        if (AugmentManager.Instance != null && AugmentManager.Instance.HasGiantSlayer)
        {
            if (health.isSpecial) finalDamage *= 2f;
            else finalDamage *= augmentNormalDamagePenalty;
        }
        if (characterData.tier >= 5 && AugmentManager.Instance != null && AugmentManager.Instance.HasTheBigOne)
            if (Random.Range(0f, 100f) < 10f) finalDamage *= 5f;

        float twinsChaosMultiplier = 1f;
        if (characterData.tier >= 5 && AugmentManager.Instance != null && AugmentManager.Instance.HasTwinsOfChaos)
        {
            int sameCount = 0;
            PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
            foreach (PlayerAttack unit in allUnits)
                if (unit != null && unit.characterData != null && unit.characterData.characterName == characterData.characterName)
                    sameCount++;
            if (sameCount >= 2) twinsChaosMultiplier = 2f;
        }

        bool slamFired = slamChance > 0f && Random.Range(0f, 100f) < slamChance * twinsChaosMultiplier;
        bool missileFired = magicMissileChance > 0f && Random.Range(0f, 100f) < magicMissileChance * twinsChaosMultiplier;

        if (!slamFired && !missileFired)
        {
            if (characterData.projectilePrefab != null)
                StartCoroutine(FireProjectileWithDelay(currentTarget, finalDamage));
            else
                StartCoroutine(DealDamageWithDelay(currentTarget, finalDamage, 0));
        }

        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance * twinsChaosMultiplier)
            StartCoroutine(MultiAttackRoutine((int)attackTwiceCount - 1));
        if (selfSpeedUpChance > 0f && !isSelfSpeedBoosted && Random.Range(0f, 100f) < selfSpeedUpChance * twinsChaosMultiplier)
            StartCoroutine(SelfSpeedBoostRoutine());
        if (selfDamageUpChance > 0f && !isSelfDamageBoosted && Random.Range(0f, 100f) < selfDamageUpChance * twinsChaosMultiplier)
            StartCoroutine(SelfDamageBoostRoutine());
        if (stunChance > 0f && Random.Range(0f, 100f) < stunChance * twinsChaosMultiplier)
            currentTarget.ApplyStun(stunDuration);
        if (executeChance > 0f && Random.Range(0f, 100f) < executeChance * twinsChaosMultiplier)
            StartCoroutine(ExecuteCheckRoutine(currentTarget));
        if (buffAllyChance > 0f && Random.Range(0f, 100f) < buffAllyChance * twinsChaosMultiplier)
            BuffNearbyAllies();
        if (aoeStunEveryN > 0f)
        {
            hitCounter++;
            if (hitCounter >= (int)aoeStunEveryN) { hitCounter = 0; StartCoroutine(AoeStunRoutine(currentTarget)); }
        }
        if (areaSpeedDownChance > 0f && Random.Range(0f, 100f) < areaSpeedDownChance * twinsChaosMultiplier)
            StartCoroutine(AreaSpeedDownRoutine());
        if (slamFired) StartCoroutine(SlamRoutine(currentTarget));
        if (missileFired) StartCoroutine(MagicMissileRoutine());

        cooldownTimer = 0f;
    }

    IEnumerator FireProjectileWithDelay(EnemyMove target, float damage)
    {
        Vector3 savedPos = target != null ? target.transform.position : transform.position;
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
        if (characterData.projectilePrefab == null) yield break;

        Transform targetTransform;
        GameObject tempObj = null;

        if (target != null)
        {
            targetTransform = target.transform;
        }
        else
        {
            tempObj = new GameObject("TempProjectileTarget");
            tempObj.transform.position = savedPos;
            targetTransform = tempObj.transform;
            Destroy(tempObj, 3f);
        }

        GameObject proj = Instantiate(characterData.projectilePrefab, transform.position, Quaternion.identity);
        Projectile projectile = proj.AddComponent<Projectile>();
        projectile.Init(targetTransform, characterData.projectileSpeed, damage, this, characterData.hitEffectPrefab, characterData.hitEffectDuration, characterData.hitEffectOffsetY);
    }

    IEnumerator MultiAttackRoutine(int remainCount)
    {
        if (remainCount <= 0) yield break;
        yield return new WaitForSeconds(appliedCooldown * 0.3f);
        PlayAttackAnimAll();
        if (currentTarget != null)
        {
            float damage = appliedDamage * GetSlotUnitCount();
            StartCoroutine(DealDamageWithDelay(currentTarget, damage, remainCount - 1));
        }
    }

    IEnumerator SlamRoutine(EnemyMove target)
    {
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
        if (target == null) yield break;
        Vector3 targetPos = target.transform.position;
        float damage = appliedDamage * GetSlotUnitCount() * (slamDamagePercent / 100f);
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
        {
            if (eh == null) continue;
            float dx = Mathf.Abs(eh.transform.position.x - targetPos.x);
            float dy = Mathf.Abs(eh.transform.position.y - targetPos.y);
            if (dx <= slamRange && dy <= slamRange) eh.TakeDamage(damage, this);
        }
    }

    IEnumerator MagicMissileRoutine()
    {
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
        float damage = appliedDamage * GetSlotUnitCount() * (magicMissileDamagePercent / 100f);
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null) eh.TakeDamage(damage, this);
    }

    IEnumerator AreaSpeedDownRoutine()
    {
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
        EnemyMove[] allEnemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        foreach (EnemyMove enemy in allEnemies)
        {
            if (enemy == null) continue;
            if (Vector2.Distance(transform.position, enemy.transform.position) <= appliedRange)
                enemy.ApplyTempSpeedPenalty(areaSpeedDownAmount, areaSpeedDownDuration);
        }
    }

    IEnumerator AoeStunRoutine(EnemyMove target)
    {
        Vector3 targetPos = target != null ? target.transform.position : transform.position;
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
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
        List<int> adjacentSlots = new List<int>();
        if (spawnIndex % gridWidth != 0) adjacentSlots.Add(spawnIndex - 1);
        if (spawnIndex % gridWidth != gridWidth - 1) adjacentSlots.Add(spawnIndex + 1);
        if (spawnIndex - gridWidth >= 0) adjacentSlots.Add(spawnIndex - gridWidth);
        if (spawnIndex + gridWidth < 25) adjacentSlots.Add(spawnIndex + gridWidth);
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (int slot in adjacentSlots)
            foreach (PlayerAttack unit in allUnits)
                if (unit != null && unit.spawnIndex == slot && unit.isLeader)
                    unit.ReceiveAllySpeedBuff(buffAllyAmount, buffAllyDuration);
    }

    IEnumerator ExecuteCheckRoutine(EnemyMove target)
    {
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
        if (target == null) yield break;
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health == null) yield break;
        if (health.isSpecial) { if (executeBossDamagePercent > 0f) health.TakePercentDamage(executeBossDamagePercent, this); }
        else { if (health.CurrentHp / health.MaxHp * 100f < executeHpThreshold) health.ExecuteKill(); }
    }

    IEnumerator DealDamageWithDelay(EnemyMove target, float damage, int remainMultiAttack)
    {
        yield return new WaitForSeconds((1f / characterData.attackSpeed) * 0.3f);
        if (target == null) yield break;
        Vector3 targetPos = target.transform.position;
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health != null) health.TakeDamage(damage, this);
        SpawnHitEffect(targetPos, null);
        if (remainMultiAttack > 0) StartCoroutine(MultiAttackRoutine(remainMultiAttack));
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
        appliedDamage *= (1f + selfDamageUpAmount / 100f);
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
        switch (characterData.characterName)
        {
            case "Tier5_1": StartCoroutine(ManaSkill_Tier5_1()); break;
            case "Tier5_2": StartCoroutine(ManaSkill_Tier5_2()); break;
            case "Tier5_3": StartCoroutine(ManaSkill_Tier5_3()); break;
            case "Tier5_4": StartCoroutine(ManaSkill_Tier5_4()); break;
        }
        yield break;
    }

    IEnumerator ManaSkill_Tier5_1()
    {
        EnemyMove target = GetCurrentTarget();
        if (target == null) target = FindBackmostEnemyInRange();
        if (target == null) yield break;
        Vector3 pitPosition = new Vector3(target.transform.position.x, target.transform.position.y - 0.3f, 0f);
        StartCoroutine(SpawnPitForUnit(this, pitPosition));
        yield break;
    }

    IEnumerator SpawnPitForUnit(PlayerAttack unit, Vector3 pitPosition)
    {
        float elapsed = 0f;
        float damage = unit.appliedDamagePublic * (unit.manaSkillDamagePublic / 100f);
        float interval = unit.manaSkillIntervalPublic > 0f ? unit.manaSkillIntervalPublic : 0.1f;
        float range = unit.characterData.attackRange;

        float borderThreshold = 4.5f;
        Quaternion rotation = Mathf.Abs(pitPosition.x) >= borderThreshold
            ? Quaternion.Euler(0f, 0f, 270f)
            : Quaternion.identity;

        GameObject pit = null;
        if (unit.characterData.manaSkillEffectPrefab != null)
            pit = Instantiate(unit.characterData.manaSkillEffectPrefab, pitPosition, rotation);
        else
        {
            pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pit.transform.position = pitPosition;
            pit.transform.localScale = new Vector3(range * 2f, 0.05f, range * 2f);
            pit.GetComponent<Renderer>().material.color = new Color(0.5f, 0f, 0f, 0.5f);
            Destroy(pit.GetComponent<Collider>());
        }

        while (elapsed < unit.manaSkillDurationPublic)
        {
            EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth eh in allEnemies)
                if (eh != null && Vector2.Distance(eh.transform.position, pitPosition) <= range)
                    eh.TakeDamage(damage, unit, true);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        if (pit != null) Destroy(pit);
    }

    IEnumerator ManaSkill_Tier5_2()
    {
        if (characterData.manaSkillEffectPrefab != null)
        {
            GameObject effect = Instantiate(characterData.manaSkillEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
            Destroy(effect, manaSkillDuration > 0f ? manaSkillDuration : 2f);
        }
        float originalDamage = appliedDamage;
        float originalCooldown = appliedCooldown;
        appliedDamage = appliedDamage * (manaSkillDamage / 100f);
        appliedCooldown = Mathf.Max(0.1f, appliedCooldown * 0.5f);
        SPUM_Prefabs spum = GetComponentInChildren<SPUM_Prefabs>();
        Animator anim = spum != null ? spum.GetComponent<Animator>() : null;
        if (anim != null) anim.speed = 2f;
        yield return new WaitForSeconds(manaSkillDuration);
        appliedDamage = originalDamage;
        appliedCooldown = originalCooldown;
        if (anim != null) anim.speed = 1f;
    }

    IEnumerator ManaSkill_Tier5_3()
    {
        float damage = appliedDamage * (manaSkillDamage / 100f);

        EnemyHealth bossTarget = null;
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
        {
            if (eh == null) continue;
            if (eh.isBoss && Vector2.Distance(transform.position, eh.transform.position) <= appliedRange)
            { bossTarget = eh; break; }
        }

        EnemyHealth finalTarget = bossTarget;
        if (finalTarget == null && currentTarget != null)
            finalTarget = currentTarget.GetComponent<EnemyHealth>();
        if (finalTarget == null)
        {
            EnemyMove nearest = FindBackmostEnemyInRange();
            if (nearest != null) finalTarget = nearest.GetComponent<EnemyHealth>();
        }
        if (finalTarget == null) yield break;

        if (characterData.manaSkillEffectPrefab != null)
        {
            GameObject effect = Instantiate(characterData.manaSkillEffectPrefab, finalTarget.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        finalTarget.TakeDamage(damage, this, true);
        yield break;
    }

    IEnumerator ManaSkill_Tier5_4()
    {
        if (characterData.manaSkillEffectPrefab != null)
        {
            Vector3 centerPos = Camera.main.transform.position;
            centerPos.z = 0f;
            GameObject effect = Instantiate(characterData.manaSkillEffectPrefab, centerPos, Quaternion.identity);
            Destroy(effect, manaSkillDuration > 0f ? manaSkillDuration : 2f);
        }
        float damage = appliedDamage * GetSlotUnitCount() * (manaSkillDamage / 100f);
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth eh in allEnemies)
            if (eh != null) eh.TakeDamage(damage, this, true);
        yield break;
    }

    public EnemyMove FindBackmostEnemyInRange()
    {
        EnemyMove[] enemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        EnemyMove backmostEnemy = null;
        float smallestProgress = Mathf.Infinity;
        float fallbackNearest = Mathf.Infinity;
        foreach (EnemyMove enemy in enemies)
        {
            if (enemy == null) continue;
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist > appliedRange) continue;
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
        return Vector2.Distance(transform.position, enemy.transform.position) <= appliedRange;
    }

    void OnDrawGizmosSelected()
    {
        if (characterData == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, appliedRange);
    }
}