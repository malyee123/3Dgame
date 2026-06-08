using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaManager : MonoBehaviour
{
    [Header("Data")]
    public EncyclopediaCharacterData[] allCharacters; // 도감 데이터 에셋들

    [Header("List UI")]
    public Transform contentParent; // LeftPanel -> Viewport -> Content
    public GameObject tierSectionPrefab; // 1티어, 2티어 등 섹션 프리팹
    public GameObject slotPrefab; // 개별 캐릭터 카드(초상화) 프리팹

    [Header("Detail Panel UI")]
    public GameObject detailPanel; // 선택 전 숨겨둘 전체 우측 패널
    public TextMeshProUGUI detailNameText; // 상단 캐릭터 이름 텍스트
    public Image detailPortraitImage; // 좌측 큰 전신 일러스트 이미지
    public TextMeshProUGUI detailAttackText; // 스탯 - 공격력
    public TextMeshProUGUI detailSpeedText; // 스탯 - 공격속도
    public TextMeshProUGUI detailRangeText; // 스탯 - 사거리

    [Header("Stars UI")]
    public Image[] detailStars; // 별 5개가 들어갈 이미지 슬롯 배열
    public Sprite starOnSprite; // 활성화된 노란 별 스프라이트
    public Sprite starOffSprite; // 비활성화된 회색 별 스프라이트

    [Header("Skill UI")]
    public Transform detailSkillContainer; // 스킬 아이콘들이 생성될 부모 (SkillIcons)
    public GameObject skillIconPrefab; // 단일 스킬 아이콘 프리팹
    public GameObject detailSkillDescPanel; // 스킬 툴팁(말풍선) 패널 자체 —— [★가이드북 기반 명칭 수정]
    public TextMeshProUGUI detailSkillDescText; // 툴팁 내 스킬 설명 텍스트 —— [★가이드북 기반 명칭 수정]

    // 생성된 스킬 아이콘들을 추적하고 지우기 위한 리스트
    private List<GameObject> activeSkillIcons = new List<GameObject>();

    void Start()
    {
        // 게임 시작 시 우측 상세 정보 패널은 꺼둡니다. (카드를 눌러야 켜짐)
        if (detailPanel != null) detailPanel.SetActive(false);
        BuildList();
    }

    /// <summary>
    /// 좌측 도감 스크롤 목록을 동적으로 생성하는 함수
    /// </summary>
    private void BuildList()
    {
        if (contentParent == null || allCharacters == null) return;

        // 1. 기존 생성된 리스트가 있다면 모두 청소 (중복 생성 방지)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 캐릭터 데이터를 티어(Tier) 번호별로 묶어주기 위한 Dictionary 활용
        Dictionary<int, List<EncyclopediaCharacterData>> tierGroups = new Dictionary<int, List<EncyclopediaCharacterData>>();
        foreach (var cd in allCharacters)
        {
            if (cd == null) continue;
            if (!tierGroups.ContainsKey(cd.tier))
            {
                tierGroups[cd.tier] = new List<EncyclopediaCharacterData>();
            }
            tierGroups[cd.tier].Add(cd);
        }

        // 3. 티어 숫자를 기준으로 오름차순 정렬 (1티어 -> 2티어 -> 3티어 순서)
        List<int> tiers = new List<int>(tierGroups.Keys);
        tiers.Sort();

        // 4. 정렬된 티어 순서대로 화면에 프리팹 생성 시작
        foreach (int tier in tiers)
        {
            // 기본적으로 슬롯이 생성될 부모 위치는 contentParent로 설정해둡니다.
            Transform targetSlotParent = contentParent;

            // [A] 티어 섹션(헤더 텍스트 + 가로선 + SlotContainer) 통째로 생성
            if (tierSectionPrefab != null)
            {
                GameObject sectionObj = Instantiate(tierSectionPrefab, contentParent);

                // 티어 이름 텍스트 적용 (예: "1티어")
                TextMeshProUGUI label = sectionObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = $"{tier}티어";

                // ★핵심: 하이어라키 개편에 맞춰, 생성된 섹션 내부의 'SlotContainer'를 찾아서 타겟 부모로 변경합니다.
                // 이렇게 해야 카드들이 일렬로 세로 정렬되지 않고, Grid 바둑판 안에 예쁘게 들어갑니다.
                Transform containerFinder = sectionObj.transform.Find("SlotContainer");
                if (containerFinder != null)
                {
                    targetSlotParent = containerFinder;
                }
            }

            // [B] 해당 티어에 속하는 캐릭터 슬롯(카드)들을 'SlotContainer' 내부에 생성
            foreach (var cd in tierGroups[tier])
            {
                if (slotPrefab == null) continue;

                // targetSlotParent(즉, SlotContainer) 하위에 카드 생성
                GameObject slotObj = Instantiate(slotPrefab, targetSlotParent);
                EncyclopediaSlot slot = slotObj.GetComponent<EncyclopediaSlot>();
                if (slot != null)
                {
                    slot.Setup(cd, this); // 카드에 데이터 전달 및 매니저 본인 연결
                }
            }
        }

        // 레이아웃이 겹치거나 깨지는 현상을 방지하기 위한 UI 강제 새로고침 명령
        Canvas.ForceUpdateCanvases();
        var layoutGroups = contentParent.GetComponentsInChildren<LayoutGroup>();
        foreach (var lg in layoutGroups)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(lg.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// 좌측 리스트에서 특정 캐릭터 카드를 클릭했을 때 우측 상세 패널을 업데이트하는 함수
    /// </summary>
    public void ShowDetail(EncyclopediaCharacterData data)
    {
        // 숨겨두었던 전체 상세 패널을 활성화합니다.
        if (detailPanel != null) detailPanel.SetActive(true);

        // 1. 이름과 일러스트 세팅
        if (detailNameText != null) detailNameText.text = data.characterName;
        if (detailPortraitImage != null) detailPortraitImage.sprite = data.fullBodySprite;

        // 2. 스탯 텍스트 세팅 (데이터 파싱)
        if (detailAttackText != null) detailAttackText.text = $"공격력 : {data.attackPower}";
        if (detailSpeedText != null) detailSpeedText.text = $"공격 속도 : {data.attackSpeed}";
        if (detailRangeText != null) detailRangeText.text = $"사거리 : {data.range}";

        // 3. 별점(Star) UI 교체 로직
        // 배열 길이는 5개 고정. 보유한 starRating 수치만큼 노란별을 채우고 나머지는 회색별로 덮습니다.
        if (detailStars != null)
        {
            for (int i = 0; i < detailStars.Length; i++)
            {
                if (i < data.starRating)
                    detailStars[i].sprite = starOnSprite;
                else
                    detailStars[i].sprite = starOffSprite;
            }
        }

        // 4. 스킬 영역 및 툴팁 초기화 로직 호출
        UpdateSkillSection(data);
    }

    /// <summary>
    /// 우측 하단의 스킬 아이콘들을 동적으로 생성하고 버튼 이벤트를 연결하는 함수
    /// </summary>
    private void UpdateSkillSection(EncyclopediaCharacterData data)
    {
        // 이전에 선택했던 캐릭터의 스킬 아이콘들이 남아있다면 모두 파괴
        foreach (var icon in activeSkillIcons)
        {
            Destroy(icon);
        }
        activeSkillIcons.Clear();

        // 다른 캐릭터를 눌렀을 때 기본적으로 말풍선 툴팁창은 닫힌 상태로 초기화
        if (detailSkillDescPanel != null) detailSkillDescPanel.SetActive(false);

        // 데이터에 스킬 이미지가 배열로 존재할 경우 아이콘 생성
        if (data.skillSprites != null && data.skillSprites.Length > 0)
        {
            for (int i = 0; i < data.skillSprites.Length; i++)
            {
                // 부모(SkillIcons) 아래에 단일 스킬 아이콘 생성
                GameObject iconObj = Instantiate(skillIconPrefab, detailSkillContainer);
                activeSkillIcons.Add(iconObj);

                // 스킬 이미지 교체
                Image iconImg = iconObj.GetComponent<Image>();
                if (iconImg != null) iconImg.sprite = data.skillSprites[i];

                // 스킬 아이콘 버튼을 클릭하면 해당 스킬의 툴팁을 띄우도록 이벤트 연결
                Button btn = iconObj.GetComponent<Button>();
                if (btn != null)
                {
                    // 스킬 설명 데이터가 비어있을 경우를 대비한 안전 장치
                    string desc = (data.skillDescriptions != null && i < data.skillDescriptions.Length)
                                  ? data.skillDescriptions[i] : "해당 스킬에 대한 설명이 없습니다.";

                    // 람다식을 이용해 클릭 이벤트 등록
                    btn.onClick.AddListener(() => ShowSkillTooltip(desc));
                }
            }

            // 편의성: 캐릭터를 처음 클릭했을 때 첫 번째 스킬의 말풍선이 자동으로 열려있게 세팅
            if (data.skillDescriptions != null && data.skillDescriptions.Length > 0)
            {
                ShowSkillTooltip(data.skillDescriptions[0]);
            }
        }
    }

    /// <summary>
    /// 하단 말풍선 패널을 켜고, 매개변수로 받은 설명 텍스트를 출력하는 함수
    /// </summary>
    public void ShowSkillTooltip(string description)
    {
        if (detailSkillDescPanel != null) detailSkillDescPanel.SetActive(true);
        if (detailSkillDescText != null) detailSkillDescText.text = description;
    }
}