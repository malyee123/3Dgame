using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CompendiumCard : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image  portrait;
    public Image  tierBadge;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI tierText;
    public GameObject lockOverlay;

    [Header("Tier Badge Colors")]
    public Color[] tierColors = new Color[]
    {
        new Color(0.60f, 0.71f, 0.78f),
        new Color(0.24f, 0.71f, 0.31f),
        new Color(0.35f, 0.63f, 1.00f),
        new Color(0.74f, 0.55f, 1.00f),
        new Color(0.91f, 0.76f, 0.26f),
    };

    private CharacterData characterData;
    private bool          isUnlocked;
    private CompendiumManager manager;

    public void Setup(CharacterData cd, bool unlocked, CompendiumManager mgr)
    {
        characterData = cd;
        isUnlocked    = unlocked;
        manager       = mgr;

        if (portrait != null)
        {
            portrait.sprite  = unlocked ? cd.characterSprite : null;
            portrait.color   = unlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            portrait.enabled = cd.characterSprite != null || !unlocked;
        }

        if (nameText != null)
            nameText.text = unlocked ? cd.characterName : "???";

        if (tierText != null)
            tierText.text = $"T{cd.tier}";

        if (tierBadge != null)
        {
            int idx = Mathf.Clamp(cd.tier - 1, 0, tierColors.Length - 1);
            tierBadge.color = unlocked ? tierColors[idx] : Color.gray;
        }

        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null && characterData != null)
            manager.ShowTooltip(characterData, isUnlocked);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager != null)
            manager.HideTooltip();
    }
}
