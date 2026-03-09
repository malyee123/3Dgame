// PlayerSpawner.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// V키 입력 시 5x5 스폰 포인트 중 아직 사용되지 않은 위치에 Player를 생성하는 스크립트
/// 빈 게임오브젝트에 붙여서 사용
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player 설정")]
    public GameObject playerPrefab;

    [Header("스폰 포인트 설정")]
    public Transform[] spawnPoints;

    private List<int> availableIndexes = new List<int>();

    void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] 스폰 포인트가 없습니다! Inspector에서 연결해주세요.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab이 없습니다! Inspector에서 연결해주세요.");
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
            Debug.Log("[PlayerSpawner] 모든 스폰 포인트가 가득 찼습니다! (25/25)");
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

        Debug.Log($"[PlayerSpawner] {spawnIndex}번 위치 {spawnPoints[spawnIndex].position}에 Player 생성! 남은 슬롯: {availableIndexes.Count}/25");
    }
}