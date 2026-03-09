using UnityEngine;

/// <summary>
/// Enemy 오브젝트가 웨이포인트를 따라 이동하는 스크립트
/// </summary>
public class EnemyMove : MonoBehaviour
{
    public float speed = 2f;

    // 현재 이동 목표 웨이포인트 인덱스
    private int waypointIndex = 0;

    private PathManager pathManager;

    public void SetPathManager(PathManager pm)
    {
        pathManager = pm;
    }

    /// <summary>
    /// Path 상에서 현재 Enemy의 진행도를 반환합니다.
    /// 값이 작을수록 뒤쪽(후미)입니다.
    /// </summary>
    public float GetPathProgress()
    {
        if (pathManager == null)
        {
            return 0f;
        }

        int waypointCount = pathManager.GetWaypointCount();
        if (waypointCount <= 0)
        {
            return 0f;
        }

        int currentIndex = waypointIndex;
        int prevIndex = (currentIndex - 1 + waypointCount) % waypointCount;

        Transform prevWaypoint = pathManager.GetWaypoint(prevIndex);
        Transform currentWaypoint = pathManager.GetWaypoint(currentIndex);

        if (prevWaypoint == null || currentWaypoint == null)
        {
            return currentIndex;
        }

        Vector2 a = prevWaypoint.position;
        Vector2 b = currentWaypoint.position;
        Vector2 p = transform.position;

        Vector2 ab = b - a;
        float abLenSqr = ab.sqrMagnitude;
        float t = 0f;

        if (abLenSqr > 0f)
        {
            t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSqr);
        }

        return prevIndex + t;
    }

    void Start()
    {
        if (pathManager == null)
        {
            pathManager = FindFirstObjectByType<PathManager>();
        }

        if (pathManager == null)
        {
            Debug.LogError("[EnemyMove] PathManager를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        if (pathManager.GetWaypointCount() == 0)
        {
            Debug.LogError("[EnemyMove] Waypoint가 0개입니다.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        Transform target = pathManager.GetWaypoint(waypointIndex);
        if (target == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            waypointIndex++;

            if (waypointIndex >= pathManager.GetWaypointCount())
            {
                waypointIndex = 0;
            }
        }
    }
}
