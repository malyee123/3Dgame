using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

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

    [Header("Tier Spawn Weights")]
    public float[] tierSpawnWeights = { 70f, 30f };

    [Header("Special Spawn Settings")]
    public int specialSpawnCost = 3;
    public int specialSpawnMinTier = 2;
    public int specialSpawnMaxTier = 4;
    public float[] specialTierSpawnWeights = { 70f, 25f, 5f };

    [Header("UI")]
    public Button spawnButton;
    public GameObject[] auraPrefabs;

    private int[] slotOccupancy;
    private string[] slotTagOwners;
    private readonly Dictionary<string, List<int>> tagToSlots = new Dictionary<string, List<int>>();
    private bool slotDirty = false;

    void Awake()
    {
        CharacterData[] loaded = Resources.LoadAll<CharacterData>("CharacterData");
        if (loaded != null && loaded.Length > 0)
            characterDataList = loaded;
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
        UpdateSpawnButton();
    }

    void Update()
    {
        if (slotDirty) { SyncSlotStateFromScene(); slotDirty = false; UpdateSpawnButton(); }
    }

    public void TrySpawnPlayer()
    {
        if (slotOccupancy == null || slotOccupancy.Length == 0) { Debug.LogError("[PlayerSpawner] Slots are not initialized."); return; }
        if (IsFieldFull()) return;
        SyncSlotStateFromScene();
        CharacterData selectedData = GetRandomCharacterData();
        if (selectedData == null) { Debug.LogWarning("[PlayerSpawner] No valid CharacterData."); return; }
        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, out int spawnIndex)) return;
        if (CoinManager.Instance != null) { if (!CoinManager.Instance.SpendCoins(CoinManager.Instance.spawnCost)) return; }
        if (MergeManager.Instance != null) MergeManager.Instance.HideUnitActionUI();
        SpawnPlayer(spawnIndex, selectedData, unitTag);
    }

    public void TrySpecialSpawn()
    {
        if (IsFieldFull()) { Debug.Log("[SpecialSpawn] 필드 가득 참"); return; }
        if (SpecialCoinManager.Instance == null) { Debug.Log("[SpecialSpawn] SpecialCoinManager 없음"); return; }
        if (!SpecialCoinManager.Instance.SpendSpecialCoins(specialSpawnCost)) { Debug.Log("[SpecialSpawn] 코인 부족"); return; }
        SyncSlotStateFromScene();
        CharacterData selectedData = GetRandomSpecialCharacterData();
        if (selectedData == null) { Debug.Log("[SpecialSpawn] CharacterData 선택 실패 - 티어 해금 확인 필요"); return; }
        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, out int spawnIndex)) { Debug.Log("[SpecialSpawn] 슬롯 없음"); return; }
        if (MergeManager.Instance != null) MergeManager.Instance.HideUnitActionUI();
        SpawnPlayer(spawnIndex, selectedData, unitTag);
        Debug.Log($"[SpecialSpawn] {selectedData.characterName} 소환 성공!");
    }

    
    CharacterData GetRandomSpecialCharacterData()
    {
        if (characterDataList == null || characterDataList.Length == 0) return null;
        int unlockedTier = UpgradeManager.Instance != null ? UpgradeManager.Instance.UnlockedTier : 1;
        int maxTier = Mathf.Min(specialSpawnMaxTier, unlockedTier);
        if (unlockedTier < specialSpawnMinTier) return null;

        Dictionary<int, List<CharacterData>> tierMap = new Dictionary<int, List<CharacterData>>();
        foreach (CharacterData data in characterDataList)
        {
            if (data == null) continue;
            int tier = Mathf.Max(1, data.tier);
            if (tier < specialSpawnMinTier || tier > maxTier) continue;
            if (!tierMap.ContainsKey(tier)) tierMap[tier] = new List<CharacterData>();
            tierMap[tier].Add(data);
        }

        float totalWeight = 0f;
        for (int i = 0; i < specialTierSpawnWeights.Length; i++)
        {
            int tier = specialSpawnMinTier + i;
            if (tierMap.ContainsKey(tier)) totalWeight += specialTierSpawnWeights[i];
        }

        if (totalWeight <= 0f) return null;

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < specialTierSpawnWeights.Length; i++)
        {
            int tier = specialSpawnMinTier + i;
            if (!tierMap.ContainsKey(tier)) continue;
            cumulative += specialTierSpawnWeights[i];
            if (rand <= cumulative)
                return tierMap[tier][Random.Range(0, tierMap[tier].Count)];
        }

        return null;
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
            playerAttack.isLeader = (stackIndex == 0);
        }
        if (characterData.characterPrefab != null)
        {
            GameObject visual = Instantiate(characterData.characterPrefab, obj.transform);
            visual.transform.localPosition = Vector3.zero;
        }
        slotOccupancy[spawnIndex]++;
        slotTagOwners[spawnIndex] = unitTag;
        slotDirty = true;

        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.spawnIndex == spawnIndex && unit.isLeader)
                unit.MarkSlotMatesDirty();

        if (PassiveManager.Instance != null) PassiveManager.Instance.RecalculatePassives();
        UpdateSpawnButton();

        if (auraPrefabs != null && characterData.tier - 1 < auraPrefabs.Length)
        {
            GameObject auraPrefab = auraPrefabs[characterData.tier - 1];
            if (auraPrefab != null)
            {
                GameObject aura = Instantiate(auraPrefab, obj.transform);
                aura.transform.localPosition = Vector3.zero;
            }
        }
    }

    public void SpawnSpecificCharacter(CharacterData characterData)
    {
        if (characterData == null) return;
        string unitTag = GetUnitTag(characterData);
        if (!TryGetSpawnSlot(unitTag, out int spawnIndex)) return;
        SpawnPlayer(spawnIndex, characterData, unitTag);
    }

    CharacterData GetRandomNextLevelCharacterData(List<PlayerAttack> unitsInSlot)
    {
        if (unitsInSlot == null || unitsInSlot.Count == 0) return null;
        int currentTier = -1;
        for (int i = 0; i < unitsInSlot.Count; i++)
        {
            CharacterData baseData = unitsInSlot[i].characterData;
            if (baseData == null) continue;
            currentTier = Mathf.Max(1, baseData.tier);
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
                candidates.Add(data);
            }
        }
        if (candidates.Count == 0) { Debug.LogWarning($"[PlayerSpawner] Merge failed: no data for tier={targetTier}"); return null; }
        return candidates[Random.Range(0, candidates.Count)];
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
        if (sameTagUnits.Count < maxUnitsPerSlot) return false;
        int survivorIndex = 0;
        PlayerAttack survivor = sameTagUnits[survivorIndex];
        CharacterData mergedData = GetRandomNextLevelCharacterData(sameTagUnits);
        if (mergedData == null) return false;
        for (int i = 0; i < maxUnitsPerSlot; i++)
        {
            if (i == survivorIndex) continue;
            GameObject removeObj = sameTagUnits[i].gameObject;
            removeObj.SetActive(false);
            Destroy(removeObj);
        }
        survivor.ApplyCharacterData(mergedData);
        survivor.isLeader = true;
        string newUnitTag = GetUnitTag(mergedData);
        survivor.unitTag = newUnitTag;
        survivor.spawnIndex = -1;
        SyncSlotStateFromScene();
        if (!tagToSlots.TryGetValue(newUnitTag, out List<int> existingSlots)) existingSlots = new List<int>();
        List<int> availableTaggedSlots = new List<int>();
        for (int i = 0; i < existingSlots.Count; i++)
        {
            int s = existingSlots[i];
            if (s < 0 || s >= slotOccupancy.Length) continue;
            if (slotOccupancy[s] >= maxUnitsPerSlot) continue;
            availableTaggedSlots.Add(s);
        }
        if (availableTaggedSlots.Count > 0)
        {
            int targetSlot = availableTaggedSlots[Random.Range(0, availableTaggedSlots.Count)];
            survivor.spawnIndex = targetSlot;
            int stackIndex = slotOccupancy[targetSlot];
            survivor.transform.position = spawnPoints[targetSlot].position + GetTriangleOffset(stackIndex);
        }
        else
        {
            List<int> emptySlots = new List<int>();
            for (int i = 0; i < slotOccupancy.Length; i++) { if (slotOccupancy[i] == 0) emptySlots.Add(i); }
            if (emptySlots.Count > 0)
            {
                int targetSlot = emptySlots[Random.Range(0, emptySlots.Count)];
                survivor.spawnIndex = targetSlot;
                survivor.transform.position = spawnPoints[targetSlot].position + GetTriangleOffset(0);
            }
        }
        SyncSlotStateFromScene();
        if (PassiveManager.Instance != null) PassiveManager.Instance.RecalculatePassives();
        return true;
    }

    List<PlayerAttack> GetUnitsInSlot(int spawnIndex, string unitTag, int requiredTier = -1)
    {
        List<PlayerAttack> result = new List<PlayerAttack>();
        PlayerAttack[] players = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack player in players)
        {
            if (player == null || player.spawnIndex != spawnIndex) continue;
            string playerTag = !string.IsNullOrWhiteSpace(player.unitTag) ? player.unitTag.Trim() : GetUnitTag(player.characterData);
            if (playerTag != unitTag) continue;
            if (requiredTier > 0) { int playerTier = player.characterData != null ? Mathf.Max(1, player.characterData.tier) : -1; if (playerTier != requiredTier) continue; }
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
        if (slots.Count == 0) tagToSlots.Remove(tag);
        slotDirty = true;
    }

    public void SyncSlotStateFromScene()
    {
        for (int i = 0; i < slotOccupancy.Length; i++) { slotOccupancy[i] = 0; slotTagOwners[i] = null; }
        tagToSlots.Clear();
        PlayerAttack[] players = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack player in players)
        {
            if (player == null) continue;
            int index = player.spawnIndex;
            if (index < 0 || index >= slotOccupancy.Length) continue;
            string tag = !string.IsNullOrWhiteSpace(player.unitTag) ? player.unitTag.Trim() : GetUnitTag(player.characterData);
            if (string.IsNullOrWhiteSpace(tag)) continue;
            slotOccupancy[index]++;
            slotTagOwners[index] = tag;
            if (!tagToSlots.TryGetValue(tag, out List<int> slots)) { slots = new List<int>(); tagToSlots[tag] = slots; }
            if (!slots.Contains(index)) slots.Add(index);
        }
    }

    CharacterData GetRandomCharacterData()
    {
        if (characterDataList == null || characterDataList.Length == 0) return null;
        int unlockedTier = UpgradeManager.Instance != null ? UpgradeManager.Instance.UnlockedTier : 1;
        Dictionary<int, List<CharacterData>> tierMap = new Dictionary<int, List<CharacterData>>();
        foreach (CharacterData data in characterDataList)
        {
            if (data == null) continue;
            int tier = Mathf.Max(1, data.tier);
            if (tier > unlockedTier) continue;
            if (!tierMap.ContainsKey(tier)) tierMap[tier] = new List<CharacterData>();
            tierMap[tier].Add(data);
        }
        float totalWeight = 0f;
        for (int i = 0; i < tierSpawnWeights.Length; i++) { int tier = i + 1; if (tierMap.ContainsKey(tier)) totalWeight += tierSpawnWeights[i]; }
        if (totalWeight <= 0f) return characterDataList[Random.Range(0, characterDataList.Length)];
           float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < tierSpawnWeights.Length; i++)
        {
            int tier = i + 1;
            if (!tierMap.ContainsKey(tier)) continue;
            cumulative += tierSpawnWeights[i];
            if (rand <= cumulative) return tierMap[tier][Random.Range(0, tierMap[tier].Count)];
        }
        return characterDataList[Random.Range(0, characterDataList.Length)];
    }

    bool TryGetSpawnSlot(string unitTag, out int slotIndex)
    {
        if (!tagToSlots.TryGetValue(unitTag, out List<int> slots)) { slots = new List<int>(); tagToSlots[unitTag] = slots; }
        List<int> availableTaggedSlots = new List<int>();
        for (int i = 0; i < slots.Count; i++)
        {
            int existingSlot = slots[i];
            if (existingSlot < 0 || existingSlot >= slotOccupancy.Length) continue;
            if (slotOccupancy[existingSlot] >= maxUnitsPerSlot) continue;
            if (slotOccupancy[existingSlot] == 0) continue;
            availableTaggedSlots.Add(existingSlot);
        }
        if (availableTaggedSlots.Count > 0) { slotIndex = availableTaggedSlots[Random.Range(0, availableTaggedSlots.Count)]; return true; }
        List<int> emptySlots = new List<int>();
        for (int i = 0; i < slotOccupancy.Length; i++) { if (slotOccupancy[i] == 0) emptySlots.Add(i); }
        if (emptySlots.Count > 0) { int randomEmptySlot = emptySlots[Random.Range(0, emptySlots.Count)]; slotTagOwners[randomEmptySlot] = unitTag; if (!slots.Contains(randomEmptySlot)) slots.Add(randomEmptySlot); slotIndex = randomEmptySlot; return true; }
        slotIndex = -1;
        return false;
    }

    public bool IsFieldFull() { if (slotOccupancy == null) return false; for (int i = 0; i < slotOccupancy.Length; i++) { if (slotOccupancy[i] == 0) return false; } return true; }
    public bool IsSlotEmpty(int slotIndex) { if (slotOccupancy == null || slotIndex < 0 || slotIndex >= slotOccupancy.Length) return false; return slotOccupancy[slotIndex] == 0; }

    public void UnregisterUnit(PlayerAttack unit, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotOccupancy.Length) return;
        if (slotOccupancy[slotIndex] > 0) slotOccupancy[slotIndex]--;
        if (slotOccupancy[slotIndex] == 0) slotTagOwners[slotIndex] = null;
        slotDirty = true;
        if (PassiveManager.Instance != null)
            StartCoroutine(RecalculateNextFrame());
    }

    System.Collections.IEnumerator RecalculateNextFrame()
    {
        yield return null;
        if (PassiveManager.Instance != null)
            PassiveManager.Instance.RecalculatePassives();
    }

    public void RegisterUnit(PlayerAttack unit, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotOccupancy.Length) return;
        slotOccupancy[slotIndex]++;
        slotTagOwners[slotIndex] = GetUnitTag(unit.characterData);
        slotDirty = true;
    }

    public Vector3 GetTriangleOffsetPublic(int stackIndex) => GetTriangleOffset(stackIndex);
    void UpdateSpawnButton() { if (spawnButton != null) spawnButton.interactable = !IsFieldFull(); }
    public void ForceUpdateSpawnButton() { UpdateSpawnButton(); }
    Vector3 GetTriangleOffset(int stackIndex)
    {
        if (stackIndex <= 0) return Vector3.up * triangleOffsetY;
        if (stackIndex == 1) return new Vector3(-triangleOffsetX, -triangleOffsetY, 0f);
        if (stackIndex == 2) return new Vector3(triangleOffsetX, -triangleOffsetY, 0f);
        return Vector3.zero;
    }

    string GetUnitTag(CharacterData characterData) { if (characterData == null) return "Unknown"; if (!string.IsNullOrWhiteSpace(characterData.unitTag)) return characterData.unitTag.Trim(); return characterData.characterName; }
}