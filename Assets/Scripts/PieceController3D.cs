using UnityEngine;
using System.Collections.Generic;

public class PieceController3D : MonoBehaviour
{
    [Header("Piece Definition")]
    [Tooltip("Local integer coordinates of each unit cell relative to this piece's pivot (root). Assign in Inspector.")]
    public List<Vector3Int> unitCellLocalPositions = new List<Vector3Int>();

    // �ܻ� �����̹Ƿ�, �� �������� �ڽ� ���鿡 ����� ��Ƽ������ �״�� ���˴ϴ�.
    // �Ǵ�, ��� ������ �ϰ������� ������ ���� ��Ƽ������ GameManager ��� �����ϰ�
    // ���� ���� ������ ������ ���� �ֽ��ϴ�. ���⼭�� �����տ� ������ ��Ƽ������ ����Ѵٰ� �����մϴ�.

    [HideInInspector] public bool isPlaced = false;
    [HideInInspector] public List<Vector3Int> lastPlacedIndices = new List<Vector3Int>();

    void Awake()
    {
        // unitCellLocalPositions �ڵ� ä��� ���� (���� ����, Inspector���� ��Ȯ�� ���� ����)
        if (unitCellLocalPositions.Count == 0 && transform.childCount > 0)
        {
            if (Application.isPlaying)
                Debug.LogWarning($"Piece '{gameObject.name}' has no unitCellLocalPositions. Auto-populating from children.");
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    Vector3Int cellPos = Vector3Int.RoundToInt(child.localPosition);
                    if (!unitCellLocalPositions.Contains(cellPos)) unitCellLocalPositions.Add(cellPos);
                }
            }
        }
        if (unitCellLocalPositions.Count == 0) // ���� �� ����
        {
            unitCellLocalPositions.Add(Vector3Int.zero);
        }
    }

    /// <summary>
    /// ���� ������ ���� ��ġ�� ȸ���� ��������, �� ���� ���� ������ �׸��� �ε��� ����� ��ȯ�մϴ�.
    /// </summary>
    public List<Vector3Int> GetOccupiedGridIndices(GridManager3D gridManager)
    {
        if (gridManager == null || unitCellLocalPositions == null || unitCellLocalPositions.Count == 0)
        {
            Debug.LogError($"Cannot GetOccupiedGridIndices for {gameObject.name}: gridManager or unitCellLocalPositions issue.");
            return new List<Vector3Int>();
        }

        List<Vector3Int> occupiedIndices = new List<Vector3Int>();
        Quaternion currentRotation = transform.rotation;
        Vector3 piecePivotWorldPosition = transform.position;

        foreach (Vector3Int localCellRelativeIndex in unitCellLocalPositions)
        {
            Vector3 localOffsetFromPivot = (Vector3)localCellRelativeIndex * gridManager.cellSize;
            Vector3 rotatedOffsetFromPivot = currentRotation * localOffsetFromPivot;
            Vector3 cellWorldPosition = piecePivotWorldPosition + rotatedOffsetFromPivot;
            Vector3Int gridIndex = gridManager.WorldToGridIndex(cellWorldPosition);
            occupiedIndices.Add(gridIndex);
        }
        return occupiedIndices;
    }
}