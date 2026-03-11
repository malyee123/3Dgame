using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerDragMerge : MonoBehaviour
{
    private Camera mainCamera;
    private bool isDragging;
    private Vector3 dragOffset;
    private Vector3 originalPosition;
    private int spawnIndex = -1;

    private PlayerAttack playerAttack;

    void Awake()
    {
        mainCamera = Camera.main;
        playerAttack = GetComponent<PlayerAttack>();
    }

    public void SetSpawnIndex(int index)
    {
        spawnIndex = index;
    }

    void OnMouseDown()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        isDragging = true;
        originalPosition = transform.position;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        dragOffset = transform.position - mouseWorld;
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null)
        {
            return;
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = mouseWorld + dragOffset;
    }

    void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        PlayerAttack target = FindMergeTarget();
        if (target != null)
        {
            target.MergeFrom(playerAttack);
            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.Instance.RegisterFreedSlot(spawnIndex);
            }

            Destroy(gameObject);
            return;
        }

        transform.position = originalPosition;
    }

    PlayerAttack FindMergeTarget()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject)
            {
                continue;
            }

            PlayerAttack otherPlayer = hit.GetComponent<PlayerAttack>();
            if (otherPlayer == null)
            {
                continue;
            }

            if (playerAttack != null && playerAttack.CanMergeWith(otherPlayer))
            {
                return otherPlayer;
            }
        }

        return null;
    }
}
