using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecialCoinManager : MonoBehaviour
{
    public static SpecialCoinManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI specialCoinText;
    public Button anvilButton;
    public int anvilCost = 3;

    private int specialCoins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
        if (anvilButton != null)
        {
            anvilButton.onClick.RemoveAllListeners();
            anvilButton.onClick.AddListener(TryOpenAnvil);
        }
    }

    public void TryOpenAnvil()
    {
        if (specialCoins < anvilCost) return;
        if (AnvilUI.Instance == null) return;
        specialCoins -= anvilCost;
        UpdateUI();
        AnvilUI.Instance.ShowAnvils();
        ForceUpdateButton();
    }

    public void AddSpecialCoins(int amount)
    {
        specialCoins += amount;
        UpdateUI();
        ForceUpdateButton();
    }

    public bool SpendSpecialCoins(int amount)
    {
        if (specialCoins < amount) return false;
        specialCoins -= amount;
        UpdateUI();
        ForceUpdateButton();
        return true;
    }

    public int GetSpecialCoins() => specialCoins;

    void ForceUpdateButton()
    {
        PlayerSpawner.Instance?.ForceUpdateSpawnButton();
        if (anvilButton != null)
            anvilButton.interactable = specialCoins >= anvilCost;
    }

    void UpdateUI()
    {
        if (specialCoinText != null) specialCoinText.text = $"Special: {specialCoins}";
        if (anvilButton != null)
            anvilButton.interactable = specialCoins >= anvilCost;
    }
}
