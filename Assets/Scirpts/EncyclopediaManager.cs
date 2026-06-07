using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaManager : MonoBehaviour
{
    public static EncyclopediaManager Instance { get; private set; }

    [Header("데이터")]
    public EncyclopediaCharacterData[] allCharacters;

    [Header("목록 패널 (좌측)")]
    public Transform   listContent;
    public GameObject  tierSectionPrefab;
    public GameObject  slotPrefab;

    [Header("상세 패널 (우측)")]
    public Image           detailFullBody;
    public TextMeshProUGUI detailName;
    public Image[]         detailStars;
    public Sprite          starOnSprite;
    public Sprite          starOffSprite;
    public TextMeshProUGUI detailAttackText;
    public TextMeshProUGUI detailSpeedText;
    public TextMeshProUGUI detailRangeText;
    public Transform       detailSkillContainer;
    public GameObject      skillIconPrefab;
    public TextMeshProUGUI detailSkillDesc;
    public TextMeshProUGUI detailDescription;
    public GameObject      detailPanel;

    [Header("네비게이션")]
    public Button backButton;

    private EncyclopediaSlot selectedSlot;
    private List<EncyclopediaSlot> allSlots = new List<EncyclopediaSlot>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneLoader.GoTo("LobbyScene"));
        }

        if (detailPanel != null) detailPanel.SetActive(false);

        BuildList();
        CheckRoundUnlocks();
    }

    void BuildList()
    {
        if (listContent == null || allCharacters == null) return;

        foreach (Transform child in listContent)
            Destroy(child.gameObject);
        allSlots.Clear();

        // 티어별 그룹핑
        Dictionary<int, List<EncyclopediaCharacterData>> tierGroups
            = new Dictionary<int, List<EncyclopediaCharacterData>>();

        foreach (var cd in allCharacters)
        {
            if (cd == null) continue;
            if (!tierGroups.ContainsKey(cd.tier))
                tierGroups[cd.tier] = new List<EncyclopediaCharacterData>();
            tierGroups[cd.tier].Add(cd);
        }

        List<int> tiers = new List<int>(tierGroups.Keys);
        tiers.Sort();

        foreach (int tier in tiers)
        {
            // 티어 섹션 헤더 생성
            if (tierSectionPrefab != null)
            {
                GameObject section = Instantiate(tierSectionPrefab, listContent);
                TextMeshProUGUI label = section.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = $"{tier}티어";
            }

            // 해당 티어 캐릭터 슬롯 생성
            foreach (var cd in tierGroups[tier])
            {
                if (slotPrefab == null) continue;
                GameObject slotObj = Instantiate(slotPrefab, listContent);
                EncyclopediaSlot slot = slotObj.GetComponent<EncyclopediaSlot>();
                if (slot != null)
                {
                    slot.Setup(cd, this);
                    allSlots.Add(slot);
                }
            }
        }
    }

    public void OnSlotSelected(EncyclopediaSlot slot, EncyclopediaCharacterData data)
    {
        if (selectedSlot != null) selectedSlot.SetHighlight(false);
        selectedSlot = slot;
        slot.SetHighlight(true);
        ShowDetail(data);
    }

    void ShowDetail(EncyclopediaCharacterData data)
    {
        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailFullBody != null)
        {
            detailFullBody.sprite  = data.fullBodySprite;
            detailFullBody.enabled = data.fullBodySprite != null;
        }

        if (detailName != null)
            detailName.text = data.characterName;

        // 별점
        if (detailStars != null)
        {
            for (int i = 0; i < detailStars.Length; i++)
            {
                if (detailStars[i] == null) continue;
                bool on = i < data.starRating;
                if (starOnSprite  != null && on)  detailStars[i].sprite = starOnSprite;
                if (starOffSprite != null && !on) detailStars[i].sprite = starOffSprite;
                detailStars[i].color = on ? Color.white : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        // 스탯
        if (detailAttackText != null) detailAttackText.text = $"공격력 : {data.attackPower}";
        if (detailSpeedText  != null) detailSpeedText.text  = $"공격 속도 : {data.attackSpeed}";
        if (detailRangeText  != null) detailRangeText.text  = $"사거리 : {data.range}";

        // 스킬 아이콘
        if (detailSkillContainer != null)
        {
            foreach (Transform child in detailSkillContainer) Destroy(child.gameObject);

            if (data.skillSprites != null)
            {
                for (int i = 0; i < data.skillSprites.Length; i++)
                {
                    if (skillIconPrefab == null) break;
                    GameObject iconObj = Instantiate(skillIconPrefab, detailSkillContainer);
                    Image img = iconObj.GetComponent<Image>();
                    if (img != null && data.skillSprites[i] != null)
                        img.sprite = data.skillSprites[i];
                }
            }
        }

        // 스킬 설명 (첫 번째 스킬)
        if (detailSkillDesc != null)
        {
            detailSkillDesc.text = (data.skillDescriptions != null && data.skillDescriptions.Length > 0)
                ? data.skillDescriptions[0] : "";
        }

        // 캐릭터 설명
        if (detailDescription != null)
            detailDescription.text = data.description;
    }

    // 현재 라운드 기준 해금 체크 (GameScene에서 호출하거나 Start에서 체크)
    public void CheckRoundUnlocks()
    {
        int currentRound = PlayerPrefs.GetInt("LastRound", 0);
        bool anyUnlocked = false;

        foreach (var cd in allCharacters)
        {
            if (cd == null) continue;
            if (EncyclopediaCharacterData.CheckAndUnlockByRound(cd, currentRound))
                anyUnlocked = true;
        }

        if (anyUnlocked) RefreshList();
    }

    // 외부에서 특정 캐릭터 해금 (처치 시 등)
    public static void UnlockCharacter(string characterName, int tier)
    {
        if (Instance == null) return;
        foreach (var cd in Instance.allCharacters)
        {
            if (cd == null) continue;
            if (cd.characterName == characterName && cd.tier == tier)
            {
                EncyclopediaCharacterData.Unlock(cd);
                Instance.RefreshList();
                return;
            }
        }
    }

    void RefreshList()
    {
        BuildList();
    }
}
