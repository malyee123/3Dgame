using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerDragMerge : MonoBehaviour
{
    private const bool dragEnabled = true;
    private Camera mainCamera;
    private bool isDragging;
    private Vector3 originalPosition;
    private int originalSlotIndex = -1;

    private PlayerAttack playerAttack;

    private List<Transform> slotMates = new List<Transform>();
    private List<Vector3> slotMateOriginalPositions = new List<Vector3>();
    private List<Vector3> slotMateOffsets = new List<Vector3>();

    void Awake()
    {
        mainCamera = Camera.main;
        playerAttack = GetComponent<PlayerAttack>();
    }

    public void SetSpawnIndex(int index)
    {
        originalSlotIndex = index;
    }

    void OnMouseDown()
    {
        if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
        if (!dragEnabled) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        isDragging = true;
        originalPosition = transform.position;

        if (playerAttack != null)
            originalSlotIndex = playerAttack.spawnIndex;

        CollectSlotMates();
    }

    void OnMouseDrag()
    {
        if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
        if (!isDragging || mainCamera == null) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = mouseWorld;

        for (int i = 0; i < slotMates.Count; i++)
        {
            if (slotMates[i] == null) continue;
            slotMates[i].position = mouseWorld + slotMateOffsets[i];
        }
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        int targetSlot = FindNearestSlot();

        if (targetSlot >= 0 && targetSlot != originalSlotIndex && CanMoveToSlot(targetSlot))
        {
            MoveToSlot(targetSlot);
        }
        else
        {
            transform.position = originalPosition;
            for (int i = 0; i < slotMates.Count; i++)
            {
                if (slotMates[i] == null) continue;
                slotMates[i].position = slotMateOriginalPositions[i];
            }
        }

        slotMates.Clear();
        slotMateOriginalPositions.Clear();
        slotMateOffsets.Clear();
    }

    void CollectSlotMates()
    {
        slotMates.Clear();
        slotMateOriginalPositions.Clear();
        slotMateOffsets.Clear();

        if (originalSlotIndex < 0) return;

        PlayerAttack[] allPlayers = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack p in allPlayers)
        {
            if (p == null || p == playerAttack) continue;
            if (p.spawnIndex != originalSlotIndex) continue;

            slotMates.Add(p.transform);
            slotMateOriginalPositions.Add(p.transform.position);
            slotMateOffsets.Add(p.transform.position - transform.position);
        }
    }

    int FindNearestSlot()
    {
        if (PlayerSpawner.Instance == null) return -1;

        Transform[] spawnPoints = PlayerSpawner.Instance.spawnPoints;
        if (spawnPoints == null || spawnPoints.Length == 0) return -1;

        int nearestIndex = -1;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;
            float dist = Vector2.Distance(transform.position, spawnPoints[i].position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestIndex = i;
            }
        }

        return nearestDist <= 1.0f ? nearestIndex : -1;
    }

    bool CanMoveToSlot(int slotIndex)
    {
        if (PlayerSpawner.Instance == null) return false;
        return PlayerSpawner.Instance.IsSlotEmpty(slotIndex);
    }

    void MoveToSlot(int targetSlot)
    {
        if (PlayerSpawner.Instance == null || playerAttack == null) return;

        PlayerSpawner.Instance.UnregisterUnit(playerAttack, originalSlotIndex);
        PlayerSpawner.Instance.RegisterUnit(playerAttack, targetSlot);

        for (int i = 0; i < slotMates.Count; i++)
        {
            if (slotMates[i] == null) continue;
            PlayerAttack mate = slotMates[i].GetComponent<PlayerAttack>();
            if (mate != null) mate.spawnIndex = targetSlot;

            Vector3 newPos = PlayerSpawner.Instance.spawnPoints[targetSlot].position
                             + PlayerSpawner.Instance.GetTriangleOffsetPublic(i + 1);
            slotMates[i].position = newPos;
        }

        Vector3 myNewPos = PlayerSpawner.Instance.spawnPoints[targetSlot].position
                           + PlayerSpawner.Instance.GetTriangleOffsetPublic(0);
        transform.position = myNewPos;

        playerAttack.spawnIndex = targetSlot;
        originalSlotIndex = targetSlot;
        originalPosition = myNewPos;
    }
}       