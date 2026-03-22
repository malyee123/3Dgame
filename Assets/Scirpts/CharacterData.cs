using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName = "New Character";


    [Header("Visual")]
    public GameObject characterPrefab; 

    public string unitTag = "";
    public Color characterColor = Color.white;


    [Header("Attack Stats")]
    public float attackRange = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 0.5f;

    [Header("Merge Settings")]
    [Min(1)] public int tier = 1;
    public string mergeGroupId = "";

    [Header("Upgrade Cost")]
    public int upgradeCost = 100;
}
