using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 모든 씬 전환은 이 static 메서드로 통일
    public static void GoTo(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // 인스턴스 메서드 — 버튼 onClick 슬롯 연결용
    public void LoadGameScene()            => GoTo("GameScene");
    public void LoadLobbyScene()           => GoTo("LobbyScene");
    public void LoadStageSelectScene()     => GoTo("StageSelectScene");
    public void LoadUpgradeScene()         => GoTo("UpgradeScene");
    public void LoadPassiveUpgradeScene()  => GoTo("PassiveUpgradeScene");
    public void LoadCompendiumScene()      => GoTo("CompendiumScene");

    // 딜레이 없이 즉시 전환 (TitleManager 전용)
    public static void GoToImmediate(string sceneName) => SceneManager.LoadScene(sceneName);
    public void LoadSceneImmediate(string sceneName)   => GoToImmediate(sceneName);

    public void LoadGameSceneWithStage(int stage)
    {
        PlayerPrefs.SetInt("SelectedStage", stage);
        PlayerPrefs.Save();
        GoTo("GameScene");
    }
}
