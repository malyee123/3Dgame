using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Image = UnityEngine.UI.Image;

public class AugmentCard : MonoBehaviour
{
    [Header("UI")]
    public Image cardBackground;
    public Button selectButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI summaryText;

    private AugmentData currentData;
    private AugmentUI parentUI;

    public void Setup(AugmentData data, AugmentUI ui)
    {
        currentData = data;
        parentUI = ui;

        if (cardBackground != null && data != null && data.sprite != null)
            cardBackground.sprite = data.sprite;

        if (nameText != null)
        {
            nameText.text = data != null ? data.augmentName : string.Empty;
            nameText.raycastTarget = false;
        }

        if (summaryText != null)
        {
            summaryText.text = data != null ? data.summary : string.Empty;
            summaryText.raycastTarget = false;
        }

        if (cardBackground != null)
            cardBackground.raycastTarget = false;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
            selectButton.interactable = true;

            if (selectButton.targetGraphic == null && selectButton.image != null)
                selectButton.targetGraphic = selectButton.image;
        }
    }

    private void HandleClick()
    {
        if (parentUI == null || currentData == null)
            return;

        parentUI.OnSelectAugment(currentData);
    }
}