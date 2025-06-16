using UnityEngine;

public class OrbitCameraController : MonoBehaviour
{
    [Header("Target & Basic Controls")]
    public Transform target; // ī�޶� ������ �߽� ��� (��: PuzzleCube_Container)
    public float distance = 10.0f;  // Ÿ�����κ����� �ʱ� �� ���� ��ǥ �Ÿ�
    public float xSpeed = 600f;   // ���콺 X�� ȸ�� �ӵ�
    public float ySpeed = 600f;   // ���콺 Y�� ȸ�� �ӵ�
    public int mouseButtonForRotation = 1; // ȸ���� ����� ���콺 ��ư (0:����, 1:������, 2:���)

    [Header("Rotation Limits")]
    public float yMinLimit = -89.9f;  // Y�� ȸ�� �ּ� ���� (ī�޶� �Ʒ��� ������ �� �ִ� �Ѱ�)
    public float yMaxLimit = 89.9f;   // Y�� ȸ�� �ִ� ���� (ī�޶� ���� �ö� �� �ִ� �Ѱ�)

    [Header("Zoom Limits")]
    public float distanceMin = 1f;   // �ּ� �� �Ÿ�
    public float distanceMax = 20f;  // �ִ� �� �Ÿ�
    public float zoomSpeedFactor = 10f; // ���콺 �� �� ����

    [Header("Panning Controls")]
    public int mouseButtonForPanning = 2;  // �д׿� ����� ���콺 ��ư
    public float panSpeed = 1f;         // �д� �ӵ�
    public bool usePanningBounds = true;  // �д� ���� ���� ��� ����
    public float maxPanOffsetMagnitude = 15f; // ���� �д� ���� ���� (usePanningBounds�� false�� ��)
    public Vector3 panningBoundsMin = new Vector3(-10, -5, -10); // �ڽ��� �д� �ּ� ��� (Ÿ�� ���� ���� ������)
    public Vector3 panningBoundsMax = new Vector3(10, 10, 10);   // �ڽ��� �д� �ִ� ��� (Ÿ�� ���� ���� ������)

    [Header("Smoothing")]
    public float smoothTime = 0.3f; // ī�޶� �̵� �� ȸ���� �ε巯�� ����

    // ���� ����
    private float x = 0.0f; // ���� X�� ���� ȸ���� (Yaw)
    private float y = 0.0f; // ���� Y�� ���� ȸ���� (Pitch)

    private float currentDistance;    // ���� ī�޶�� (����)�߽� ������ �ε巴�� ���ϴ� �Ÿ�
    private Quaternion currentRotation; // ���� ī�޶��� �ε巴�� ���ϴ� ȸ����
    private Vector3 currentPosition;  // ���� ī�޶��� �ε巴�� ���ϴ� ���� ��ġ

    private Vector3 positionVelocity; // Vector3.SmoothDamp ������

    private Vector3 panOffset = Vector3.zero;        // �д����� ���� Ÿ�����κ����� ���� ������
    private Vector3 lastPanMousePosition;          // ���� �������� �д� ���콺 ��ġ

    void Start()
    {
        if (target == null)
        {
            // Ÿ���� ������ ���� ������ �ӽ� Ÿ������ ���
            GameObject tempTargetGO = new GameObject("CameraTarget_AutoOrigin");
            tempTargetGO.transform.position = Vector3.zero;
            target = tempTargetGO.transform;
            Debug.LogWarning("OrbitCameraController: Target not set. Defaulting to world origin. Please assign a target (e.g., PuzzleCube_Container).");
        }

        Vector3 angles = transform.eulerAngles;
        x = angles.y; // �ʱ� Yaw
        y = angles.x; // �ʱ� Pitch

        currentDistance = distance;
        currentRotation = Quaternion.Euler(y, x, 0);
        // �ʱ� ī�޶� ��ġ: (Ÿ���� ���� ��ġ + �д� ������)�� �߽����� ���
        currentPosition = (target.position + panOffset) - (currentRotation * Vector3.forward * currentDistance);

        // ���� �� ī�޶� ��� ����
        transform.rotation = currentRotation;
        transform.position = currentPosition;
    }

