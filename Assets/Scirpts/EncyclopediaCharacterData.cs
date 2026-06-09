using UnityEngine;

/// <summary>
/// 도감 캐릭터 1개의 모든 데이터를 담는 ScriptableObject
/// Project 창 우클릭 → Create → Game → Encyclopedia Character Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "EncyclopediaCharacterData",
                 menuName  = "Game/Encyclopedia Character Data")]
public class EncyclopediaCharacterData : ScriptableObject
{
    // ───────────── 기본 정보 ─────────────
    [Header("기본 정보")]
    public string characterName;   // 캐릭터 이름
    public int    tier;            // 티어 번호 (1~5)
    public int    starRating;      // 별점 (1~5)

    // ───────────── 이미지 ─────────────
    [Header("도감 이미지")]
    [Tooltip("좌측 그리드 카드에 표시할 초상화 Sprite")]
    public Sprite portraitSprite;
    [Tooltip("우측 상세 패널에 크게 표시할 풀바디 Sprite")]
    public Sprite fullBodySprite;

    // ───────────── 스탯 ─────────────
    [Header("스탯")]
    public float attackPower;
    public float attackSpeed;
    public float range;

    // ───────────── 스킬 (1:1 매칭) ─────────────
    [Header("스킬 아이콘 배열 (skillIcons[i] ↔ skillTooltipSprites[i] 반드시 인덱스 일치)")]
    [Tooltip("스킬 아이콘 이미지 배열")]
    public Sprite[] skillIcons;
    [Tooltip("마우스 Hover/Click 시 팝업으로 띄울 스킬 설명 통짜 이미지 배열")]
    public Sprite[] skillTooltipSprites;

    // ───────────── 설명 ─────────────
    [Header("캐릭터 설명")]
    [TextArea(3, 6)]
    public string description;

    // ───────────── 해금 조건 ─────────────
    [Header("해금 조건")]
    [Tooltip("0 이면 처음부터 해금. N 이면 N라운드 도달 시 자동 해금")]
    public int unlockRound = 0;

    // PlayerPrefs 키 (캐릭터명+티어로 고유하게 생성)
    public string UnlockKey => $"CharUnlocked_{characterName}_{tier}";

    /// <summary>현재 해금 여부 확인</summary>
    public bool IsUnlocked =>
        unlockRound == 0 || PlayerPrefs.GetInt(UnlockKey, 0) == 1;

    /// <summary>즉시 해금 처리 및 저장</summary>
    public static void Unlock(EncyclopediaCharacterData data)
    {
        if (data == null) return;
        PlayerPrefs.SetInt(data.UnlockKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 현재 라운드 기준 해금 조건 달성 여부 체크.
    /// 조건 달성 시 해금 처리 후 true 반환.
    /// </summary>
    public static bool CheckAndUnlockByRound(EncyclopediaCharacterData data, int currentRound)
    {
        if (data == null || data.IsUnlocked) return false;
        if (data.unlockRound > 0 && currentRound >= data.unlockRound)
        {
            Unlock(data);
            return true;
        }
        return false;
    }
}
