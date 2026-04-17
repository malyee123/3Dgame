using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectManager : MonoBehaviour
{
    [Header("UI")]
    public Transform stageButtonContainer;
    public GameObject stageButtonPrefab;
    public ScrollRect scrollRect;

    void Start()
    {
        GenerateStageButtons();
        StartCoroutine(ScrollToUnlockedStage());
    }

    IEnumerator ScrollToUnlockedStage()
    {
        yield return null;
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
        int maxStage = GetMaxStageFromCSV();
        float normalizedPos = (float)(unlockedStage - 1) / Mathf.Max(1, maxStage - 1);
        scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(normalizedPos);
    }

    void GenerateStageButtons()
    {
        if (CSVLoader.Instance == null || stageButtonPrefab == null || stageButtonContainer == null) return;

        int maxStage = GetMaxStageFromCSV();
        int unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);

        for (int i = 1; i <= maxStage; i++)
        {
            int stageIndex = i;
            GameObject btn = Instantiate(stageButtonPrefab, stageButtonContainer);

            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = $"Stage {stageIndex}";

            Button button = btn.GetComponent<Button>();
            Image image = btn.GetComponent<Image>();
            bool isUnlocked = stageIndex <= unlockedStage;

            if (button != null)
            {
                button.interactable = isUnlocked;
                if (isUnlocked)
                {
                    button.onClick.AddListener(() =>
                    {
                        PlayerPrefs.SetInt("SelectedStage", stageIndex);
                        PlayerPrefs.Save();
                        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                    });
                }
            }

            if (image != null)
                image.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    int GetMaxStageFromCSV()
    {
        int max = 1;
        foreach (RoundData rd in CSVLoader.Instance.roundDataList)
            if (rd.stage > max) max = rd.stage;
        return max;
    }

    public void GoToLobby()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
}
