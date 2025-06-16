// GameSetup.cs
using UnityEngine;

public class GameSetup : MonoBehaviour
{
    [Header("Required Managers (Assign in Inspector)")]
    public GameManager3D gameManager;
    public GridManager3D gridManager;

    [Header("Fixed Puzzle Configuration")]
    public int puzzleWidth = 3;
    public int puzzleHeight = 3;
    public int puzzleDepth = 3;
    public int numberOfColorsToUse = 1; // 단색 계획이므로 1로 설정하거나, PieceSpawner가 내부적으로 처리

    void Start()
    {
        if (gridManager == null) { /* ... null 체크 ... */ }
        if (gameManager == null) { /* ... null 체크 ... */ }
        InitializeNewPuzzle();
    }

    public void InitializeNewPuzzle()
    {
        if (gridManager == null || gameManager == null)
        {
            Debug.LogError("GameSetup: Cannot initialize puzzle, GridManager or GameManager not properly assigned.");
            return;
        }

        Debug.Log($"GameSetup: Initializing new puzzle ({puzzleWidth}x{puzzleHeight}x{puzzleDepth}). Single color mode."); // numberOfColorsToUse 제거

        // 1. GridManager 크기 설정 및 그리드 생성 요청
        gridManager.width = puzzleWidth;
        gridManager.height = puzzleHeight;
        gridManager.depth = puzzleDepth;
        gridManager.InitializeGridAndCells();

        // 2. GameManager에게 퍼즐 설정 요청 (파라미터 없이 호출)
        gameManager.SetupNewPuzzle(); // 수정된 부분: numberOfColorsToUse 인수 제거

        // TODO: 추가적인 게임 초기화 로직
    }
}