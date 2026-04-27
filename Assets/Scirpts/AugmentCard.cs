using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Image = UnityEngine.UI.Image;

public class AugmentCard : MonoBehaviour
{
    [Header("UI")]
    public Image cardBackground;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button selectButton;

    private AugmentData currentData;
    private AugmentUI parentUI;

    private readonly Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    private readonly Color goldColor = new Color(1f, 0.84f, 0f);
    private readonly Color platinumColor = new Color(0.6f, 0.2f, 1f);

    public void Setup(AugmentData data, AugmentUI ui)
    {
        currentData = data;
        parentUI = ui;

        if (nameText != null) nameText.text = data.augmentName;
        if (descriptionText != null) descriptionText.text = data.description;

        if (cardBackground != null)
        {
            switch (data.grade)
            {
                case AugmentGrade.Silver: cardBackground.color = silverColor; break;
                case AugmentGrade.Gold: cardBackground.color = goldColor; break;
                case AugmentGrade.Platinum: cardBackground.color = platinumColor; break;
            }
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => parentUI.OnSelectAugment(currentData));
        }
    }
}
