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

    private readonly List<int> availableIndexes = new List<int>();

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

        for (int i = 0; i < spawnPoints.Length; i++)
            availableIndexes.Add(i);
    }


    public void TrySpawnPlayer()
    {
        if (availableIndexes.Count == 0)
        {
            Debug.Log("[PlayerSpawner] All spawn points are occupied!");
            return;
        }

        if (CoinManager.Instance != null)
        {
            bool success = CoinManager.Instance.SpendCoins(CoinManager.Instance.spawnCost);
            if (!success) return;
        }

        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        int listPos = Random.Range(0, availableIndexes.Count);
        int spawnIndex = availableIndexes[listPos];

        GameObject obj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position, Quaternion.identity);

        PlayerAttack playerAttack = obj.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.spawnIndex = spawnIndex;
            playerAttack.characterData = characterDataList[0];
        }

        PlayerDragMerge dragMerge = obj.GetComponent<PlayerDragMerge>();
        if (dragMerge == null) dragMerge = obj.AddComponent<PlayerDragMerge>();
        dragMerge.SetSpawnIndex(spawnIndex);

        availableIndexes.RemoveAt(listPos);

        Debug.Log($"[PlayerSpawner] Spawned Player at index {spawnIndex}. Remaining slots: {availableIndexes.Count}");
    }

    public void RegisterFreedSlot(int spawnIndex)
    {
        if (spawnIndex < 0) return;
        if (availableIndexes.Contains(spawnIndex)) return;
        availableIndexes.Add(spawnIndex);
    }
}