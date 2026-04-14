using System.Collections;
using System.Collections.Generic;
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
        float aoeStunEveryN, float aoeStunRange, float aoeStunDuration)
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
        Debug.Log($"[Passive] BuffNearbyAlly 공속 버프 받음! +{amount}% {duration}초");
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
            aoeStunEveryN = aoeStunRange = aoeStunDuration = 0f;
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
        Debug.Log($"[Cache] 전체 유닛 수: {all.Length}, 내 spawnIndex: {spawnIndex}");
        foreach (PlayerAttack unit in all)
        {
            if (unit == null || unit == this || unit.spawnIndex != spawnIndex) continue;
            cachedSlotMates.Add(unit);
            Debug.Log($"[Cache] 슬롯메이트 추가: {unit.characterData?.characterName}");
        }
        slotMatesDirty = false;
    }

    void PlayAttackAnimAll()
    {
        if (spumPrefabs != null && spumPrefabs.OverrideController != null)
            spumPrefabs.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);

        if (slotMatesDirty) RefreshSlotMatesCache();

        Debug.Log($"[PlayerAttack] 슬롯메이트 수: {cachedSlotMates.Count} / spawnIndex: {spawnIndex}");

        foreach (PlayerAttack mate in cachedSlotMates)
        {
            if (mate == null) continue;
            SPUM_Prefabs mateSpum = mate.GetComponentInChildren<SPUM_Prefabs>();
            if (mateSpum != null && mateSpum.OverrideController != null)
                mateSpum.PlayAnimation(PlayerState.ATTACK, characterData.attackAnimIndex);
        }
    }

    void AttackWithLockedTarget()
    {
        if (!IsTargetInRange(currentTarget))
            currentTarget = FindBackmostEnemyInRange();
        if (currentTarget == null) return;

        EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
        if (health == null) { currentTarget = null; return; }

        PlayAttackAnimAll();

        float finalDamage = appliedDamage;
        if (doubleDamageChance > 0f && Random.Range(0f, 100f) < doubleDamageChance)
        {
            finalDamage *= 2f;
            Debug.Log($"[Passive] DoubleDamage 발동! 피해: {finalDamage}");
        }

        StartCoroutine(DealDamageWithDelay(currentTarget, finalDamage, false));

        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
        {
            Debug.Log("[Passive] AttackTwice 발동!");
            StartCoroutine(SecondAttackAnimRoutine());
        }

        if (selfSpeedUpChance > 0f && !isSelfSpeedBoosted && Random.Range(0f, 100f) < selfSpeedUpChance)
        {
            Debug.Log($"[Passive] SelfAttackSpeedUp 발동! +{selfSpeedUpAmount}% {selfSpeedUpDuration}초");
            StartCoroutine(SelfSpeedBoostRoutine());
        }

        if (selfDamageUpChance > 0f && !isSelfDamageBoosted && Random.Range(0f, 100f) < selfDamageUpChance)
        {
            Debug.Log($"[Passive] SelfAttackDamageUp 발동! +{selfDamageUpAmount}% {selfDamageUpDuration}초");
            StartCoroutine(SelfDamageBoostRoutine());
        }

        if (stunChance > 0f && Random.Range(0f, 100f) < stunChance)
        {
            Debug.Log($"[Passive] Stun 발동! {stunDuration}초");
            currentTarget.ApplyStun(stunDuration);
        }

        if (executeChance > 0f && Random.Range(0f, 100f) < executeChance)
            StartCoroutine(ExecuteCheckRoutine(currentTarget));

        if (buffAllyChance > 0f && Random.Range(0f, 100f) < buffAllyChance)
        {
            Debug.Log($"[Passive] BuffNearbyAlly 발동! +{buffAllyAmount}% {buffAllyDuration}초");
            BuffNearbyAllies();
        }

        if (aoeStunEveryN > 0f)
        {
            hitCounter++;
            if (hitCounter >= (int)aoeStunEveryN)
            {
                hitCounter = 0;
                Debug.Log($"[Passive] AoeStun 발동! 범위:{aoeStunRange} 지속:{aoeStunDuration}초");
                StartCoroutine(AoeStunRoutine(currentTarget));
            }
        }

        cooldownTimer = 0f;
    }

    IEnumerator AoeStunRoutine(EnemyMove target)
    {
        yield return new WaitForSeconds(0.35f);
        float targetProgress = target != null ? target.GetPathProgress() : -1f;
        EnemyMove[] allEnemies = FindObjectsByType<EnemyMove>(FindObjectsSortMode.None);
        if (allEnemies.Length == 0) yield break;
        if (target == null) targetProgress = allEnemies[0].GetPathProgress();
        int stunCount = 0;
        foreach (EnemyMove enemy in allEnemies)
        {
            if (enemy == null) continue;
            if (Mathf.Abs(enemy.GetPathProgress() - targetProgress) <= aoeStunRange)
            {
                enemy.ApplyStun(aoeStunDuration);
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null) health.TakeDamage(appliedDamage, this);
                stunCount++;
            }
        }
        Debug.Log($"[Passive] AoeStun {stunCount}마리 기절 + 데미지!");
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
        yield return new WaitForSeconds(0.35f);
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
        yield return new WaitForSeconds(0.3f);
        if (target == null) yield break;
        EnemyHealth health = target.GetComponent<EnemyHealth>();
        if (health == null) yield break;
        health.TakeDamage(damage, this);
        if (isTwice) yield break;
        if (attackTwiceChance > 0f && Random.Range(0f, 100f) < attackTwiceChance)
            StartCoroutine(DealDamageWithDelay(target, appliedDamage, true));
    }

    IEnumerator SecondAttackAnimRoutine()
    {
        yield return new WaitForSeconds(appliedCooldown * 0.5f);
        PlayAttackAnimAll();
    }

    // 버그 수정: 기존 SelfSpeedBoostRoutine이 SelfDamage 로직을 쓰고 있었음
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