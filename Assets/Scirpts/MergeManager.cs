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
    private GameObject currentRangeIndicator;

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
            HideUnitActionUI();
        }
    }

    public void SelectUnit(PlayerAttack unit)
    {
        if (selectedUnit == unit) { HideUnitActionUI(); return; }
        selectedUnit = unit;
        justSelected = true;

        if (unitActionUI != null && unit != null)
        {
            // 슬롯 중앙 위치 기준으로 UI 배치
            Vector3 slotPos = (PlayerSpawner.Instance != null && unit.spawnIndex >= 0)
                ? PlayerSpawner.Instance.spawnPoints[unit.spawnIndex].position
                : unit.transform.position;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(slotPos);
            screenPos.z = 0f;
            unitActionUI.GetComponent<RectTransform>().position = screenPos + new Vector3(0, 80f, 0);
            unitActionUI.SetActive(true);
        }

        HideRangeIndicator();
        if (unit != null && unit.characterData != null)
        {
            // 사거리도 슬롯 중앙 기준
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
        if (unitActionUI != null) unitActionUI.SetActive(false);
        HideRangeIndicator();
    }

    void HideRangeIndicator()
    {
        if (currentRangeIndicator != null) { Destroy(currentRangeIndicator); currentRangeIndicator = null; }
    }

    void RefreshMergeUI()
    {
        bool canMerge = false;
        if (selectedUnit != null && PlayerSpawner.Instance != null)
        {
            int tier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
            int cost = selectedUnit.characterData != null ? selectedUnit.characterData.upgradeCost : 150;
            bool tierAllowed = UpgradeManager.Instance == null || (tier + 1) <= UpgradeManager.Instance.UnlockedTier;
            bool hasCoins = CoinManager.Instance != null && CoinManager.Instance.GetCoins() >= cost;
            bool canManualMerge = PlayerSpawner.Instance.CanManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData);
            canMerge = tierAllowed && hasCoins && canManualMerge;
        }
        if (mergeButton != null) mergeButton.interactable = canMerge;
        if (mergeButtonCanvasGroup != null) mergeButtonCanvasGroup.alpha = canMerge ? 1f : 0.4f;
        if (sellPriceText != null && selectedUnit?.characterData != null)
        {
            int count = 0;
            foreach (PlayerAttack unit in FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None))
                if (unit != null && unit.spawnIndex == selectedUnit.spawnIndex) count++;
            sellPriceText.text = $"{selectedUnit.characterData.sellPrice * count}G";
        }
    }

    public void CheckMergeAvailable() => RefreshMergeUI();

    public void ExecuteMerge()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;
        int tier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
        int cost = selectedUnit.characterData != null ? selectedUnit.characterData.upgradeCost : 150;
        if (UpgradeManager.Instance != null && (tier + 1) > UpgradeManager.Instance.UnlockedTier) return;
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(cost)) return;
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
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        List<PlayerAttack> toSell = new List<PlayerAttack>();
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.spawnIndex == spawnIndex) toSell.Add(unit);
        foreach (PlayerAttack unit in toSell) { PlayerSpawner.Instance.UnregisterUnit(unit, unit.spawnIndex); Destroy(unit.gameObject); }
        if (CoinManager.Instance != null) CoinManager.Instance.AddCoins(sellPrice * toSell.Count);
        HideUnitActionUI();
        RefreshMergeUI();
    }

    public bool IsUnitActionUIActive() => unitActionUI != null && unitActionUI.activeSelf;
    public bool IsSelectedSlot(int slotIndex) => selectedUnit != null && selectedUnit.spawnIndex == slotIndex;
}