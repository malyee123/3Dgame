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

    [Header("Skill Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Canvas")]
    public Canvas gameCanvas;

    [HideInInspector] public float upgradeCostMultiplier = 1f;
    [HideInInspector] public bool  mergeAccelActive        = false;

    private PlayerAttack selectedUnit;
    private bool justSelected = false;
    private GameObject currentRangeIndicator;
    private PlayerAttack cachedLeader;
    private int currentSkillIndex = -1;

    private static readonly string[] tier5Names = { "Tier5_1", "Tier5_2", "Tier5_3", "Tier5_4" };

    private static readonly string[] skillNames = { "심연의 균열", "광전사의 각성", "심판의 일격", "천벌" };
    private static readonly string[] skillDescs =
    {
        "대상 위치에 장판을 생성해 범위 내 적에게 지속 피해를 입힙니다.",
        "일정 시간 동안 공격력과 공격속도가 대폭 증가합니다.",
        "보스를 우선으로 단일 대상에게 강력한 피해를 입힙니다.",
        "맵 전체의 모든 적에게 동시에 피해를 입힙니다."
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    void Start()
    {
        if (mergeButton != null) { mergeButton.onClick.RemoveAllListeners(); mergeButton.onClick.AddListener(ExecuteMerge); }
        if (sellButton != null) { sellButton.onClick.RemoveAllListeners(); sellButton.onClick.AddListener(ExecuteSell); }
        for (int i = 0; i < skillButtons.Length; i++)
        {
            int idx = i;
            if (skillButtons[i] == null) continue;
            skillButtons[i].onClick.RemoveAllListeners();
            skillButtons[i].onClick.AddListener(() => OnSkillButtonClick(idx));
            AddTooltipEvents(skillButtons[i], idx);
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
            currentRangeIndicator.AddComponent<RangeIndicator>().SetRange(unit.AppliedRange);
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
        string charName = cachedLeader.characterData.characterName;
        bool hasTarget = (charName == "Tier5_2" || charName == "Tier5_4")
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
        if (mergeAccelActive)
        {
            float roll = Random.Range(0f, 100f);
            if (roll < 1f)
            {
                if (!CoinManager.Instance.SpendCoins(actualCost * 2)) return;
            }
            else if (roll >= 30f)
            {
                if (!CoinManager.Instance.SpendCoins(actualCost)) return;
            }
        }
        else if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(actualCost)) return;
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

    void AddTooltipEvents(Button btn, int index)
    {
        UnityEngine.EventSystems.EventTrigger trigger = btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        trigger.triggers.Clear();

        var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) =>
        {
            if (tooltipPanel == null) return;
            if (tooltipText != null) tooltipText.text = $"{skillNames[index]}\n{skillDescs[index]}";
            tooltipPanel.SetActive(true);

            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt == null) return;

            Canvas.ForceUpdateCanvases();

            Vector3 btnPos = btn.transform.position;
            float tooltipW = rt.rect.width;
            float tooltipH = rt.rect.height;
            float screenW = Screen.width;
            float screenH = Screen.height;

            float x = btnPos.x;
            float y = btnPos.y + tooltipH + 10f;

            if (x + tooltipW * 0.5f > screenW) x = screenW - tooltipW * 0.5f;
            if (x - tooltipW * 0.5f < 0) x = tooltipW * 0.5f;
            if (y + tooltipH > screenH) y = btnPos.y - tooltipH - 10f;

            rt.position = new Vector3(x, y, 0f);
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { if (tooltipPanel != null) tooltipPanel.SetActive(false); });
        trigger.triggers.Add(exitEntry);
    }

    public bool IsUnitActionUIActive() => unitActionUI != null && unitActionUI.activeSelf;
    public bool IsSelectedSlot(int slotIndex) => selectedUnit != null && selectedUnit.spawnIndex == slotIndex;
}
