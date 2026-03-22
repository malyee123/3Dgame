using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject upgradePanel;

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

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(true);
        RefreshUI();
    }

    public void CloseUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    void RefreshUI()
    {
        if (UpgradeManager.Instance == null) return;

        if (skillPointText != null)
            skillPointText.text = $"Skill Points: {UpgradeManager.Instance.GetSkillPoints()}";

        UpdateSlot(attackDamageLevelText, attackDamageCostText, attackDamageButton,
            UpgradeManager.Instance.AttackDamageLevel, UpgradeManager.Instance.maxAttackDamageLevel,
            UpgradeManager.Instance.attackDamageCosts, UpgradeManager.Instance.attackDamagePercentPerLevel);

        UpdateSlot(attackSpeedLevelText, attackSpeedCostText, attackSpeedButton,
            UpgradeManager.Instance.AttackSpeedLevel, UpgradeManager.Instance.maxAttackSpeedLevel,
            UpgradeManager.Instance.attackSpeedCosts, UpgradeManager.Instance.attackSpeedPercentPerLevel);

        UpdateSlot(coinPerKillLevelText, coinPerKillCostText, coinPerKillButton,
            UpgradeManager.Instance.CoinPerKillLevel, UpgradeManager.Instance.maxCoinPerKillLevel,
            UpgradeManager.Instance.coinPerKillCosts, UpgradeManager.Instance.coinPerKillPercentPerLevel);

        UpdateSlot(startingCoinLevelText, startingCoinCostText, startingCoinButton,
            UpgradeManager.Instance.StartingCoinLevel, UpgradeManager.Instance.maxStartingCoinLevel,
            UpgradeManager.Instance.startingCoinCosts, UpgradeManager.Instance.startingCoinPercentPerLevel);

        int currentTier = UpgradeManager.Instance.UnlockedTier;
        int[] tierCosts = UpgradeManager.Instance.tierUnlockCosts;
        bool tierMaxed = tierCosts == null || currentTier >= tierCosts.Length;

        if (tierLevelText != null) tierLevelText.text = $"Tier Unlocked: {currentTier}";
        if (tierCostText != null) tierCostText.text = tierMaxed ? "MAX" : $"Cost: {tierCosts[currentTier - 1]}";
        if (tierUnlockButton != null) tierUnlockButton.interactable = !tierMaxed && UpgradeManager.Instance.GetSkillPoints() >= (tierMaxed ? 0 : tierCosts[currentTier - 1]);
    }

    void UpdateSlot(TextMeshProUGUI levelText, TextMeshProUGUI costText, Button button, int level, int maxLevel, int[] costs, float percentPerLevel)
    {
        bool isMaxed = level >= maxLevel;
        int cost = UpgradeManager.Instance.GetCost(costs, level);

        if (levelText != null) levelText.text = $"Level: {level}/{maxLevel} (+{level * percentPerLevel}%)";
        if (costText != null) costText.text = isMaxed ? "MAX" : $"Cost: {cost}";
        if (button != null) button.interactable = !isMaxed && UpgradeManager.Instance.GetSkillPoints() >= cost;
    }

    public void OnClickAttackDamage() { UpgradeManager.Instance.UpgradeAttackDamage(); RefreshUI(); }
    public void OnClickAttackSpeed() { UpgradeManager.Instance.UpgradeAttackSpeed(); RefreshUI(); }
    public void OnClickCoinPerKill() { UpgradeManager.Instance.UpgradeCoinPerKill(); RefreshUI(); }
    public void OnClickStartingCoin() { UpgradeManager.Instance.UpgradeStartingCoin(); RefreshUI(); }
    public void OnClickTierUnlock() { UpgradeManager.Instance.UnlockNextTier(); RefreshUI(); }
}