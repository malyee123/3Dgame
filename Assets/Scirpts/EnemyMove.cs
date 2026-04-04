using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public float speed = 2f;

    private int waypointIndex = 0;
    private PathManager pathManager;
    private SpriteRenderer spriteRenderer;
    private float speedPenalty = 0f;

    public void SetPathManager(PathManager pm) { pathManager = pm; }
    public void ApplySpeedPenalty(float penalty) { speedPenalty = penalty; }

    public float GetPathProgress()
    {
        if (pathManager == null) return 0f;
        int waypointCount = pathManager.GetWaypointCount();
        if (waypointCount <= 0) return 0f;
        int prevIndex = (waypointIndex - 1 + waypointCount) % waypointCount;
        Transform prevWaypoint = pathManager.GetWaypoint(prevIndex);
        Transform currentWaypoint = pathManager.GetWaypoint(waypointIndex);
        if (prevWaypoint == null || currentWaypoint == null) return waypointIndex;
        Vector2 a = prevWaypoint.position;
        Vector2 b = currentWaypoint.position;
        Vector2 ab = b - a;
        float abLenSqr = ab.sqrMagnitude;
        float t = abLenSqr > 0f ? Mathf.Clamp01(Vector2.Dot((Vector2)transform.position - a, ab) / abLenSqr) : 0f;
        return prevIndex + t;
    }

    void Start()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (pathManager == null) { Debug.LogError("[EnemyMove] PathManager not found."); enabled = false; return; }
        if (pathManager.GetWaypointCount() == 0) { Debug.LogError("[EnemyMove] Waypoint count is 0."); enabled = false; return; }
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Transform target = pathManager.GetWaypoint(waypointIndex);
        if (target == null) return;
        float currentSpeed = Mathf.Max(0.1f, speed - speedPenalty);
        transform.position = Vector2.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 10);
        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            waypointIndex++;
            if (waypointIndex >= pathManager.GetWaypointCount()) waypointIndex = 0;
        }
    }
}