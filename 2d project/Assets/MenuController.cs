using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리에 필수

public class MenuController : MonoBehaviour
{
    // 환경설정 씬을 기존 씬 위에 얹기 (설정 열기)
    public void OpenSettings()
    {
        // 1. "SettingsScene"이라는 이름의 씬 정보를 가져와서 상태를 확인합니다.
        Scene settingsScene = SceneManager.GetSceneByName("SettingsScene");

        // 2. 만약 해당 씬이 로드되어 있지 않다면 (! 기호는 '아니다'라는 뜻)
        if (!settingsScene.isLoaded)
        {
            // 씬을 병합해서 띄워줍니다.
            SceneManager.LoadScene("SettingsScene", LoadSceneMode.Additive);
        }
        else
        {
            // 이미 열려있다면 아무 작동도 하지 않고 콘솔창에 메시지만 띄웁니다. (테스트용)
            Debug.Log("설정 창이 이미 켜져 있습니다!");
        }
    }

    // 환경설정 씬만 메모리에서 지우기 (설정 닫기)
    public void CloseSettings()
    {
        // UnloadSceneAsync를 사용해 해당 씬만 걷어냅니다.
        SceneManager.UnloadSceneAsync("SettingsScene");
    }

    public void OpenShop()
    {
        Scene shopScene = SceneManager.GetSceneByName("ShopScene");
        if (!shopScene.isLoaded)
        {
            SceneManager.LoadScene("ShopScene", LoadSceneMode.Additive);
        }
    }

    public void CloseShop()
    {
        SceneManager.UnloadSceneAsync("ShopScene");
    }

    public void OpenUpgrade()
    {
        Scene shopScene = SceneManager.GetSceneByName("Upgrade");
        if (!shopScene.isLoaded)
        {
            SceneManager.LoadScene("Upgrade", LoadSceneMode.Additive);
        }
    }

    public void CloseUpgrade()
    {
        SceneManager.UnloadSceneAsync("Upgrade");
    }
}