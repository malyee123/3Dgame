using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("UI")]
    public GameObject unitActionUI;
    public Button mergeButton;
    public CanvasGroup mergeButtonCanvasGroup;
    public Button sellButton;
    public TextMeshProUGUI sellPriceText;

    [Header("Skill UI (Tier5_1 ~ Tier5_4)")]
    public Button[] skillButtons = new Button[4];
    public Image[] skillButtonOverlays = new Image[4];
    public Image[] manaFillImages = new Image[4];

    [Header("Canvas")]
    public Canvas gameCanvas;

    [HideInInspector] public float upgradeCostMultiplier = 1f;

    private PlayerAttack selectedUnit;
    private bool justSelected = false;
    private GameObject currentRangeIndicator;
    private PlayerAttack cachedLeader;
    private int currentSkillIndex = -1;

    private static readonly string[] tier5Names = { "Tier5_1", "Tier5_2", "Tier5_3", "Tier5_4" };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (mergeButton != null) { mergeButton.onClick.RemoveAllListeners(); mergeButton.onClick.AddListener(ExecuteMerge); }
        if (sellButton != null) { sellButton.onClick.RemoveAllListeners(); sellButton.onClick.AddListener(ExecuteSell); }
        for (int i = 0; i < skillButtons.Length; i++)
        {
            int idx = i;
            if (skillButtons[i] != null) { skillButtons[i].onClick.RemoveAllListeners(); skillButtons[i].onClick.AddListener(() => OnSkillButtonClick(idx)); }
        }
        if (unitActionUI != null) unitActionUI.SetActive(false);
        RefreshMergeUI();
    }

    void Update()
    {
        if (justSelected) { justSelected = false; return; }

        if (unitActionUI != null && unitActionUI.activeSelf && currentSkillIndex >= 0)
            UpdateManaUI(currentSkillIndex);

        if (Input.GetMouseButtonDown(0) && unitActionUI != null && unitActionUI.activeSelf)
        {
            if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            HideUnitActionUI();
        }
    }

    public void SelectUnit(PlayerAttack unit)
    {
        if (selectedUnit == unit) { HideUnitActionUI(); return; }
        selectedUnit = unit;
        cachedLeader = null;
        justSelected = true;

        if (unitActionUI != null && unit != null)
        {
            Vector3 slotPos = (PlayerSpawner.Instance != null && unit.spawnIndex >= 0)
                ? PlayerSpawner.Instance.spawnPoints[unit.spawnIndex].position
                : unit.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(slotPos);
            screenPos.z = 0f;
            unitActionUI.GetComponent<RectTransform>().position = screenPos + new Vector3(0, 80f, 0);
            unitActionUI.SetActive(true);
            RefreshActionButtons();
        }

        HideRangeIndicator();
        if (unit?.characterData != null)
        {
            Vector3 slotPos = (PlayerSpawner.Instance != null && unit.spawnIndex >= 0)
                ? PlayerSpawner.Instance.spawnPoints[unit.spawnIndex].position
                : unit.transform.position;
            currentRangeIndicator = new GameObject("RangeIndicator");
            currentRangeIndicator.transform.position = slotPos;
            currentRangeIndicator.AddComponent<RangeIndicator>().SetRange(unit.characterData.attackRange);
        }

        RefreshMergeUI();
    }

    public void HideUnitActionUI()
    {
        selectedUnit = null;
        cachedLeader = null;
        currentSkillIndex = -1;
        if (unitActionUI != null) unitActionUI.SetActive(false);
        HideRangeIndicator();
    }

    void HideRangeIndicator()
    {
        if (currentRangeIndicator != null) { Destroy(currentRangeIndicator); currentRangeIndicator = null; }
    }

    int GetTier5Index()
    {
        if (selectedUnit?.characterData == null) return -1;
        for (int i = 0; i < tier5Names.Length; i++)
            if (selectedUnit.characterData.characterName == tier5Names[i]) return i;
        return -1;
    }

    PlayerAttack GetSelectedLeader()
    {
        if (selectedUnit == null) return null;
        if (selectedUnit.isLeader) return selectedUnit;
        PlayerAttack[] all = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in all)
            if (unit != null && unit.spawnIndex == selectedUnit.spawnIndex && unit.isLeader)
                return unit;
        return null;
    }

    void RefreshActionButtons()
    {
        currentSkillIndex = GetTier5Index();
        bool isTier5 = currentSkillIndex >= 0;

        if (mergeButton != null) mergeButton.gameObject.SetActive(!isTier5);
        if (mergeButtonCanvasGroup != null) mergeButtonCanvasGroup.gameObject.SetActive(!isTier5);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            bool show = isTier5 && i == currentSkillIndex;
            if (skillButtons[i] != null) skillButtons[i].gameObject.SetActive(show);
            if (manaFillImages[i] != null) manaFillImages[i].gameObject.SetActive(show);
        }

        if (isTier5) { cachedLeader = GetSelectedLeader(); UpdateManaUI(currentSkillIndex); }
    }

    void UpdateManaUI(int index)
    {
        if (index < 0 || index >= skillButtons.Length) return;
        if (cachedLeader == null) cachedLeader = GetSelectedLeader();
        if (cachedLeader == null) return;

        float maxMana = cachedLeader.characterData != null ? cachedLeader.characterData.maxMana : 1f;
        if (manaFillImages[index] != null)
            manaFillImages[index].fillAmount = maxMana > 0f ? cachedLeader.currentMana / maxMana : 0f;

        bool isFull = cachedLeader.IsManaFull();
        bool hasTarget = cachedLeader.characterData.characterName == "Tier5_2"
            ? true
            : (cachedLeader.GetCurrentTarget() != null || cachedLeader.FindBackmostEnemyInRange() != null);
        bool canUse = isFull && hasTarget;

        if (skillButtons[index] != null) skillButtons[index].interactable = canUse;
        if (skillButtonOverlays[index] != null) skillButtonOverlays[index].gameObject.SetActive(!canUse);
    }

    void OnSkillButtonClick(int index)
    {
        if (cachedLeader == null) cachedLeader = GetSelectedLeader();
        if (cachedLeader == null || !cachedLeader.IsManaFull()) return;
        cachedLeader.ActivateManaSkill();
        cachedLeader.ResetMana();
        UpdateManaUI(index);
    }

    int GetActualUpgradeCost(int baseCost) => Mathf.Max(1, Mathf.RoundToInt(baseCost * upgradeCostMultiplier));

    void RefreshMergeUI()
    {
        if (sellPriceText != null && selectedUnit?.characterData != null)
        {
            int count = 0;
            foreach (PlayerAttack unit in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
                if (unit != null && unit.spawnIndex == selectedUnit.spawnIndex) count++;
            float mult = PlayerSpawner.Instance != null ? PlayerSpawner.Instance.sellPriceMultiplier : 1f;
            sellPriceText.text = $"{Mathf.RoundToInt(selectedUnit.characterData.sellPrice * count * mult)}G";
        }

        if (currentSkillIndex >= 0) return;

        bool canMerge = false;
        if (selectedUnit != null && PlayerSpawner.Instance != null)
        {
            int tier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
            int baseCost = selectedUnit.characterData != null ? selectedUnit.characterData.upgradeCost : 150;
            int actualCost = GetActualUpgradeCost(baseCost);
            bool tierAllowed = UpgradeManager.Instance == null || (tier + 1) <= UpgradeManager.Instance.UnlockedTier;
            bool hasCoins = CoinManager.Instance != null && CoinManager.Instance.GetCoins() >= actualCost;
            bool canManualMerge = PlayerSpawner.Instance.CanManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData);
            canMerge = tierAllowed && hasCoins && canManualMerge;
        }
        if (mergeButton != null) mergeButton.interactable = canMerge;
        if (mergeButtonCanvasGroup != null) mergeButtonCanvasGroup.alpha = canMerge ? 1f : 0.4f;
    }

    public void CheckMergeAvailable() => RefreshMergeUI();

    public void ExecuteMerge()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;
        int tier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
        int baseCost = selectedUnit.characterData != null ? selectedUnit.characterData.upgradeCost : 150;
        int actualCost = GetActualUpgradeCost(baseCost);
        if (UpgradeManager.Instance != null && (tier + 1) > UpgradeManager.Instance.UnlockedTier) return;
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(actualCost)) return;
        if (PlayerSpawner.Instance.TryManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData))
            HideUnitActionUI();
        else
            RefreshMergeUI();
    }

    public void ExecuteSell()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;
        int spawnIndex = selectedUnit.spawnIndex;
        int sellPrice = selectedUnit.characterData != null ? selectedUnit.characterData.sellPrice : 0;
        float mult = PlayerSpawner.Instance.sellPriceMultiplier;
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        List<PlayerAttack> toSell = new List<PlayerAttack>();
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.spawnIndex == spawnIndex) toSell.Add(unit);
        foreach (PlayerAttack unit in toSell) { PlayerSpawner.Instance.UnregisterUnit(unit, unit.spawnIndex); Destroy(unit.gameObject); }
        CoinManager.Instance?.AddCoins(Mathf.RoundToInt(sellPrice * toSell.Count * mult));
        HideUnitActionUI();
        RefreshMergeUI();
        StartCoroutine(UpdateButtonNextFrame());
    }

    IEnumerator UpdateButtonNextFrame()
    {
        yield return null;
        if (PlayerSpawner.Instance != null)
        {
            PlayerSpawner.Instance.SyncSlotStateFromScene();
            PlayerSpawner.Instance.ForceUpdateSpawnButton();
        }
    }

    public bool IsUnitActionUIActive() => unitActionUI != null && unitActionUI.activeSelf;
    public bool IsSelectedSlot(int slotIndex) => selectedUnit != null && selectedUnit.spawnIndex == slotIndex;
}