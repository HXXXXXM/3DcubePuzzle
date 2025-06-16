using UnityEngine;
using UnityEngine.UI; // Button, RawImage 사용
using System.Collections.Generic;
using System.Collections; // 코루틴 사용을 위해

public class PieceButtonGenerator : MonoBehaviour
{
    [Header("UI References (Assign in Inspector)")]
    public GameObject pieceButtonPrefab_UI; // RawImage와 Button 컴포넌트가 있는 UI 버튼 프리팹
    public Transform buttonContainer_UI;   // ScrollView의 Content 오브젝트 (버튼들이 추가될 부모)

    [Header("Preview Rendering (Assign in Inspector)")]
    public Camera piecePreviewCamera;     // "PiecePreview" 레이어만 찍는 전용 카메라
    // public Vector3 previewSpawnPosition = new Vector3(1000f, 1000f, 1000f); // 조각을 임시로 놓을 씬의 보이지 않는 위치
    public Vector3 previewCameraOffset = new Vector3(0, 0, -2.5f); // 미리보기 조각으로부터 카메라가 떨어질 상대적 거리
    public Vector2Int renderTextureSize = new Vector2Int(128, 128); // 생성할 Render Texture의 크기
    public Light previewLight; // 미리보기 조각을 비출 전용 조명 (선택 사항)

    // 생성된 버튼과 연결된 조각 정보 (선택 사항, GameManager에서 관리할 수도 있음)
    // private Dictionary<Button, GameObject> buttonToPiecePrefabMap = new Dictionary<Button, GameObject>();

    void Start()
    {
        if (piecePreviewCamera == null)
        {
            Debug.LogError("PiecePreviewCamera is not assigned in PieceButtonGenerator!");
            this.enabled = false;
            return;
        }
        if (pieceButtonPrefab_UI == null)
        {
            Debug.LogError("PieceButtonPrefab_UI is not assigned!");
            this.enabled = false;
            return;
        }
        if (buttonContainer_UI == null)
        {
            Debug.LogError("ButtonContainer_UI (ScrollView Content) is not assigned!");
            this.enabled = false;
            return;
        }

        // 초기에는 카메라와 조명을 비활성화 해둘 수 있음
        piecePreviewCamera.gameObject.SetActive(false);
        if (previewLight != null) previewLight.gameObject.SetActive(false);
    }

    /// <summary>
    /// 주어진 조각 프리팹 목록에 대해 UI 버튼을 생성하고 미리보기를 렌더링합니다.
    /// GameManager 등에서 호출됩니다.
    /// </summary>
    public void GenerateAndDisplayPieceButtons(List<GameObject> piecePrefabsToDisplay, GameManager3D gameManagerRef)
    {
        // 1. 기존 UI 버튼들 제거
        foreach (Transform child in buttonContainer_UI)
        {
            Destroy(child.gameObject);
        }
        // buttonToPiecePrefabMap.Clear(); // 만약 사용한다면

        if (piecePrefabsToDisplay == null || piecePrefabsToDisplay.Count == 0)
        {
            Debug.LogWarning("No piece prefabs provided to generate buttons.");
            return;
        }

        // 2. 각 조각 프리팹에 대해 버튼 생성 및 미리보기 렌더링 시작
        StartCoroutine(GenerateButtonsCoroutine(piecePrefabsToDisplay, gameManagerRef));
    }

