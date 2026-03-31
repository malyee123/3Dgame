using UnityEngine;
using TMPro;

public class SpecialCoinManager : MonoBehaviour
{
    public static SpecialCoinManager Instance { get; private set; }

    [Header("Special Coin UI")]
    public TextMeshProUGUI specialCoinText;

    private int specialCoins = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() { UpdateUI(); }

    public void AddSpecialCoins(int amount)
    {
        specialCoins += amount;
        UpdateUI();
        // Debug.Log($"[SpecialCoinManager] Special coins added: +{amount} (Total: {specialCoins})");
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
    }
}