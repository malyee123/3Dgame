using UnityEngine;
using UnityEngine.UI;

public class AugmentCard : MonoBehaviour
{
    [Header("UI")]
    public Image cardImage;
    public Button selectButton;

    private AugmentData currentData;
    private AugmentUI parentUI;

    public void Setup(AugmentData data, AugmentUI ui)
    {
        currentData = data;
        parentUI = ui;

        if (cardImage != null && data.sprite != null)
            cardImage.sprite = data.sprite;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => parentUI.OnSelectAugment(currentData));
        }
    }
}