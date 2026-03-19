using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 1. 어디서든 GameManager.Instance로 접근할 수 있게 만듭니다. (싱글톤)
    public static GameManager Instance;

    // 2. 씬이 넘어가도 유지되어야 할 데이터들
    public int playerLevel = 1;
    public int playerGold = 0;

    void Awake()
    {
        // 처음 만들어졌을 때
        if (Instance == null)
        {
            Instance = this;
            // 씬이 변경되어도 이 오브젝트를 파괴하지 않음
            DontDestroyOnLoad(gameObject);
        }
        // 이미 GameManager가 존재하는데 또 생성되려 할 때 (타이틀 씬으로 돌아왔을 때 등)
        else
        {
            Destroy(gameObject); // 중복 생성을 막기 위해 파괴
        }
    }

    // 다른 스크립트에서 골드를 추가할 때 부를 함수
    public void AddGold(int amount)
    {
        playerGold += amount;
        Debug.Log("현재 골드: " + playerGold);
    }
}