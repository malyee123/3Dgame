using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감 좌측 그리드의 캐릭터 카드 1장을 제어하는 컴포넌트.
/// EncyclopediaSlotPrefab의 루트 오브젝트에 부착.
/// </summary>
public class EncyclopediaSlot : MonoBehaviour
{
    // ───────── Inspector 연결 필드 ─────────
    [Header("UI 참조")]
    [Tooltip("캐릭터 초상화 Image")]
    public Image  portraitImage;
    [Tooltip("미해금 시 덮는 검은 오버레이 Image")]
    public Image  lockOverlay;
    [Tooltip("미해금 시 표시할 '?' 텍스트")]
    public TextMeshProUGUI lockQuestionText;
    [Tooltip("선택 시 표시할 노란 테두리 하이라이트 Image")]
    public Image  selectHighlight;
    [Tooltip("클릭 이벤트를 받을 Button 컴포넌트")]
    public Button slotButton;

    // ───────── 내부 상태 ─────────
    private EncyclopediaCharacterData data;
    private EncyclopediaManager       manager;

    // ══════════════════════════════════════════════════════
    // Setup — EncyclopediaManager.BuildList() 에서 호출
    // ══════════════════════════════════════════════════════
    /// <summary>슬롯에 캐릭터 데이터를 주입하고 UI를 초기화한다.</summary>
    public void Setup(EncyclopediaCharacterData charData, EncyclopediaManager mgr)
    {
        data    = charData;
        manager = mgr;

        bool unlocked = charData.IsUnlocked;

        if (portraitImage != null)
        {
            portraitImage.sprite = charData.portraitSprite;
            portraitImage.enabled = true; // 강제 활성화
            Debug.Log($"{data.characterName} 초상화 할당됨: {portraitImage.sprite != null}");
        }
        else
        {
            Debug.LogError("PortraitImage 컴포넌트가 인스펙터에 연결되지 않았습니다!");
        }

        // ── 초상화 처리 ──────────────────────────────────
        if (portraitImage != null)
        {
            if (unlocked && charData.portraitSprite != null)
            {
                portraitImage.sprite = charData.portraitSprite;
                portraitImage.color  = Color.white;         // 완전 불투명
            }
            else
            {
                // 미해금: 초상화를 검게 실루엣으로 표시
                portraitImage.sprite = charData.portraitSprite;
                portraitImage.color  = new Color(0.08f, 0.08f, 0.08f, 1f);
            }
        }

        // ── 잠금 오버레이 ────────────────────────────────
        if (lockOverlay != null)
            lockOverlay.gameObject.SetActive(!unlocked);

        // ── '?' 텍스트 ───────────────────────────────────
        if (lockQuestionText != null)
            lockQuestionText.gameObject.SetActive(!unlocked);

        // ── 선택 하이라이트 초기화(비활성) ──────────────
        if (selectHighlight != null)
            selectHighlight.gameObject.SetActive(false);

        // ── 버튼 이벤트 연결 ─────────────────────────────
        if (slotButton != null)
        {
            slotButton.interactable = unlocked; // 미해금 슬롯은 클릭 불가
            slotButton.onClick.RemoveAllListeners();
            if (unlocked)
                slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    // ══════════════════════════════════════════════════════
    // 클릭 이벤트 → 매니저에 전달
    // ══════════════════════════════════════════════════════
    private void OnSlotClicked()
    {
        if (manager != null)
            manager.OnSlotSelected(this, data);
    }

    // ══════════════════════════════════════════════════════
    // 하이라이트 ON/OFF
    // ══════════════════════════════════════════════════════
    /// <summary>선택 하이라이트를 켜거나 끈다.</summary>
    public void SetHighlight(bool active)
    {
        if (selectHighlight != null)
            selectHighlight.gameObject.SetActive(active);
    }

    // ══════════════════════════════════════════════════════
    // 외부에서 해금 상태를 즉시 반영 (실시간 해금 이벤트용)
    // ══════════════════════════════════════════════════════
    /// <summary>런타임 중 해금이 발생했을 때 슬롯 외형을 갱신한다.</summary>
    public void RefreshUnlockState()
    {
        if (data == null) return;
        Setup(data, manager); // 동일 데이터로 재세팅하면 해금 상태 자동 반영
    }
}
