using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("상품 정보")]
    public int itemPrice = 500; // 이 상품의 가격
    public string itemName = "레벨"; // 상품 이름

    [Header("UI 연결")]
    // (선택) 구매 성공/실패 메시지를 띄워줄 텍스트가 있다면 연결합니다.
    public TextMeshProUGUI noticeText;

    // 구매 버튼을 눌렀을 때 실행될 함수
    public void BuyItem()
    {
        // 1. 보안 검사: GameManager가 잘 있는지 확인
        if (GameManager.Instance == null) return;

        // 2. 돈이 충분한지 검사
        if (GameManager.Instance.playerGold >= itemPrice)
        {
            // 3. 골드 차감
            GameManager.Instance.playerGold -= itemPrice;

            // 4. 아이템 지급 로직 (나중에는 인벤토리에 추가하는 코드가 들어갑니다)
            // 예시로 플레이어의 레벨을 1 올려보겠습니다.
            GameManager.Instance.playerLevel += 1;

            Debug.Log($"{itemName} 구매 성공! 남은 골드: {GameManager.Instance.playerGold}");

            if (noticeText != null) noticeText.text = "구매 성공!";
        }
        else
        {
            // 돈이 부족할 때
            Debug.Log("골드가 부족합니다.");

            if (noticeText != null) noticeText.text = "골드가 부족합니다!";
        }
    }
}