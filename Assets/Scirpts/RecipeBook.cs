using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class RecipeBook : MonoBehaviour
{
    public static RecipeBook Instance { get; private set; }

    [Header("Recipe Data")]
    public RecipeData[] recipes;

    [Header("UI - Panel")]
    public GameObject recipePanel;
    public Transform recipeListParent;
    public GameObject recipeItemPrefab;

    [Header("UI - Notification")]
    public GameObject notificationBadge;

    [Header("UI - Buttons To Hide")]
    public GameObject spawnButton;
    public GameObject mergeButton;
    public GameObject recipeBookButton;

    [Header("Notification Settings")]
    public float notificationDisplayTime = 3f;

    private List<RecipeItemUI> recipeItems = new List<RecipeItemUI>();
    private bool isPanelOpen = false;
    public bool IsPanelOpen => isPanelOpen;

    private float checkInterval = 0.5f;
    private float checkTimer = 0f;
    private float notificationTimer = 0f;
    private bool isNotificationShowing = false;
    private bool wasCraftable = false;
    private bool notificationShownThisCycle = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (recipePanel != null) recipePanel.SetActive(false);
        if (notificationBadge != null) notificationBadge.SetActive(false);
        BuildRecipeList();
    }

    void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            if (isPanelOpen) RefreshRecipeStates();
            else CheckNotificationOnly();
        }

        if (isNotificationShowing && !isPanelOpen)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f)
            {
                isNotificationShowing = false;
                notificationShownThisCycle = true;
                if (notificationBadge != null) notificationBadge.SetActive(false);
            }
        }
    }

    void BuildRecipeList()
    {
        if (recipes == null || recipeListParent == null || recipeItemPrefab == null) return;
        foreach (Transform child in recipeListParent) Destroy(child.gameObject);
        recipeItems.Clear();
        foreach (RecipeData recipe in recipes)
        {
            if (recipe == null) continue;
            GameObject obj = Instantiate(recipeItemPrefab, recipeListParent);
            RecipeItemUI item = obj.GetComponent<RecipeItemUI>();
            if (item != null) { item.Setup(recipe); recipeItems.Add(item); }
        }
    }

    void CheckNotificationOnly()
    {
        if (recipeItems == null || recipeItems.Count == 0) return;
        Dictionary<string, int> fieldUnits = GetFieldUnitCounts();
        bool anyCanCraft = false;
        foreach (RecipeItemUI item in recipeItems)
        {
            if (item == null) continue;
            if (CanCraft(item.recipe, fieldUnits)) { anyCanCraft = true; break; }
        }

        if (anyCanCraft && !wasCraftable)
        {
            wasCraftable = true;
            if (!notificationShownThisCycle)
            {
                isNotificationShowing = true;
                notificationTimer = notificationDisplayTime;
                if (notificationBadge != null) notificationBadge.SetActive(true);
            }
        }
        else if (!anyCanCraft)
        {
            wasCraftable = false;
            isNotificationShowing = false;
            notificationShownThisCycle = false;
            if (notificationBadge != null) notificationBadge.SetActive(false);
        }
    }

    void RefreshRecipeStates()
    {
        if (recipeItems == null || recipeItems.Count == 0) return;
        Dictionary<string, int> fieldUnits = GetFieldUnitCounts();
        foreach (RecipeItemUI item in recipeItems)
        {
            if (item == null) continue;
            item.UpdateState(fieldUnits, CanCraft(item.recipe, fieldUnits));
        }
        SortRecipeList();
    }

    void SortRecipeList()
    {
        for (int i = recipeItems.Count - 1; i >= 0; i--)
            if (recipeItems[i] != null && recipeItems[i].CanCraft)
                recipeItems[i].transform.SetAsFirstSibling();
    }

    Dictionary<string, int> GetFieldUnitCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        PlayerAttack[] players = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack p in players)
        {
            if (p == null || p.characterData == null) continue;
            string name = p.characterData.characterName;
            if (!counts.ContainsKey(name)) counts[name] = 0;
            counts[name]++;
        }
        return counts;
    }

    bool CanCraft(RecipeData recipe, Dictionary<string, int> fieldUnits)
    {
        if (recipe == null || recipe.ingredients == null) return false;
        Dictionary<string, int> required = new Dictionary<string, int>();
        foreach (CharacterData data in recipe.ingredients)
        {
            if (data == null) continue;
            string name = data.characterName;
            if (!required.ContainsKey(name)) required[name] = 0;
            required[name]++;
        }
        foreach (var kv in required)
        {
            if (!fieldUnits.ContainsKey(kv.Key)) return false;
            if (fieldUnits[kv.Key] < kv.Value) return false;
        }
        return true;
    }

    public void ExecuteCraft(RecipeData recipe)
    {
        if (recipe == null) return;
        Dictionary<string, int> fieldUnits = GetFieldUnitCounts();
        if (!CanCraft(recipe, fieldUnits)) return;
        foreach (CharacterData data in recipe.ingredients) RemoveIngredient(data);
        if (PlayerSpawner.Instance != null && recipe.result != null)
            PlayerSpawner.Instance.SpawnSpecificCharacter(recipe.result);
        RefreshRecipeStates();
    }

    void RemoveIngredient(CharacterData data)
    {
        if (data == null) return;
        PlayerAttack[] players = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack p in players)
        {
            if (p == null || p.characterData == null) continue;
            if (p.characterData.characterName != data.characterName) continue;
            if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.UnregisterUnit(p, p.spawnIndex);
            Destroy(p.gameObject);
            return;
        }
    }

    void SetPlayerInteraction(bool enabled)
    {
        PlayerDragMerge[] dragMerges = FindObjectsByType<PlayerDragMerge>(FindObjectsSortMode.None);
        foreach (PlayerDragMerge drag in dragMerges) drag.enabled = enabled;
    }

    public void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        if (recipePanel != null) recipePanel.SetActive(isPanelOpen);
        if (spawnButton != null) spawnButton.SetActive(!isPanelOpen);
        if (mergeButton != null) mergeButton.SetActive(!isPanelOpen);
        if (recipeBookButton != null) recipeBookButton.SetActive(!isPanelOpen);
        SetPlayerInteraction(!isPanelOpen);

        // ·¹½ÃÇÇºÏ ¿­¸± ¶§ UnitActionUI ¼û±â±â
        if (isPanelOpen && MergeManager.Instance != null)
            MergeManager.Instance.HideUnitActionUI();

        if (isPanelOpen)
        {
            isNotificationShowing = false;
            notificationShownThisCycle = true;
            if (notificationBadge != null) notificationBadge.SetActive(false);
            RefreshRecipeStates();
        }
    }

    public void ClosePanel()
    {
        isPanelOpen = false;
        if (recipePanel != null) recipePanel.SetActive(false);
        if (spawnButton != null) spawnButton.SetActive(true);
        if (mergeButton != null) mergeButton.SetActive(true);
        if (recipeBookButton != null) recipeBookButton.SetActive(true);
        SetPlayerInteraction(true);
        if (MergeManager.Instance != null) MergeManager.Instance.HideUnitActionUI();
    }
}