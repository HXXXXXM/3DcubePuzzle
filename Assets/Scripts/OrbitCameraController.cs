using UnityEngine;

public class OrbitCameraController : MonoBehaviour
{
    [Header("Target & Basic Controls")]
    public Transform target; // 카메라가 공전할 중심 대상 (예: PuzzleCube_Container)
    public float distance = 10.0f;  // 타겟으로부터의 초기 및 현재 목표 거리
    public float xSpeed = 600f;   // 마우스 X축 회전 속도
    public float ySpeed = 600f;   // 마우스 Y축 회전 속도
    public int mouseButtonForRotation = 1; // 회전에 사용할 마우스 버튼 (0:왼쪽, 1:오른쪽, 2:가운데)

    [Header("Rotation Limits")]
    public float yMinLimit = -89.9f;  // Y축 회전 최소 각도 (카메라가 아래로 내려갈 수 있는 한계)
    public float yMaxLimit = 89.9f;   // Y축 회전 최대 각도 (카메라가 위로 올라갈 수 있는 한계)

    [Header("Zoom Limits")]
    public float distanceMin = 1f;   // 최소 줌 거리
    public float distanceMax = 20f;  // 최대 줌 거리
    public float zoomSpeedFactor = 10f; // 마우스 휠 줌 감도

    [Header("Panning Controls")]
    public int mouseButtonForPanning = 2;  // 패닝에 사용할 마우스 버튼
    public float panSpeed = 1f;         // 패닝 속도
    public bool usePanningBounds = true;  // 패닝 범위 제한 사용 여부
    public float maxPanOffsetMagnitude = 15f; // 원형 패닝 범위 제한 (usePanningBounds가 false일 때)
    public Vector3 panningBoundsMin = new Vector3(-10, -5, -10); // 박스형 패닝 최소 경계 (타겟 기준 로컬 오프셋)
    public Vector3 panningBoundsMax = new Vector3(10, 10, 10);   // 박스형 패닝 최대 경계 (타겟 기준 로컬 오프셋)

    [Header("Smoothing")]
    public float smoothTime = 0.3f; // 카메라 이동 및 회전의 부드러움 정도

    // 내부 변수
    private float x = 0.0f; // 현재 X축 누적 회전값 (Yaw)
    private float y = 0.0f; // 현재 Y축 누적 회전값 (Pitch)

    private float currentDistance;    // 현재 카메라와 (가상)중심 사이의 부드럽게 변하는 거리
    private Quaternion currentRotation; // 현재 카메라의 부드럽게 변하는 회전값
    private Vector3 currentPosition;  // 현재 카메라의 부드럽게 변하는 월드 위치

    private Vector3 positionVelocity; // Vector3.SmoothDamp 참조용

    private Vector3 panOffset = Vector3.zero;        // 패닝으로 인한 타겟으로부터의 월드 오프셋
    private Vector3 lastPanMousePosition;          // 이전 프레임의 패닝 마우스 위치

