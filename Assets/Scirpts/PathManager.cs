using UnityEngine;

public class PathManager : MonoBehaviour
{
    public Transform[] waypoints;

    void Awake()
    {
        if (waypoints == null || waypoints.Length == 0)
            Debug.LogError("[PathManager] No waypoints configured.");
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0 || index < 0 || index >= waypoints.Length) return null;
        return waypoints[index];
    }

    public int GetWaypointCount() => waypoints != null ? waypoints.Length : 0;
}