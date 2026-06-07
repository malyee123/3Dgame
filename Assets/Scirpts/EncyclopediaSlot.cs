using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaSlot : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image    portraitImage;
    public Image    lockOverlay;
    public Image    selectHighlight;
    public TextMeshProUGUI lockText;
    public Button   slotButton;

    private EncyclopediaCharacterData data;
    private EncyclopediaManager       manager;

    public void Setup(EncyclopediaCharacterData charData, EncyclopediaManager mgr)
    {
        data    = charData;
        manager = mgr;

        bool unlocked = charData.IsUnlocked;

        if (portraitImage != null)
        {
            if (unlocked && charData.portraitSprite != null)
            {
                portraitImage.sprite = charData.portraitSprite;
                portraitImage.color  = Color.white;
            }
            else
            {
                portraitImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            }
        }

        if (lockOverlay != null)
            lockOverlay.gameObject.SetActive(!unlocked);

        if (lockText != null)
            lockText.text = unlocked ? "" : "?";

        if (selectHighlight != null)
            selectHighlight.gameObject.SetActive(false);

        if (slotButton != null)
        {
            slotButton.interactable = unlocked;
            slotButton.onClick.RemoveAllListeners();
            if (unlocked)
                slotButton.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        manager?.OnSlotSelected(this, data);
    }

    public void SetHighlight(bool active)
    {
        if (selectHighlight != null)
            selectHighlight.gameObject.SetActive(active);
    }
}
