using System.Collections;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public float speed = 2f;

    private int waypointIndex = 0;
    private PathManager pathManager;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private float speedPenalty = 0f;
    [SerializeField] private float tempSpeedPenalty = 0f;
    private bool isStunned = false;
    private bool isPaused = false;
    private bool isTempSlowed = false;

    public void SetPathManager(PathManager pm) => pathManager = pm;
    public void ApplySpeedPenalty(float penalty) => speedPenalty = penalty;
    public void SetPaused(bool paused) => isPaused = paused;

    public void ApplyStun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    public void ApplyTempSpeedPenalty(float amount, float duration)
    {
        if (isTempSlowed) return;
        StartCoroutine(TempSpeedPenaltyRoutine(amount, duration));
    }

    IEnumerator TempSpeedPenaltyRoutine(float amount, float duration)
    {
        isTempSlowed = true;
        tempSpeedPenalty = amount;
        yield return new WaitForSeconds(duration);
        tempSpeedPenalty = 0f;
        isTempSlowed = false;
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        float originalSpeed = speed;
        speed = 0f;
        yield return new WaitForSeconds(duration);
        speed = originalSpeed;
        isStunned = false;
    }

    public float GetPathProgress()
    {
        if (pathManager == null) return 0f;
        int count = pathManager.GetWaypointCount();
        if (count <= 0) return 0f;
        int prevIndex = (waypointIndex - 1 + count) % count;
        Transform prev = pathManager.GetWaypoint(prevIndex);
        Transform curr = pathManager.GetWaypoint(waypointIndex);
        if (prev == null || curr == null) return waypointIndex;
        Vector2 a = prev.position, b = curr.position;
        Vector2 ab = b - a;
        float sqrLen = ab.sqrMagnitude;
        float t = sqrLen > 0f ? Mathf.Clamp01(Vector2.Dot((Vector2)transform.position - a, ab) / sqrLen) : 0f;
        return prevIndex + t;
    }

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (pathManager == null) { Debug.LogError("[EnemyMove] PathManager not found."); enabled = false; return; }
        if (pathManager.GetWaypointCount() == 0) { Debug.LogError("[EnemyMove] No waypoints."); enabled = false; return; }
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isPaused) return;
        Transform target = pathManager.GetWaypoint(waypointIndex);
        if (target == null) return;
        float currentSpeed = Mathf.Max(0f, speed - speedPenalty - tempSpeedPenalty);
        transform.position = Vector2.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
        if (spriteRenderer != null) spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10);
        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            waypointIndex++;
            if (waypointIndex >= pathManager.GetWaypointCount()) waypointIndex = 0;
        }
    }
}