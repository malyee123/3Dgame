using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Debug Buttons")]
    public Button debugOnButton;
    public Button debugOffButton;
    public Button debugAugmentButton;
    public TextMeshProUGUI debugStatusText;

    private const int DEBUG_COINS = 99999;
    private const int DEBUG_SPECIAL_COINS = 99999;
    private const int DEBUG_UPGRADE_LEVEL = 100;
    private const int MAX_TIER_UPGRADE = 10;

    private const string PREFS_SAVE_ATK = "Save_UpgradeAttackDamage";
    private const string PREFS_SAVE_CLIMIT = "Save_UpgradeCharacterLimit";
    private const string PREFS_SAVE_COIN = "Save_UpgradeCoinPerKill";
    private const string PREFS_SAVE_START = "Save_UpgradeStartingCoin";
    private const string PREFS_SAVE_T2 = "Save_Tier2PassiveLevel";
    private const string PREFS_SAVE_T3 = "Save_Tier3PassiveLevel";
    private const string PREFS_SAVE_T4 = "Save_Tier4PassiveLevel";
    private const string PREFS_SAVE_T5 = "Save_Tier5PassiveLevel";
    private const string PREFS_SAVE_TIER = "Save_UnlockedTier";

    private bool isDebugMode = false;
    private int savedCoins;
    private int savedSpecialCoins;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (debugOnButton != null)
        {
            debugOnButton.onClick.RemoveAllListeners();
            debugOnButton.onClick.AddListener(ActivateDebugMode);
        }
        if (debugOffButton != null)
        {
            debugOffButton.onClick.RemoveAllListeners();
            debugOffButton.onClick.AddListener(DeactivateDebugMode);
        }
        if (debugAugmentButton != null)
        {
            debugAugmentButton.onClick.RemoveAllListeners();
            debugAugmentButton.onClick.AddListener(ShowDebugAugment);
        }

        UpdateStatusText();
        RefreshButtonVisibility();
    }

    // 씬 전환 시 자동으로 PlayerPrefs 복원
    void OnDestroy()
    {
        if (isDebugMode) RestorePlayerPrefs();
    }

    public void ActivateDebugMode()
    {
        if (isDebugMode) return;

        SaveCurrentState();
        isDebugMode = true;

        int maxLevel = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetMaxUpgradeLevel()
            : DEBUG_UPGRADE_LEVEL;

        PlayerPrefs.SetInt("UpgradeAttackDamage", maxLevel);
        PlayerPrefs.SetInt("UpgradeCharacterLimit", MAX_TIER_UPGRADE);
        PlayerPrefs.SetInt("UpgradeCoinPerKill", maxLevel);
        PlayerPrefs.SetInt("UpgradeStartingCoin", maxLevel);
        PlayerPrefs.SetInt("Tier2PassiveLevel", DEBUG_UPGRADE_LEVEL);
        PlayerPrefs.SetInt("Tier3PassiveLevel", DEBUG_UPGRADE_LEVEL);
        PlayerPrefs.SetInt("Tier4PassiveLevel", DEBUG_UPGRADE_LEVEL);
        PlayerPrefs.SetInt("Tier5PassiveLevel", DEBUG_UPGRADE_LEVEL);
        PlayerPrefs.SetInt("UnlockedTier", 5);
        PlayerPrefs.Save();

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(DEBUG_COINS - CoinManager.Instance.GetCoins());
        if (SpecialCoinManager.Instance != null)
            SpecialCoinManager.Instance.AddSpecialCoins(DEBUG_SPECIAL_COINS - SpecialCoinManager.Instance.GetSpecialCoins());

        PassiveManager.Instance?.RecalculatePassives();
        RefreshAllUnits();
        UpdateStatusText();
        RefreshButtonVisibility();
    }

    public void DeactivateDebugMode()
    {
        if (!isDebugMode) return;
        isDebugMode = false;

        RestorePlayerPrefs();

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(savedCoins - CoinManager.Instance.GetCoins());
        if (SpecialCoinManager.Instance != null)
            SpecialCoinManager.Instance.AddSpecialCoins(savedSpecialCoins - SpecialCoinManager.Instance.GetSpecialCoins());

        PassiveManager.Instance?.RecalculatePassives();
        RefreshAllUnits();
        UpdateStatusText();
        RefreshButtonVisibility();
    }

    void RestorePlayerPrefs()
    {
        PlayerPrefs.SetInt("UpgradeAttackDamage", PlayerPrefs.GetInt(PREFS_SAVE_ATK, 0));
        PlayerPrefs.SetInt("UpgradeCharacterLimit", PlayerPrefs.GetInt(PREFS_SAVE_CLIMIT, 0));
        PlayerPrefs.SetInt("UpgradeCoinPerKill", PlayerPrefs.GetInt(PREFS_SAVE_COIN, 0));
        PlayerPrefs.SetInt("UpgradeStartingCoin", PlayerPrefs.GetInt(PREFS_SAVE_START, 0));
        PlayerPrefs.SetInt("Tier2PassiveLevel", PlayerPrefs.GetInt(PREFS_SAVE_T2, 0));
        PlayerPrefs.SetInt("Tier3PassiveLevel", PlayerPrefs.GetInt(PREFS_SAVE_T3, 0));
        PlayerPrefs.SetInt("Tier4PassiveLevel", PlayerPrefs.GetInt(PREFS_SAVE_T4, 0));
        PlayerPrefs.SetInt("Tier5PassiveLevel", PlayerPrefs.GetInt(PREFS_SAVE_T5, 0));
        PlayerPrefs.SetInt("UnlockedTier", PlayerPrefs.GetInt(PREFS_SAVE_TIER, 1));

        PlayerPrefs.DeleteKey(PREFS_SAVE_ATK);
        PlayerPrefs.DeleteKey(PREFS_SAVE_CLIMIT);
        PlayerPrefs.DeleteKey(PREFS_SAVE_COIN);
        PlayerPrefs.DeleteKey(PREFS_SAVE_START);
        PlayerPrefs.DeleteKey(PREFS_SAVE_T2);
        PlayerPrefs.DeleteKey(PREFS_SAVE_T3);
        PlayerPrefs.DeleteKey(PREFS_SAVE_T4);
        PlayerPrefs.DeleteKey(PREFS_SAVE_T5);
        PlayerPrefs.DeleteKey(PREFS_SAVE_TIER);
        PlayerPrefs.Save();
    }

    void SaveCurrentState()
    {
        PlayerPrefs.SetInt(PREFS_SAVE_ATK, PlayerPrefs.GetInt("UpgradeAttackDamage", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_CLIMIT, PlayerPrefs.GetInt("UpgradeCharacterLimit", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_COIN, PlayerPrefs.GetInt("UpgradeCoinPerKill", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_START, PlayerPrefs.GetInt("UpgradeStartingCoin", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_T2, PlayerPrefs.GetInt("Tier2PassiveLevel", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_T3, PlayerPrefs.GetInt("Tier3PassiveLevel", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_T4, PlayerPrefs.GetInt("Tier4PassiveLevel", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_T5, PlayerPrefs.GetInt("Tier5PassiveLevel", 0));
        PlayerPrefs.SetInt(PREFS_SAVE_TIER, PlayerPrefs.GetInt("UnlockedTier", 1));
        PlayerPrefs.Save();

        savedCoins = CoinManager.Instance != null ? CoinManager.Instance.GetCoins() : 0;
        savedSpecialCoins = SpecialCoinManager.Instance != null ? SpecialCoinManager.Instance.GetSpecialCoins() : 0;
    }

    void RefreshAllUnits()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null) unit.RefreshAugmentState();
    }

    public void ShowDebugAugment()
    {
        if (AugmentUI.Instance != null)
            AugmentUI.Instance.ShowAugmentsDebug();
    }

    void RefreshButtonVisibility()
    {
        if (debugOnButton != null) debugOnButton.gameObject.SetActive(!isDebugMode);
        if (debugOffButton != null) debugOffButton.gameObject.SetActive(isDebugMode);
        if (debugAugmentButton != null) debugAugmentButton.gameObject.SetActive(isDebugMode);
    }

    void UpdateStatusText()
    {
        if (debugStatusText == null) return;
        if (isDebugMode)
        {
            debugStatusText.text = "[ DEBUG 모드 ]";
            debugStatusText.color = new Color(1f, 0.27f, 0.27f);
        }
        else
        {
            debugStatusText.text = "[ 일반 모드 ]";
            debugStatusText.color = new Color(0.24f, 0.71f, 0.31f);
        }
    }
}