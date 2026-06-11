using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpecialMonsterManager : MonoBehaviour
{
    public static SpecialMonsterManager Instance;

    [SerializeField] private GameObject specialMonsterPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Button spawnButton;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private int sessionKillCount = 0;
    private bool isOnField = false;
    private bool isOnCooldown = false;
    private float cooldownRemaining = 0f;
    private float durationRemaining = 0f;
    private GameObject currentMonster = null;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (spawnButton != null)
        {
            spawnButton.onClick.RemoveAllListeners();
            spawnButton.onClick.AddListener(OnSpawnButtonPressed);
        }

        // 현재 스테이지 기준으로 초기 쿨다운 설정
        var data = CSVLoader.Instance?.GetSpecialMonsterData(GetCurrentStage(), 0);
        if (data != null && data.cooldown > 0f)
        {
            isOnCooldown = true;
            cooldownRemaining = data.cooldown;
        }

        UpdateButton();
    }

    private void Update()
    {
        if (isOnField)
        {
            if (currentMonster == null)
            {
                isOnField = false;
                StartCooldown();
            }
            else
            {
                durationRemaining -= Time.deltaTime;
                if (durationRemaining <= 0f)
                {
                    isOnField = false;
                    Destroy(currentMonster);
                    currentMonster = null;
                    StartCooldown();
                }
            }
        }

        if (!isOnField && isOnCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                cooldownRemaining = 0f;
                UpdateButton();
            }
        }

        if (cooldownText != null)
            cooldownText.text = (!isOnField && isOnCooldown)
                ? Mathf.CeilToInt(cooldownRemaining).ToString()
                : string.Empty;
    }

    private void StartCooldown()
    {
        // 현재 스테이지 + 누적 처치 횟수 기준 쿨다운 적용
        var data = CSVLoader.Instance?.GetSpecialMonsterData(GetCurrentStage(), sessionKillCount);
        float cooldown = data != null ? data.cooldown : 30f;
        isOnCooldown = true;
        cooldownRemaining = cooldown;
        UpdateButton();
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null) return spawnPoint.position;
        if (EnemySpawner.Instance != null) return EnemySpawner.Instance.transform.position;
        return Vector3.zero;
    }

    public void OnSpawnButtonPressed()
    {
        if (isOnField || isOnCooldown) return;
        if (specialMonsterPrefab == null) return;

        // 현재 스테이지 + 누적 처치 횟수 기준 스탯 적용
        var data = CSVLoader.Instance?.GetSpecialMonsterData(GetCurrentStage(), sessionKillCount);
        if (data == null) return;

        var go = Instantiate(specialMonsterPrefab, GetSpawnPosition(), Quaternion.identity);
        var eh = go.GetComponentInChildren<EnemyHealth>();
        if (eh != null) eh.InitAsSpecialMonster(data.hp, data.coinReward, data.defense);
        var em = go.GetComponentInChildren<EnemyMove>();
        if (em != null) em.speed = data.speed;

        currentMonster = go;
        durationRemaining = data.lifetime;
        isOnField = true;
        UpdateButton();
    }

    public void OnSpecialMonsterKilled()
    {
        if (!isOnField) return;
        isOnField = false;
        currentMonster = null;
        sessionKillCount++;
        StartCooldown();
    }

    private void UpdateButton()
    {
        if (spawnButton != null)
            spawnButton.interactable = !isOnField && !isOnCooldown;
    }

    // 현재 스테이지 반환 헬퍼
    private int GetCurrentStage()
        => GameManager.Instance != null ? GameManager.Instance.GetCurrentStage() : 1;
}