    void LateUpdate() // ��� Update ������ ���� �� ī�޶� ��ġ�� ������Ʈ�Ͽ� ���� ����
    {
        if (target == null) return;

        bool isRotating = Input.GetMouseButton(mouseButtonForRotation);
        bool isPanning = Input.GetMouseButton(mouseButtonForPanning);
        // ���콺 �� �Է��� GetAxis("ScrollWheel")�� ��ȯ���� 0�� �ƴ����� �Ǵ�
        bool isZooming = Mathf.Abs(Input.GetAxis("ScrollWheel")) > 0.01f;


        // 1. ȸ�� �Է� ó��
        if (isRotating)
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }
        // 2. �д� �Է� ó�� (ȸ�� ���� �ƴ� ��)
        else if (isPanning)
        {
            if (Input.GetMouseButtonDown(mouseButtonForPanning)) // �д� ����
            {
                lastPanMousePosition = Input.mousePosition;
            }
            else // �д� �� (GetMouseButton�̹Ƿ� ��ư�� ��� �������� ��)
            {
                Vector3 mouseDelta = Input.mousePosition - lastPanMousePosition;
                // ī�޶��� ���� ���� �������� �̵��� ��� (ī�޶� ���⿡ ��������� �д�)
                Vector3 panTranslation = -transform.right * mouseDelta.x * panSpeed * Time.deltaTime +
                                         -transform.up * mouseDelta.y * panSpeed * Time.deltaTime;

                Vector3 nextPanOffset = panOffset + panTranslation;

                // �д� ��� ����
                if (usePanningBounds)
                {
                    nextPanOffset.x = Mathf.Clamp(nextPanOffset.x, panningBoundsMin.x, panningBoundsMax.x);
                    nextPanOffset.y = Mathf.Clamp(nextPanOffset.y, panningBoundsMin.y, panningBoundsMax.y);
                    nextPanOffset.z = Mathf.Clamp(nextPanOffset.z, panningBoundsMin.z, panningBoundsMax.z);
                }
                else // ���� ��踸 ���
                {
                    if (nextPanOffset.magnitude > maxPanOffsetMagnitude)
                    {
                        nextPanOffset = nextPanOffset.normalized * maxPanOffsetMagnitude;
                    }
                }
                panOffset = nextPanOffset; // ���� ���ѵ� ������ ����
                lastPanMousePosition = Input.mousePosition;
            }
        }
        // 3. �� �Է� ó�� (ȸ���̳� �д� ���� �ƴ� ��)
        else if (isZooming)
        {
            distance -= Input.GetAxis("ScrollWheel") * zoomSpeedFactor;
            distance = Mathf.Clamp(distance, distanceMin, distanceMax);
        }

        // ��ǥ ȸ�� (������ x, y �� ���)
        Quaternion targetIdealRotation = Quaternion.Euler(y, x, 0);

        // ��ǥ �Ÿ� (������ distance �� ���)
        float targetIdealDistance = distance;

        // ī�޶��� ���� ��ǥ ��ġ ���: (���� Ÿ�� ��ġ + �д� ������)�� ���� �߽����� ���
        Vector3 currentOrbitCenter = target.position + panOffset;

        // �ε巯�� �� ������Ʈ
        currentRotation = Quaternion.Slerp(currentRotation, targetIdealRotation, Time.deltaTime * (1f / smoothTime));
        currentDistance = Mathf.Lerp(currentDistance, targetIdealDistance, Time.deltaTime * (1f / smoothTime));

        // �ε巴�� ������Ʈ�� ȸ���� �Ÿ��� ����Ͽ� ī�޶��� ��ǥ ��ġ ���
        Vector3 desiredCameraPosition = currentOrbitCenter - (currentRotation * Vector3.forward * currentDistance);

        // ���� ī�޶� ��ġ�� �ε巴�� �̵�
        currentPosition = Vector3.SmoothDamp(currentPosition, desiredCameraPosition, ref positionVelocity, smoothTime);

        // ���� ���� ������ ī�޶� Ʈ�������� ����
        transform.rotation = currentRotation;
        transform.position = currentPosition;
    }

    /// <summary>
    /// ������ �־��� �ּ�, �ִ밪 ���̷� �����մϴ�.
    /// </summary>
    public static float ClampAngle(float angle, float min, float max)
    {
        // �� �Լ��� Y��(Pitch) �������� ����ǹǷ�, 360�� ������ �ʿ����� ����.
        // min/max �� ��ü�� -90 ~ 90 ������ ���� �ʵ��� �����ϱ� ����.
        return Mathf.Clamp(angle, min, max);
    }
}