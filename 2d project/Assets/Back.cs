using UnityEngine;
using UnityEngine.SceneManagement;
public class Back : MonoBehaviour
{
    public void NextSceneWithString()
    {
        // 문자열 이용해서 씬 전환
        //SceneManager.LoadScene("Title");        // OK
        SceneManager.LoadScene("Title"); // OK
    }

    public void NextSceneWithNum()
    {
        // 씬 번호를 이용해서 씬 이동
        SceneManager.LoadScene(1);  // 0 번째 씬 로드
    }
}