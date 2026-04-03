using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("UI")]
    public GameObject unitActionUI;          // 버튼 2개 담는 빈 오브젝트
    public Button mergeButton;
    public CanvasGroup mergeButtonCanvasGroup;
    public Button sellButton;
    public TextMeshProUGUI sellPriceText;

    [Header("Canvas")]
    public Canvas gameCanvas;               // 메인 Canvas 연결

    private PlayerAttack selectedUnit;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[MergeManager] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        if (mergeButton != null)
        {
            mergeButton.onClick.RemoveAllListeners();
            mergeButton.onClick.AddListener(ExecuteMerge);
        }
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(ExecuteSell);
        }
        if (unitActionUI != null) unitActionUI.SetActive(false);
        RefreshMergeUI();
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && unitActionUI != null && unitActionUI.activeSelf)
        {
            if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;

            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                selectedUnit = null;
                unitActionUI.SetActive(false);
            }
        }
    }

    public void SelectUnit(PlayerAttack unit)
    {
        selectedUnit = unit;

        if (unitActionUI != null && unit != null)
        {
            // 유닛 월드 좌표 → UI 위치로 변환
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            RectTransform uiRect = unitActionUI.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gameCanvas.GetComponent<RectTransform>(),
                screenPos,
                gameCanvas.worldCamera,
                out Vector2 localPos
            );
            uiRect.anchoredPosition = localPos + new Vector2(0, 80f); // 캐릭터 위에 표시
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
            bool tierAllowed = UpgradeManager.Instance == null ||
                               (currentTier + 1) <= UpgradeManager.Instance.UnlockedTier;

            bool hasEnoughCoins = CoinManager.Instance != null &&
                                  CoinManager.Instance.GetCoins() >= 150;

            bool canManualMerge = PlayerSpawner.Instance.CanManualMerge(
                selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData);

            canMerge = tierAllowed && hasEnoughCoins && canManualMerge;
        }

        if (mergeButton != null) mergeButton.interactable = canMerge;
        if (mergeButtonCanvasGroup != null) mergeButtonCanvasGroup.alpha = canMerge ? 1f : 0.4f;

        if (sellPriceText != null && selectedUnit != null && selectedUnit.characterData != null)
        {
            PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
            int count = 0;
            foreach (PlayerAttack unit in allUnits)
                if (unit != null && unit.spawnIndex == selectedUnit.spawnIndex) count++;
            int totalPrice = selectedUnit.characterData.sellPrice * count;
            sellPriceText.text = $"{totalPrice}G";
        }
    }

    public void CheckMergeAvailable() { RefreshMergeUI(); }

    public void ExecuteMerge()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;

        int currentTier = selectedUnit.characterData != null ? selectedUnit.characterData.tier : 1;
        if (UpgradeManager.Instance != null)
            if ((currentTier + 1) > UpgradeManager.Instance.UnlockedTier) return;

        if (CoinManager.Instance != null)
            if (!CoinManager.Instance.SpendCoins(150)) return;

        bool merged = PlayerSpawner.Instance.TryManualMerge(
            selectedUnit.spawnIndex, selectedUnit.unitTag, selectedUnit.characterData);

        if (merged)
        {
            selectedUnit = null;
            if (unitActionUI != null) unitActionUI.SetActive(false);
        }

        RefreshMergeUI();
    }

    public void ExecuteSell()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;

        int spawnIndex = selectedUnit.spawnIndex;
        int sellPrice = selectedUnit.characterData != null ? selectedUnit.characterData.sellPrice : 0;

        // 같은 슬롯 유닛 전체 수집
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        System.Collections.Generic.List<PlayerAttack> unitsToSell = new System.Collections.Generic.List<PlayerAttack>();
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.spawnIndex == spawnIndex) unitsToSell.Add(unit);

        // 전체 판매 가격
        int totalSellPrice = sellPrice * unitsToSell.Count;

        // 전체 제거
        foreach (PlayerAttack unit in unitsToSell)
        {
            PlayerSpawner.Instance.UnregisterUnit(unit, unit.spawnIndex);
            Destroy(unit.gameObject);
        }

        // 코인 지급
        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(totalSellPrice);

        selectedUnit = null;
        if (unitActionUI != null) unitActionUI.SetActive(false);

        RefreshMergeUI();
    }
    
    public void HideUnitActionUI()
    {
        selectedUnit = null;
        if (unitActionUI != null) unitActionUI.SetActive(false);
    }
}