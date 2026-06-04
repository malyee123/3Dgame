using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatIconUI : MonoBehaviour
{
    [System.Serializable]
    public class StatSlot
    {
        public string statKey;
        public Image  iconImage;
        public Sprite iconSprite;
        public TextMeshProUGUI valueText;
    }

    [Header("Stat Slots")]
    public StatSlot[] statSlots;

    void Start()
    {
        foreach (StatSlot slot in statSlots)
            if (slot.iconImage != null && slot.iconSprite != null)
                slot.iconImage.sprite = slot.iconSprite;
    }

    public void SetValue(string key, string value)
    {
        foreach (StatSlot slot in statSlots)
        {
            if (slot.statKey != key) continue;
            if (slot.valueText != null) slot.valueText.text = value;
            break;
        }
    }

    public void SetValue(string key, float value, string format = "0.##")
        => SetValue(key, value.ToString(format));

    public void SetValue(string key, int value)
        => SetValue(key, value.ToString());
}
