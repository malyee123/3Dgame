using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Image = UnityEngine.UI.Image;

public class AnvilCard : MonoBehaviour
{
    [Header("UI")]
    public Image cardBackground;
    public TextMeshProUGUI descriptionText;
    public Button selectButton;

    private AnvilData currentData;
    private AnvilUI parentUI;

    public void Setup(AnvilData data, AnvilUI ui)
    {
        currentData = data;
        parentUI = ui;

        if (descriptionText != null) descriptionText.text = data.description;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => parentUI.OnSelectAnvil(currentData));
        }
    }
}