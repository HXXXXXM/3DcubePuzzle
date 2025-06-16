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
    public int numberOfColorsToUse = 1; // �ܻ� ��ȹ�̹Ƿ� 1�� �����ϰų�, PieceSpawner�� ���������� ó��

    void Start()
    {
        if (gridManager == null) { /* ... null üũ ... */ }
        if (gameManager == null) { /* ... null üũ ... */ }
        InitializeNewPuzzle();
    }

    public void InitializeNewPuzzle()
    {
        if (gridManager == null || gameManager == null)
        {
            Debug.LogError("GameSetup: Cannot initialize puzzle, GridManager or GameManager not properly assigned.");
            return;
        }

        Debug.Log($"GameSetup: Initializing new puzzle ({puzzleWidth}x{puzzleHeight}x{puzzleDepth}). Single color mode."); // numberOfColorsToUse ����

        // 1. GridManager ũ�� ���� �� �׸��� ���� ��û
        gridManager.width = puzzleWidth;
        gridManager.height = puzzleHeight;
        gridManager.depth = puzzleDepth;
        gridManager.InitializeGridAndCells();

        // 2. GameManager���� ���� ���� ��û (�Ķ���� ���� ȣ��)
        gameManager.SetupNewPuzzle(); // ������ �κ�: numberOfColorsToUse �μ� ����

        // TODO: �߰����� ���� �ʱ�ȭ ����
    }
}