using System.Collections;
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
    public Button specialSpawnButton;

    [Header("Aura Settings")]
    public GameObject[] auraPrefabs;

    private GameObject[] slotAuras;
    private int[] slotAuraTiers;
    private int[] slotOccupancy;
    private string[] slotTagOwners;
    private readonly Dictionary<string, List<int>> tagToSlots = new Dictionary<string, List<int>>();
    private bool slotDirty = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CharacterData[] loaded = Resources.LoadAll<CharacterData>("CharacterData");
        if (loaded != null && loaded.Length > 0) characterDataList = loaded;
    }

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogError("[PlayerSpawner] No spawn points assigned!"); return; }
        if (playerPrefab == null) { Debug.LogError("[PlayerSpawner] playerPrefab is missing!"); return; }
        if (characterDataList == null || characterDataList.Length == 0) { Debug.LogError("[PlayerSpawner] CharacterData list is empty!"); return; }
        slotOccupancy = new int[spawnPoints.Length];
        slotTagOwners = new string[spawnPoints.Length];
        slotAuras = new GameObject[spawnPoints.Length];
        slotAuraTiers = new int[spawnPoints.Length];
        StartCoroutine(InitButtonNextFrame());
    }

    IEnumerator InitButtonNextFrame()
    {
        yield return null;
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
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(CoinManager.Instance.spawnCost)) return;
        MergeManager.Instance?.HideUnitActionUI();
        SpawnPlayer(spawnIndex, selectedData, unitTag);
    }

    public void TrySpecialSpawn()
    {
        if (IsFieldFull()) return;
        if (SpecialCoinManager.Instance == null) return;
        if (!SpecialCoinManager.Instance.SpendSpecialCoins(specialSpawnCost)) return;
        SyncSlotStateFromScene();
        CharacterData selectedData = GetRandomSpecialCharacterData();
        if (selectedData == null) return;
        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, out int spawnIndex)) return;
        MergeManager.Instance?.HideUnitActionUI();
        SpawnPlayer(spawnIndex, selectedData, unitTag);
    }

    CharacterData GetRandomSpecialCharacterData()
    {
        if (characterDataList == null || characterDataList.Length == 0) return null;
        int unlockedTier = UpgradeManager.Instance != null ? UpgradeManager.Instance.UnlockedTier : 1;
        Debug.Log($"UnlockedTier={unlockedTier}, MinTier={specialSpawnMinTier}, MaxTier={specialSpawnMaxTier}");
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
        GameObject obj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position + GetTriangleOffset(stackIndex), Quaternion.identity);
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

        if (PassiveManager.Instance != null) PassiveManager.Instance.RecalculatePassives();
        UpdateSpawnButton();
        UpdateSlotAura(spawnIndex, characterData.tier);

        // 다음 프레임에 슬롯메이트 캐시 갱신 (새 유닛 등록 완료 후)
        StartCoroutine(MarkSlotMatesDirtyNextFrame(spawnIndex));
    }

    IEnumerator MarkSlotMatesDirtyNextFrame(int spawnIndex)
    {
        yield return null;
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
            if (unit != null && unit.spawnIndex == spawnIndex)
                unit.MarkSlotMatesDirty();
    }

    public void UpdateSlotAura(int slotIndex, int tier)
    {
        if (auraPrefabs == null || auraPrefabs.Length == 0) return;
        int prefabIndex = Mathf.Clamp(tier - 1, 0, auraPrefabs.Length - 1);
        GameObject auraPrefab = auraPrefabs[prefabIndex];
        if (auraPrefab == null) return;

        // 같은 티어면 위치만 슬롯으로 복귀
        if (slotAuras[slotIndex] != null && slotAuraTiers[slotIndex] == tier)
        {
            slotAuras[slotIndex].transform.position = spawnPoints[slotIndex].position;
            return;
        }

        if (slotAuras[slotIndex] != null) Destroy(slotAuras[slotIndex]);
        GameObject aura = Instantiate(auraPrefab, spawnPoints[slotIndex].position, Quaternion.identity);
        slotAuras[slotIndex] = aura;
        slotAuraTiers[slotIndex] = tier;
    }

    public void RemoveSlotAura(int slotIndex)
    {
        if (slotAuras == null || slotIndex < 0 || slotIndex >= slotAuras.Length) return;
        if (slotAuras[slotIndex] != null)
        {
            Destroy(slotAuras[slotIndex]);
            slotAuras[slotIndex] = null;
            slotAuraTiers[slotIndex] = 0;
        }
    }

    public GameObject GetSlotAura(int slotIndex)
    {
        if (slotAuras == null || slotIndex < 0 || slotIndex >= slotAuras.Length) return null;
        return slotAuras[slotIndex];
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
        foreach (PlayerAttack unit in unitsInSlot)
        {
            if (unit.characterData == null) continue;
            currentTier = Mathf.Max(1, unit.characterData.tier);
            break;
        }
        if (currentTier < 0) return null;
        int targetTier = currentTier + 1;
        List<CharacterData> candidates = new List<CharacterData>();
        foreach (CharacterData data in characterDataList)
        {
            if (data == null) continue;
            if (Mathf.Max(1, data.tier) == targetTier) candidates.Add(data);
        }
        if (candidates.Count == 0) { Debug.LogWarning($"[PlayerSpawner] Merge failed: no data for tier={targetTier}"); return null; }
        return candidates[Random.Range(0, candidates.Count)];
    }

    public bool CanManualMerge(int spawnIndex, string unitTag, CharacterData selectedData)
    {
        SyncSlotStateFromScene();
        int selectedTier = selectedData != null ? Mathf.Max(1, selectedData.tier) : -1;
        return GetUnitsInSlot(spawnIndex, unitTag, selectedTier).Count >= maxUnitsPerSlot;
    }

    public bool TryManualMerge(int spawnIndex, string unitTag, CharacterData selectedData)
    {
        SyncSlotStateFromScene();
        int selectedTier = selectedData != null ? Mathf.Max(1, selectedData.tier) : -1;
        List<PlayerAttack> sameTagUnits = GetUnitsInSlot(spawnIndex, unitTag, selectedTier);
        if (sameTagUnits.Count < maxUnitsPerSlot) return false;

        PlayerAttack survivor = sameTagUnits[0];
        CharacterData mergedData = GetRandomNextLevelCharacterData(sameTagUnits);
        if (mergedData == null) return false;

        for (int i = 1; i < maxUnitsPerSlot; i++) { sameTagUnits[i].gameObject.SetActive(false); Destroy(sameTagUnits[i].gameObject); }

        survivor.ApplyCharacterData(mergedData);
        survivor.isLeader = true;
        string newUnitTag = GetUnitTag(mergedData);
        survivor.unitTag = newUnitTag;
        survivor.spawnIndex = -1;
        SyncSlotStateFromScene();

        if (!tagToSlots.TryGetValue(newUnitTag, out List<int> existingSlots)) existingSlots = new List<int>();
        List<int> availableTaggedSlots = new List<int>();
        foreach (int s in existingSlots)
        {
            if (s < 0 || s >= slotOccupancy.Length) continue;
            if (slotOccupancy[s] < maxUnitsPerSlot) availableTaggedSlots.Add(s);
        }

        int finalSlot = -1;
        if (availableTaggedSlots.Count > 0)
        {
            finalSlot = availableTaggedSlots[Random.Range(0, availableTaggedSlots.Count)];
            survivor.spawnIndex = finalSlot;
            survivor.transform.position = spawnPoints[finalSlot].position + GetTriangleOffset(slotOccupancy[finalSlot]);
        }
        else
        {
            List<int> emptySlots = new List<int>();
            for (int i = 0; i < slotOccupancy.Length; i++) { if (slotOccupancy[i] == 0) emptySlots.Add(i); }
            if (emptySlots.Count > 0)
            {
                finalSlot = emptySlots[Random.Range(0, emptySlots.Count)];
                survivor.spawnIndex = finalSlot;
                survivor.transform.position = spawnPoints[finalSlot].position + GetTriangleOffset(0);
            }
        }

        SyncSlotStateFromScene();
        if (finalSlot >= 0) UpdateSlotAura(finalSlot, mergedData.tier);
        if (slotOccupancy[spawnIndex] == 0) RemoveSlotAura(spawnIndex);

        if (finalSlot >= 0) StartCoroutine(MarkSlotMatesDirtyNextFrame(finalSlot));

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
        if (slots.Count == 0) tagToSlots.Remove(tag);
        slotDirty = true;
    }

    public void SyncSlotStateFromScene()
    {
        for (int i = 0; i < slotOccupancy.Length; i++) { slotOccupancy[i] = 0; slotTagOwners[i] = null; }
        tagToSlots.Clear();

        PlayerAttack[] players = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);

        // 슬롯별 유닛 그룹화
        Dictionary<int, List<PlayerAttack>> slotGroups = new Dictionary<int, List<PlayerAttack>>();
        foreach (PlayerAttack player in players)
        {
            if (player == null) continue;
            int index = player.spawnIndex;
            if (index < 0 || index >= slotOccupancy.Length) continue;
            if (!slotGroups.ContainsKey(index)) slotGroups[index] = new List<PlayerAttack>();
            slotGroups[index].Add(player);
        }

        // 슬롯별로 Y값 내림차순 정렬 후 첫 번째만 리더
        foreach (var kv in slotGroups)
        {
            int index = kv.Key;
            List<PlayerAttack> unitList = kv.Value;
            unitList.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

            for (int i = 0; i < unitList.Count; i++)
            {
                PlayerAttack player = unitList[i];
                player.isLeader = (i == 0);

                string tag = !string.IsNullOrWhiteSpace(player.unitTag) ? player.unitTag.Trim() : GetUnitTag(player.characterData);
                if (string.IsNullOrWhiteSpace(tag)) continue;
                slotOccupancy[index]++;
                slotTagOwners[index] = tag;
                if (!tagToSlots.TryGetValue(tag, out List<int> slots)) { slots = new List<int>(); tagToSlots[tag] = slots; }
                if (!slots.Contains(index)) slots.Add(index);
            }
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
        for (int i = 0; i < tierSpawnWeights.Length; i++) { if (tierMap.ContainsKey(i + 1)) totalWeight += tierSpawnWeights[i]; }
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
        foreach (int existingSlot in slots)
        {
            if (existingSlot < 0 || existingSlot >= slotOccupancy.Length) continue;
            if (slotOccupancy[existingSlot] >= maxUnitsPerSlot || slotOccupancy[existingSlot] == 0) continue;
            availableTaggedSlots.Add(existingSlot);
        }
        if (availableTaggedSlots.Count > 0) { slotIndex = availableTaggedSlots[Random.Range(0, availableTaggedSlots.Count)]; return true; }
        List<int> emptySlots = new List<int>();
        for (int i = 0; i < slotOccupancy.Length; i++) { if (slotOccupancy[i] == 0) emptySlots.Add(i); }
        if (emptySlots.Count > 0)
        {
            int randomEmptySlot = emptySlots[Random.Range(0, emptySlots.Count)];
            slotTagOwners[randomEmptySlot] = unitTag;
            if (!slots.Contains(randomEmptySlot)) slots.Add(randomEmptySlot);
            slotIndex = randomEmptySlot;
            return true;
        }
        slotIndex = -1;
        return false;
    }

    public bool IsFieldFull() { if (slotOccupancy == null) return false; foreach (int o in slotOccupancy) { if (o == 0) return false; } return true; }
    public bool IsSlotEmpty(int slotIndex) { if (slotOccupancy == null || slotIndex < 0 || slotIndex >= slotOccupancy.Length) return false; return slotOccupancy[slotIndex] == 0; }

    public void UnregisterUnit(PlayerAttack unit, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotOccupancy.Length) return;
        if (slotOccupancy[slotIndex] > 0) slotOccupancy[slotIndex]--;
        if (slotOccupancy[slotIndex] == 0) { slotTagOwners[slotIndex] = null; RemoveSlotAura(slotIndex); }
        slotDirty = true;
        StartCoroutine(RecalculateNextFrame());
    }

    IEnumerator RecalculateNextFrame()
    {
        yield return null;
        if (PassiveManager.Instance != null) PassiveManager.Instance.RecalculatePassives();
    }

    public void RegisterUnit(PlayerAttack unit, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotOccupancy.Length) return;
        slotOccupancy[slotIndex]++;
        slotTagOwners[slotIndex] = GetUnitTag(unit.characterData);
        slotDirty = true;
        if (unit.characterData != null) UpdateSlotAura(slotIndex, unit.characterData.tier);
    }

    public Vector3 GetTriangleOffsetPublic(int stackIndex) => GetTriangleOffset(stackIndex);
    public void ForceUpdateSpawnButton() => UpdateSpawnButton();

    void UpdateSpawnButton()
    {
        bool fieldFull = IsFieldFull();
        if (spawnButton != null) spawnButton.interactable = !fieldFull;
        if (specialSpawnButton != null)
            specialSpawnButton.interactable = !fieldFull &&
                SpecialCoinManager.Instance != null &&
                SpecialCoinManager.Instance.GetSpecialCoins() >= specialSpawnCost;
    }

    Vector3 GetTriangleOffset(int stackIndex)
    {
        if (stackIndex <= 0) return Vector3.up * triangleOffsetY;
        if (stackIndex == 1) return new Vector3(-triangleOffsetX, -triangleOffsetY, 0f);
        if (stackIndex == 2) return new Vector3(triangleOffsetX, -triangleOffsetY, 0f);
        return Vector3.zero;
    }

    string GetUnitTag(CharacterData data)
    {
        if (data == null) return "Unknown";
        return !string.IsNullOrWhiteSpace(data.unitTag) ? data.unitTag.Trim() : data.characterName;
    }
}