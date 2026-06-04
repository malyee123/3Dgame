using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    void Load(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void LoadGameScene()          => Load("GameScene");
    public void LoadLobbyScene()         => Load("LobbyScene");
    public void LoadStageSelectScene()   => Load("StageSelectScene");
    public void LoadUpgradeScene()       => Load("UpgradeScene");
    public void LoadPassiveUpgradeScene()=> Load("PassiveUpgradeScene");
    public void LoadTitleScene()         => Load("TitleScene");
    public void LoadCompendiumScene()    => Load("CompendiumScene");

    public void LoadGameSceneWithStage(int stage)
    {
        PlayerPrefs.SetInt("SelectedStage", stage);
        PlayerPrefs.Save();
        Load("GameScene");
    }
}
