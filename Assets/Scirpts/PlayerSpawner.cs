using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [HideInInspector] public float sellPriceMultiplier = 1f;
    [HideInInspector] public float luckyDayMultiplier = 1f;

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
        if (slotOccupancy == null || slotOccupancy.Length == 0) return;
        SyncSlotStateFromScene();
        if (!HasAvailableSlot()) return;
        // 일반 소환 - 운빨 좋은 날 적용된 확률로 캐릭터 선택
        CharacterData selectedData = GetRandomCharacterData();
        if (selectedData == null) return;
        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, selectedData.tier, out int spawnIndex)) return;
        if (CoinManager.Instance != null && !CoinManager.Instance.SpendCoins(CoinManager.Instance.spawnCost)) return;
        MergeManager.Instance?.HideUnitActionUI();
        SpawnPlayer(spawnIndex, selectedData, unitTag);
    }

    public void TrySpecialSpawn()
    {
        SyncSlotStateFromScene();
        if (!HasAvailableSlot()) return;
        if (SpecialCoinManager.Instance == null) return;
        if (!SpecialCoinManager.Instance.SpendSpecialCoins(specialSpawnCost)) return;
        // 특수 소환 - 운빨 좋은 날 미적용
        CharacterData selectedData = GetRandomSpecialCharacterData();
        if (selectedData == null) return;
        string unitTag = GetUnitTag(selectedData);
        if (!TryGetSpawnSlot(unitTag, selectedData.tier, out int spawnIndex)) return;
        MergeManager.Instance?.HideUnitActionUI();
        SpawnPlayer(spawnIndex, selectedData, unitTag);
    }

    public void SpawnSpecificCharacter(CharacterData characterData)
    {
        if (characterData == null) return;
        SyncSlotStateFromScene();
        string unitTag = GetUnitTag(characterData);
        if (!TryGetSpawnSlot(unitTag, characterData.tier, out int spawnIndex)) return;
        SpawnPlayer(spawnIndex, characterData, unitTag);
    }

    public void SpawnRareUnits(int count)
    {
        List<CharacterData> rareList = new List<CharacterData>();
        foreach (CharacterData data in characterDataList)
            if (data != null && data.tier == 3) rareList.Add(data);
        if (rareList.Count == 0) return;
        for (int i = 0; i < count; i++)
        {
            SyncSlotStateFromScene();
            if (!HasAvailableSlot()) break;
            CharacterData selected = rareList[Random.Range(0, rareList.Count)];
            string unitTag = GetUnitTag(selected);
            if (!TryGetSpawnSlot(unitTag, selected.tier, out int spawnIndex)) break;
            SpawnPlayer(spawnIndex, selected, unitTag);
        }
    }

    bool HasAvailableSlot()
    {
        for (int i = 0; i < slotOccupancy.Length; i++)
            if (slotOccupancy[i] == 0) return true;
        return false;
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

    // 일반 소환 캐릭터 선택 - 운빨 좋은 날 적용 시 2티어 가중치 1.5배
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

        // 운빨 좋은 날 - 일반 소환 2티어 가중치에만 적용
        float[] weights = (float[])tierSpawnWeights.Clone();
        if (AugmentManager.Instance != null && AugmentManager.Instance.HasLuckyDay && weights.Length >= 2)
            weights[1] *= luckyDayMultiplier;

        float totalWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
            if (tierMap.ContainsKey(i + 1)) totalWeight += weights[i];
        if (totalWeight <= 0f) return characterDataList[Random.Range(0, characterDataList.Length)];

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            int tier = i + 1;
            if (!tierMap.ContainsKey(tier)) continue;
            cumulative += weights[i];
            if (rand <= cumulative) return tierMap[tier][Random.Range(0, tierMap[tier].Count)];
        }
        return characterDataList[Random.Range(0, characterDataList.Length)];
    }

    void SpawnPlayer(int spawnIndex, CharacterData characterData, string unitTag)
    {
        int stackIndex = slotOccupancy[spawnIndex];
        Vector3 offset = characterData.tier >= 5 ? Vector3.zero : GetTriangleOffset(stackIndex);
        GameObject obj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position + offset, Quaternion.identity);
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
        if (!tagToSlots.TryGetValue(unitTag, out List<int> slots)) { slots = new List<int>(); tagToSlots[unitTag] = slots; }
        if (!slots.Contains(spawnIndex)) slots.Add(spawnIndex);
        slotDirty = true;
        if (PassiveManager.Instance != null) PassiveManager.Instance.RecalculatePassives();
        UpdateSpawnButton();
        UpdateSlotAura(spawnIndex, characterData.tier);
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
        if (slotAuras[slotIndex] != null) Destroy(slotAuras[slotIndex]);
        if (auraPrefabs[prefabIndex] == null) return;
        slotAuras[slotIndex] = Instantiate(auraPrefabs[prefabIndex], spawnPoints[slotIndex].position, auraPrefabs[prefabIndex].transform.rotation);
        slotAuraTiers[slotIndex] = tier;
    }

    public void RemoveSlotAura(int slotIndex)
    {
        if (slotAuras == null || slotIndex < 0 || slotIndex >= slotAuras.Length) return;
        if (slotAuras[slotIndex] != null) { Destroy(slotAuras[slotIndex]); slotAuras[slotIndex] = null; }
        slotAuraTiers[slotIndex] = 0;
    }

    public GameObject GetSlotAura(int slotIndex)
    {
        if (slotAuras == null || slotIndex < 0 || slotIndex >= slotAuras.Length) return null;
        return slotAuras[slotIndex];
    }

    public bool CanManualMerge(int spawnIndex, string unitTag, CharacterData characterData)
    {
        if (characterData == null) return false;
        int tier = Mathf.Max(1, characterData.tier);
        int cost = characterData.upgradeCost;
        bool tierAllowed = UpgradeManager.Instance == null || (tier + 1) <= UpgradeManager.Instance.UnlockedTier;
        bool hasCoins = CoinManager.Instance != null && CoinManager.Instance.GetCoins() >= cost;
        List<PlayerAttack> slotUnits = GetUnitsInSlot(spawnIndex, unitTag, tier);
        bool hasEnough = slotUnits.Count >= 3;
        return tierAllowed && hasCoins && hasEnough;
    }

    public bool TryManualMerge(int spawnIndex, string unitTag, CharacterData characterData)
    {
        if (characterData == null) return false;
        int tier = Mathf.Max(1, characterData.tier);
        List<PlayerAttack> slotUnits = GetUnitsInSlot(spawnIndex, unitTag, tier);
        if (slotUnits.Count < 3) return false;

        List<PlayerAttack> toRemove = slotUnits.GetRange(0, 3);
        foreach (PlayerAttack unit in toRemove) { UnregisterUnit(unit, spawnIndex); Destroy(unit.gameObject); }

        int unlockedTier = UpgradeManager.Instance != null ? UpgradeManager.Instance.UnlockedTier : 1;
        List<CharacterData> nextTierList = new List<CharacterData>();
        foreach (CharacterData data in characterDataList)
            if (data != null && data.tier == tier + 1 && data.tier <= unlockedTier) nextTierList.Add(data);

        if (nextTierList.Count == 0) return false;
        CharacterData nextData = nextTierList[Random.Range(0, nextTierList.Count)];
        string newTag = GetUnitTag(nextData);

        StartCoroutine(MergeSpawnNextFrame(spawnIndex, nextData, newTag));
        return true;
    }

    IEnumerator MergeSpawnNextFrame(int originalSlot, CharacterData nextData, string newTag)
    {
        yield return null;
        SyncSlotStateFromScene();

        int finalSlot = -1;
        int maxUnits = nextData.tier >= 5 ? 1 : maxUnitsPerSlot;

        if (tagToSlots.TryGetValue(newTag, out List<int> existingSlots))
            foreach (int s in existingSlots)
                if (slotOccupancy[s] > 0 && slotOccupancy[s] < maxUnits) { finalSlot = s; break; }

        if (finalSlot < 0)
        {
            List<int> emptySlots = new List<int>();
            for (int i = 0; i < slotOccupancy.Length; i++)
                if (slotOccupancy[i] == 0) emptySlots.Add(i);
            if (emptySlots.Count > 0)
                finalSlot = emptySlots[Random.Range(0, emptySlots.Count)];
        }

        if (finalSlot < 0) yield break;

        SpawnPlayer(finalSlot, nextData, newTag);
        StartCoroutine(MarkSlotMatesDirtyNextFrame(finalSlot));
        if (PassiveManager.Instance != null) PassiveManager.Instance.RecalculatePassives();
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
        Dictionary<int, List<PlayerAttack>> slotGroups = new Dictionary<int, List<PlayerAttack>>();
        foreach (PlayerAttack player in players)
        {
            if (player == null) continue;
            int index = player.spawnIndex;
            if (index < 0 || index >= slotOccupancy.Length) continue;
            if (!slotGroups.ContainsKey(index)) slotGroups[index] = new List<PlayerAttack>();
            slotGroups[index].Add(player);
        }

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

    bool TryGetSpawnSlot(string unitTag, int tier, out int slotIndex)
    {
        if (!tagToSlots.TryGetValue(unitTag, out List<int> slots)) { slots = new List<int>(); tagToSlots[unitTag] = slots; }
        int maxUnits = tier >= 5 ? 1 : maxUnitsPerSlot;

        List<int> availableTaggedSlots = new List<int>();
        foreach (int existingSlot in slots)
        {
            if (existingSlot < 0 || existingSlot >= slotOccupancy.Length) continue;
            if (slotOccupancy[existingSlot] <= 0 || slotOccupancy[existingSlot] >= maxUnits) continue;
            availableTaggedSlots.Add(existingSlot);
        }
        if (availableTaggedSlots.Count > 0)
        {
            slotIndex = availableTaggedSlots[Random.Range(0, availableTaggedSlots.Count)];
            return true;
        }

        List<int> emptySlots = new List<int>();
        for (int i = 0; i < slotOccupancy.Length; i++)
            if (slotOccupancy[i] == 0) emptySlots.Add(i);

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

    public bool IsFieldFull()
    {
        if (slotOccupancy == null) return false;
        return !HasAvailableSlot();
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotOccupancy == null || slotIndex < 0 || slotIndex >= slotOccupancy.Length) return false;
        return slotOccupancy[slotIndex] == 0;
    }

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
        if (!tagToSlots.TryGetValue(slotTagOwners[slotIndex], out List<int> slots)) { slots = new List<int>(); tagToSlots[slotTagOwners[slotIndex]] = slots; }
        if (!slots.Contains(slotIndex)) slots.Add(slotIndex);
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