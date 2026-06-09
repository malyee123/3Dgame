using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ─────────────────────────────────────────────────────────
//  SkillIconPrefab 루트에 부착
//  RequireComponent(Image) 로 Raycast Target 보장
// ─────────────────────────────────────────────────────────
[RequireComponent(typeof(Image))]
public class SkillTooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // Initialize() 로 주입
    private Sprite              _tooltipSprite;
    private EncyclopediaManager _manager;
    private Image               _img;

    // ══════════════════════════════════════════════════════
    //  Awake — RaycastTarget 선행 보장
    // ══════════════════════════════════════════════════════
    private void Awake()
    {
        _img = GetComponent<Image>();
        if (_img != null)
        {
            _img.raycastTarget = true; // 이벤트 수신 절대 보장
            _img.enabled       = true;
        }
        Debug.Log($"[Tooltip] Awake: {gameObject.name} | raycastTarget=true 설정");
    }

    // ══════════════════════════════════════════════════════
    //  Initialize — EncyclopediaManager 가 Instantiate 직후 호출
    // ══════════════════════════════════════════════════════
    public void Initialize(Sprite tooltipSprite, EncyclopediaManager manager)
    {
        _tooltipSprite = tooltipSprite;
        _manager       = manager;

        // Prefab 설정과 무관하게 강제 재보장
        if (_img == null) _img = GetComponent<Image>();
        if (_img != null)
        {
            _img.raycastTarget = true;
            _img.enabled       = true;
        }

        Debug.Log($"[Tooltip] Initialize: {gameObject.name} | 툴팁={tooltipSprite?.name}");
    }

    // ══════════════════════════════════════════════════════
    //  마우스 진입 → 툴팁 표시
    // ══════════════════════════════════════════════════════
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[Tooltip] OnPointerEnter: {gameObject.name}");

        if (_manager == null)
        {
            Debug.LogError("[Tooltip] _manager 가 null — Initialize 미호출 또는 매니저 연결 누락");
            return;
        }
        if (_tooltipSprite == null)
        {
            Debug.LogWarning("[Tooltip] tooltipSprite 가 null — skillTooltipSprites 배열 확인");
            return;
        }

        _manager.ShowTooltip(_tooltipSprite);
    }

    // ══════════════════════════════════════════════════════
    //  마우스 이탈 → 툴팁 숨김
    // ══════════════════════════════════════════════════════
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[Tooltip] OnPointerExit: {gameObject.name}");
        _manager?.HideTooltip();
    }

    private void OnDestroy() => _manager?.HideTooltip();
}
