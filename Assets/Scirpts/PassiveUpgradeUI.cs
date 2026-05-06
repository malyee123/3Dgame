using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PassiveUpgradeUI : MonoBehaviour
{
    [Header("Skill Point")]
    public TextMeshProUGUI skillPointText;

    [Header("Tier2")]
    public TextMeshProUGUI tier2LevelText;
    public TextMeshProUGUI tier2CostText;
    public Button tier2Button;

    [Header("Tier3")]
    public TextMeshProUGUI tier3LevelText;
    public TextMeshProUGUI tier3CostText;
    public Button tier3Button;

    [Header("Tier4")]
    public TextMeshProUGUI tier4LevelText;
    public TextMeshProUGUI tier4CostText;
    public Button tier4Button;

    [Header("Tier5")]
    public TextMeshProUGUI tier5LevelText;
    public TextMeshProUGUI tier5CostText;
    public Button tier5Button;

    [Header("Close")]
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
        if (skillPointText != null) skillPointText.text = $"Skill Points: {sp}";
        UpdateSlot(tier2LevelText, tier2CostText, tier2Button, 2, sp);
        UpdateSlot(tier3LevelText, tier3CostText, tier3Button, 3, sp);
        UpdateSlot(tier4LevelText, tier4CostText, tier4Button, 4, sp);
        UpdateSlot(tier5LevelText, tier5CostText, tier5Button, 5, sp);
    }

    void UpdateSlot(TextMeshProUGUI levelText, TextMeshProUGUI costText, Button button, int tier, int sp)
    {
        int level = UpgradeManager.Instance.GetTierPassiveLevel(tier);
        int cost = UpgradeManager.Instance.GetTierPassiveCost(tier);
        float bonus = UpgradeManager.Instance.GetTierPassiveBonus(tier);
        if (levelText != null) levelText.text = $"티어{tier} 패시브  Lv.{level}  (+{bonus}%)";
        if (costText != null) costText.text = $"Cost: {cost}";
        if (button != null) button.interactable = sp >= cost;
    }

    void OnClickTierPassive(int tier)
    {
        if (UpgradeManager.Instance == null) return;
        UpgradeManager.Instance.UpgradeTierPassive(tier);
        RefreshUI();
    }

    public void OnClickTier2() => OnClickTierPassive(2);
    public void OnClickTier3() => OnClickTierPassive(3);
    public void OnClickTier4() => OnClickTierPassive(4);
    public void OnClickTier5() => OnClickTierPassive(5);
    public void OnClickClose() => SceneManager.LoadScene("LobbyScene");
}