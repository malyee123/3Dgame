using UnityEngine;
using UnityEngine.SceneManagement; // 필요

public class NewMonoBehaviour : MonoBehaviour
{
    public void NextSceneWithString()
    {
        // 문자열 이용해서 씬 전환
        //SceneManager.LoadScene("Main");        // OK
        SceneManager.LoadScene("Main"); // OK
    }

    public void NextSceneWithNum()
    {
        // 씬 번호를 이용해서 씬 이동
        SceneManager.LoadScene(0);  // 0 번째 씬 로드
    }
}