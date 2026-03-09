using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerPrefab;

    [Header("Spawn Point Settings")]
    public Transform[] spawnPoints;

    private List<int> availableIndexes = new List<int>();

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] No spawn points found. Assign spawn points in Inspector.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab is missing. Assign a prefab in Inspector.");
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            availableIndexes.Add(i);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        if (availableIndexes.Count == 0)
        {
            Debug.Log("[PlayerSpawner] All spawn points are occupied.");
            return;
        }

        int listPos = Random.Range(0, availableIndexes.Count);
        int spawnIndex = availableIndexes[listPos];

        GameObject obj = Instantiate(playerPrefab, spawnPoints[spawnIndex].position, Quaternion.identity);

        PlayerAttack playerAttack = obj.GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.spawnIndex = spawnIndex;
        }

        availableIndexes.RemoveAt(listPos);

        Debug.Log($"[PlayerSpawner] Spawned Player at index {spawnIndex}, position {spawnPoints[spawnIndex].position}. Remaining slots: {availableIndexes.Count}");
    }
}
