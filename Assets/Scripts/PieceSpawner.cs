using UnityEngine;
using System.Collections.Generic;

public class PieceSpawner : MonoBehaviour
{
    [Header("Soma Cube Piece Prefabs (Assign in Inspector)")]
    // �� �������� PieceController3D�� ������ �ְ�,
    // ���ϴ� ���� ������ ��Ƽ������ �̹� ����Ǿ� �־�� ��.
    public GameObject prefabSoma_A;
    public GameObject prefabSoma_B;
    public GameObject prefabSoma_L;
    public GameObject prefabSoma_P;
    public GameObject prefabSoma_T;
    public GameObject prefabSoma_V;
    public GameObject prefabSoma_Z;


    [Header("World Spawning Configuration")]
    public Transform puzzleCubeCenter;
    public float minSpawnRadius = 5f;
    public float maxSpawnRadius = 8f;
    public float spawnHeightOffset = 1f;
    // public Transform pieceWorldSpawnParent; // ���� ����

    private GridManager3D gridManager; // ���� ���� ��� X

    // GameManager�κ��� GridManager�� PuzzleCube �߽� Transform ���� ����
    public void Initialize(GridManager3D gridManagerRef, Transform puzzleCenterRef)
    {
        this.gridManager = gridManagerRef; // ����� ��� �� ��
        if (puzzleCenterRef != null)
        {
            this.puzzleCubeCenter = puzzleCenterRef;
        }
        else if (gridManagerRef != null)
        {
            this.puzzleCubeCenter = gridManagerRef.transform; // GridManager ��ü�� �߽��� ����
        }
        else
        {
            Debug.LogError("PieceSpawner: PuzzleCubeCenter could not be determined during Initialize!");
        }
    }

    /// <summary>
    /// �Ҹ� ť�� ���� ��Ʈ�� ���忡 �����ϰ� ��ȯ�մϴ�. (�ܻ� ����)
    /// </summary>
    public List<GameObject> GenerateSomaCubePiecesInWorld() // numColorsToUse �Ķ���� ����
    {
        List<GameObject> spawnedPieces = new List<GameObject>();

        if (puzzleCubeCenter == null)
        {
            Debug.LogError("PieceSpawner: Cannot generate pieces. PuzzleCubeCenter not set. Call Initialize first.");
            return spawnedPieces;
        }

        List<GameObject> somaPrefabs = new List<GameObject>
        {
            prefabSoma_V, prefabSoma_L, prefabSoma_T, prefabSoma_Z,
            prefabSoma_A, prefabSoma_B, prefabSoma_P
        };

        // ������ �Ҵ� Ȯ��
        foreach (var prefab in somaPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("PieceSpawner: One or more Soma Cube prefabs are not assigned in the Inspector!");
                return new List<GameObject>(); // �� ����Ʈ ��ȯ
            }
        }

        Vector3 currentSpawnPosBase = puzzleCubeCenter.position; // ���� ������

        for (int i = 0; i < somaPrefabs.Count; i++)
        {
            GameObject piecePrefab = somaPrefabs[i];

            // ���� ���� ��ġ ���
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 spawnPosition = currentSpawnPosBase + spawnDirection * radius + Vector3.up * spawnHeightOffset;

            GameObject newPiece = Instantiate(piecePrefab /*, pieceWorldSpawnParent */);
            newPiece.transform.position = spawnPosition;
            newPiece.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

            PieceController3D pc = newPiece.GetComponent<PieceController3D>();
            if (pc != null)
            {
                // colorType ���� ���� ����
                pc.isPlaced = false;
                pc.lastPlacedIndices.Clear();
                // PieceController3D�� Start/Awake���� �����տ� ������ ��Ƽ������ ����ϰų�,
                // �Ǵ� PieceController3D���� colorType ���� ��ü�� ���ٸ� �ƹ��͵� ���ص� ��.
            }
            else
            {
                Debug.LogError($"PieceSpawner: Spawned Soma piece '{piecePrefab.name}' is missing PieceController3D component.");
            }
            spawnedPieces.Add(newPiece);
        }

        Debug.Log($"PieceSpawner: Generated {spawnedPieces.Count} Soma Cube pieces in the world (single color based on prefabs).");
        return spawnedPieces;
    }

    // GetAvailableColors �Լ� ���ŵ�
}