    private IEnumerator GenerateButtonsCoroutine(List<GameObject> piecePrefabs, GameManager3D gameManager)
    {
        // 미리보기 카메라 및 조명 활성화
        piecePreviewCamera.gameObject.SetActive(true);
        if (previewLight != null) previewLight.gameObject.SetActive(true);

        // 미리보기 조각을 놓을 임시 부모 (카메라와 함께 움직이도록 설정 가능)
        GameObject previewAnchor = new GameObject("PreviewAnchor");
        previewAnchor.transform.position = piecePreviewCamera.transform.position + piecePreviewCamera.transform.forward * Mathf.Abs(previewCameraOffset.z); // 카메라 앞 일정 거리에 위치
        if (previewLight != null) previewLight.transform.SetParent(previewAnchor.transform, true); // 조명도 함께

        for (int i = 0; i < piecePrefabs.Count; i++)
        {
            GameObject piecePrefab = piecePrefabs[i];
            if (piecePrefab == null) continue;

            // --- UI 버튼 생성 ---
            GameObject buttonGO = Instantiate(pieceButtonPrefab_UI, buttonContainer_UI);
            RawImage rawImage = buttonGO.GetComponentInChildren<RawImage>();
            Button button = buttonGO.GetComponent<Button>();

            if (rawImage == null || button == null)
            {
                Debug.LogError($"Button prefab '{pieceButtonPrefab_UI.name}' is missing RawImage or Button.");
                Destroy(buttonGO); // 잘못된 버튼은 제거
                continue;
            }

            // --- Render Texture 생성 및 할당 ---
            RenderTexture rt = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24, RenderTextureFormat.Default);
            rt.Create(); // 중요: RT 사용 전 생성 필요
            rawImage.texture = rt;
            rawImage.color = Color.white; // RT가 제대로 보이도록

            // --- 미리보기용 조각 인스턴스 생성 및 설정 ---
            // 이전 미리보기 조각이 있다면 제거
            foreach (Transform child in previewAnchor.transform)
            {
                if (child != previewLight?.transform) Destroy(child.gameObject);
            }

            GameObject previewPieceInstance = Instantiate(piecePrefab, previewAnchor.transform); // Anchor의 자식으로
            previewPieceInstance.transform.localPosition = Vector3.zero; // Anchor의 로컬 (0,0,0)에 배치
            previewPieceInstance.transform.localRotation = Quaternion.Euler(-30, 45, 0); // 미리보기 좋은 각도로 회전 (조절 필요)

            // 모든 자식 오브젝트의 레이어를 "PiecePreview"로 설정
            SetLayerRecursively(previewPieceInstance, LayerMask.NameToLayer("PiecePreview"));

            // PieceController3D를 가져와서 색상에 맞는 머티리얼 적용 (PieceController3D의 Start/Awake에서 처리)
            // PieceController3D pc = previewPieceInstance.GetComponent<PieceController3D>();
            // if (pc != null) { /* pc.SetMaterialBasedOnColor(); 만약 필요하다면 */ }


            // --- 카메라 설정 및 렌더링 ---
            piecePreviewCamera.targetTexture = rt; // 이 카메라의 출력을 이 버튼의 RT로

            // 카메라가 조각을 적절히 비추도록 위치/방향 조절 (중요!)
            // Bounds를 사용하여 조각 크기에 맞게 카메라 거리/FOV/OrthographicSize 조절 필요
            // 여기서는 previewAnchor를 기준으로 카메라가 고정된 오프셋을 가진다고 가정
            // piecePreviewCamera.transform.position = previewAnchor.transform.position + previewCameraOffset;
            // piecePreviewCamera.transform.LookAt(previewAnchor.transform.position); // 항상 Anchor의 중심을 바라봄

            // 조각의 Bounds를 계산하여 카메라가 딱 맞게 비추도록 자동 조절 (고급)
            Bounds bounds = GetRendererBounds(previewPieceInstance);
            FrameObject(piecePreviewCamera, bounds, previewCameraOffset.z); // 카메라가 조각을 프레임에 맞추도록

            piecePreviewCamera.Render(); // 현재 프레임을 RT에 한 번 렌더링
            // previewCamera.targetTexture = null; // 다음 렌더링을 위해 즉시 해제 (선택 사항)

            // --- 버튼 클릭 이벤트 연결 ---
            // 플레이어가 이 UI 버튼을 클릭하면, 실제 게임 월드에 해당 조각을 "선택"하거나 "스폰"하는 로직
            GameObject prefabForButton = piecePrefab; // 클로저를 위해 지역 변수에 할당
            button.onClick.AddListener(() =>
            {
                if (gameManager != null)
                {
                    // gameManager.SelectPieceToSpawn(prefabForButton); // 예시: GameManager에 이런 함수가 있다고 가정
                    Debug.Log($"UI Button clicked for piece: {prefabForButton.name}");
                    // TODO: 이 버튼에 해당하는 조각을 게임 월드에서 선택하거나 새로 생성하는 로직 호출
                }
            });

            // buttonToPiecePrefabMap.Add(button, prefabForButton); // 맵에 추가 (선택 사항)

            yield return null; // 한 프레임 대기하여 각 버튼 미리보기를 순차적으로 생성/렌더링
        }

