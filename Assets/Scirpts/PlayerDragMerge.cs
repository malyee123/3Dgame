using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class PlayerDragMerge : MonoBehaviour
{
    [Header("Hold Settings")]
    public float holdThreshold = 0.2f; // Inspector에서 변경 가능

    [Header("Drag Settings")]
    public float dragSmoothSpeed = 15f; // 드래그 부드러움 (높을수록 빠르게 따라옴)

    private bool dragEnabled = true;
    private Camera mainCamera;
    private bool isDragging;
    private bool isHolding;
    private float holdTimer;
    private Vector3 originalPosition;
    private int originalSlotIndex = -1;
    private Vector3 targetDragPosition;

    private PlayerAttack playerAttack;

    private List<Transform> slotMates = new List<Transform>();
    private List<Vector3> slotMateOriginalPositions = new List<Vector3>();
    private List<Vector3> slotMateOffsets = new List<Vector3>();

    void Awake()
    {
        mainCamera = Camera.main;
        playerAttack = GetComponent<PlayerAttack>();
    }

    public void SetSpawnIndex(int index) { originalSlotIndex = index; }

    void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        if (!dragEnabled) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        isHolding = true;
        holdTimer = 0f;
        originalPosition = transform.position;
        targetDragPosition = transform.position;

        if (playerAttack != null)
            originalSlotIndex = playerAttack.spawnIndex;
    }

    void OnMouseUp()
    {
        if (!isHolding) return;

        if (!isDragging)
        {
            ShowPanel();
        }
        else
        {
            EndDrag();
        }

        isHolding = false;
        holdTimer = 0f;
    }

    void Update()
    {
        if (!isHolding) return;

        holdTimer += Time.deltaTime;

        // holdThreshold 초 이상 누르면 드래그 시작
        if (!isDragging && holdTimer >= holdThreshold)
        {
            StartDrag();
        }

        if (isDragging)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (RecipeBook.Instance != null && RecipeBook.Instance.IsPanelOpen) return;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

            // 마우스 위치 계산
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = transform.position.z;
            targetDragPosition = mouseWorld;

            // Lerp로 부드럽게 따라가기
            transform.position = Vector3.Lerp(transform.position, targetDragPosition, Time.deltaTime * dragSmoothSpeed);

            // 슬롯메이트도 함께 이동
            for (int i = 0; i < slotMates.Count; i++)
            {
                if (slotMates[i] == null) continue;
                Vector3 mateTarget = targetDragPosition + slotMateOffsets[i];
                slotMates[i].position = Vector3.Lerp(slotMates[i].position, mateTarget, Time.deltaTime * dragSmoothSpeed);
            }
        }
    }

    void StartDrag()
    {
        isDragging = true;

        if (playerAttack != null)
            playerAttack.SetDragging(true);

        CollectSlotMates();

        foreach (Transform mate in slotMates)
        {
            PlayerAttack mateAttack = mate.GetComponent<PlayerAttack>();
            if (mateAttack != null) mateAttack.SetDragging(true);
        }
    }

    void EndDrag()
    {
        isDragging = false;

        if (playerAttack != null) playerAttack.SetDragging(false);
        foreach (Transform mate in slotMates)
        {
            PlayerAttack mateAttack = mate.GetComponent<PlayerAttack>();
            if (mateAttack != null) mateAttack.SetDragging(false);
        }

        int targetSlot = FindNearestSlot();
        if (targetSlot >= 0 && targetSlot != originalSlotIndex && CanMoveToSlot(targetSlot))
            MoveToSlot(targetSlot);
        else
        {
            // 원래 위치로 부드럽게 복귀
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

    void ShowPanel()
    {
        if (MergeManager.Instance == null) return;

        if (playerAttack != null && playerAttack.isLeader)
        {
            MergeManager.Instance.SelectUnit(playerAttack);
        }
        else
        {
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
            slotMates[i].position = PlayerSpawner.Instance.spawnPoints[targetSlot].position
                                    + PlayerSpawner.Instance.GetTriangleOffsetPublic(i + 1);
        }
        transform.position = PlayerSpawner.Instance.spawnPoints[targetSlot].position
                             + PlayerSpawner.Instance.GetTriangleOffsetPublic(0);
        playerAttack.spawnIndex = targetSlot;
        originalSlotIndex = targetSlot;
        originalPosition = transform.position;
    }
}   