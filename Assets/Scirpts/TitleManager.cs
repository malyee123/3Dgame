using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("Settings")]
    public float waitTime = 3f;

    void Start()
    {
        StartCoroutine(LoadLobby());
    }

    System.Collections.IEnumerator LoadLobby()
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene("LobbyScene");
    }
}