using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Collider2D))]
public class PlayerDragMerge : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragSmoothSpeed = 15f;

    private Camera mainCamera;
    private bool isDragging;
    private bool isSelected;
    private Vector3 originalPosition;
    private int originalSlotIndex = -1;
    private Vector3 targetDragPosition;
    private PlayerAttack playerAttack;
    private GameObject currentAura;

    private List<Transform> slotMates = new List<Transform>();
    private List<Vector3> slotMateOriginalPositions = new List<Vector3>();
    private List<Vector3> slotMateOffsets = new List<Vector3>();

    void Awake()
    {
        mainCamera = Camera.main;
        playerAttack = GetComponent<PlayerAttack>();
    }

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        if (playerAttack != null) originalSlotIndex = playerAttack.spawnIndex;

        if (MergeManager.Instance != null && MergeManager.Instance.IsUnitActionUIActive())
        {
            if (MergeManager.Instance.IsSelectedSlot(originalSlotIndex))
            {
                isSelected = true;
                originalPosition = transform.position;
                targetDragPosition = transform.position;
                StartDrag();
            }
            else
            {
                MergeManager.Instance.HideUnitActionUI();
                ShowPanel();
            }
        }
        else
        {
            ShowPanel();
        }
    }

    void OnMouseUp()
    {
        if (!isSelected) return;
        if (isDragging) EndDrag();
        isSelected = false;
    }

    void Update()
    {
        if (!isDragging) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        targetDragPosition = mouseWorld;
        transform.position = Vector3.Lerp(transform.position, targetDragPosition, Time.deltaTime * dragSmoothSpeed);

        // 오라도 같이 이동
        if (currentAura != null)
            currentAura.transform.position = Vector3.Lerp(currentAura.transform.position, targetDragPosition, Time.deltaTime * dragSmoothSpeed);

        for (int i = 0; i < slotMates.Count; i++)
        {
            if (slotMates[i] == null) continue;
            slotMates[i].position = Vector3.Lerp(slotMates[i].position, targetDragPosition + slotMateOffsets[i], Time.deltaTime * dragSmoothSpeed);
        }
    }

    void StartDrag()
    {
        isDragging = true;
        if (playerAttack != null) playerAttack.SetDragging(true);
        CollectSlotMates();
        foreach (Transform mate in slotMates)
        {
            PlayerAttack mateAttack = mate.GetComponent<PlayerAttack>();
            if (mateAttack != null) mateAttack.SetDragging(true);
        }

        // 드래그 시작 시 오라 참조
        if (PlayerSpawner.Instance != null)
            currentAura = PlayerSpawner.Instance.GetSlotAura(originalSlotIndex);
    }

    void EndDrag()
    {
        isDragging = false;
        currentAura = null;
        if (playerAttack != null) playerAttack.SetDragging(false);
        foreach (Transform mate in slotMates)
        {
            PlayerAttack mateAttack = mate.GetComponent<PlayerAttack>();
            if (mateAttack != null) mateAttack.SetDragging(false);
        }

        int targetSlot = FindNearestSlot();
        if (targetSlot >= 0 && targetSlot != originalSlotIndex)
        {
            if (PlayerSpawner.Instance.IsSlotEmpty(targetSlot)) MoveToSlot(targetSlot);
            else SwapSlots(targetSlot);
        }
        else
        {
            // 이동 실패 시 원래 위치로 복귀
            transform.position = originalPosition;
            for (int i = 0; i < slotMates.Count; i++)
            {
                if (slotMates[i] == null) continue;
                slotMates[i].position = slotMateOriginalPositions[i];
            }
            // 오라도 원래 슬롯 위치로 복귀
            if (PlayerSpawner.Instance != null)
                PlayerSpawner.Instance.UpdateSlotAura(originalSlotIndex, playerAttack?.characterData?.tier ?? 1);
        }

        slotMates.Clear();
        slotMateOriginalPositions.Clear();
        slotMateOffsets.Clear();
    }

    void SwapSlots(int targetSlot)
    {
        if (PlayerSpawner.Instance == null || playerAttack == null) return;
        PlayerAttack[] allPlayers = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        List<PlayerAttack> targetUnits = new List<PlayerAttack>();
        List<PlayerAttack> myUnits = new List<PlayerAttack>();
        myUnits.Add(playerAttack);

        foreach (Transform mate in slotMates)
        {
            if (mate == null) continue;
            PlayerAttack mateAttack = mate.GetComponent<PlayerAttack>();
            if (mateAttack != null) myUnits.Add(mateAttack);
        }
        foreach (PlayerAttack p in allPlayers)
        {
            if (p == null) continue;
            if (p.spawnIndex == targetSlot) targetUnits.Add(p);
        }

        for (int i = 0; i < targetUnits.Count; i++)
        {
            targetUnits[i].spawnIndex = originalSlotIndex;
            targetUnits[i].transform.position = PlayerSpawner.Instance.spawnPoints[originalSlotIndex].position + PlayerSpawner.Instance.GetTriangleOffsetPublic(i);
        }
        for (int i = 0; i < myUnits.Count; i++)
        {
            myUnits[i].spawnIndex = targetSlot;
            myUnits[i].transform.position = PlayerSpawner.Instance.spawnPoints[targetSlot].position + PlayerSpawner.Instance.GetTriangleOffsetPublic(i);
        }

        foreach (PlayerAttack p in targetUnits) p.isLeader = false;
        foreach (PlayerAttack p in myUnits) p.isLeader = false;
        if (targetUnits.Count > 0) targetUnits[0].isLeader = true;
        if (myUnits.Count > 0) myUnits[0].isLeader = true;

        // 스왑 후 양쪽 슬롯 오라 강제 교체 (같은 티어여도 위치 갱신 위해 제거 후 재생성)
        PlayerSpawner.Instance.RemoveSlotAura(targetSlot);
        PlayerSpawner.Instance.RemoveSlotAura(originalSlotIndex);

        if (myUnits.Count > 0 && myUnits[0].characterData != null)
            PlayerSpawner.Instance.UpdateSlotAura(targetSlot, myUnits[0].characterData.tier);
        if (targetUnits.Count > 0 && targetUnits[0].characterData != null)
            PlayerSpawner.Instance.UpdateSlotAura(originalSlotIndex, targetUnits[0].characterData.tier);

        PlayerSpawner.Instance.SyncSlotStateFromScene();
    }

    void ShowPanel()
    {
        if (MergeManager.Instance == null) return;
        if (playerAttack != null && playerAttack.isLeader) { MergeManager.Instance.SelectUnit(playerAttack); return; }
        PlayerAttack[] allUnits = FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None);
        foreach (PlayerAttack unit in allUnits)
        {
            if (unit.spawnIndex == originalSlotIndex && unit.isLeader)
            {
                MergeManager.Instance.SelectUnit(unit);
                return;
            }
        }
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
            if (dist < nearestDist) { nearestDist = dist; nearestIndex = i; }
        }
        return nearestDist <= 1.0f ? nearestIndex : -1;
    }

    void MoveToSlot(int targetSlot)
    {
        if (PlayerSpawner.Instance == null || playerAttack == null) return;
        PlayerSpawner.Instance.UnregisterUnit(playerAttack, originalSlotIndex);
        PlayerSpawner.Instance.RemoveSlotAura(originalSlotIndex);
        PlayerSpawner.Instance.RegisterUnit(playerAttack, targetSlot);

        for (int i = 0; i < slotMates.Count; i++)
        {
            if (slotMates[i] == null) continue;
            PlayerAttack mate = slotMates[i].GetComponent<PlayerAttack>();
            if (mate != null) mate.spawnIndex = targetSlot;
            slotMates[i].position = PlayerSpawner.Instance.spawnPoints[targetSlot].position + PlayerSpawner.Instance.GetTriangleOffsetPublic(i + 1);
        }

        transform.position = PlayerSpawner.Instance.spawnPoints[targetSlot].position + PlayerSpawner.Instance.GetTriangleOffsetPublic(0);
        playerAttack.spawnIndex = targetSlot;
        originalSlotIndex = targetSlot;
        originalPosition = transform.position;
    }
}