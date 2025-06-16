using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "MainPuzzle3DScene"; // 로드할 게임 플레이 Scene의 이름

    public void StartGame()
    {
        // GameSetup.CurrentLevel을 1로 초기화 (선택 사항, GameSetup.Awake에서 처리 가능)
        // 또는 GameSetup.ResetProgress(); 같은 함수 호출
        // 여기서는 GameSetup.Awake()가 항상 1단계 또는 저장된 단계로 시작한다고 가정

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