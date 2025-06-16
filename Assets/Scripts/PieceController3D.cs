using UnityEngine;
using System.Collections.Generic;

public class PieceController3D : MonoBehaviour
{
    [Header("Piece Definition")]
    [Tooltip("Local integer coordinates of each unit cell relative to this piece's pivot (root). Assign in Inspector.")]
    public List<Vector3Int> unitCellLocalPositions = new List<Vector3Int>();

    // 단색 조각이므로, 이 프리팹의 자식 셀들에 적용된 머티리얼이 그대로 사용됩니다.
    // 또는, 모든 조각에 일괄적으로 적용할 단일 머티리얼을 GameManager 등에서 관리하고
    // 조각 생성 시점에 적용할 수도 있습니다. 여기서는 프리팹에 설정된 머티리얼을 사용한다고 가정합니다.

    [HideInInspector] public bool isPlaced = false;
    [HideInInspector] public List<Vector3Int> lastPlacedIndices = new List<Vector3Int>();

    void Awake()
    {
        // unitCellLocalPositions 자동 채우기 로직 (선택 사항, Inspector에서 정확히 설정 권장)
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
        if (unitCellLocalPositions.Count == 0) // 단일 셀 조각
        {
            unitCellLocalPositions.Add(Vector3Int.zero);
        }
    }

    /// <summary>
    /// 현재 조각의 월드 위치와 회전을 기준으로, 각 단위 셀이 차지할 그리드 인덱스 목록을 반환합니다.
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