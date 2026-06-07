using UnityEngine;

[CreateAssetMenu(fileName = "EncyclopediaCharacterData",
                 menuName = "Game/Encyclopedia Character Data")]
public class EncyclopediaCharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName;
    public int    tier;
    public int    starRating;

    [Header("도감 이미지")]
    public Sprite portraitSprite;
    public Sprite fullBodySprite;

    [Header("스탯")]
    public float attackPower;
    public float attackSpeed;
    public float range;

    [Header("스킬")]
    public Sprite[] skillSprites;
    [TextArea(2, 4)]
    public string[] skillDescriptions;

    [Header("설명")]
    [TextArea(3, 6)]
    public string description;

    [Header("해금 조건")]
    [Tooltip("이 라운드에 도달하면 자동 해금 (0 = 기본 해금)")]
    public int unlockRound = 0;

    // PlayerPrefs 저장 키
    public string UnlockKey => $"CharUnlocked_{characterName}_{tier}";

    public bool IsUnlocked
    {
        get => unlockRound == 0 || PlayerPrefs.GetInt(UnlockKey, 0) == 1;
    }

    public static void Unlock(EncyclopediaCharacterData data)
    {
        if (data == null) return;
        PlayerPrefs.SetInt(data.UnlockKey, 1);
        PlayerPrefs.Save();
    }

    public static bool CheckAndUnlockByRound(EncyclopediaCharacterData data, int currentRound)
    {
        if (data == null || data.IsUnlocked) return false;
        if (currentRound >= data.unlockRound && data.unlockRound > 0)
        {
            Unlock(data);
            return true;
        }
        return false;
    }
}
