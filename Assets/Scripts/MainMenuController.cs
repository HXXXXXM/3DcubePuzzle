using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "MainPuzzle3DScene"; // �ε��� ���� �÷��� Scene�� �̸�

    public void StartGame()
    {
        // GameSetup.CurrentLevel�� 1�� �ʱ�ȭ (���� ����, GameSetup.Awake���� ó�� ����)
        // �Ǵ� GameSetup.ResetProgress(); ���� �Լ� ȣ��
        // ���⼭�� GameSetup.Awake()�� �׻� 1�ܰ� �Ǵ� ����� �ܰ�� �����Ѵٰ� ����

        Debug.Log($"Starting game. Loading scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game button pressed.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}