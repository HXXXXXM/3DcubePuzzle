using UnityEngine;
using System.Collections.Generic;

public class PieceSpawner : MonoBehaviour
{
    [Header("Soma Cube Piece Prefabs (Assign in Inspector)")]
    // 각 프리팹은 PieceController3D를 가지고 있고,
    // 원하는 단일 색상의 머티리얼이 이미 적용되어 있어야 함.
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
    // public Transform pieceWorldSpawnParent; // 선택 사항

    private GridManager3D gridManager; // 현재 직접 사용 X

    // GameManager로부터 GridManager와 PuzzleCube 중심 Transform 참조 받음
    public void Initialize(GridManager3D gridManagerRef, Transform puzzleCenterRef)
    {
        this.gridManager = gridManagerRef; // 현재는 사용 안 함
        if (puzzleCenterRef != null)
        {
            this.puzzleCubeCenter = puzzleCenterRef;
        }
        else if (gridManagerRef != null)
        {
            this.puzzleCubeCenter = gridManagerRef.transform; // GridManager 자체가 중심점 역할
        }
        else
        {
            Debug.LogError("PieceSpawner: PuzzleCubeCenter could not be determined during Initialize!");
        }
    }

    /// <summary>
    /// 소마 큐브 조각 세트를 월드에 생성하고 반환합니다. (단색 버전)
    /// </summary>
    public List<GameObject> GenerateSomaCubePiecesInWorld() // numColorsToUse 파라미터 제거
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

        // 프리팹 할당 확인
        foreach (var prefab in somaPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("PieceSpawner: One or more Soma Cube prefabs are not assigned in the Inspector!");
                return new List<GameObject>(); // 빈 리스트 반환
            }
        }

        Vector3 currentSpawnPosBase = puzzleCubeCenter.position; // 스폰 기준점

        for (int i = 0; i < somaPrefabs.Count; i++)
        {
            GameObject piecePrefab = somaPrefabs[i];

            // 랜덤 스폰 위치 계산
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
                // colorType 설정 로직 제거
                pc.isPlaced = false;
                pc.lastPlacedIndices.Clear();
                // PieceController3D의 Start/Awake에서 프리팹에 설정된 머티리얼을 사용하거나,
                // 또는 PieceController3D에서 colorType 변수 자체가 없다면 아무것도 안해도 됨.
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

    // GetAvailableColors 함수 제거됨
}