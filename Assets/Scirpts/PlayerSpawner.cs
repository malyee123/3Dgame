using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Header("Player Settings")]
    public GameObject playerPrefab;

    [Header("Character Data List")]
    public CharacterData[] characterDataList;

    [Header("Spawn Point Settings")]
    public Transform[] spawnPoints;

    [Header("Stack Settings")]
    [SerializeField] private int maxUnitsPerSlot = 3;
    [SerializeField] private float triangleOffsetX = 0.28f;
    [SerializeField] private float triangleOffsetY = 0.22f;

    private int[] slotOccupancy;
    private string[] slotTagOwners;
    private readonly Dictionary<string, List<int>> tagToSlots = new Dictionary<string, List<int>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[PlayerSpawner] Duplicate instance found!");
        Instance = this;
    }

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogError("[PlayerSpawner] No spawn points assigned!"); return; }
        if (playerPrefab == null) { Debug.LogError("[PlayerSpawner] playerPrefab is missing!"); return; }
        if (characterDataList == null || characterDataList.Length == 0) { Debug.LogError("[PlayerSpawner] CharacterData list is empty!"); return; }

        slotOccupancy = new int[spawnPoints.Length];
        slotTagOwners = new string[spawnPoints.Length];
    }

    public void TrySpawnPlayer()
    {

        

        if (slotOccupancy == null || slotOccupancy.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] Slots are not initialized.");
            return;
        }

        SyncSlotStateFromScene();

        CharacterData selectedData = GetRandomCharacterData();
        if (selectedData == null)
        {
            Debug.LogWarning("[PlayerSpawner] No valid CharacterData could be selected.");
            return;
        }

        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, out int spawnIndex))
        {
            Debug.Log($"[PlayerSpawner] Cannot spawn '{unitTag}'. All allowed slots are full.");
            return;
        }

        if (CoinManager.Instance != null)
        {
            bool success = CoinManager.Instance.SpendCoins(CoinManager.Instance.spawnCost);
            if (!success) return;
        }

        SpawnPlayer(spawnIndex, selectedData, unitTag);
    }

    void SpawnPlayer(int spawnIndex, CharacterData characterData, string unitTag)
    {
        int stackIndex = slotOccupancy[spawnIndex];
        Vector3 spawnPosition = spawnPoints[spawnIndex].position + GetTriangleOffset(stackIndex);

        GameObject obj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        PlayerAttack playerAttack = obj.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.spawnIndex = spawnIndex;
            playerAttack.characterData = characterData;
            playerAttack.unitTag = unitTag;
        }

        if (characterData.characterPrefab != null)
        {
            GameObject visual = Instantiate(characterData.characterPrefab, obj.transform);
            visual.transform.localPosition = Vector3.zero;
        }

        slotOccupancy[spawnIndex]++;
        slotTagOwners[spawnIndex] = unitTag;

        Debug.Log($"[PlayerSpawner] Spawned '{unitTag}' at slot {spawnIndex} ({slotOccupancy[spawnIndex]}/{maxUnitsPerSlot}).");


       

    }

    CharacterData GetRandomNextLevelCharacterData(List<PlayerAttack> unitsInSlot)
    {
        if (unitsInSlot == null || unitsInSlot.Count == 0) return null;

        int currentTier = -1;
        string mergeGroupKey = "";
        for (int i = 0; i < unitsInSlot.Count; i++)
        {
            CharacterData baseData = unitsInSlot[i].characterData;
            if (baseData == null) continue;

            currentTier = Mathf.Max(1, baseData.tier);
            mergeGroupKey = GetMergeGroupKey(baseData);
            break;
        }

        if (currentTier < 0) return null;

        int targetTier = currentTier + 1;

        List<CharacterData> candidates = new List<CharacterData>();
        if (characterDataList != null)
        {
            for (int i = 0; i < characterDataList.Length; i++)
            {
                CharacterData data = characterDataList[i];
                if (data == null) continue;
                if (Mathf.Max(1, data.tier) != targetTier) continue;
                if (GetMergeGroupKey(data) != mergeGroupKey) continue;

                candidates.Add(data);
            }
        }

        if (candidates.Count == 0) return null;

        int randomIndex = Random.Range(0, candidates.Count);
        return candidates[randomIndex];
    }

 

    public bool CanManualMerge(int spawnIndex, string unitTag, CharacterData selectedData)
    {
        SyncSlotStateFromScene();
        int selectedTier = selectedData != null ? Mathf.Max(1, selectedData.tier) : -1;
        List<PlayerAttack> sameTagUnits = GetUnitsInSlot(spawnIndex, unitTag, selectedTier);
        return sameTagUnits.Count >= maxUnitsPerSlot;
    }

    public bool TryManualMerge(int spawnIndex, string unitTag, CharacterData selectedData)
    {
        SyncSlotStateFromScene();

        int selectedTier = selectedData != null ? Mathf.Max(1, selectedData.tier) : -1;
        List<PlayerAttack> sameTagUnits = GetUnitsInSlot(spawnIndex, unitTag, selectedTier);
        if (sameTagUnits.Count < maxUnitsPerSlot)
            return false;

        int survivorIndex = Random.Range(0, maxUnitsPerSlot);
        PlayerAttack survivor = sameTagUnits[survivorIndex];

        CharacterData mergedData = GetRandomNextLevelCharacterData(sameTagUnits);
        if (mergedData == null)
            return false;

        for (int i = 0; i < maxUnitsPerSlot; i++)
        {
            if (i == survivorIndex) continue;
            GameObject removeObj = sameTagUnits[i].gameObject;
            removeObj.SetActive(false);
            Destroy(removeObj);
        }

        survivor.ApplyCharacterData(mergedData);
        survivor.unitTag = GetUnitTag(mergedData);
        survivor.transform.position = spawnPoints[spawnIndex].position + GetTriangleOffset(0);

        SyncSlotStateFromScene();
        return true;
    }


    List<PlayerAttack> GetUnitsInSlot(int spawnIndex, string unitTag, int requiredTier = -1)
    {
        List<PlayerAttack> result = new List<PlayerAttack>();

        PlayerAttack[] players = FindObjectsOfType<PlayerAttack>();
        foreach (PlayerAttack player in players)
        {
            if (player == null) continue;
            if (player.spawnIndex != spawnIndex) continue;

            string playerTag = !string.IsNullOrWhiteSpace(player.unitTag)
                ? player.unitTag.Trim()
                : GetUnitTag(player.characterData);

            if (playerTag != unitTag) continue;

            if (requiredTier > 0)
            {
                int playerTier = player.characterData != null ? Mathf.Max(1, player.characterData.tier) : -1;
                if (playerTier != requiredTier) continue;
            }

            result.Add(player);
        }

        return result;

    }

    public void RegisterFreedSlot(int spawnIndex)
    {
        if (slotOccupancy == null || slotTagOwners == null) return;
        if (spawnIndex < 0 || spawnIndex >= slotOccupancy.Length) return;
        if (slotOccupancy[spawnIndex] <= 0) return;

        slotOccupancy[spawnIndex]--;

        if (slotOccupancy[spawnIndex] > 0) return;

        string tag = slotTagOwners[spawnIndex];
        slotTagOwners[spawnIndex] = null;

        if (string.IsNullOrWhiteSpace(tag)) return;
        if (!tagToSlots.TryGetValue(tag, out List<int> slots)) return;

        slots.Remove(spawnIndex);
        if (slots.Count == 0)
            tagToSlots.Remove(tag);
    }


    void SyncSlotStateFromScene()
    {
        for (int i = 0; i < slotOccupancy.Length; i++)
        {
            slotOccupancy[i] = 0;
            slotTagOwners[i] = null;
        }

        tagToSlots.Clear();

        PlayerAttack[] players = FindObjectsOfType<PlayerAttack>();
        foreach (PlayerAttack player in players)
        {
            if (player == null) continue;

            int index = player.spawnIndex;
            if (index < 0 || index >= slotOccupancy.Length) continue;

            string tag = !string.IsNullOrWhiteSpace(player.unitTag)
                ? player.unitTag.Trim()
                : GetUnitTag(player.characterData);

            if (string.IsNullOrWhiteSpace(tag)) continue;

            slotOccupancy[index]++;
            slotTagOwners[index] = tag;

            if (!tagToSlots.TryGetValue(tag, out List<int> slots))
            {
                slots = new List<int>();
                tagToSlots[tag] = slots;
            }

            if (!slots.Contains(index))
                slots.Add(index);
        }
    }

    CharacterData GetRandomCharacterData()
    {
        if (characterDataList == null || characterDataList.Length == 0) return null;
        int randomIndex = Random.Range(0, characterDataList.Length);
        return characterDataList[randomIndex];
    }

    bool TryGetSpawnSlot(string unitTag, out int slotIndex)
    {
        if (!tagToSlots.TryGetValue(unitTag, out List<int> slots))
        {
            slots = new List<int>();
            tagToSlots[unitTag] = slots;
        }

        List<int> availableTaggedSlots = new List<int>();
        for (int i = 0; i < slots.Count; i++)
        {
            int existingSlot = slots[i];
            if (existingSlot < 0 || existingSlot >= slotOccupancy.Length) continue;
            if (slotOccupancy[existingSlot] >= maxUnitsPerSlot) continue;
            availableTaggedSlots.Add(existingSlot);
        }

        if (availableTaggedSlots.Count > 0)
        {
            int randomTaggedSlot = availableTaggedSlots[Random.Range(0, availableTaggedSlots.Count)];
            slotIndex = randomTaggedSlot;
            return true;
        }

        List<int> emptySlots = new List<int>();
        for (int i = 0; i < slotOccupancy.Length; i++)
        {
            if (slotOccupancy[i] > 0) continue;
            emptySlots.Add(i);
        }

        if (emptySlots.Count > 0)
        {
            int randomEmptySlot = emptySlots[Random.Range(0, emptySlots.Count)];
            slotTagOwners[randomEmptySlot] = unitTag;
            if (!slots.Contains(randomEmptySlot))
                slots.Add(randomEmptySlot);
            slotIndex = randomEmptySlot;
            return true;
        }

        slotIndex = -1;
        return false;
    }

    Vector3 GetTriangleOffset(int stackIndex)
    {
        if (stackIndex <= 0)
            return Vector3.up * triangleOffsetY;

        if (stackIndex == 1)
            return new Vector3(-triangleOffsetX, -triangleOffsetY, 0f);

        if (stackIndex == 2)
            return new Vector3(triangleOffsetX, -triangleOffsetY, 0f);

        return Vector3.zero;
    }

    string GetUnitTag(CharacterData characterData)
    {
        if (characterData == null) return "Unknown";

        if (!string.IsNullOrWhiteSpace(characterData.unitTag))
            return characterData.unitTag.Trim();

        return characterData.characterName;
    }

    string GetMergeGroupKey(CharacterData characterData)
    {
        string rawTag = GetUnitTag(characterData);
        if (string.IsNullOrWhiteSpace(rawTag))
            return "";

        return Regex.Replace(rawTag.Trim(), @"^(?i:tier)\d+[_-]?", "");
    }
}
