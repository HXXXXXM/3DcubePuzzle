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
    public TextMeshProUGUI timerText_UI; // Ÿ�̸�

    [Header("Game State")]
    private float elapsedTime = 0f;
    private bool isTimerRunning = false;
    private string finalClearTime = ""; // Ŭ���� �� �ð��� ������ ����


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

        // ���� ���� �� Ŭ���� �г� ��Ȱ��ȭ �� Ÿ�̸� �ʱ�ȭ/����
        if (clearPanel_UI != null) clearPanel_UI.SetActive(false);
        // SetupNewPuzzle(); // GameSetup���� ȣ��ǰų� ���⼭ ȣ��Ǵ� �κп��� Ÿ�̸� ����

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

        // Ŭ���� ȭ���� ���������� �Է� ����
        if (clearPanel_UI != null && clearPanel_UI.activeSelf) return;

        HandlePieceInteractions();

        if (Input.GetKeyDown(KeyCode.Space)) // �ӽ� Ŭ���� ���� Ȯ��
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

        // Ÿ�̸� �ʱ�ȭ �� ����
        elapsedTime = 0f;
        isTimerRunning = true;
        UpdateTimerDisplay(); // �ʱ� �ð� ǥ�� (00:00)

        if (enableDebugLogs) Debug.Log($"GameManager3D: New Puzzle Setup. Grid: {gridManager.width}x{gridManager.height}x{gridManager.depth}. Pieces: {currentActivePieces.Count}");
    }
    void UpdateTimerDisplay()
    {
        if (timerText_UI != null)
        {
            // �ð��� �а� �ʷ� ��ȯ
            int minutes = Mathf.FloorToInt(elapsedTime / 60F);
            int seconds = Mathf.FloorToInt(elapsedTime % 60F);
            // �� �ڸ� ���ڷ� ������ (��: 01:05)
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
        if (Input.GetMouseButton(0) && selectedPiece != null) DragSelectedPiece(); // �� ������ �巡��
        if (Input.GetMouseButtonUp(0) && selectedPiece != null) ReleaseSelectedPiece();
        if (selectedPiece != null) HandleCameraRelativeRotation();
    }

    void TrySelectPiece() // ���� �亯�� ����� �αװ� ���Ե� ���� ���
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
                        // TODO: ���� ���� ���� ó�� (��: ���� ��ġ�� �ǵ�����)
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
                pieceDragPlane = new Plane(mainCamera.transform.forward, initialSnapPos); // �巡�� ����� ���⼭ �� �� ����
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
            // TODO: ��ġ �� �� ������ ���� ��ġ�� �ǵ����� ����
            selectedPiece = null;
        }
    }

    void DragSelectedPiece()
    {
        if (selectedPiece == null || gridManager == null || mainCamera == null) return; // mainCamera null üũ �߰�

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        // pieceDragPlane�� TrySelectPiece���� ������ ���� ��� ���
        if (pieceDragPlane.Raycast(ray, out float dist))
        {
            if (enableDebugLogs) Debug.Log($"Dragging: Ray hit drag plane at distance {dist}");
            Vector3 pointOnPlane = ray.GetPoint(dist);
            selectedPiece.transform.position = gridManager.GetSnappedWorldPosition(pointOnPlane + pieceDragOffset);
        }
        else if (enableDebugLogs)
        {
            // �� �αװ� ��� ��ٸ�, ī�޶� �ʹ� ���� �������� ���콺 ���̰� ���� dragPlane�� �������� �ʴ� ��Ȳ�� �� ����.
            // �Ǵ� pieceDragPlane�� �߸� ������ ���.
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
                gridManager.OccupyCell(index); // �ܻ��̹Ƿ� ���� ���� ���� ����
            }
            pc.isPlaced = true;
            pc.lastPlacedIndices.Clear();
            pc.lastPlacedIndices.AddRange(occupiedIndices);
            if (enableDebugLogs) Debug.Log($"Piece '{selectedPiece.name}' PLACED.");
            if (IsPuzzleComplete())
            {
                isTimerRunning = false; // Ŭ���� �� Ÿ�̸� ����
                finalClearTime = timerText_UI.text; // ���� �ð� ���
                HandlePuzzleComplete();
            }
        }
        else
        {
            if (enableDebugLogs) Debug.LogWarning($"Piece '{selectedPiece.name}' placement FAILED. Stays selected, isPlaced = false.");
            pc.isPlaced = false; pc.lastPlacedIndices.Clear();
            // TODO: ��ġ ���� �� ������ ������ ��ġ�� �̵� (��: �巡�� ���� �� ��ġ)
        }
        // selectedPiece�� ��� ���õ� ���·� �� (�ٸ� ���� Ŭ�� �� ����)
    }

    void HandleCameraRelativeRotation()
    {
        if (selectedPiece == null || mainCamera == null) return;
        Vector3 rotationAxis = Vector3.zero; float currentRotationAmount = 0f;
        Vector3 camUpWorld = mainCamera.transform.up; Vector3 camRightWorld = mainCamera.transform.right;
        // Vector3 camForwardWorld = mainCamera.transform.forward; // �ʿ��

        Vector3 yawRotationAxis = GetDominantWorldAxis(camUpWorld);
        Vector3 pitchRotationAxis = GetDominantWorldAxis(camRightWorld);
        // Vector3 rollRotationAxis = GetDominantWorldAxis(camForwardWorld); // Q/E ������ �� ȸ�� �߰� ��

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

        // TODO: ȸ�� �� ��ȿ�� �˻� (ȸ�� �� ������ ������ ����ִ���, �׸��� ���� ������ ��)
        // bool canRotate = CheckRotationValidity(selectedPiece, worldAxis, angle);
        // if (!canRotate) {
        //     if(enableDebugLogs) Debug.LogWarning("Rotation blocked due to invalid placement after rotation.");
        //     return;
        // }

        if (pc != null && pc.isPlaced) // �̹� ��ġ�� ������ ȸ����Ű�� ���
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

        // Ŭ���� UI Panel Ȱ��ȭ
        if (clearPanel_UI != null)
        {
            clearPanel_UI.SetActive(true);
            // Ŭ���� �г� ���� �ؽ�Ʈ�� ã�� �ҿ� �ð� ������Ʈ
            TextMeshProUGUI clearTimeText = clearPanel_UI.transform.Find("ClearTimeText")?.GetComponent<TextMeshProUGUI>(); // ���� �̸�
            if (clearTimeText != null)
            {
                clearTimeText.text = "Clear Time: " + finalClearTime;
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("ClearTimeText object not found in ClearPanel_UI or TextMeshProUGUI component missing.");
            }
        }

        selectedPiece = null; // �� �̻� ���� �Ұ�
    }
    public void RetryGame()
    {
        if (enableDebugLogs) Debug.Log("RetryGame button clicked.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        if (enableDebugLogs) Debug.Log("GoToMainMenu button clicked.");
        Time.timeScale = 1f; // �ð� �帧 ���� (���� �޴������� ���������� �����ϵ���)
        SceneManager.LoadScene("MainMenuScene"); // "MainMenuScene"�̶�� �̸��� ������ �̵�
    }

    // ������ ���� �Լ� (TrySelectPiece���� ���)
    string GetSingleLayerFromMask(int layerMask)
    {
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0) return LayerMask.LayerToName(i);
        }
        return "Nothing_Or_Everything_If_~0"; // ~0�̸� Everything
    }
}