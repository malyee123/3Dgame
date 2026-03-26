using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class RecipeItemUI : MonoBehaviour
{
    [Header("Ingredients Parent")]
    public Transform ingredientsParent;

    [Header("Ingredient Image Prefab")]
    public GameObject ingredientImagePrefab;

    [Header("Result Image")]
    public Image resultImage;

    [Header("Craft Button")]
    public Button craftButton;

    [Header("Can Craft Highlight")]
    public GameObject canCraftHighlight;

    private readonly Color dimColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    private readonly Color brightColor = Color.white;

    public RecipeData recipe { get; private set; }
    public bool CanCraft { get; private set; }

    private List<Image> ingredientImages = new List<Image>();

    public void Setup(RecipeData data)
    {
        recipe = data;
        if (recipe == null) return;

        BuildIngredientImages();
        SetIngredientImage(resultImage, recipe.result);

        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(() => RecipeBook.Instance?.ExecuteCraft(recipe));
        }

        if (canCraftHighlight != null) canCraftHighlight.SetActive(false);
    }

    void BuildIngredientImages()
    {
        if (ingredientsParent == null || ingredientImagePrefab == null) return;

        foreach (Transform child in ingredientsParent)
            Destroy(child.gameObject);

        ingredientImages.Clear();

        if (recipe.ingredients == null) return;

        foreach (CharacterData data in recipe.ingredients)
        {
            GameObject obj = Instantiate(ingredientImagePrefab, ingredientsParent);
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                SetIngredientImage(img, data);
                ingredientImages.Add(img);
            }
        }
    }

    public void UpdateState(Dictionary<string, int> fieldUnits, bool canCraft)
    {
        CanCraft = canCraft;

        if (recipe.ingredients != null)
        {
            for (int i = 0; i < ingredientImages.Count; i++)
            {
                if (i >= recipe.ingredients.Length) break;
                UpdateIngredientState(ingredientImages[i], recipe.ingredients[i], fieldUnits);
            }
        }

        if (canCraftHighlight != null) canCraftHighlight.SetActive(canCraft);
        if (craftButton != null) craftButton.interactable = canCraft;
    }

    void UpdateIngredientState(Image img, CharacterData data, Dictionary<string, int> fieldUnits)
    {
        if (img == null || data == null) return;
        bool has = fieldUnits.ContainsKey(data.characterName) && fieldUnits[data.characterName] > 0;
        img.color = has ? brightColor : dimColor;
    }

    void SetIngredientImage(Image img, CharacterData data)
    {
        if (img == null) return;
        if (data == null) { img.enabled = false; return; }

        if (data.characterSprite != null)
        {
            img.sprite = data.characterSprite;
            img.enabled = true;
            return;
        }

        if (data.characterPrefab != null)
        {
            SpriteRenderer sr = data.characterPrefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                img.sprite = sr.sprite;
                img.enabled = true;
                return;
            }
        }

        img.enabled = false;
    }
}