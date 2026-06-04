using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CompendiumManager : MonoBehaviour
{
    public static CompendiumManager Instance { get; private set; }

    [Header("Character Data")]
    public CharacterData[] allCharacters;

    [Header("Card Grid")]
    public Transform cardContainer;
    public GameObject cardPrefab;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public Image       tooltipPortrait;
    public TextMeshProUGUI tooltipName;
    public TextMeshProUGUI tooltipTier;
    public TextMeshProUGUI tooltipStats;
    public TextMeshProUGUI tooltipPassives;

    [Header("Navigation")]
    public Button closeButton;

    [Header("Filter Buttons")]
    public Button[] tierFilterButtons;

    private int currentTierFilter = 0;
    private CompendiumCard hoveredCard;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));
        }

        if (tierFilterButtons != null)
        {
            for (int i = 0; i < tierFilterButtons.Length; i++)
            {
                int tier = i;
                if (tierFilterButtons[i] != null)
                {
                    tierFilterButtons[i].onClick.RemoveAllListeners();
                    tierFilterButtons[i].onClick.AddListener(() => FilterByTier(tier));
                }
            }
        }

        BuildCards();
    }

    void BuildCards()
    {
        if (cardContainer == null || cardPrefab == null || allCharacters == null) return;

        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        int unlockedTier = PlayerPrefs.GetInt("UnlockedTier", 1);

        foreach (CharacterData cd in allCharacters)
        {
            if (cd == null) continue;
            if (currentTierFilter > 0 && cd.tier != currentTierFilter) continue;

            GameObject obj = Instantiate(cardPrefab, cardContainer);
            CompendiumCard card = obj.GetComponent<CompendiumCard>();
            if (card != null)
            {
                bool unlocked = cd.tier <= unlockedTier;
                card.Setup(cd, unlocked, this);
            }
        }
    }

    void FilterByTier(int tier)
    {
        currentTierFilter = (currentTierFilter == tier) ? 0 : tier;
        BuildCards();
    }

    public void ShowTooltip(CharacterData cd, bool unlocked)
    {
        if (tooltipPanel == null || cd == null) return;

        if (tooltipPortrait != null)
            tooltipPortrait.sprite = unlocked ? cd.characterSprite : null;

        if (tooltipName != null)
            tooltipName.text = unlocked ? cd.characterName : "???";

        if (tooltipTier != null)
            tooltipTier.text = $"Tier {cd.tier}";

        if (tooltipStats != null)
        {
            if (unlocked)
            {
                tooltipStats.text =
                    $"공격력:  {cd.attackDamage}\n" +
                    $"공격속도: {cd.attackSpeed}/s\n" +
                    $"사거리:  {cd.attackRange}\n" +
                    $"판매가:  {cd.sellPrice}";
            }
            else
            {
                tooltipStats.text = "해금 필요";
            }
        }

        if (tooltipPassives != null)
        {
            if (unlocked && cd.passives != null && cd.passives.Count > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder("패시브:\n");
                foreach (var p in cd.passives)
                    if (p.passiveType != PassiveType.None)
                        sb.Append($"• {p.passiveType} ({p.passiveValue}%)\n");
                tooltipPassives.text = sb.ToString().TrimEnd();
            }
            else
            {
                tooltipPassives.text = "";
            }
        }

        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}
