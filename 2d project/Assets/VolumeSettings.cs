using UnityEngine;
using UnityEngine.UI; // 🌟 슬라이더(UI)를 제어하기 위해 반드시 필요합니다!

public class VolumeSettings : MonoBehaviour
{
    public Slider bgmSlider; // 화면에 만든 슬라이더를 연결할 빈칸

    void Start()
    {
        // 1. 설정 창이 열릴 때, 슬라이더의 손잡이 위치를 현재 배경음악 볼륨과 똑같이 맞춰줍니다.
        if (SoundManager.instance != null)
        {
            bgmSlider.value = SoundManager.instance.GetVolume();
        }
    }

    // 2. 유저가 슬라이더를 드래그할 때마다 실행될 함수
    public void OnSliderValueChanged(float value)
    {
        // SoundManager의 볼륨 조절 함수로 드래그한 숫자(value)를 넘겨줍니다.
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetVolume(value);
        }
    }
}