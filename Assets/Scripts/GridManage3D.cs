using UnityEngine;

public class GridManager3D : MonoBehaviour
{
    [Header("Grid Settings (Typically set by GameSetup)")]
    public int width = 3;
    public int height = 3;
    public int depth = 3;
    public float cellSize = 1.0f;
    public GameObject gridCellPrefab_VisualOnly; // 빈 그리드 셀 시각화용 프리팹 (선택 사항)

    // 시각적 셀을 위한 머티리얼 (선택 사항, gridCellPrefab_VisualOnly에 이미 설정되어 있을 수 있음)
    // public Material emptyCellMaterial_VisualOnly;

    private bool[,,] isOccupiedArray; // 각 셀이 점유되었는지 여부만 저장
    private Vector3 firstCellCenterLocal;
    private GameObject[,,] visualGridCellsArray; // 시각적 셀 오브젝트 저장 (선택 사항)


    public void InitializeGridAndCells()
    {
        Debug.Log($"GridManager3D: InitializeGridAndCells() called with width={width}, height={height}, depth={depth}");

        ClearVisualGrid(); // 기존 시각적 셀들 제거

        isOccupiedArray = new bool[width, height, depth];
        if (gridCellPrefab_VisualOnly != null) // 시각적 셀을 사용한다면
        {
            visualGridCellsArray = new GameObject[width, height, depth];
        }

        float gridTotalWidth = width * cellSize;
        float gridTotalHeight = height * cellSize;
        float gridTotalDepth = depth * cellSize;

        firstCellCenterLocal = new Vector3(
            -gridTotalWidth / 2f + cellSize / 2f,
            -gridTotalHeight / 2f + cellSize / 2f,
            -gridTotalDepth / 2f + cellSize / 2f
        );

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    isOccupiedArray[x, y, z] = false; // 모든 셀은 초기에 비어있음

                    if (gridCellPrefab_VisualOnly != null) // 시각적 셀 생성
                    {
                        Vector3 cellLocalPosition = firstCellCenterLocal + new Vector3(x * cellSize, y * cellSize, z * cellSize);
                        GameObject cellVisual = Instantiate(gridCellPrefab_VisualOnly, transform);
                        cellVisual.transform.localPosition = cellLocalPosition;
                        cellVisual.transform.localScale = Vector3.one * cellSize;
                        cellVisual.name = $"VisualCell_{x}_{y}_{z}";
                        visualGridCellsArray[x, y, z] = cellVisual;
                        // if (emptyCellMaterial_VisualOnly != null) cellVisual.GetComponent<Renderer>().material = emptyCellMaterial_VisualOnly;
                    }
                }
            }
        }
        if (Application.isPlaying) Debug.Log($"GridManager3D: Grid initialized ({width}x{height}x{depth}). Occupancy array ready.");
    }

    private void ClearVisualGrid()
    {
        // 자식으로 생성된 모든 시각적 셀 오브젝트 제거
        foreach (Transform child in transform)
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        visualGridCellsArray = null;
    }

    public void ResetGridOccupancy()
    {
        if (isOccupiedArray == null) return;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    isOccupiedArray[x, y, z] = false;
                    // 시각적 셀 업데이트는 필요 없음 (조각이 사라지면 빈 공간이 드러남)
                    // 만약 빈 셀 모양을 다르게 하고 싶다면 여기서 visualGridCellsArray[x,y,z]의 머티리얼 변경
                }
            }
        }
        if (Application.isPlaying) Debug.Log("GridManager3D: Grid occupancy reset to all empty.");
    }

    public Vector3Int WorldToGridIndex(Vector3 worldPosition)
    {
        if (cellSize <= 0) { Debug.LogError("CellSize must be > 0."); return Vector3Int.one * -1; }
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        int x = Mathf.RoundToInt((localPos.x - firstCellCenterLocal.x) / cellSize);
        int y = Mathf.RoundToInt((localPos.y - firstCellCenterLocal.y) / cellSize);
        int z = Mathf.RoundToInt((localPos.z - firstCellCenterLocal.z) / cellSize);
        return new Vector3Int(x, y, z);
    }

    public Vector3 GridIndexToWorldCellCenter(Vector3Int gridIndex)
    {
        Vector3 cellCenterLocalPos = firstCellCenterLocal + new Vector3(gridIndex.x * cellSize, gridIndex.y * cellSize, gridIndex.z * cellSize);
        return transform.TransformPoint(cellCenterLocalPos);
    }

    public Vector3 GetSnappedWorldPosition(Vector3 worldPosition)
    {
        Vector3Int gridIndex = WorldToGridIndex(worldPosition);
        return GridIndexToWorldCellCenter(gridIndex);
    }

    public bool IsValidGridIndex(Vector3Int gridIndex)
    {
        return gridIndex.x >= 0 && gridIndex.x < width &&
               gridIndex.y >= 0 && gridIndex.y < height &&
               gridIndex.z >= 0 && gridIndex.z < depth;
    }

    public bool IsCellEmpty(Vector3Int gridIndex)
    {
        if (IsValidGridIndex(gridIndex))
        {
            return !isOccupiedArray[gridIndex.x, gridIndex.y, gridIndex.z];
        }
        return false; // 유효하지 않은 인덱스는 비어있지 않다고 간주 (배치 불가)
    }

    public void OccupyCell(Vector3Int gridIndex) // 색상 파라미터 제거
    {
        if (IsValidGridIndex(gridIndex))
        {
            isOccupiedArray[gridIndex.x, gridIndex.y, gridIndex.z] = true;
            // 시각적 셀 업데이트 불필요 (배치된 조각이 그 자리를 덮음)
            // 만약 빈 셀 오브젝트를 숨기거나 하고 싶다면 여기서 visualGridCellsArray[x,y,z].SetActive(false);
        }
        else Debug.LogWarning($"Attempted to occupy invalid grid index: {gridIndex}");
    }

    public void FreeCell(Vector3Int gridIndex) // 색상 파라미터 제거
    {
        if (IsValidGridIndex(gridIndex))
        {
            isOccupiedArray[gridIndex.x, gridIndex.y, gridIndex.z] = false;
            // 시각적 셀 업데이트 불필요
            // 만약 숨겼던 빈 셀 오브젝트를 다시 보이게 하려면 여기서 visualGridCellsArray[x,y,z].SetActive(true);
        }
        else Debug.LogWarning($"Attempted to free invalid grid index: {gridIndex}");
    }

    /// <summary>
    /// 모든 셀이 채워졌는지 확인합니다. (클리어 조건용)
    /// </summary>
    public bool AreAllCellsOccupied()
    {
        if (isOccupiedArray == null) // 또는 cellOccupiedByColorArray 사용 시 (단색이면 isOccupiedArray가 더 적합)
        {
            Debug.LogError("GridManager3D: Occupancy array not initialized for win check!");
            return false;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (!isOccupiedArray[x, y, z]) // 하나라도 비어있으면 (false 이면)
                    {
                        return false; // 아직 클리어 아님
                    }
                }
            }
        }
        return true; // 모든 셀이 점유됨 (true)
    }
}