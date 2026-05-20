using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AnvilUI : MonoBehaviour
{
    public static AnvilUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject anvilPanel;

    [Header("Cards")]
    public AnvilCard[] cards = new AnvilCard[3];

    [Header("Active Anvils")]
    public TextMeshProUGUI activeAnvilText;

    private Dictionary<AnvilType, float> accumulatedValues = new Dictionary<AnvilType, float>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (anvilPanel != null) anvilPanel.SetActive(false);
        UpdateActiveAnvilText();
    }

    public void ShowAnvils()
    {
        if (AnvilManager.Instance == null) return;
        AnvilManager.Instance.CacheCurrentStage();
        AnvilData[] anvils = AnvilManager.Instance.GetRandomAnvils();
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (i < anvils.Length) { cards[i].gameObject.SetActive(true); cards[i].Setup(anvils[i], this); }
            else cards[i].gameObject.SetActive(false);
        }
        if (anvilPanel != null) anvilPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnSelectAnvil(AnvilData data)
    {
        AnvilManager.Instance?.ApplyAnvil(data);
        if (accumulatedValues.ContainsKey(data.type))
            accumulatedValues[data.type] += data.value;
        else
            accumulatedValues[data.type] = data.value;
        if (anvilPanel != null) anvilPanel.SetActive(false);
        Time.timeScale = SpeedManager.Instance != null ? SpeedManager.Instance.CurrentSpeed : 1f;
        UpdateActiveAnvilText();
    }

    void UpdateActiveAnvilText()
    {
        if (activeAnvilText == null) return;
        if (accumulatedValues.Count == 0) { activeAnvilText.text = ""; return; }
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[ 모루 ]\n");
        foreach (var kv in accumulatedValues)
        {
            switch (kv.Key)
            {
                case AnvilType.AttackDamage:   sb.Append($"공격력 +{kv.Value}%\n"); break;
                case AnvilType.AttackSpeed:    sb.Append($"공격속도 +{kv.Value}%\n"); break;
                case AnvilType.DefenseDown:    sb.Append($"방어력 감소 {kv.Value}%\n"); break;
                case AnvilType.BossTime:       sb.Append($"보스전 시간 +{kv.Value}초\n"); break;
                case AnvilType.EnemyLimit:     sb.Append($"적 최대 인원수 +{(int)kv.Value}\n"); break;
                case AnvilType.CharacterLimit: sb.Append($"배치 캐릭터 +{(int)kv.Value}\n"); break;
            }
        }
        activeAnvilText.text = sb.ToString().TrimEnd();
    }

    public void ResetAnvilUI()
    {
        accumulatedValues.Clear();
        UpdateActiveAnvilText();
    }
}
