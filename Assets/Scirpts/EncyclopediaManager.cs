using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 도감 씬 전체를 총괄하는 매니저.
/// - 캐릭터 데이터를 티어별로 분류하여 좌측 그리드에 동적 생성
/// - 슬롯 클릭 시 우측 상세 패널 갱신
/// - 스킬 아이콘 이미지 툴팁 팝업 제어
/// - 버그 방어: 코루틴 기반 강제 레이아웃 재계산
/// </summary>
public class EncyclopediaManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 싱글턴
    // ══════════════════════════════════════════════════════
    public static EncyclopediaManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════
    // Inspector 연결 필드 — 좌측 목록
    // ══════════════════════════════════════════════════════
    [Header("▼ 좌측 목록 패널")]
    [Tooltip("LeftPanel → Viewport → Content 오브젝트를 여기에 연결")]
    public Transform  listContent;
    [Tooltip("TierSectionPrefab (Project 폴더에서 드래그)")]
    public GameObject tierSectionPrefab;
    [Tooltip("EncyclopediaSlotPrefab (Project 폴더에서 드래그)")]
    public GameObject slotPrefab;

    // ══════════════════════════════════════════════════════
    // Inspector 연결 필드 — 캐릭터 데이터
    // ══════════════════════════════════════════════════════
    [Header("▼ 캐릭터 데이터 배열")]
    [Tooltip("모든 EncyclopediaCharacterData 에셋을 여기에 등록")]
    public EncyclopediaCharacterData[] allCharacters;

    // ══════════════════════════════════════════════════════
    // Inspector 연결 필드 — 우측 상세 패널
    // ══════════════════════════════════════════════════════
    [Header("▼ 우측 상세 패널")]
    [Tooltip("DetailPanel 오브젝트 (기본 비활성화 상태)")]
    public GameObject detailPanel;
    [Tooltip("DetailPanel → DetailFullBody Image")]
    public Image      detailFullBody;
    [Tooltip("DetailPanel → DetailName TMP")]
    public TextMeshProUGUI detailName;

    [Header("▼ 별점 (Star1~Star5)")]
    [Tooltip("Star1~Star5 Image 배열, Size=5 로 설정 후 각각 연결")]
    public Image[] detailStars;
    [Tooltip("채워진 노란 별 Sprite")]
    public Sprite  starOnSprite;
    [Tooltip("빈 회색 별 Sprite")]
    public Sprite  starOffSprite;

    [Header("▼ 스탯 텍스트")]
    public TextMeshProUGUI detailAttackText;
    public TextMeshProUGUI detailSpeedText;
    public TextMeshProUGUI detailRangeText;
    public TextMeshProUGUI detailDescription;

    [Header("▼ 스킬 아이콘 영역")]
    [Tooltip("SkillIconsRow 오브젝트 (Horizontal Layout Group 부착)")]
    public Transform  skillIconsContainer;
    [Tooltip("스킬 아이콘 1개짜리 프리팹 (SkillTooltipTrigger 부착)")]
    public GameObject skillIconPrefab;

    // ══════════════════════════════════════════════════════
    // Inspector 연결 필드 — 이미지 툴팁 팝업
    // ══════════════════════════════════════════════════════
    [Header("▼ 이미지 툴팁 팝업 (Canvas 최상위에 배치)")]
    [Tooltip("ImageTooltipPopup 오브젝트 (Canvas 직속 자식, 기본 비활성)")]
    public GameObject tooltipPopupPanel;
    [Tooltip("ImageTooltipPopup → TooltipImage Image 컴포넌트")]
    public Image      tooltipImage;

    [Header("▼ 뒤로가기 버튼")]
    public UnityEngine.UI.Button backButton;

    // ══════════════════════════════════════════════════════
    // 내부 상태
    // ══════════════════════════════════════════════════════
    private EncyclopediaSlot currentSelectedSlot;

    // ══════════════════════════════════════════════════════
    // Unity 생명주기
    // ══════════════════════════════════════════════════════
    private void Awake()
    {
        // 싱글턴 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 상세 패널과 툴팁 팝업은 처음에 비활성
        if (detailPanel       != null) detailPanel.SetActive(false);
        if (tooltipPopupPanel != null) tooltipPopupPanel.SetActive(false);

        // 뒤로가기 버튼 이벤트 연결
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneLoader.GoTo("LobbyScene"));
        }

        // 목록 빌드 및 해금 체크
        BuildList();
        CheckRoundUnlocks();
    }

    // ══════════════════════════════════════════════════════
    // BuildList — 티어별 그룹핑 → SlotContainer 하위에 동적 생성
    //
    // ⚠ 버그 3 방어: 코루틴으로 강제 레이아웃 재계산
    // ══════════════════════════════════════════════════════
    public void BuildList()
    {
        if (listContent == null)
        {
            Debug.LogError("[EncyclopediaManager] listContent가 연결되지 않았습니다!");
            return;
        }
        if (allCharacters == null || allCharacters.Length == 0)
        {
            Debug.LogWarning("[EncyclopediaManager] allCharacters 배열이 비어있습니다.");
            return;
        }

        // ── 기존 목록 전부 제거 ──────────────────────────
        foreach (Transform child in listContent)
            Destroy(child.gameObject);

        // ── 티어별 Dictionary 그룹핑 ────────────────────
        // Key: 티어 번호(int)  Value: 해당 티어 캐릭터 리스트
        var tierGroups = new Dictionary<int, List<EncyclopediaCharacterData>>();

        foreach (var cd in allCharacters)
        {
            if (cd == null) continue;
            if (!tierGroups.ContainsKey(cd.tier))
                tierGroups[cd.tier] = new List<EncyclopediaCharacterData>();
            tierGroups[cd.tier].Add(cd);
        }

        // ── 티어 오름차순 정렬 ───────────────────────────
        var sortedTiers = new List<int>(tierGroups.Keys);
        sortedTiers.Sort();

        // ── 티어 섹션 및 슬롯 생성 ──────────────────────
        foreach (int tier in sortedTiers)
        {
            if (tierSectionPrefab == null) break;

            // TierSectionPrefab 생성 → listContent 직속 자식
            GameObject section = Instantiate(tierSectionPrefab, listContent);

            // TierLabel 텍스트 설정
            Transform header = section.transform.Find("TierHeader");
            var label = header != null
                ? header.GetComponentInChildren<TextMeshProUGUI>()
                : section.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"{tier}티어";

            // SlotContainer (GridLayoutGroup 보유 오브젝트) 탐색
            // ⭐ 핵심: listContent가 아닌 SlotContainer 하위에 생성해야
            //         GridLayoutGroup 이 올바르게 배치
            Transform slotContainer = section.transform.Find("SlotContainer");
            if (slotContainer == null)
            {
                // 이름으로 찾지 못하면 GridLayoutGroup으로 자동 탐색 (폴백)
                var grid = section.GetComponentInChildren<GridLayoutGroup>();
                slotContainer = (grid != null) ? grid.transform : section.transform;
            }

            // 해당 티어 캐릭터마다 슬롯 생성
            foreach (var cd in tierGroups[tier])
            {
                if (slotPrefab == null) break;
                var slotObj = Instantiate(slotPrefab, slotContainer);
                var slot    = slotObj.GetComponent<EncyclopediaSlot>();
                if (slot != null)
                    slot.Setup(cd, this);
            }
        }

        // ⚠ 버그 3 방어: 동적 생성 직후 강제 레이아웃 재계산
        StartCoroutine(ForceLayoutRefresh());

    }

    // ══════════════════════════════════════════════════════
    // 코루틴 기반 강제 레이아웃 재계산 (버그 3 방어)
    // ══════════════════════════════════════════════════════
    private IEnumerator ForceLayoutRefresh()
    {
        // 1프레임 대기: Instantiate 완전 완료 보장
        yield return null;
        // 추가 1프레임 대기: LayoutGroup 내부 계산 완료 보장
        yield return null;

        // Canvas 전체 강제 업데이트
        Canvas.ForceUpdateCanvases();

        // listContent의 RectTransform 강제 재계산
        if (listContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                listContent.GetComponent<RectTransform>());
        }

        Debug.Log("[EncyclopediaManager] 레이아웃 강제 재계산 완료");
    }

    // ══════════════════════════════════════════════════════
    // 슬롯 클릭 콜백 — EncyclopediaSlot에서 호출
    // ══════════════════════════════════════════════════════
    public void OnSlotSelected(EncyclopediaSlot slot, EncyclopediaCharacterData data)
    {
        // 이전 선택 하이라이트 해제
        if (currentSelectedSlot != null)
            currentSelectedSlot.SetHighlight(false);

        currentSelectedSlot = slot;
        slot.SetHighlight(true);

        // 툴팁 팝업 닫기 (다른 캐릭터 선택 시 자동 닫힘)
        HideImageTooltip();

        // 우측 패널 갱신
        ShowDetailPanel(data);
    }

    // ══════════════════════════════════════════════════════
    // 우측 상세 패널 갱신
    // ══════════════════════════════════════════════════════
    private void ShowDetailPanel(EncyclopediaCharacterData data)
    {
        if (detailPanel == null) return;
        detailPanel.SetActive(true);

        // ── 풀바디 이미지 ────────────────────────────────
        if (detailFullBody != null)
        {
            detailFullBody.sprite  = data.fullBodySprite;
            detailFullBody.enabled = data.fullBodySprite != null;
        }

        // ── 캐릭터 이름 ──────────────────────────────────
        if (detailName != null) detailName.text = data.characterName;

        // ── 별점 (starRating 수만큼 노란별) ─────────────
        if (detailStars != null)
        {
            for (int i = 0; i < detailStars.Length; i++)
            {
                if (detailStars[i] == null) continue;
                bool on = (i < data.starRating);
                if (on  && starOnSprite  != null) detailStars[i].sprite = starOnSprite;
                if (!on && starOffSprite != null) detailStars[i].sprite = starOffSprite;
                detailStars[i].color = on
                    ? new Color(1.0f, 0.85f, 0.0f, 1f)  // 노란색
                    : new Color(0.3f, 0.3f, 0.3f, 1f);  // 회색
            }
        }

        // ── 스탯 텍스트 ──────────────────────────────────
        if (detailAttackText != null)
            detailAttackText.text = $"공격력 : {data.attackPower}";
        if (detailSpeedText != null)
            detailSpeedText.text  = $"공격 속도 : {data.attackSpeed}";
        if (detailRangeText != null)
            detailRangeText.text  = $"사거리 : {data.range}";

        // ── 설명 텍스트 ──────────────────────────────────
        if (detailDescription != null)
            detailDescription.text = data.description;

        // ── 스킬 아이콘 동적 생성 ─────────────────────────
        RefreshSkillIcons(data);
    }

    // ══════════════════════════════════════════════════════
    // 스킬 아이콘 갱신
    // ══════════════════════════════════════════════════════
    private void RefreshSkillIcons(EncyclopediaCharacterData data)
    {
        if (skillIconsContainer == null) return;

        // 기존 아이콘 전부 삭제
        foreach (Transform child in skillIconsContainer)
            Destroy(child.gameObject);

        if (data.skillIcons == null || data.skillIcons.Length == 0) return;

        for (int i = 0; i < data.skillIcons.Length; i++)
        {
            if (skillIconPrefab == null) break;
            if (data.skillIcons[i] == null) continue;

            // 스킬 아이콘 프리팹 생성
            var iconObj = Instantiate(skillIconPrefab, skillIconsContainer);

            // 아이콘 이미지 설정
            var img = iconObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite         = data.skillIcons[i];
                img.preserveAspect = true;
                img.color          = Color.white;
            }

            // SkillTooltipTrigger 초기화 (툴팁 스프라이트 주입)
            var trigger = iconObj.GetComponent<SkillTooltipTrigger>();
            if (trigger != null)
            {
                // 1:1 매칭: skillTooltipSprites[i]가 있으면 사용, 없으면 null
                Sprite tooltip = (data.skillTooltipSprites != null && i < data.skillTooltipSprites.Length)
                    ? data.skillTooltipSprites[i]
                    : null;
                trigger.Initialize(tooltip, this);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 이미지 툴팁 팝업 제어 — SkillTooltipTrigger 에서 호출
    // ══════════════════════════════════════════════════════
    /// <summary>툴팁 이미지 팝업을 표시한다.</summary>
    public void ShowImageTooltip(Sprite tooltipSprite)
    {
        if (tooltipPopupPanel == null || tooltipImage == null) return;
        if (tooltipSprite == null) return;

        tooltipImage.sprite  = tooltipSprite;
        tooltipImage.enabled = true;
        tooltipPopupPanel.SetActive(true);
    }

    /// <summary>툴팁 이미지 팝업을 숨긴다.</summary>
    public void HideImageTooltip()
    {
        if (tooltipPopupPanel != null)
            tooltipPopupPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════
    // 해금 관련
    // ══════════════════════════════════════════════════════
    /// <summary>현재 게임 라운드 기준으로 자동 해금 체크.</summary>
    public void CheckRoundUnlocks()
    {
        int  currentRound = PlayerPrefs.GetInt("LastRound", 0);
        bool anyUnlocked  = false;

        foreach (var cd in allCharacters)
        {
            if (EncyclopediaCharacterData.CheckAndUnlockByRound(cd, currentRound))
                anyUnlocked = true;
        }

        // 새로 해금된 캐릭터가 있으면 목록 재빌드
        if (anyUnlocked) BuildList();
    }

    /// <summary>
    /// 외부(GameScene 등)에서 캐릭터 이름+티어로 즉시 해금 처리.
    /// 예) EncyclopediaManager.UnlockByName("기사", 5);
    /// </summary>
    public static void UnlockByName(string characterName, int tier)
    {
        if (Instance == null) return;
        foreach (var cd in Instance.allCharacters)
        {
            if (cd == null) continue;
            if (cd.characterName == characterName && cd.tier == tier)
            {
                EncyclopediaCharacterData.Unlock(cd);
                Instance.BuildList(); // 목록 즉시 갱신
                return;
            }
        }
    }
}
