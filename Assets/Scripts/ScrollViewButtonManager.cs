using UnityEngine;
using UnityEngine.UI; // Button, RawImage ���
using System.Collections.Generic;
using System.Collections; // �ڷ�ƾ ����� ����

public class PieceButtonGenerator : MonoBehaviour
{
    [Header("UI References (Assign in Inspector)")]
    public GameObject pieceButtonPrefab_UI; // RawImage�� Button ������Ʈ�� �ִ� UI ��ư ������
    public Transform buttonContainer_UI;   // ScrollView�� Content ������Ʈ (��ư���� �߰��� �θ�)

    [Header("Preview Rendering (Assign in Inspector)")]
    public Camera piecePreviewCamera;     // "PiecePreview" ���̾ ��� ���� ī�޶�
    // public Vector3 previewSpawnPosition = new Vector3(1000f, 1000f, 1000f); // ������ �ӽ÷� ���� ���� ������ �ʴ� ��ġ
    public Vector3 previewCameraOffset = new Vector3(0, 0, -2.5f); // �̸����� �������κ��� ī�޶� ������ ����� �Ÿ�
    public Vector2Int renderTextureSize = new Vector2Int(128, 128); // ������ Render Texture�� ũ��
    public Light previewLight; // �̸����� ������ ���� ���� ���� (���� ����)

    // ������ ��ư�� ����� ���� ���� (���� ����, GameManager���� ������ ���� ����)
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

