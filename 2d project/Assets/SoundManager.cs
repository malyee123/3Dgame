using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // 핵심! 씬이 넘어가도 이 스피커를 파괴하지 마라!
            DontDestroyOnLoad(gameObject);

            // 내 오브젝트에 붙어있는 스피커(AudioSource)를 찾아서 저장해둡니다.
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            // 중복 재생 방지
            Destroy(gameObject);
        }
    }

    // 슬라이더가 움직일 때 볼륨을 조절해 주는 함수
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    // 설정 창을 열 때 현재 볼륨 위치를 알려주는 함수
    public float GetVolume()
    {
        if (audioSource != null) return audioSource.volume;
        return 1f;
    }
}