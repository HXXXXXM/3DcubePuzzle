// SoundManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 변경 감지를 위해

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource; // 배경음악 재생용
    public AudioSource sfxSource; // 효과음 재생용

    [Header("Audio Clips")]
    public AudioClip mainMenuBGM;     // "1.mp3" (메인 메뉴 씬 배경음악)
    public AudioClip gameplayBGM;     // "2.mp3" (게임 플레이 씬 배경음악)
    public AudioClip uiButtonClickSound; // UI 버튼 클릭 효과음

    private string currentSceneName;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 이 SoundManager는 파괴되지 않음

            // AudioSource 컴포넌트 자동 추가 (만약 Inspector에서 할당 안 했을 경우 대비)
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true; // BGM은 기본적으로 반복
                bgmSource.playOnAwake = false; // Awake에서 바로 재생하지 않음
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 있다면 이 오브젝트는 파괴
            return;
        }
    }

    void OnEnable()
    {
        // 씬이 로드될 때마다 호출될 이벤트에 함수 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화되거나 파괴될 때 이벤트에서 함수 제거
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드될 때 호출되는 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        PlayBGMForCurrentScene();
    }

    /// <summary>
    /// 현재 씬 이름에 따라 적절한 BGM을 재생합니다.
    /// </summary>
    public void PlayBGMForCurrentScene()
    {
        AudioClip clipToPlay = null;

        if (currentSceneName == "MainMenuScene") // 메인 메뉴 씬 이름 (정확히 일치해야 함)
        {
            clipToPlay = mainMenuBGM;
        }
        else if (currentSceneName == "MainPuzzle3DScene") // 게임 플레이 씬 이름 (정확히 일치해야 함)
        {
            clipToPlay = gameplayBGM;
        }
        // 다른 씬에 대한 BGM도 추가 가능

        if (clipToPlay != null)
        {
            if (bgmSource.clip == clipToPlay && bgmSource.isPlaying)
            {
                return; // 이미 같은 BGM이 재생 중이면 다시 재생하지 않음
            }
            bgmSource.clip = clipToPlay;
            bgmSource.loop = true;
            bgmSource.Play();
            Debug.Log($"SoundManager: Playing BGM '{clipToPlay.name}' for scene '{currentSceneName}'.");
        }
        else
        {
            bgmSource.Stop(); // 해당 씬에 BGM이 없으면 정지
            Debug.LogWarning($"SoundManager: No BGM assigned for scene '{currentSceneName}'. BGM stopped.");
        }
    }

    /// <summary>
    /// UI 버튼 클릭 효과음을 재생합니다.
    /// </summary>
    public void PlayButtonClickSound()
    {
        if (sfxSource != null && uiButtonClickSound != null)
        {
            sfxSource.PlayOneShot(uiButtonClickSound);
        }
        else
        {
            Debug.LogWarning("SoundManager: SFX Source or Button Click Sound not assigned.");
        }
    }

    // 필요하다면 다른 효과음 재생 함수 추가
    // public void PlayPiecePlaceSound() { ... }
}