        // �ʱ⿡�� ī�޶�� ������ ��Ȱ��ȭ �ص� �� ����
        piecePreviewCamera.gameObject.SetActive(false);
        if (previewLight != null) previewLight.gameObject.SetActive(false);
    }

    /// <summary>
    /// �־��� ���� ������ ��Ͽ� ���� UI ��ư�� �����ϰ� �̸����⸦ �������մϴ�.
    /// GameManager ��� ȣ��˴ϴ�.
    /// </summary>
    public void GenerateAndDisplayPieceButtons(List<GameObject> piecePrefabsToDisplay, GameManager3D gameManagerRef)
    {
        // 1. ���� UI ��ư�� ����
        foreach (Transform child in buttonContainer_UI)
        {
            Destroy(child.gameObject);
        }
        // buttonToPiecePrefabMap.Clear(); // ���� ����Ѵٸ�

        if (piecePrefabsToDisplay == null || piecePrefabsToDisplay.Count == 0)
        {
            Debug.LogWarning("No piece prefabs provided to generate buttons.");
            return;
        }

        // 2. �� ���� �����տ� ���� ��ư ���� �� �̸����� ������ ����
        StartCoroutine(GenerateButtonsCoroutine(piecePrefabsToDisplay, gameManagerRef));
    }

    private IEnumerator GenerateButtonsCoroutine(List<GameObject> piecePrefabs, GameManager3D gameManager)
    {
        // �̸����� ī�޶� �� ���� Ȱ��ȭ
        piecePreviewCamera.gameObject.SetActive(true);
        if (previewLight != null) previewLight.gameObject.SetActive(true);

        // �̸����� ������ ���� �ӽ� �θ� (ī�޶�� �Բ� �����̵��� ���� ����)
        GameObject previewAnchor = new GameObject("PreviewAnchor");
        previewAnchor.transform.position = piecePreviewCamera.transform.position + piecePreviewCamera.transform.forward * Mathf.Abs(previewCameraOffset.z); // ī�޶� �� ���� �Ÿ��� ��ġ
        if (previewLight != null) previewLight.transform.SetParent(previewAnchor.transform, true); // ���� �Բ�

        for (int i = 0; i < piecePrefabs.Count; i++)
        {
            GameObject piecePrefab = piecePrefabs[i];
            if (piecePrefab == null) continue;

            // --- UI ��ư ���� ---
            GameObject buttonGO = Instantiate(pieceButtonPrefab_UI, buttonContainer_UI);
            RawImage rawImage = buttonGO.GetComponentInChildren<RawImage>();
            Button button = buttonGO.GetComponent<Button>();

            if (rawImage == null || button == null)
            {
                Debug.LogError($"Button prefab '{pieceButtonPrefab_UI.name}' is missing RawImage or Button.");
                Destroy(buttonGO); // �߸��� ��ư�� ����
                continue;
            }

            // --- Render Texture ���� �� �Ҵ� ---
            RenderTexture rt = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24, RenderTextureFormat.Default);
            rt.Create(); // �߿�: RT ��� �� ���� �ʿ�
            rawImage.texture = rt;
            rawImage.color = Color.white; // RT�� ����� ���̵���

            // --- �̸������ ���� �ν��Ͻ� ���� �� ���� ---
            // ���� �̸����� ������ �ִٸ� ����
            foreach (Transform child in previewAnchor.transform)
            {
                if (child != previewLight?.transform) Destroy(child.gameObject);
            }

            GameObject previewPieceInstance = Instantiate(piecePrefab, previewAnchor.transform); // Anchor�� �ڽ�����
            previewPieceInstance.transform.localPosition = Vector3.zero; // Anchor�� ���� (0,0,0)�� ��ġ
            previewPieceInstance.transform.localRotation = Quaternion.Euler(-30, 45, 0); // �̸����� ���� ������ ȸ�� (���� �ʿ�)

            // ��� �ڽ� ������Ʈ�� ���̾ "PiecePreview"�� ����
            SetLayerRecursively(previewPieceInstance, LayerMask.NameToLayer("PiecePreview"));

            // PieceController3D�� �����ͼ� ���� �´� ��Ƽ���� ���� (PieceController3D�� Start/Awake���� ó��)
            // PieceController3D pc = previewPieceInstance.GetComponent<PieceController3D>();
            // if (pc != null) { /* pc.SetMaterialBasedOnColor(); ���� �ʿ��ϴٸ� */ }


            // --- ī�޶� ���� �� ������ ---
            piecePreviewCamera.targetTexture = rt; // �� ī�޶��� ����� �� ��ư�� RT��

            // ī�޶� ������ ������ ���ߵ��� ��ġ/���� ���� (�߿�!)
            // Bounds�� ����Ͽ� ���� ũ�⿡ �°� ī�޶� �Ÿ�/FOV/OrthographicSize ���� �ʿ�
            // ���⼭�� previewAnchor�� �������� ī�޶� ������ �������� �����ٰ� ����
            // piecePreviewCamera.transform.position = previewAnchor.transform.position + previewCameraOffset;
            // piecePreviewCamera.transform.LookAt(previewAnchor.transform.position); // �׻� Anchor�� �߽��� �ٶ�

            // ������ Bounds�� ����Ͽ� ī�޶� �� �°� ���ߵ��� �ڵ� ���� (���)
            Bounds bounds = GetRendererBounds(previewPieceInstance);
            FrameObject(piecePreviewCamera, bounds, previewCameraOffset.z); // ī�޶� ������ �����ӿ� ���ߵ���

            piecePreviewCamera.Render(); // ���� �������� RT�� �� �� ������
            // previewCamera.targetTexture = null; // ���� �������� ���� ��� ���� (���� ����)

            // --- ��ư Ŭ�� �̺�Ʈ ���� ---
            // �÷��̾ �� UI ��ư�� Ŭ���ϸ�, ���� ���� ���忡 �ش� ������ "����"�ϰų� "����"�ϴ� ����
            GameObject prefabForButton = piecePrefab; // Ŭ������ ���� ���� ������ �Ҵ�
            button.onClick.AddListener(() =>
            {
                if (gameManager != null)
                {
                    // gameManager.SelectPieceToSpawn(prefabForButton); // ����: GameManager�� �̷� �Լ��� �ִٰ� ����
                    Debug.Log($"UI Button clicked for piece: {prefabForButton.name}");
                    // TODO: �� ��ư�� �ش��ϴ� ������ ���� ���忡�� �����ϰų� ���� �����ϴ� ���� ȣ��
                }
            });

            // buttonToPiecePrefabMap.Add(button, prefabForButton); // �ʿ� �߰� (���� ����)

            yield return null; // �� ������ ����Ͽ� �� ��ư �̸����⸦ ���������� ����/������
        }

        // ��� ������ �Ϸ� �� �̸������ ������Ʈ/ī�޶� ����
        Destroy(previewAnchor); // �ӽ� ��Ŀ ���� (���� �Բ� ���ŵ�)
        piecePreviewCamera.targetTexture = null; // ī�޶� Ÿ�� �ؽ�ó ����
        piecePreviewCamera.gameObject.SetActive(false); // �̸����� ī�޶� ��Ȱ��ȭ
        // if (previewLight != null) previewLight.gameObject.SetActive(false); // �̹� previewAnchor �ڽ��̹Ƿ� �Բ� ���ŵ�

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

    // ������Ʈ�� ��� Renderer Bounds�� �����ϴ� ��ü Bounds ���
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

    // ������ Bounds�� ���� ī�޶� �����̹��ϴ� �Լ� (Orthographic ���� ����)
    void FrameObject(Camera cam, Bounds bounds, float desiredDistanceFactor)
    {
        if (!cam.orthographic)
        {
            // Perspective ī�޶� �����̹��� �� �� ���� (FOV�� �Ÿ� ��� ���)
            // ���⼭�� Orthographic�� �����ϰų�, Perspective�� LookAt �߽����� �ܼ�ȭ
            cam.transform.position = bounds.center - cam.transform.forward * Mathf.Abs(desiredDistanceFactor); // �Ÿ� ����
            cam.transform.LookAt(bounds.center);
            // Perspective�� ���:
            // float objectSize = bounds.extents.magnitude;
            // float cameraDistance = objectSize / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            // cam.transform.position = bounds.center - cam.transform.forward * cameraDistance * 1.5f; // �ణ�� ����
            // cam.transform.LookAt(bounds.center);
            return;
        }

        float screenAspect = (float)Screen.width / Screen.height;
        float cameraHeight = bounds.size.y;
        float cameraWidth = bounds.size.x;

        // Orthographic size�� ȭ�� ������ ����
        if (cameraHeight / screenAspect > cameraWidth) // ���̰� �ʺ񺸴� ��������� ũ�� ���� ����
        {
            cam.orthographicSize = cameraHeight / 2f;
        }
        else // �ʺ� ���̺��� ��������� ũ�� �ʺ� ����
        {
            cam.orthographicSize = cameraWidth / (2f * screenAspect);
        }
        // �ణ�� �е� �߰�
        cam.orthographicSize *= 1.1f; // 10% �е�

        // ī�޶� ��ġ�� Bounds�� �߽����� �����ϰ� Z������ ������ �̵�
        // (ī�޶� Orthographic�̹Ƿ� Z ��ġ�� ������ ũ�⿡ ���� ������ ���� ������, Clipping Planes�� ���)
        Vector3 newCamPos = bounds.center;
        // newCamPos.z = cam.transform.position.z; // ���� Z ���� �Ǵ� �Ʒ�ó�� ���
        newCamPos -= cam.transform.forward * Mathf.Abs(cam.nearClipPlane + bounds.extents.magnitude); // �ʹ� ������ �ʰ�
        cam.transform.position = newCamPos;
        // Orthographic ī�޶�� LookAt�� �ʼ��� �ƴ�����, ������ �����ִ� ���� ����
        // cam.transform.rotation = Quaternion.LookRotation(bounds.center - cam.transform.position); // �̷��� �ϸ� �ȵ�
        // Orthographic ī�޶�� ���� ������ ȸ������ ���� (��: (30, 45, 0) ��)
        // ���⼭�� ī�޶� �̹� ������ ������ ������ ���� �ִٰ� �����ϰ�, ��ġ�� orthographicSize�� ����
    }
}