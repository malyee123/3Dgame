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

        bool hasSprite = data.sprite != null;

        if (cardBackground != null && hasSprite)
            cardBackground.sprite = data.sprite;

        if (nameText != null)
        {
            nameText.gameObject.SetActive(!hasSprite);
            nameText.text = data.augmentName;
        }

        if (summaryText != null)
        {
            summaryText.gameObject.SetActive(!hasSprite);
            summaryText.text = data.summary;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => parentUI.OnSelectAugment(currentData));
        }
    }
}