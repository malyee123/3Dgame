using UnityEngine;

public class StageMapManager : MonoBehaviour
{
    public static StageMapManager Instance { get; private set; }

    [System.Serializable]
    public class StageMapData
    {
        [Header("스테이지 번호")]
        public int stage;

        [Header("배경")]
        public Sprite backgroundSprite;
        public Color  backgroundColor = Color.white;

        [Header("웨이포인트 위치 (PathManager 동기화)")]
        public Vector3[] waypointPositions;

        [Header("스폰 위치")]
        public Vector2 spawnPosition = new Vector2(-6f, 3f);
    }

    [Header("스테이지별 맵 데이터")]
    public StageMapData[] stageMaps;

    [Header("씬 오브젝트 참조")]
    public SpriteRenderer backgroundRenderer;
    public PathManager    pathManager;
    public EnemySpawner   enemySpawner;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (pathManager  == null) pathManager  = FindFirstObjectByType<PathManager>();
        if (enemySpawner == null) enemySpawner  = FindFirstObjectByType<EnemySpawner>();

        int currentStage = GameManager.Instance != null
            ? GameManager.Instance.GetCurrentStage() : 1;
        ApplyStageMap(currentStage);
    }

    public void ApplyStageMap(int stage)
    {
        StageMapData data = GetMapData(stage);
        if (data == null) return;

        ApplyBackground(data);
        ApplyWaypoints(data);
        ApplySpawnPosition(data);
    }

    void ApplyBackground(StageMapData data)
    {
        if (backgroundRenderer == null) return;
        if (data.backgroundSprite != null)
            backgroundRenderer.sprite = data.backgroundSprite;
        backgroundRenderer.color = data.backgroundColor;
    }

    void ApplyWaypoints(StageMapData data)
    {
        if (pathManager == null) return;
        if (data.waypointPositions == null || data.waypointPositions.Length == 0) return;

        int count = Mathf.Min(data.waypointPositions.Length, pathManager.GetWaypointCount());
        for (int i = 0; i < count; i++)
        {
            Transform wp = pathManager.GetWaypoint(i);
            if (wp != null) wp.position = data.waypointPositions[i];
        }
    }

    void ApplySpawnPosition(StageMapData data)
    {
        if (enemySpawner == null) return;
        enemySpawner.SetSpawnPosition(data.spawnPosition);
    }

    StageMapData GetMapData(int stage)
    {
        if (stageMaps == null) return null;
        StageMapData best = null;
        foreach (StageMapData d in stageMaps)
        {
            if (d.stage <= stage)
            {
                if (best == null || d.stage > best.stage) best = d;
            }
        }
        return best;
    }
}
