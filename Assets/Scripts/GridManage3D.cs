using UnityEngine;

public class GridManager3D : MonoBehaviour
{
    [Header("Grid Settings (Typically set by GameSetup)")]
    public int width = 3;
    public int height = 3;
    public int depth = 3;
    public float cellSize = 1.0f;
    public GameObject gridCellPrefab_VisualOnly; // �� �׸��� �� �ð�ȭ�� ������ (���� ����)

    // �ð��� ���� ���� ��Ƽ���� (���� ����, gridCellPrefab_VisualOnly�� �̹� �����Ǿ� ���� �� ����)
    // public Material emptyCellMaterial_VisualOnly;

    private bool[,,] isOccupiedArray; // �� ���� �����Ǿ����� ���θ� ����
    private Vector3 firstCellCenterLocal;
    private GameObject[,,] visualGridCellsArray; // �ð��� �� ������Ʈ ���� (���� ����)


    public void InitializeGridAndCells()
    {
        Debug.Log($"GridManager3D: InitializeGridAndCells() called with width={width}, height={height}, depth={depth}");

        ClearVisualGrid(); // ���� �ð��� ���� ����

        isOccupiedArray = new bool[width, height, depth];
        if (gridCellPrefab_VisualOnly != null) // �ð��� ���� ����Ѵٸ�
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
                    isOccupiedArray[x, y, z] = false; // ��� ���� �ʱ⿡ �������

                    if (gridCellPrefab_VisualOnly != null) // �ð��� �� ����
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
        // �ڽ����� ������ ��� �ð��� �� ������Ʈ ����
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
                    // �ð��� �� ������Ʈ�� �ʿ� ���� (������ ������� �� ������ �巯��)
                    // ���� �� �� ����� �ٸ��� �ϰ� �ʹٸ� ���⼭ visualGridCellsArray[x,y,z]�� ��Ƽ���� ����
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
        return false; // ��ȿ���� ���� �ε����� ������� �ʴٰ� ���� (��ġ �Ұ�)
    }

    public void OccupyCell(Vector3Int gridIndex) // ���� �Ķ���� ����
    {
        if (IsValidGridIndex(gridIndex))
        {
            isOccupiedArray[gridIndex.x, gridIndex.y, gridIndex.z] = true;
            // �ð��� �� ������Ʈ ���ʿ� (��ġ�� ������ �� �ڸ��� ����)
            // ���� �� �� ������Ʈ�� ����ų� �ϰ� �ʹٸ� ���⼭ visualGridCellsArray[x,y,z].SetActive(false);
        }
        else Debug.LogWarning($"Attempted to occupy invalid grid index: {gridIndex}");
    }

    public void FreeCell(Vector3Int gridIndex) // ���� �Ķ���� ����
    {
        if (IsValidGridIndex(gridIndex))
        {
            isOccupiedArray[gridIndex.x, gridIndex.y, gridIndex.z] = false;
            // �ð��� �� ������Ʈ ���ʿ�
            // ���� ����� �� �� ������Ʈ�� �ٽ� ���̰� �Ϸ��� ���⼭ visualGridCellsArray[x,y,z].SetActive(true);
        }
        else Debug.LogWarning($"Attempted to free invalid grid index: {gridIndex}");
    }

    /// <summary>
    /// ��� ���� ä�������� Ȯ���մϴ�. (Ŭ���� ���ǿ�)
    /// </summary>
    public bool AreAllCellsOccupied()
    {
        if (isOccupiedArray == null) // �Ǵ� cellOccupiedByColorArray ��� �� (�ܻ��̸� isOccupiedArray�� �� ����)
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
                    if (!isOccupiedArray[x, y, z]) // �ϳ��� ��������� (false �̸�)
                    {
                        return false; // ���� Ŭ���� �ƴ�
                    }
                }
            }
        }
        return true; // ��� ���� ������ (true)
    }
}