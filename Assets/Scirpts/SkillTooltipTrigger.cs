using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 각 스킬 아이콘 오브젝트에 부착.
/// 마우스 Hover(Enter/Exit) 및 Click 으로 스킬 설명 이미지 팝업을 제어한다.
///
/// 요구 컴포넌트: Image (아이콘 표시용)
/// </summary>
public class SkillTooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,   // 마우스가 올라왔을 때
    IPointerExitHandler,    // 마우스가 벗어났을 때
    IPointerClickHandler    // 클릭했을 때 (고정 토글용)
{
    // ───────── 런타임 주입 데이터 ─────────
    // Initialize() 메서드를 통해 EncyclopediaManager가 주입
    private UnityEngine.Sprite tooltipSprite; // 표시할 툴팁 이미지
    private EncyclopediaManager manager;

    // 클릭으로 팝업이 '고정' 된 상태인지 여부
    private bool isPinnedByClick = false;

    // ══════════════════════════════════════════════════════
    // Initialize — EncyclopediaManager.RefreshSkillIcons() 에서 호출
    // ══════════════════════════════════════════════════════
    /// <summary>
    /// 이 트리거가 사용할 툴팁 스프라이트와 매니저를 주입한다.
    /// Instantiate 직후 반드시 호출해야 한다.
    /// </summary>
    public void Initialize(UnityEngine.Sprite tooltip, EncyclopediaManager mgr)
    {
        tooltipSprite  = tooltip;
        manager        = mgr;
        isPinnedByClick = false;
    }

    // ══════════════════════════════════════════════════════
    // IPointerEnterHandler — 마우스 올라옴 → 팝업 표시
    // ══════════════════════════════════════════════════════
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 클릭으로 다른 팝업이 고정된 상태가 아닐 때만 Hover 팝업 표시
        if (!isPinnedByClick)
            ShowTooltip();
    }

    // ══════════════════════════════════════════════════════
    // IPointerExitHandler — 마우스 벗어남 → 팝업 숨김
    // ══════════════════════════════════════════════════════
    public void OnPointerExit(PointerEventData eventData)
    {
        // 클릭 고정 상태라면 Exit 이벤트 무시
        if (isPinnedByClick) return;
        HideTooltip();
    }

    // ══════════════════════════════════════════════════════
    // IPointerClickHandler — 클릭 → 팝업 고정/해제 토글
    // ══════════════════════════════════════════════════════
    public void OnPointerClick(PointerEventData eventData)
    {
        isPinnedByClick = !isPinnedByClick;

        if (isPinnedByClick)
            ShowTooltip();
        else
            HideTooltip();
    }

    // ══════════════════════════════════════════════════════
    // 내부 팝업 표시 / 숨김 메서드
    // ══════════════════════════════════════════════════════
    private void ShowTooltip()
    {
        if (manager == null || tooltipSprite == null) return;
        manager.ShowImageTooltip(tooltipSprite);
    }

    private void HideTooltip()
    {
        if (manager == null) return;
        manager.HideImageTooltip();
    }

    // ══════════════════════════════════════════════════════
    // 오브젝트 소멸 시 열린 팝업 자동 닫기
    // ══════════════════════════════════════════════════════
    private void OnDestroy()
    {
        if (isPinnedByClick)
            HideTooltip();
    }
}
