using UnityEngine;
using UnityEngine.UI;

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("UI")]
    public GameObject mergePanel;
    public Button mergeButton;
    public CanvasGroup mergeButtonCanvasGroup;

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

        if (mergePanel != null)
            mergePanel.SetActive(false);

        RefreshMergeUI();
    }

    public void SelectUnit(PlayerAttack unit)
    {
        selectedUnit = unit;

        if (mergePanel != null)
            mergePanel.SetActive(selectedUnit != null);

        RefreshMergeUI();
    }

    void RefreshMergeUI()
    {
        bool canMerge = false;

        if (selectedUnit != null && PlayerSpawner.Instance != null)
            canMerge = PlayerSpawner.Instance.CanManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag);

        if (mergeButton != null)
            mergeButton.interactable = canMerge;

        if (mergeButtonCanvasGroup != null)
            mergeButtonCanvasGroup.alpha = canMerge ? 1f : 0.4f;
    }


    public void CheckMergeAvailable()
    {
        RefreshMergeUI();
    }

    public void ExecuteMerge()
    {
        if (selectedUnit == null || PlayerSpawner.Instance == null) return;

        bool merged = PlayerSpawner.Instance.TryManualMerge(selectedUnit.spawnIndex, selectedUnit.unitTag);
        if (merged)
        {
            selectedUnit = null;
            if (mergePanel != null)
                mergePanel.SetActive(false);
        }

        RefreshMergeUI();
    }
}
