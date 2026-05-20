using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StageSelectManager : MonoBehaviour
{
    [Header("Stage Display")]
    public Button stageButtonPrefab;
    public Transform stageButtonContainer;

    [Header("Navigation")]
    public Button prevButton;
    public Button nextButton;

    [Header("Stage Info")]
    public TextMeshProUGUI stageNameText;
    public Image stageImage;
    public Sprite[] stageSprites;

    private int currentIndex = 0;
    private int maxStage = 1;
    private int unlockedStage = 1;

    void Start()
    {
        maxStage = GetMaxStageFromCSV();
        unlockedStage = PlayerPrefs.GetInt("UnlockedStage", 1);
        currentIndex = unlockedStage - 1;

        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrev);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNext);
        }

        RefreshUI();
    }

    void OnPrev()
    {
        if (currentIndex > 0) currentIndex--;
        RefreshUI();
    }

    void OnNext()
    {
        if (currentIndex < maxStage - 1) currentIndex++;
        RefreshUI();
    }

    void RefreshUI()
    {
        int stage = currentIndex + 1;
        bool isUnlocked = stage <= unlockedStage;

        if (stageNameText != null)
            stageNameText.text = $"Stage {stage}";

        if (stageImage != null && stageSprites != null && currentIndex < stageSprites.Length)
            stageImage.sprite = stageSprites[currentIndex];

        if (prevButton != null) prevButton.interactable = currentIndex > 0;
        if (nextButton != null) nextButton.interactable = currentIndex < maxStage - 1;

        if (stageButtonContainer != null)
        {
            Button btn = stageButtonContainer.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.interactable = isUnlocked;
                btn.onClick.RemoveAllListeners();
                if (isUnlocked)
                {
                    int selected = stage;
                    btn.onClick.AddListener(() =>
                    {
                        PlayerPrefs.SetInt("SelectedStage", selected);
                        PlayerPrefs.Save();
                        SceneManager.LoadScene("GameScene");
                    });
                }
            }
        }
    }

    int GetMaxStageFromCSV()
    {
        if (CSVLoader.Instance == null) return 1;
        int max = 1;
        foreach (RoundData rd in CSVLoader.Instance.roundDataList)
            if (rd.stage > max) max = rd.stage;
        return max;
    }

    public void GoToLobby() => SceneManager.LoadScene("LobbyScene");
}
