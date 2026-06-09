using UnityEngine;

[CreateAssetMenu(fileName = "EC_New",
                 menuName = "Game/Encyclopedia Character Data")]
public class EncyclopediaCharacterData : ScriptableObject
{
    [Header("── 기본 정보 ──")]
    public string characterName = "캐릭터";
    public int tier = 1;
    public int starRating = 1;

    [Header("── 이미지 ──")]
    public Sprite portraitSprite;
    public Sprite fullBodySprite;

    [Header("── 스탯 ──")]
    public float attackPower;
    public float attackSpeed;
    public float range;

    [Header("── 스킬 (아이콘[i] ↔ 툴팁[i] 인덱스 반드시 일치) ──")]
    public Sprite[] skillIcons;
    public Sprite[] skillTooltipSprites;

    [Header("── 설명 ──")]
    [TextArea(3, 5)]
    public string description;

    [Header("── 해금 조건 ──")]
    [Tooltip("0 = 처음부터 해금 / N = N라운드 도달 시 해금")]
    public int unlockRound = 0;

    public string UnlockKey => $"EC_Unlocked_{characterName}_{tier}";
    public bool IsUnlocked => unlockRound == 0 || PlayerPrefs.GetInt(UnlockKey, 0) == 1;

    public static void Unlock(EncyclopediaCharacterData d)
    {
        if (d == null) return;
        PlayerPrefs.SetInt(d.UnlockKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[ECD] 해금 저장: {d.characterName}");
    }

    public static bool TryUnlockByRound(EncyclopediaCharacterData d, int round)
    {
        if (d == null || d.IsUnlocked) return false;
        if (d.unlockRound > 0 && round >= d.unlockRound)
        {
            Unlock(d);
            return true;
        }
        return false;
    }
}