using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaSlot : MonoBehaviour
{
    [Header("── UI 연결 (Inspector에서 반드시 연결) ──")]
    public Image portraitImage;
    public Image lockOverlay;
    public TextMeshProUGUI lockMark;
    public Image highlight;
    public Button slotButton;

    private EncyclopediaCharacterData _data;
    private EncyclopediaManager _manager;

    public void Setup(EncyclopediaCharacterData data, EncyclopediaManager manager)
    {
        _data = data;
        _manager = manager;

        Debug.Log($"[Slot] Setup 호출: {data.characterName} | 해금={data.IsUnlocked}");

        bool unlocked = data.IsUnlocked;

        if (portraitImage != null)
        {
            portraitImage.enabled = true;
            portraitImage.raycastTarget = false;
            portraitImage.sprite = data.portraitSprite;
            portraitImage.color = unlocked
                                            ? Color.white
                                            : new Color(0.1f, 0.1f, 0.1f, 1f);
        }
        else
        {
            Debug.LogError($"[Slot] portraitImage 미연결 — 오브젝트: {gameObject.name}");
        }

        if (lockOverlay != null) lockOverlay.gameObject.SetActive(!unlocked);
        if (lockMark != null) lockMark.gameObject.SetActive(!unlocked);

        SetHighlight(false);

        if (slotButton != null)
        {
            slotButton.interactable = unlocked;
            slotButton.onClick.RemoveAllListeners();
            if (unlocked)
                slotButton.onClick.AddListener(OnClicked);
        }
        else
        {
            Debug.LogError($"[Slot] slotButton 미연결 — 오브젝트: {gameObject.name}");
        }

        var rt = GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void OnClicked()
    {
        Debug.Log($"[Slot] 클릭: {_data?.characterName}");
        _manager?.OnSlotClicked(this, _data);
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null)
            highlight.gameObject.SetActive(on);
    }

    public void Refresh() { if (_data != null) Setup(_data, _manager); }
}