using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecialCoinManager : MonoBehaviour
{
    public static SpecialCoinManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI specialCoinText;
    public Button anvilButton;

    [HideInInspector] public int anvilCost = 3;

    private int specialCoins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (CSVLoader.Instance?.GameSettings != null)
            anvilCost = CSVLoader.Instance.GameSettings.anvilCost;

        if (anvilButton != null)
        {
            anvilButton.onClick.RemoveAllListeners();
            anvilButton.onClick.AddListener(TryOpenAnvil);
        }
        UpdateUI();
    }

    public void TryOpenAnvil()
    {
        if (specialCoins < anvilCost) return;
        if (AnvilUI.Instance == null) return;
        specialCoins -= anvilCost;
        UpdateUI();
        AnvilUI.Instance.ShowAnvils();
    }

    public void AddSpecialCoins(int amount)
    {
        specialCoins += amount;
        UpdateUI();
    }

    public bool SpendSpecialCoins(int amount)
    {
        if (specialCoins < amount) return false;
        specialCoins -= amount;
        UpdateUI();
        return true;
    }

    public int GetSpecialCoins() => specialCoins;

    void UpdateUI()
    {
        if (specialCoinText != null) specialCoinText.text = $"Special: {specialCoins}";
        if (anvilButton != null) anvilButton.interactable = specialCoins >= anvilCost;
    }
}
