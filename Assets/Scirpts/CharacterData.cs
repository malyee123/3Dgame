// CharacterData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName = "New Character";

    [Header("Visual")]
    public GameObject characterPrefab; 

    [Header("Attack Stats")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("Merge Settings")]
    public CharacterData nextLevel;

    [Header("Upgrade Cost")]
    public int upgradeCost = 100;
}