    void Start()
    {
        if (target == null)
        {
            // 타겟이 없으면 월드 원점을 임시 타겟으로 사용
            GameObject tempTargetGO = new GameObject("CameraTarget_AutoOrigin");
            tempTargetGO.transform.position = Vector3.zero;
            target = tempTargetGO.transform;
            Debug.LogWarning("OrbitCameraController: Target not set. Defaulting to world origin. Please assign a target (e.g., PuzzleCube_Container).");
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y; // 초기 Yaw
        y = angles.x; // 초기 Pitch

        currentDistance = distance;
        currentRotation = Quaternion.Euler(y, x, 0);
        // 초기 카메라 위치: (타겟의 실제 위치 + 패닝 오프셋)을 중심으로 계산
        currentPosition = (target.position + panOffset) - (currentRotation * Vector3.forward * currentDistance);

        // 시작 시 카메라 즉시 설정
        transform.rotation = currentRotation;
        transform.position = currentPosition;
    }

    void LateUpdate() // 모든 Update 로직이 끝난 후 카메라 위치를 업데이트하여 떨림 방지
    {
        if (target == null) return;

        bool isRotating = Input.GetMouseButton(mouseButtonForRotation);
        bool isPanning = Input.GetMouseButton(mouseButtonForPanning);
        // 마우스 휠 입력은 GetAxis("ScrollWheel")의 반환값이 0이 아닌지로 판단
        bool isZooming = Mathf.Abs(Input.GetAxis("ScrollWheel")) > 0.01f;


        // 1. 회전 입력 처리
        if (isRotating)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }
        // 2. 패닝 입력 처리 (회전 중이 아닐 때)
        else if (isPanning)
        {
            if (Input.GetMouseButtonDown(mouseButtonForPanning)) // 패닝 시작
            {
                lastPanMousePosition = Input.mousePosition;
            }
            else // 패닝 중 (GetMouseButton이므로 버튼이 계속 눌려있을 때)
            {
                Vector3 mouseDelta = Input.mousePosition - lastPanMousePosition;
                // 카메라의 로컬 축을 기준으로 이동량 계산 (카메라 방향에 상대적으로 패닝)
                Vector3 panTranslation = -transform.right * mouseDelta.x * panSpeed * Time.deltaTime +
                                         -transform.up * mouseDelta.y * panSpeed * Time.deltaTime;

                Vector3 nextPanOffset = panOffset + panTranslation;

                // 패닝 경계 적용
                if (usePanningBounds)
                {
                    nextPanOffset.x = Mathf.Clamp(nextPanOffset.x, panningBoundsMin.x, panningBoundsMax.x);
                    nextPanOffset.y = Mathf.Clamp(nextPanOffset.y, panningBoundsMin.y, panningBoundsMax.y);
                    nextPanOffset.z = Mathf.Clamp(nextPanOffset.z, panningBoundsMin.z, panningBoundsMax.z);
                }
                else // 원형 경계만 사용
                {
                    if (nextPanOffset.magnitude > maxPanOffsetMagnitude)
                    {
                        nextPanOffset = nextPanOffset.normalized * maxPanOffsetMagnitude;
                    }
                }
                panOffset = nextPanOffset; // 최종 제한된 오프셋 적용
                lastPanMousePosition = Input.mousePosition;
            }
        }
        // 3. 줌 입력 처리 (회전이나 패닝 중이 아닐 때)
        else if (isZooming)
        {
            distance -= Input.GetAxis("ScrollWheel") * zoomSpeedFactor;
            distance = Mathf.Clamp(distance, distanceMin, distanceMax);
        }

        // 목표 회전 (누적된 x, y 값 사용)
        Quaternion targetIdealRotation = Quaternion.Euler(y, x, 0);

        // 목표 거리 (누적된 distance 값 사용)
        float targetIdealDistance = distance;

        // 카메라의 최종 목표 위치 계산: (실제 타겟 위치 + 패닝 오프셋)을 공전 중심으로 사용
        Vector3 currentOrbitCenter = target.position + panOffset;

        // 부드러운 값 업데이트
        currentRotation = Quaternion.Slerp(currentRotation, targetIdealRotation, Time.deltaTime * (1f / smoothTime));
        currentDistance = Mathf.Lerp(currentDistance, targetIdealDistance, Time.deltaTime * (1f / smoothTime));

        // 부드럽게 업데이트된 회전과 거리를 사용하여 카메라의 목표 위치 계산
        Vector3 desiredCameraPosition = currentOrbitCenter - (currentRotation * Vector3.forward * currentDistance);

        // 최종 카메라 위치를 부드럽게 이동
        currentPosition = Vector3.SmoothDamp(currentPosition, desiredCameraPosition, ref positionVelocity, smoothTime);

        // 계산된 최종 값들을 카메라 트랜스폼에 적용
        transform.rotation = currentRotation;
        transform.position = currentPosition;
    }

    /// <summary>
    /// 각도를 주어진 최소, 최대값 사이로 제한합니다.
    /// </summary>
    public static float ClampAngle(float angle, float min, float max)
    {
        // 이 함수는 Y축(Pitch) 각도에만 적용되므로, 360도 래핑은 필요하지 않음.
        // min/max 값 자체가 -90 ~ 90 범위를 넘지 않도록 설정하기 때문.
        return Mathf.Clamp(angle, min, max);
    }
}