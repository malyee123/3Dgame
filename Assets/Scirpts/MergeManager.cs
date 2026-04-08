using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("UI")]
    public GameObject unitActionUI;
    public Button mergeButton;
    public CanvasGroup mergeButtonCanvasGroup;
    public Button sellButton;
    public TextMeshProUGUI sellPriceText;

    [Header("Canvas")]
    public Canvas gameCanvas;

    private PlayerAttack selectedUnit;
    private bool justSelected = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (mergeButton != null) { mergeButton.onClick.RemoveAllListeners(); mergeButton.onClick.AddListener(ExecuteMerge); }
        if (sellButton != null) { sellButton.onClick.RemoveAllListeners(); sellButton.onClick.AddListener(ExecuteSell); }
        if (unitActionUI != null) unitActionUI.SetActive(false);
        RefreshMergeUI();
    }

    void Update()
    {
        if (justSelected) { justSelected = false; return; }
        if (Input.GetMouseButtonDown(0) && unitActionUI != null && unitActionUI.activeSelf)
        {
            if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            selectedUnit = null;
            unitActionUI.SetActive(false);
        }
    }

    public void SelectUnit(PlayerAttack unit)
    {
        selectedUnit = unit;
        justSelected = true;
        if (unitActionUI != null && unit != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            screenPos.z = 0f;
            unitActionUI.GetComponent<RectTransform>().position = screenPos + new Vector3(0, 80f, 0);
            unitActionUI.SetActive(true);
        }
        RefreshMergeUI();
    }

    void RefreshMergeUI()
    {
        bool canMerge = false;
        if (selectedUnit != null && PlayerSpawner.Instance != null)
        {
            int currentTier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
            int mergeCost = selectedUnit.characterData != null ? selectedUnit.characterData.upgradeCost : 150;
            bool tierAllowed = UpgradeManager.Instance == null || (currentTier + 1) <= UpgradeManager.Instance.UnlockedTier;
            bool hasEnoughCoins = CoinManager.Instance != null && CoinManager.Instance.GetCoins() >= mergeCost;
            bool canManualMerge = PlayerSpawner.Instance.CanManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData);
            canMerge = tierAllowed && hasEnoughCoins && canManualMerge;
        }
        if (mergeButton != null) mergeButton.interactable = canMerge;
        if (mergeButtonCanvasGroup != null) mergeButtonCanvasGroup.alpha = canMerge ? 1f : 0.4f;
        if (sellPriceText != null && selectedUnit?.characterData != null)
        {
            PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
            int count = 0;
            foreach (PlayerAttack unit in allUnits)
                if (unit != null && unit.spawnIndex == selectedUnit.spawnIndex) count++;
            sellPriceText.text = $"{selectedUnit.characterData.sellPrice * count}G";
        }
    }

    public void CheckMergeAvailable() { RefreshMergeUI(); }

    public void ExecuteMerge()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;
        int currentTier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
        int mergeCost = selectedUnit.characterData != null ? selectedUnit.characterData.upgradeCost : 150;
        if (UpgradeManager.Instance != null && (currentTier + 1) > UpgradeManager.Instance.UnlockedTier) return;
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(mergeCost)) return;
        bool merged = PlayerSpawner.Instance.TryManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData);
        if (merged) { selectedUnit = null; if (unitActionUI != null) unitActionUI.SetActive(false); }
        RefreshMergeUI();
    }

    public void ExecuteSell()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;
        int spawnIndex = selectedUnit.spawnIndex;
        int sellPrice = selectedUnit.characterData != null ? selectedUnit.characterData.sellPrice : 0;
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        List<PlayerAttack> unitsToSell = new List<PlayerAttack>();
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.spawnIndex == spawnIndex) unitsToSell.Add(unit);
        int totalSellPrice = sellPrice * unitsToSell.Count;
        foreach (PlayerAttack unit in unitsToSell) { PlayerSpawner.Instance.UnregisterUnit(unit, unit.spawnIndex); Destroy(unit.gameObject); }
        if (CoinManager.Instance != null) CoinManager.Instance.AddCoins(totalSellPrice);
        selectedUnit = null;
        if (unitActionUI != null) unitActionUI.SetActive(false);
        RefreshMergeUI();
    }

    public void HideUnitActionUI()
    {
        selectedUnit = null;
        if (unitActionUI != null) unitActionUI.SetActive(false);
    }

    public bool IsUnitActionUIActive() => unitActionUI != null && unitActionUI.activeSelf;

    public bool IsSelectedSlot(int slotIndex) => selectedUnit != null && selectedUnit.spawnIndex == slotIndex;
}