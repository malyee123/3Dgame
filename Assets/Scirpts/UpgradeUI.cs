using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UpgradeUI : MonoBehaviour
{
    [Header("Skill Point UI")]
    public TextMeshProUGUI skillPointText;

    [Header("Attack Damage UI")]
    public TextMeshProUGUI attackDamageLevelText;
    public TextMeshProUGUI attackDamageCostText;
    public Button attackDamageButton;

    [Header("Attack Speed UI")]
    public TextMeshProUGUI attackSpeedLevelText;
    public TextMeshProUGUI attackSpeedCostText;
    public Button attackSpeedButton;

    [Header("Coin Per Kill UI")]
    public TextMeshProUGUI coinPerKillLevelText;
    public TextMeshProUGUI coinPerKillCostText;
    public Button coinPerKillButton;

    [Header("Starting Coin UI")]
    public TextMeshProUGUI startingCoinLevelText;
    public TextMeshProUGUI startingCoinCostText;
    public Button startingCoinButton;

    [Header("Tier Unlock UI")]
    public TextMeshProUGUI tierLevelText;
    public TextMeshProUGUI tierCostText;
    public Button tierUnlockButton;

    [Header("Close Button")]
    public Button closeButton;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClickClose);
        }
        RefreshUI();
    }

    void RefreshUI()
    {
        if (UpgradeManager.Instance == null) return;
        int sp = UpgradeManager.Instance.GetSkillPoints();
        int maxLevel = UpgradeManager.Instance.GetMaxUpgradeLevel();

        if (skillPointText != null) skillPointText.text = $"스킬 포인트: {sp}";

        UpdateSlot(attackDamageLevelText, attackDamageCostText, attackDamageButton,
            UpgradeManager.Instance.AttackDamageLevel, maxLevel,
            UpgradeManager.Instance.GetUpgradeCost("AttackDamage", UpgradeManager.Instance.AttackDamageLevel),
            sp, $"+{UpgradeManager.Instance.GetAttackDamageMultiplier() * 100 - 100:F0}%");

        UpdateSlot(attackSpeedLevelText, attackSpeedCostText, attackSpeedButton,
            UpgradeManager.Instance.AttackSpeedLevel, maxLevel,
            UpgradeManager.Instance.GetUpgradeCost("AttackSpeed", UpgradeManager.Instance.AttackSpeedLevel),
            sp, $"+{(UpgradeManager.Instance.GetAttackSpeedMultiplier() - 1f) * 100:F0}%");

        UpdateSlot(coinPerKillLevelText, coinPerKillCostText, coinPerKillButton,
            UpgradeManager.Instance.CoinPerKillLevel, maxLevel,
            UpgradeManager.Instance.GetUpgradeCost("CoinPerKill", UpgradeManager.Instance.CoinPerKillLevel),
            sp, $"+{UpgradeManager.Instance.GetCoinPerKillBonus()}");

        UpdateSlot(startingCoinLevelText, startingCoinCostText, startingCoinButton,
            UpgradeManager.Instance.StartingCoinLevel, maxLevel,
            UpgradeManager.Instance.GetUpgradeCost("StartingCoin", UpgradeManager.Instance.StartingCoinLevel),
            sp, $"+{UpgradeManager.Instance.GetStartingCoinBonus()}");

        int currentTier = UpgradeManager.Instance.UnlockedTier;
        int[] tierCosts = UpgradeManager.Instance.TierUnlockCosts;
        bool tierMaxed = tierCosts == null || currentTier >= tierCosts.Length;
        if (tierLevelText != null) tierLevelText.text = $"티어 해금  Lv.{currentTier}";
        if (tierCostText != null) tierCostText.text = tierMaxed ? "MAX" : $"Cost: {tierCosts[currentTier - 1]}";
        if (tierUnlockButton != null) tierUnlockButton.interactable = !tierMaxed && sp >= (tierMaxed ? 0 : tierCosts[currentTier - 1]);
    }

    void UpdateSlot(TextMeshProUGUI levelText, TextMeshProUGUI costText, Button button, int level, int maxLevel, int cost, int sp, string bonusText)
    {
        bool isMaxed = level >= maxLevel;
        if (levelText != null) levelText.text = isMaxed ? $"Lv.{level}  ({bonusText})  MAX" : $"Lv.{level}  ({bonusText})";
        if (costText != null) costText.text = isMaxed ? "MAX" : $"Cost: {cost}";
        if (button != null) button.interactable = !isMaxed && sp >= cost;
    }

    public void OnClickAttackDamage() { UpgradeManager.Instance.UpgradeAttackDamage(); RefreshUI(); }
    public void OnClickAttackSpeed() { UpgradeManager.Instance.UpgradeAttackSpeed(); RefreshUI(); }
    public void OnClickCoinPerKill() { UpgradeManager.Instance.UpgradeCoinPerKill(); RefreshUI(); }
    public void OnClickStartingCoin() { UpgradeManager.Instance.UpgradeStartingCoin(); RefreshUI(); }
    public void OnClickTierUnlock() { UpgradeManager.Instance.UnlockNextTier(); RefreshUI(); }
    public void OnClickClose() => SceneManager.LoadScene("LobbyScene");
}
