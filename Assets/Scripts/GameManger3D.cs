using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager3D : MonoBehaviour
{
    [Header("Required Components (Assign in Inspector)")]
    public GridManager3D gridManager;
    public PieceSpawner pieceSpawner;
    private Camera mainCamera;

    [Header("Piece Control Settings")]
    public float rotationAmount = 90.0f;
    public KeyCode rotateClockwiseKey = KeyCode.A;
    public KeyCode rotateCounterClockwiseKey = KeyCode.D;
    public KeyCode rotateForwardKey = KeyCode.W;
    public KeyCode rotateBackwardKey = KeyCode.S;

    private GameObject selectedPiece = null;
    private Vector3 pieceDragOffset;
    private Plane pieceDragPlane;
    private List<GameObject> currentActivePieces = new List<GameObject>();

    private int pieceLayerMaskValue;

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public Color rayHitColor = Color.green;
    public Color rayMissColor = Color.red;
    public float rayDuration = 1.0f;

    [Header("UI References (Assign in Inspector)")]
    public GameObject clearPanel_UI;

    [Header("Timer UI (Assign in Inspector)")]
    public TextMeshProUGUI timerText_UI; // 타이머

    [Header("Game State")]
    private float elapsedTime = 0f;
    private bool isTimerRunning = false;
    private string finalClearTime = ""; // 클리어 시 시간을 저장할 변수


    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogError("Main Camera not found.");

        if (gridManager == null) gridManager = FindObjectOfType<GridManager3D>();
        if (gridManager == null) Debug.LogError("GridManager3D not found!");

        if (pieceSpawner == null) pieceSpawner = FindObjectOfType<PieceSpawner>();
        if (pieceSpawner == null) Debug.LogError("PieceSpawner not found!");
        else
        {
            Transform puzzleCenter = (gridManager != null) ? gridManager.transform : null;
            pieceSpawner.Initialize(gridManager, puzzleCenter);
        }

        int pieceLayerIndex = LayerMask.NameToLayer("PieceLayer");
        if (pieceLayerIndex == -1) { Debug.LogError("'PieceLayer' layer not found."); pieceLayerMaskValue = ~0; }
        else { pieceLayerMaskValue = (1 << pieceLayerIndex); }

        if (FindObjectOfType<GameSetup>() == null)
        {
            Debug.LogWarning("GameSetup not found. GameManager3D starting default puzzle.");
            if (gridManager != null && pieceSpawner != null)
            {
                gridManager.width = 3; gridManager.height = 3; gridManager.depth = 3;
                gridManager.InitializeGridAndCells();
                SetupNewPuzzle();
            }
            else { Debug.LogError("Cannot start default puzzle: GridManager or PieceSpawner not ready."); }
        }

        if (timerText_UI == null)
        {
            Debug.LogWarning("TimerText_UI is not assigned in GameManager3D Inspector.");
        }

        // 게임 시작 시 클리어 패널 비활성화 및 타이머 초기화/시작
        if (clearPanel_UI != null) clearPanel_UI.SetActive(false);
        // SetupNewPuzzle(); // GameSetup에서 호출되거나 여기서 호출되는 부분에서 타이머 시작

        if (clearPanel_UI != null)
        {
            clearPanel_UI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ClearPanel_UI is not assigned in GameManager3D Inspector.");
        }
    }

    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }

        // 클리어 화면이 켜져있으면 입력 무시
        if (clearPanel_UI != null && clearPanel_UI.activeSelf) return;

        HandlePieceInteractions();

        if (Input.GetKeyDown(KeyCode.Space)) // 임시 클리어 조건 확인
        {
            if (IsPuzzleComplete()) HandlePuzzleComplete();
            else if (enableDebugLogs) Debug.Log("Puzzle not yet complete (manual check).");
        }
    }

    public void SetupNewPuzzle()
    {
        foreach (GameObject piece in currentActivePieces) { if (piece != null) Destroy(piece); }
        currentActivePieces.Clear();
        if (gridManager != null) { gridManager.ResetGridOccupancy(); }
        if (pieceSpawner != null)
        {
            currentActivePieces = pieceSpawner.GenerateSomaCubePiecesInWorld();
        }
        else Debug.LogError("PieceSpawner not available.");
        selectedPiece = null;

        // 타이머 초기화 및 시작
        elapsedTime = 0f;
        isTimerRunning = true;
        UpdateTimerDisplay(); // 초기 시간 표시 (00:00)

        if (enableDebugLogs) Debug.Log($"GameManager3D: New Puzzle Setup. Grid: {gridManager.width}x{gridManager.height}x{gridManager.depth}. Pieces: {currentActivePieces.Count}");
    }
    void UpdateTimerDisplay()
    {
        if (timerText_UI != null)
        {
            // 시간을 분과 초로 변환
            int minutes = Mathf.FloorToInt(elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedTime % 60F);
            // 두 자리 숫자로 포맷팅 (예: 01:05)
            timerText_UI.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    void HandlePieceInteractions()
    {
        /*
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            if (enableDebugLogs) Debug.Log("Mouse is over a UI element, 3D interaction blocked.");
            return;
        }
        */

        if (Input.GetMouseButtonDown(0)) TrySelectPiece();
        if (Input.GetMouseButton(0) && selectedPiece != null) DragSelectedPiece(); // 매 프레임 드래그
        if (Input.GetMouseButtonUp(0) && selectedPiece != null) ReleaseSelectedPiece();
        if (selectedPiece != null) HandleCameraRelativeRotation();
    }

    void TrySelectPiece() // 이전 답변의 디버깅 로그가 포함된 버전 사용
    {
        if (enableDebugLogs) Debug.Log("TrySelectPiece() CALLED");

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (enableDebugLogs)
        {
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, rayDuration);
        }

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, pieceLayerMaskValue))
        {
            if (enableDebugLogs)
            {
                Debug.DrawLine(ray.origin, hit.point, rayHitColor, rayDuration);
                Debug.Log($"Raycast HIT: Obj '{hit.collider.gameObject.name}', Tag:'{hit.collider.tag}', Layer:'{LayerMask.LayerToName(hit.collider.gameObject.layer)}'");
            }

            PieceController3D hitPc = hit.collider.GetComponentInParent<PieceController3D>();
            if (hitPc != null)
            {
                if (enableDebugLogs) Debug.Log("SUCCESS: Hit a potential piece with PieceController3D!");
                GameObject newlyClickedPiece = hitPc.gameObject;

                if (selectedPiece != null && selectedPiece != newlyClickedPiece)
                {
                    PieceController3D prevSelectedPc = selectedPiece.GetComponent<PieceController3D>();
                    if (prevSelectedPc != null && !prevSelectedPc.isPlaced)
                    {
                        if (enableDebugLogs) Debug.Log($"Switched selection from unplaced piece '{selectedPiece.name}'.");
                        // TODO: 이전 선택 조각 처리 (예: 스폰 위치로 되돌리기)
                    }
                }
                selectedPiece = newlyClickedPiece;

                if (hitPc.isPlaced)
                {
                    foreach (Vector3Int oldIndex in hitPc.lastPlacedIndices)
                        if (gridManager != null) gridManager.FreeCell(oldIndex);
                    hitPc.isPlaced = false; hitPc.lastPlacedIndices.Clear();
                    if (enableDebugLogs) Debug.Log($"Picked up placed piece '{selectedPiece.name}'. Freed its cells.");
                }

                if (enableDebugLogs) Debug.Log("Selected: " + selectedPiece.name);
                Vector3 initialSnapPos = gridManager != null ? gridManager.GetSnappedWorldPosition(selectedPiece.transform.position) : selectedPiece.transform.position;
                selectedPiece.transform.position = initialSnapPos;
                pieceDragPlane = new Plane(mainCamera.transform.forward, initialSnapPos); // 드래그 평면은 여기서 한 번 설정
                Ray planeRay = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (pieceDragPlane.Raycast(planeRay, out float dist))
                    pieceDragOffset = selectedPiece.transform.position - planeRay.GetPoint(dist);
                else if (enableDebugLogs) Debug.LogWarning("Failed to raycast to pieceDragPlane for offset calculation in TrySelectPiece.");
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning($"Raycast HIT '{hit.collider.gameObject.name}', but it's NOT a piece (no PieceController3D).");
            }
        }
        else
        {
            if (enableDebugLogs) Debug.Log("Raycast MISSED. No object hit on 'PieceLayer'.");
            if (Input.GetMouseButtonDown(0) && selectedPiece != null)
            {
                DeselectCurrentPieceIfNotPlaced();
            }
        }
    }

    void DeselectCurrentPieceIfNotPlaced()
    {
        if (selectedPiece == null) return;
        PieceController3D pc = selectedPiece.GetComponent<PieceController3D>();
        if (pc != null && !pc.isPlaced)
        {
            if (enableDebugLogs) Debug.Log($"Deselected unplaced piece '{selectedPiece.name}'. Consider returning to spawn.");
            // TODO: 배치 안 된 조각을 스폰 위치로 되돌리는 로직
            selectedPiece = null;
        }
    }

    void DragSelectedPiece()
    {
        if (selectedPiece == null || gridManager == null || mainCamera == null) return; // mainCamera null 체크 추가

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        // pieceDragPlane은 TrySelectPiece에서 설정된 것을 계속 사용
        if (pieceDragPlane.Raycast(ray, out float dist))
        {
            if (enableDebugLogs) Debug.Log($"Dragging: Ray hit drag plane at distance {dist}");
            Vector3 pointOnPlane = ray.GetPoint(dist);
            selectedPiece.transform.position = gridManager.GetSnappedWorldPosition(pointOnPlane + pieceDragOffset);
        }
        else if (enableDebugLogs)
        {
            // 이 로그가 계속 뜬다면, 카메라가 너무 많이 움직여서 마우스 레이가 기존 dragPlane과 교차하지 않는 상황일 수 있음.
            // 또는 pieceDragPlane이 잘못 설정된 경우.
            Debug.LogWarning("Dragging: Ray MISSED drag plane. Piece might not move as expected.");
        }
    }

    void ReleaseSelectedPiece()
    {
        if (selectedPiece == null || gridManager == null) return;
        PieceController3D pc = selectedPiece.GetComponent<PieceController3D>();
        if (pc == null) { selectedPiece = null; return; }

        selectedPiece.transform.position = gridManager.GetSnappedWorldPosition(selectedPiece.transform.position);
        List<Vector3Int> occupiedIndices = pc.GetOccupiedGridIndices(gridManager);
        bool canPlace = occupiedIndices.Count > 0;

        foreach (Vector3Int index in occupiedIndices)
        {
            if (!gridManager.IsValidGridIndex(index) || !gridManager.IsCellEmpty(index))
            {
                canPlace = false;
                if (enableDebugLogs) Debug.LogWarning($"Placement FAIL '{selectedPiece.name}': Cell {index} invalid/occupied.");
                break;
            }
        }

        if (canPlace)
        {
            foreach (Vector3Int index in occupiedIndices)
            {
                gridManager.OccupyCell(index); // 단색이므로 색상 정보 없이 점유
            }
            pc.isPlaced = true;
            pc.lastPlacedIndices.Clear();
            pc.lastPlacedIndices.AddRange(occupiedIndices);
            if (enableDebugLogs) Debug.Log($"Piece '{selectedPiece.name}' PLACED.");
            if (IsPuzzleComplete())
            {
                isTimerRunning = false; // 클리어 시 타이머 중지
                finalClearTime = timerText_UI.text; // 현재 시간 기록
                HandlePuzzleComplete();
            }
        }
        else
        {
            if (enableDebugLogs) Debug.LogWarning($"Piece '{selectedPiece.name}' placement FAILED. Stays selected, isPlaced = false.");
            pc.isPlaced = false; pc.lastPlacedIndices.Clear();
            // TODO: 배치 실패 시 조각을 안전한 위치로 이동 (예: 드래그 시작 전 위치)
        }
        // selectedPiece는 계속 선택된 상태로 둠 (다른 조각 클릭 시 변경)
    }

    void HandleCameraRelativeRotation()
    {
        if (selectedPiece == null || mainCamera == null) return;
        Vector3 rotationAxis = Vector3.zero; float currentRotationAmount = 0f;
        Vector3 camUpWorld = mainCamera.transform.up; Vector3 camRightWorld = mainCamera.transform.right;
        // Vector3 camForwardWorld = mainCamera.transform.forward; // 필요시

        Vector3 yawRotationAxis = GetDominantWorldAxis(camUpWorld);
        Vector3 pitchRotationAxis = GetDominantWorldAxis(camRightWorld);
        // Vector3 rollRotationAxis = GetDominantWorldAxis(camForwardWorld); // Q/E 등으로 롤 회전 추가 시

        if (Input.GetKeyDown(rotateClockwiseKey)) { rotationAxis = yawRotationAxis; currentRotationAmount = rotationAmount; }
        else if (Input.GetKeyDown(rotateCounterClockwiseKey)) { rotationAxis = yawRotationAxis; currentRotationAmount = -rotationAmount; }
        else if (Input.GetKeyDown(rotateForwardKey)) { rotationAxis = pitchRotationAxis; currentRotationAmount = rotationAmount; }
        else if (Input.GetKeyDown(rotateBackwardKey)) { rotationAxis = pitchRotationAxis; currentRotationAmount = -rotationAmount; }

        if (rotationAxis != Vector3.zero) ApplyRotation(rotationAxis, currentRotationAmount);
    }

    Vector3 GetDominantWorldAxis(Vector3 direction)
    {
        float absX = Mathf.Abs(direction.x); float absY = Mathf.Abs(direction.y); float absZ = Mathf.Abs(direction.z);
        if (absX > absY && absX > absZ) return new Vector3(Mathf.Sign(direction.x), 0, 0);
        else if (absY > absX && absY > absZ) return new Vector3(0, Mathf.Sign(direction.y), 0);
        else return new Vector3(0, 0, Mathf.Sign(direction.z));
    }

    void ApplyRotation(Vector3 worldAxis, float angle)
    {
        if (selectedPiece == null) return;
        PieceController3D pc = selectedPiece.GetComponent<PieceController3D>();

        // TODO: 회전 전 유효성 검사 (회전 후 차지할 셀들이 비어있는지, 그리드 범위 내인지 등)
        // bool canRotate = CheckRotationValidity(selectedPiece, worldAxis, angle);
        // if (!canRotate) {
        //     if(enableDebugLogs) Debug.LogWarning("Rotation blocked due to invalid placement after rotation.");
        //     return;
        // }

        if (pc != null && pc.isPlaced) // 이미 배치된 조각을 회전시키는 경우
        {
            foreach (var idx in pc.lastPlacedIndices) if (gridManager != null) gridManager.FreeCell(idx);
            pc.isPlaced = false; pc.lastPlacedIndices.Clear();
            if (enableDebugLogs) Debug.Log("Rotated a placed piece. It's now unplaced.");
        }

        selectedPiece.transform.RotateAround(selectedPiece.transform.position, worldAxis, angle);
        if (enableDebugLogs) Debug.Log($"Rotated: {selectedPiece.name} around {worldAxis} by {angle}. New Euler: {selectedPiece.transform.eulerAngles}");

        if (gridManager != null)
            selectedPiece.transform.position = gridManager.GetSnappedWorldPosition(selectedPiece.transform.position);
    }

    bool IsPuzzleComplete()
    {
        if (gridManager == null) return false;
        bool allCellsFilled = gridManager.AreAllCellsOccupied();
        if (allCellsFilled && enableDebugLogs) Debug.Log("Win Condition Met: All cells are occupied!");
        return allCellsFilled;
    }

    void HandlePuzzleComplete()
    {
        if (enableDebugLogs) Debug.Log("===== PUZZLE COMPLETE! Time: {finalClearTime} =====");

        // 클리어 UI Panel 활성화
        if (clearPanel_UI != null)
        {
            clearPanel_UI.SetActive(true);
            // 클리어 패널 내의 텍스트를 찾아 소요 시간 업데이트
            TextMeshProUGUI clearTimeText = clearPanel_UI.transform.Find("ClearTimeText")?.GetComponent<TextMeshProUGUI>(); // 예시 이름
            if (clearTimeText != null)
            {
                clearTimeText.text = "Clear Time: " + finalClearTime;
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("ClearTimeText object not found in ClearPanel_UI or TextMeshProUGUI component missing.");
            }
        }

        selectedPiece = null; // 더 이상 조작 불가
    }
    public void RetryGame()
    {
        if (enableDebugLogs) Debug.Log("RetryGame button clicked.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        if (enableDebugLogs) Debug.Log("GoToMainMenu button clicked.");
        Time.timeScale = 1f; // 시간 흐름 복구 (메인 메뉴에서도 정상적으로 동작하도록)
        SceneManager.LoadScene("MainMenuScene"); // "MainMenuScene"이라는 이름의 씬으로 이동
    }

    // 디버깅용 헬퍼 함수 (TrySelectPiece에서 사용)
    string GetSingleLayerFromMask(int layerMask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0) return LayerMask.LayerToName(i);
        }
        return "Nothing_Or_Everything_If_~0"; // ~0이면 Everything
    }
}