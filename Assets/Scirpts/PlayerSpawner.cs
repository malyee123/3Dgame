using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private float stackOffsetY = 0.25f;

    private int[] slotOccupancy;
    private readonly Dictionary<string, int> tagToSlotIndex = new Dictionary<string, int>();

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
    }

    public void TrySpawnPlayer()
    {
        if (slotOccupancy == null || slotOccupancy.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] Slots are not initialized.");
            return;
        }

        CharacterData selectedData = GetRandomCharacterData();
        if (selectedData == null)
        {
            Debug.LogWarning("[PlayerSpawner] No valid CharacterData could be selected.");
            return;
        }

        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, out int spawnIndex))
        {
            Debug.Log($"[PlayerSpawner] Cannot spawn '{unitTag}'. No slot available (or assigned slot is full).");
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
        Vector3 spawnPosition = spawnPoints[spawnIndex].position + new Vector3(0f, stackOffsetY * stackIndex, 0f);

        GameObject obj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        PlayerAttack playerAttack = obj.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.spawnIndex = spawnIndex;
            playerAttack.characterData = characterData;
            playerAttack.unitTag = unitTag;
        }

        PlayerDragMerge dragMerge = obj.GetComponent<PlayerDragMerge>();
        if (dragMerge == null) dragMerge = obj.AddComponent<PlayerDragMerge>();
        dragMerge.SetSpawnIndex(spawnIndex);

        slotOccupancy[spawnIndex]++;

        Debug.Log($"[PlayerSpawner] Spawned '{unitTag}' at slot {spawnIndex} ({slotOccupancy[spawnIndex]}/{maxUnitsPerSlot}).");
    }

    public void RegisterFreedSlot(int spawnIndex)
    {
        if (slotOccupancy == null) return;
        if (spawnIndex < 0 || spawnIndex >= slotOccupancy.Length) return;
        if (slotOccupancy[spawnIndex] <= 0) return;

        slotOccupancy[spawnIndex]--;

        if (slotOccupancy[spawnIndex] == 0)
            RemoveTagMappingForSlot(spawnIndex);
    }

    CharacterData GetRandomCharacterData()
    {
        if (characterDataList == null || characterDataList.Length == 0) return null;
        int randomIndex = Random.Range(0, characterDataList.Length);
        return characterDataList[randomIndex];
    }

    bool TryGetSpawnSlot(string unitTag, out int slotIndex)
    {
        if (tagToSlotIndex.TryGetValue(unitTag, out slotIndex))
            return slotOccupancy[slotIndex] < maxUnitsPerSlot;

        for (int i = 0; i < slotOccupancy.Length; i++)
        {
            if (slotOccupancy[i] > 0) continue;

            slotIndex = i;
            tagToSlotIndex[unitTag] = i;
            return true;
        }

        slotIndex = -1;
        return false;
    }

    void RemoveTagMappingForSlot(int slotIndex)
    {
        string keyToRemove = null;

        foreach (KeyValuePair<string, int> pair in tagToSlotIndex)
        {
            if (pair.Value != slotIndex) continue;
            keyToRemove = pair.Key;
            break;
        }

        if (keyToRemove != null)
            tagToSlotIndex.Remove(keyToRemove);
    }

    string GetUnitTag(CharacterData characterData)
    {
        if (characterData == null) return "Unknown";

        if (!string.IsNullOrWhiteSpace(characterData.unitTag))
            return characterData.unitTag.Trim();

        return characterData.characterName;
    }
}
