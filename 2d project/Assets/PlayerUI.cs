using UnityEngine;
using TMPro; // 🌟 TextMeshPro를 사용하기 위해 반드시 추가해야 하는 마법의 주문입니다!

public class PlayerUI : MonoBehaviour
{
    // 화면에 띄워둔 텍스트 오브젝트를 연결할 빈칸입니다.
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI goldText;

    void Update()
    {
        // GameManager가 살아있다면 (에러 방지용)
        if (GameManager.Instance != null)
        {
            // 매 프레임마다 GameManager의 데이터를 읽어와서 화면 글씨를 바꿔줍니다.
            // ToString()은 숫자(int)를 글자(string)로 변환해 주는 함수입니다.
            levelText.text = "Level: " + GameManager.Instance.playerLevel.ToString();
            goldText.text = "Gold: " + GameManager.Instance.playerGold.ToString();
        }
    }

    // 테스트용: 버튼을 누르면 돈이 올라가는 함수
    public void ClickEarnGoldButton()
    {
        // GameManager의 AddGold 함수를 불러와 골드를 100씩 추가합니다.
        GameManager.Instance.AddGold(100);
    }
}