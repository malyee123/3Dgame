using TMPro;
using UnityEngine;

public class SpecialCoinManager : MonoBehaviour
{
    public static SpecialCoinManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI specialCoinText;

    private int specialCoins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => UpdateUI();

    public void AddSpecialCoins(int amount)
    {
        specialCoins += amount;
        UpdateUI();
        if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.ForceUpdateSpawnButton();
    }

    public bool SpendSpecialCoins(int amount)
    {
        if (specialCoins < amount) return false;
        specialCoins -= amount;
        UpdateUI();
        if (PlayerSpawner.Instance != null) PlayerSpawner.Instance.ForceUpdateSpawnButton();
        return true;
    }

    public int GetSpecialCoins() => specialCoins;

    void UpdateUI()
    {
        if (specialCoinText != null) specialCoinText.text = $"Special: {specialCoins}";
    }
}