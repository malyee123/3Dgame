using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static void GoTo(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public static void GoToImmediate(string sceneName) => SceneManager.LoadScene(sceneName);
    public void LoadSceneImmediate(string sceneName) => GoToImmediate(sceneName);

    public void LoadGameScene() => GoTo("GameScene");
    public void LoadLobbyScene() => GoTo("LobbyScene");
    public void LoadStageSelectScene() => GoTo("StageSelectScene");
    public void LoadUpgradeScene() => GoTo("UpgradeScene");
    public void LoadPassiveUpgradeScene() => GoTo("PassiveUpgradeScene");
    public void LoadCompendiumScene() => GoTo("CompendiumScene");

    public void LoadGameSceneWithStage(int stage)
    {
        PlayerPrefs.SetInt("SelectedStage", stage);
        PlayerPrefs.Save();
        GoTo("GameScene");
    }
}