using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject upgradePanel;

    [Header("Lobby Buttons")]
    public GameObject upgradeButton;
    public GameObject startButton;

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

    [Header("Tier Passive UI")]
    public TextMeshProUGUI[] tierPassiveLevelTexts = new TextMeshProUGUI[5];
    public TextMeshProUGUI[] tierPassiveCostTexts = new TextMeshProUGUI[5];
    public Button[] tierPassiveButtons = new Button[5];

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(true);
        if (upgradeButton != null) upgradeButton.SetActive(false);
        if (startButton != null) startButton.SetActive(false);
        RefreshUI();
    }

    public void CloseUpgradePanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (upgradeButton != null) upgradeButton.SetActive(true);
        if (startButton != null) startButton.SetActive(true);
    }

    void RefreshUI()
    {
        if (UpgradeManager.Instance == null) return;

        if (skillPointText != null)
            skillPointText.text = $"Skill Points: {UpgradeManager.Instance.GetSkillPoints()}";

        int sp = UpgradeManager.Instance.GetSkillPoints();

        UpdateSlot(attackDamageLevelText, attackDamageCostText, attackDamageButton,
            UpgradeManager.Instance.AttackDamageLevel,
            UpgradeManager.Instance.GetUpgradeCost("AttackDamage", UpgradeManager.Instance.AttackDamageLevel),
            sp, "AttackDamage", UpgradeManager.Instance.GetAttackDamageMultiplier());

        UpdateSlot(attackSpeedLevelText, attackSpeedCostText, attackSpeedButton,
            UpgradeManager.Instance.AttackSpeedLevel,
            UpgradeManager.Instance.GetUpgradeCost("AttackSpeed", UpgradeManager.Instance.AttackSpeedLevel),
            sp, "AttackSpeed", UpgradeManager.Instance.GetAttackSpeedMultiplier());

        UpdateSlot(coinPerKillLevelText, coinPerKillCostText, coinPerKillButton,
            UpgradeManager.Instance.CoinPerKillLevel,
            UpgradeManager.Instance.GetUpgradeCost("CoinPerKill", UpgradeManager.Instance.CoinPerKillLevel),
            sp, "CoinPerKill", UpgradeManager.Instance.GetCoinPerKillBonus());

        UpdateSlot(startingCoinLevelText, startingCoinCostText, startingCoinButton,
            UpgradeManager.Instance.StartingCoinLevel,
            UpgradeManager.Instance.GetUpgradeCost("StartingCoin", UpgradeManager.Instance.StartingCoinLevel),
            sp, "StartingCoin", UpgradeManager.Instance.GetStartingCoinBonus());

        int currentTier = UpgradeManager.Instance.UnlockedTier;
        int[] tierCosts = UpgradeManager.Instance.tierUnlockCosts;
        bool tierMaxed = tierCosts == null || currentTier >= tierCosts.Length;
        if (tierLevelText != null) tierLevelText.text = $"Tier Unlocked: {currentTier}";
        if (tierCostText != null) tierCostText.text = tierMaxed ? "MAX" : $"Cost: {tierCosts[currentTier - 1]}";
        if (tierUnlockButton != null) tierUnlockButton.interactable = !tierMaxed && sp >= (tierMaxed ? 0 : tierCosts[currentTier - 1]);

        for (int i = 0; i < 5; i++)
        {
            int tier = i + 1;
            int level = UpgradeManager.Instance.GetTierPassiveLevel(tier);
            int cost = UpgradeManager.Instance.GetTierPassiveCost(tier);
            float bonus = UpgradeManager.Instance.GetTierPassiveBonus(tier);

            if (tierPassiveLevelTexts[i] != null)
                tierPassiveLevelTexts[i].text = $"Tier{tier} Passive Lv.{level} (+{bonus}%)";
            if (tierPassiveCostTexts[i] != null)
                tierPassiveCostTexts[i].text = $"Cost: {cost}";
            if (tierPassiveButtons[i] != null)
                tierPassiveButtons[i].interactable = sp >= cost;
        }
    }

    void UpdateSlot(TextMeshProUGUI levelText, TextMeshProUGUI costText, Button button, int level, int cost, int sp, string type, float currentValue)
    {
        if (levelText != null) levelText.text = $"Lv.{level} ({currentValue})";
        if (costText != null) costText.text = $"Cost: {cost}";
        if (button != null) button.interactable = sp >= cost;
    }

    public void OnClickAttackDamage() { UpgradeManager.Instance.UpgradeAttackDamage(); RefreshUI(); }
    public void OnClickAttackSpeed() { UpgradeManager.Instance.UpgradeAttackSpeed(); RefreshUI(); }
    public void OnClickCoinPerKill() { UpgradeManager.Instance.UpgradeCoinPerKill(); RefreshUI(); }
    public void OnClickStartingCoin() { UpgradeManager.Instance.UpgradeStartingCoin(); RefreshUI(); }
    public void OnClickTierUnlock() { UpgradeManager.Instance.UnlockNextTier(); RefreshUI(); }
    public void OnClickTierPassive(int tier) { UpgradeManager.Instance.UpgradeTierPassive(tier); RefreshUI(); }
}