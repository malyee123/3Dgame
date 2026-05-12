using UnityEngine;
using UnityEngine.UI;

public class AnvilCard : MonoBehaviour
{
    [Header("UI")]
    public Image cardImage;
    public Button selectButton;

    private AnvilData currentData;
    private AnvilUI parentUI;

    public void Setup(AnvilData data, AnvilUI ui)
    {
        currentData = data;
        parentUI = ui;

        if (cardImage != null) cardImage.sprite = data.sprite;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => parentUI.OnSelectAnvil(currentData));
        }
    }
}