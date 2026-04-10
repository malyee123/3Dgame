using System.Collections.Generic;
using UnityEngine;

public enum PassiveType
{
    None,
    AllAttackDamageUp,
    AllAttackSpeedUp,
    AllEnemySpeedDown,
    AllEnemyDefenseDown,
    DoubleDamageChance,
    AttackTwiceChance,
    SelfAttackSpeedUpChance,
    SpecialCoinOnKillChance,
    SelfAttackDamageUpChance,
    StunChance,
    ExecuteChance,
    BuffNearbyAllyAttackSpeed,
    AoeStunEveryNHits,
}

[System.Serializable]
public class PassiveEntry
{
    public PassiveType passiveType = PassiveType.None;
    [Range(0f, 100f)]
    public float passiveValue = 0f;
    public float passiveSecondValue = 0f;
    public float passiveDuration = 0f;
}

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName = "New Character";

    [Header("Visual")]
    public GameObject characterPrefab;
    public Sprite characterSprite;
    public string unitTag = "";
    public Color characterColor = Color.white;

    [Header("Attack Stats")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("Merge Settings")]
    [Min(1)] public int tier = 1;

    [Header("Upgrade Cost")]
    public int upgradeCost = 100;

    [Header("Sell Price")]
    public int sellPrice = 30;

    [Header("Animation")]
    public int attackAnimIndex = 0;

    [Header("Passives")]
    public List<PassiveEntry> passives = new List<PassiveEntry>();
}