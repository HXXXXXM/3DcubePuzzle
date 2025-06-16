// SoundManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // �� ���� ������ ����

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource; // ������� �����
    public AudioSource sfxSource; // ȿ���� �����

    [Header("Audio Clips")]
    public AudioClip mainMenuBGM;     // "1.mp3" (���� �޴� �� �������)
    public AudioClip gameplayBGM;     // "2.mp3" (���� �÷��� �� �������)
    public AudioClip uiButtonClickSound; // UI ��ư Ŭ�� ȿ����

    private string currentSceneName;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� ����Ǿ �� SoundManager�� �ı����� ����

            // AudioSource ������Ʈ �ڵ� �߰� (���� Inspector���� �Ҵ� �� ���� ��� ���)
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true; // BGM�� �⺻������ �ݺ�
                bgmSource.playOnAwake = false; // Awake���� �ٷ� ������� ����
            }
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }
        else
        {
            Destroy(gameObject); // �̹� �ν��Ͻ��� �ִٸ� �� ������Ʈ�� �ı�
            return;
        }
    }

    void OnEnable()
    {
        // ���� �ε�� ������ ȣ��� �̺�Ʈ�� �Լ� ���
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // ������Ʈ�� ��Ȱ��ȭ�ǰų� �ı��� �� �̺�Ʈ���� �Լ� ����
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ���� �ε�� �� ȣ��Ǵ� �Լ�
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        PlayBGMForCurrentScene();
    }

    /// <summary>
    /// ���� �� �̸��� ���� ������ BGM�� ����մϴ�.
    /// </summary>
    public void PlayBGMForCurrentScene()
    {
        AudioClip clipToPlay = null;

        if (currentSceneName == "MainMenuScene") // ���� �޴� �� �̸� (��Ȯ�� ��ġ�ؾ� ��)
        {
            clipToPlay = mainMenuBGM;
        }
        else if (currentSceneName == "MainPuzzle3DScene") // ���� �÷��� �� �̸� (��Ȯ�� ��ġ�ؾ� ��)
        {
            clipToPlay = gameplayBGM;
        }
        // �ٸ� ���� ���� BGM�� �߰� ����

        if (clipToPlay != null)
        {
            if (bgmSource.clip == clipToPlay && bgmSource.isPlaying)
            {
                return; // �̹� ���� BGM�� ��� ���̸� �ٽ� ������� ����
            }
            bgmSource.clip = clipToPlay;
            bgmSource.loop = true;
            bgmSource.Play();
            Debug.Log($"SoundManager: Playing BGM '{clipToPlay.name}' for scene '{currentSceneName}'.");
        }
        else
        {
            bgmSource.Stop(); // �ش� ���� BGM�� ������ ����
            Debug.LogWarning($"SoundManager: No BGM assigned for scene '{currentSceneName}'. BGM stopped.");
        }
    }

    /// <summary>
    /// UI ��ư Ŭ�� ȿ������ ����մϴ�.
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

    // �ʿ��ϴٸ� �ٸ� ȿ���� ��� �Լ� �߰�
    // public void PlayPiecePlaceSound() { ... }
}