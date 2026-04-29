using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class ManaManager : MonoBehaviour
{
    public static ManaManager Instance { get; private set; }

    [System.Serializable]
    public class ManaSlot
    {
        public string characterName;
        public float currentMana;
        public float maxMana;
        public bool isCharging;
        public Button skillButton;
        public Image fillImage;
    }

    [Header("Mana Slots (Tier5_1 ~ Tier5_4)")]
    public ManaSlot[] manaSlots = new ManaSlot[4];

    private float manaTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < manaSlots.Length; i++)
        {
            int index = i;
            if (manaSlots[i].skillButton != null)
            {
                manaSlots[i].skillButton.onClick.RemoveAllListeners();
                manaSlots[i].skillButton.onClick.AddListener(() => OnSkillButtonClick(index));
            }
        }
        UpdateAllUI();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsWarning) return;
        manaTimer += Time.deltaTime;
        if (manaTimer >= 1f) { manaTimer = 0f; ChargeMana(); }
        UpdateAllUI();
    }

    void ChargeMana()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        for (int i = 0; i < manaSlots.Length; i++)
        {
            ManaSlot slot = manaSlots[i];
            bool hasCharacter = false;
            foreach (PlayerAttack unit in allUnits)
            {
                if (unit == null || unit.characterData == null) continue;
                if (unit.characterData.characterName == slot.characterName)
                {
                    hasCharacter = true;
                    slot.maxMana = unit.characterData.maxMana;
                    break;
                }
            }
            slot.isCharging = hasCharacter;
            if (hasCharacter && slot.currentMana < slot.maxMana)
                slot.currentMana = Mathf.Min(slot.currentMana + 1f, slot.maxMana);
        }
    }

    void UpdateAllUI()
    {
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);

        for (int i = 0; i < manaSlots.Length; i++)
        {
            ManaSlot slot = manaSlots[i];
            bool isFull = slot.currentMana >= slot.maxMana;

            // Tier5_2는 버프라 적 없어도 버튼 활성화
            bool hasTargetInRange = false;
            if (slot.characterName == "Tier5_2")
            {
                // 캐릭터가 필드에 있으면 버튼 활성화
                foreach (PlayerAttack unit in allUnits)
                {
                    if (unit == null || unit.characterData == null) continue;
                    if (unit.characterData.characterName == slot.characterName && unit.isLeader)
                    {
                        hasTargetInRange = true;
                        break;
                    }
                }
            }
            else
            {
                // 나머지는 리더 중 한 마리라도 사거리 내 적 있으면 활성화
                foreach (PlayerAttack unit in allUnits)
                {
                    if (unit == null || unit.characterData == null) continue;
                    if (unit.characterData.characterName == slot.characterName && unit.isLeader)
                    {
                        if (unit.GetCurrentTarget() != null || unit.FindBackmostEnemyInRange() != null)
                        {
                            hasTargetInRange = true;
                            break;
                        }
                    }
                }
            }

            if (slot.skillButton != null)
            {
                slot.skillButton.interactable = isFull && hasTargetInRange;
                slot.skillButton.gameObject.SetActive(slot.isCharging);
            }
            if (slot.fillImage != null)
                slot.fillImage.fillAmount = slot.maxMana > 0 ? slot.currentMana / slot.maxMana : 0f;
        }
    }

    void OnSkillButtonClick(int index)
    {
        ManaSlot slot = manaSlots[index];
        if (slot.currentMana < slot.maxMana) return;

        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
        {
            if (unit == null || unit.characterData == null) continue;
            if (unit.characterData.characterName != slot.characterName || !unit.isLeader) continue;

            // Tier5_2는 버프라 적 없어도 발동
            if (unit.characterData.characterName == "Tier5_2")
            {
                unit.ActivateManaSkill();
                continue;
            }

            // 나머지는 개별 사거리 내 적 있을 때만 발동
            if (unit.GetCurrentTarget() != null || unit.FindBackmostEnemyInRange() != null)
                unit.ActivateManaSkill();
        }

        slot.currentMana = 0f;
        UpdateAllUI();
    }

    public void ResetAllMana()
    {
        for (int i = 0; i < manaSlots.Length; i++) manaSlots[i].currentMana = 0f;
        UpdateAllUI();
    }
}