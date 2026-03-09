using UnityEngine;

/// <summary>
/// Enemy가 따라갈 웨이포인트(경로 좌표) 목록을 관리하는 스크립트
/// 이 스크립트를 가진 오브젝트에 웨이포인트들을 Inspector에서 연결해서 사용
/// </summary>
public class PathManager : MonoBehaviour
{
    // Inspector에서 웨이포인트 오브젝트를 직접 드래그해서 넣는 배열
    // Transform = 오브젝트의 위치(Position) 정보를 담고 있음
    public Transform[] waypoints;


    // 게임 시작 시 웨이포인트 설정이 올바른지 미리 확인
    void Awake()
    {
        // 웨이포인트가 하나도 없으면 오류 메시지 출력
        // Inspector에서 웨이포인트를 연결하지 않았을 때 발생
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[PathManager] 웨이포인트가 없습니다! Inspector에서 웨이포인트를 연결해주세요.");
        }
    }


    /// <summary>
    /// 특정 번호의 웨이포인트 위치를 반환
    /// EnemyMove 스크립트가 "다음 이동 목표 위치"를 얻기 위해 호출
    /// </summary>
    public Transform GetWaypoint(int index)
    {
        // 배열이 비어있으면 null 반환 (크래시 방지)
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[PathManager] 웨이포인트 배열이 비어있습니다!");
            return null;
        }

        // 잘못된 번호가 들어오면 null 반환 (IndexOutOfRange 크래시 방지)
        if (index < 0 || index >= waypoints.Length)
        {
            Debug.LogError($"[PathManager] 잘못된 웨이포인트 번호: {index} (총 {waypoints.Length}개)");
            return null;
        }

        return waypoints[index];
    }


    /// <summary>
    /// 전체 웨이포인트 개수를 반환
    /// EnemyMove에서 마지막 웨이포인트 도착 여부 확인할 때 사용
    /// </summary>
    public int GetWaypointCount()
    {
        // 배열이 null이면 0 반환 (크래시 방지)
        if (waypoints == null) return 0;
        return waypoints.Length;
    }
}