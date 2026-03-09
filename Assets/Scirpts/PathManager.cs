using UnityEngine;

public class PathManager : MonoBehaviour
{
    public Transform[] waypoints;

    void Awake()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[PathManager] No waypoints configured. Assign waypoints in Inspector.");
        }
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[PathManager] Waypoint array is empty.");
            return null;
        }

        if (index < 0 || index >= waypoints.Length)
        {
            Debug.LogError($"[PathManager] Invalid waypoint index: {index} (count: {waypoints.Length})");
            return null;
        }

        return waypoints[index];
    }

    public int GetWaypointCount()
    {
        if (waypoints == null)
        {
            return 0;
        }

        return waypoints.Length;
    }
}
