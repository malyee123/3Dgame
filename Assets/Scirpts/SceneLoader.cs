using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadGameScene() => SceneManager.LoadScene("GameScene");
    public void LoadLobbyScene() => SceneManager.LoadScene("LobbyScene");
    public void LoadStageSelectScene() => SceneManager.LoadScene("StageSelectScene");
    public void LoadUpgradeScene() => SceneManager.LoadScene("UpgradeScene");
    public void LoadPassiveUpgradeScene() => SceneManager.LoadScene("PassiveUpgradeScene");
    public void LoadGameSceneWithStage(int stage)
    {
        PlayerPrefs.SetInt("SelectedStage", stage);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }
}