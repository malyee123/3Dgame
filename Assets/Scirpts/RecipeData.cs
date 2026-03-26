using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Character/RecipeData")]
public class RecipeData : ScriptableObject
{
    [Header("Ingredients (flexible count)")]
    public CharacterData[] ingredients;

    [Header("Result")]
    public CharacterData result;

    [Header("Recipe Name (optional)")]
    public string recipeName = "";
}