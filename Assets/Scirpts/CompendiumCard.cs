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
            bool hasSprite = cd.characterSprite != null;
            portrait.sprite = (unlocked && hasSprite) ? cd.characterSprite : null;
            if (!unlocked)
                portrait.color = new Color(0.15f, 0.15f, 0.15f);
            else if (!hasSprite)
            {
                // 이미지 없을 때 티어 색상으로 플레이스홀더 표시
                int idx = Mathf.Clamp(cd.tier - 1, 0, tierColors.Length - 1);
                portrait.color = new Color(tierColors[idx].r, tierColors[idx].g, tierColors[idx].b, 0.4f);
            }
            else
                portrait.color = Color.white;
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
