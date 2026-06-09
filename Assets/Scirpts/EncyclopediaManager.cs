using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaManager : MonoBehaviour
{
    public static EncyclopediaManager Instance { get; private set; }

    [Header("── 데이터 ──")]
    [Tooltip("모든 EC_ 에셋을 여기에 등록")]
    public EncyclopediaCharacterData[] allCharacters;

    [Header("── 좌측 목록 ──")]
    [Tooltip("LeftPanel > Viewport > Content")]
    public Transform listContent;
    [Tooltip("TierSectionPrefab (Project 폴더)")]
    public GameObject tierSectionPrefab;
    [Tooltip("EncyclopediaSlotPrefab (Project 폴더)")]
    public GameObject slotPrefab;

    [Header("── 우측 상세 패널 ──")]
    [Tooltip("DetailPanel 오브젝트 (기본 비활성)")]
    public GameObject detailPanel;
    [Tooltip("DetailPanel > FullBodyImage")]
    public Image detailFullBody;
    [Tooltip("DetailPanel > NameText")]
    public TextMeshProUGUI detailName;

    [Header("── 별점 (Star1~Star5, Size=5) ──")]
    public Image[] detailStars;
    public Sprite starOn;
    public Sprite starOff;

    [Header("── 스탯 텍스트 ──")]
    public TextMeshProUGUI txtAttack;
    public TextMeshProUGUI txtSpeed;
    public TextMeshProUGUI txtRange;
    public TextMeshProUGUI txtDesc;

    [Header("── 스킬 아이콘 영역 ──")]
    [Tooltip("DetailPanel > SkillIconsRow (HLG 부착)")]
    public Transform skillIconsParent;
    [Tooltip("스킬 아이콘 1개 프리팹 (SkillTooltipTrigger 부착)")]
    public GameObject skillIconPrefab;

    // ── 수정: tooltipPanel 제거, tooltipImage 단독 사용 ──
    [Header("── 툴팁 이미지 (Canvas 직속 자식으로 이동 후 연결) ──")]
    [Tooltip("Canvas 직속으로 뺀 TooltipImage 오브젝트의 Image 컴포넌트")]
    public Image tooltipImage;

    [Header("── 뒤로가기 버튼 ──")]
    public Button backButton;

    private EncyclopediaSlot _selectedSlot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[Manager] Awake 완료");
    }

    private void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);

        // ── 수정: tooltipPanel 대신 tooltipImage 오브젝트 직접 비활성 ──
        if (tooltipImage != null) tooltipImage.gameObject.SetActive(false);

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        BuildList();
        CheckRoundUnlocks();
        Debug.Log("[Manager] Start 완료 — BuildList 호출됨");
    }

    public void BuildList()
    {
        Debug.Log("[Manager] BuildList 시작");

        if (listContent == null)
        { Debug.LogError("[Manager] listContent 미연결!"); return; }
        if (allCharacters == null || allCharacters.Length == 0)
        { Debug.LogError("[Manager] allCharacters 배열이 비어있음!"); return; }
        if (tierSectionPrefab == null)
        { Debug.LogError("[Manager] tierSectionPrefab 미연결!"); return; }
        if (slotPrefab == null)
        { Debug.LogError("[Manager] slotPrefab 미연결!"); return; }

        foreach (Transform child in listContent)
            Destroy(child.gameObject);

        var groups = new Dictionary<int, List<EncyclopediaCharacterData>>();
        foreach (var cd in allCharacters)
        {
            if (cd == null) continue;
            if (!groups.ContainsKey(cd.tier))
                groups[cd.tier] = new List<EncyclopediaCharacterData>();
            groups[cd.tier].Add(cd);
        }

        var tiers = new List<int>(groups.Keys);
        tiers.Sort();
        Debug.Log($"[Manager] 티어 수: {tiers.Count} | 캐릭터 수: {allCharacters.Length}");

        foreach (int tier in tiers)
        {
            var section = Instantiate(tierSectionPrefab, listContent);

            Transform header = section.transform.Find("TierHeader");
            var label = header != null
                ? header.GetComponentInChildren<TextMeshProUGUI>()
                : section.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"{tier}티어";

            Transform container = section.transform.Find("SlotContainer");
            if (container == null)
            {
                var grid = section.GetComponentInChildren<GridLayoutGroup>();
                container = grid != null ? grid.transform : section.transform;
            }

            foreach (var cd in groups[tier])
            {
                var slotObj = Instantiate(slotPrefab, container);
                var slot = slotObj.GetComponent<EncyclopediaSlot>();
                if (slot != null)
                    slot.Setup(cd, this);
                else
                    Debug.LogError("[Manager] slotPrefab 에 EncyclopediaSlot 스크립트가 없음!");
            }
        }

        StartCoroutine(ForceRefresh());
    }

    private IEnumerator ForceRefresh()
    {
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (listContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent.GetComponent<RectTransform>());
        Debug.Log("[Manager] ForceRefresh 완료");
    }

    public void OnSlotClicked(EncyclopediaSlot slot, EncyclopediaCharacterData data)
    {
        Debug.Log($"[Manager] OnSlotClicked: {data.characterName}");

        if (_selectedSlot != null) _selectedSlot.SetHighlight(false);
        _selectedSlot = slot;
        slot.SetHighlight(true);

        HideTooltip();
        UpdateDetailPanel(data);
    }

    private void UpdateDetailPanel(EncyclopediaCharacterData data)
    {
        Debug.Log($"[Manager] UpdateDetailPanel: {data.characterName}");

        if (detailPanel == null)
        { Debug.LogError("[Manager] detailPanel 미연결!"); return; }

        detailPanel.SetActive(true);

        if (detailFullBody != null)
        {
            detailFullBody.sprite = data.fullBodySprite;
            detailFullBody.enabled = data.fullBodySprite != null;
        }

        if (detailName != null) detailName.text = data.characterName;

        if (detailStars != null)
        {
            for (int i = 0; i < detailStars.Length; i++)
            {
                if (detailStars[i] == null) continue;
                bool on = i < data.starRating;
                detailStars[i].sprite = on ? starOn : starOff;
                detailStars[i].color = on
                    ? new Color(1f, 0.85f, 0f, 1f)
                    : new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }

        if (txtAttack != null) txtAttack.text = $"공격력 : {data.attackPower}";
        if (txtSpeed != null) txtSpeed.text = $"공격 속도 : {data.attackSpeed}";
        if (txtRange != null) txtRange.text = $"사거리 : {data.range}";
        if (txtDesc != null) txtDesc.text = data.description;

        RefreshSkillIcons(data);
    }

    private void RefreshSkillIcons(EncyclopediaCharacterData data)
    {
        if (skillIconsParent == null) return;

        foreach (Transform child in skillIconsParent)
            Destroy(child.gameObject);

        if (data.skillIcons == null || data.skillIcons.Length == 0)
        {
            Debug.Log($"[Manager] {data.characterName} 스킬 없음");
            return;
        }

        for (int i = 0; i < data.skillIcons.Length; i++)
        {
            if (skillIconPrefab == null) break;
            if (data.skillIcons[i] == null) continue;

            var iconObj = Instantiate(skillIconPrefab, skillIconsParent);

            var img = iconObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = data.skillIcons[i];
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = true;
                img.enabled = true;
            }

            var trigger = iconObj.GetComponent<SkillTooltipTrigger>();
            if (trigger != null)
            {
                Sprite tip = (data.skillTooltipSprites != null && i < data.skillTooltipSprites.Length)
                    ? data.skillTooltipSprites[i]
                    : null;
                trigger.Initialize(tip, this);
            }
            else
            {
                Debug.LogWarning("[Manager] skillIconPrefab 에 SkillTooltipTrigger 미부착!");
            }
        }

        Debug.Log($"[Manager] 스킬 아이콘 {data.skillIcons.Length}개 생성 완료");
    }

    // ── 수정: tooltipPanel.SetActive 제거 → tooltipImage 오브젝트만 직접 제어 ──
    public void ShowTooltip(Sprite sprite)
    {
        Debug.Log($"[Manager] ShowTooltip: {sprite?.name}");

        if (tooltipImage == null)
        { Debug.LogError("[Manager] tooltipImage 미연결!"); return; }
        if (sprite == null)
        { Debug.LogWarning("[Manager] ShowTooltip: sprite가 null"); return; }

        tooltipImage.sprite = sprite;
        tooltipImage.enabled = true;
        tooltipImage.gameObject.SetActive(true);   // panel 없이 image만 직접 켜기
    }

    // ── 수정: tooltipPanel.SetActive 제거 → tooltipImage 오브젝트만 직접 제어 ──
    public void HideTooltip()
    {
        if (tooltipImage != null && tooltipImage.gameObject.activeSelf)
        {
            tooltipImage.gameObject.SetActive(false);   // panel 없이 image만 직접 끄기
            Debug.Log("[Manager] HideTooltip 호출");
        }
    }

    private void OnBackClicked()
    {
        Debug.Log("[Manager] 뒤로가기 클릭");
        SceneLoader.GoTo("LobbyScene");
    }

    public void CheckRoundUnlocks()
    {
        int round = PlayerPrefs.GetInt("LastRound", 0);
        bool any = false;
        foreach (var cd in allCharacters)
            if (EncyclopediaCharacterData.TryUnlockByRound(cd, round))
                any = true;
        if (any) BuildList();
    }

    public static void UnlockByName(string name, int tier)
    {
        if (Instance == null) return;
        foreach (var cd in Instance.allCharacters)
        {
            if (cd == null || cd.characterName != name || cd.tier != tier) continue;
            EncyclopediaCharacterData.Unlock(cd);
            Instance.BuildList();
            return;
        }
    }
}