        // 모든 렌더링 완료 후 미리보기용 오브젝트/카메라 정리
        Destroy(previewAnchor); // 임시 앵커 제거 (조명도 함께 제거됨)
        piecePreviewCamera.targetTexture = null; // 카메라 타겟 텍스처 해제
        piecePreviewCamera.gameObject.SetActive(false); // 미리보기 카메라 비활성화
        // if (previewLight != null) previewLight.gameObject.SetActive(false); // 이미 previewAnchor 자식이므로 함께 제거됨

        Debug.Log("All piece buttons generated with previews.");
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // 오브젝트의 모든 Renderer Bounds를 포함하는 전체 Bounds 계산
    Bounds GetRendererBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        return bounds;
    }

    // 지정된 Bounds에 맞춰 카메라를 프레이밍하는 함수 (Orthographic 기준 예시)
    void FrameObject(Camera cam, Bounds bounds, float desiredDistanceFactor)
    {
        if (!cam.orthographic)
        {
            // Perspective 카메라 프레이밍은 좀 더 복잡 (FOV와 거리 모두 고려)
            // 여기서는 Orthographic을 가정하거나, Perspective라도 LookAt 중심으로 단순화
            cam.transform.position = bounds.center - cam.transform.forward * Mathf.Abs(desiredDistanceFactor); // 거리 조절
            cam.transform.LookAt(bounds.center);
            // Perspective의 경우:
            // float objectSize = bounds.extents.magnitude;
            // float cameraDistance = objectSize / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            // cam.transform.position = bounds.center - cam.transform.forward * cameraDistance * 1.5f; // 약간의 여유
            // cam.transform.LookAt(bounds.center);
            return;
        }

        float screenAspect = (float)Screen.width / Screen.height;
        float cameraHeight = bounds.size.y;
        float cameraWidth = bounds.size.x;

        // Orthographic size는 화면 높이의 절반
        if (cameraHeight / screenAspect > cameraWidth) // 높이가 너비보다 상대적으로 크면 높이 기준
        {
            cam.orthographicSize = cameraHeight / 2f;
        }
        else // 너비가 높이보다 상대적으로 크면 너비 기준
        {
            cam.orthographicSize = cameraWidth / (2f * screenAspect);
        }
        // 약간의 패딩 추가
        cam.orthographicSize *= 1.1f; // 10% 패딩

        // 카메라 위치를 Bounds의 중심으로 설정하고 Z축으로 적절히 이동
        // (카메라가 Orthographic이므로 Z 위치는 렌더링 크기에 직접 영향을 주지 않지만, Clipping Planes는 고려)
        Vector3 newCamPos = bounds.center;
        // newCamPos.z = cam.transform.position.z; // 기존 Z 유지 또는 아래처럼 계산
        newCamPos -= cam.transform.forward * Mathf.Abs(cam.nearClipPlane + bounds.extents.magnitude); // 너무 가깝지 않게
        cam.transform.position = newCamPos;
        // Orthographic 카메라는 LookAt이 필수는 아니지만, 방향을 맞춰주는 것이 좋음
        // cam.transform.rotation = Quaternion.LookRotation(bounds.center - cam.transform.position); // 이렇게 하면 안됨
        // Orthographic 카메라는 보통 고정된 회전값을 가짐 (예: (30, 45, 0) 등)
        // 여기서는 카메라가 이미 적절한 각도로 조각을 보고 있다고 가정하고, 위치와 orthographicSize만 조절
    }
}