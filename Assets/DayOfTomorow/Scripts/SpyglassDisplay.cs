using UnityEngine;
using UnityEngine.UI;

public class SpyglassDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField]
    Camera spyglassCamera;

    [SerializeField]
    RawImage outsideView;

    [SerializeField]
    RectTransform cannonMarker;

    [SerializeField]
    SpyglassPull spyglassPull;

    [SerializeField, Range(0f, 1f)]
    float cameraActivationThreshold = 0.05f;

    [SerializeField]
    Vector2Int renderTextureResolution = new Vector2Int(1024, 512);

    [Header("Roof Camera")]
    [SerializeField]
    Transform tankRoot;

    [SerializeField]
    float cameraYawMultiplier = 2f;

    [Header("Cannon Aim")]
    [SerializeField]
    Transform cannonMuzzle;

    [SerializeField]
    Transform cannonRayOrigin;

    [SerializeField]
    Vector3 localAimAxis = Vector3.up;

    [SerializeField, Min(1f)]
    float maxAimDistance = 1000f;

    [SerializeField]
    LayerMask aimLayers = ~0;

    [SerializeField, Min(0.001f)]
    float markerSize = 0.05f;

    RenderTexture renderTexture;
    Quaternion cameraBaseRotationRelativeToTank;

    void Awake()
    {
        if (spyglassPull == null)
            spyglassPull = GetComponentInParent<SpyglassPull>();

        if (spyglassCamera == null)
            spyglassCamera = FindComponentByName<Camera>("SpyglassCamera");

        if (outsideView == null)
            outsideView = FindComponentByName<RawImage>("OutsideView");

        if (cannonMarker == null)
            cannonMarker = FindTransformByName("CannonMarker") as RectTransform;

        if (cannonMuzzle == null)
            cannonMuzzle = FindTransformByName("CannonMuzzle");

        if (cannonRayOrigin == null && cannonMuzzle != null)
            cannonRayOrigin = cannonMuzzle.Find("Gun");

        if (cannonRayOrigin == null)
            cannonRayOrigin = cannonMuzzle;

        if (tankRoot == null && spyglassPull != null)
            tankRoot = spyglassPull.transform.root;

        CacheCameraHeading();
        CreateRenderTexture();

        if (spyglassCamera != null)
        {
            spyglassCamera.stereoTargetEye = StereoTargetEyeMask.None;

            AudioListener listener = spyglassCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }

        if (outsideView != null)
            outsideView.raycastTarget = false;

        if (cannonMarker != null)
        {
            cannonMarker.anchorMin = new Vector2(0.5f, 0.5f);
            cannonMarker.anchorMax = new Vector2(0.5f, 0.5f);
            cannonMarker.sizeDelta = Vector2.one * markerSize;

            Graphic markerGraphic = cannonMarker.GetComponent<Graphic>();
            if (markerGraphic != null)
                markerGraphic.raycastTarget = false;
        }
    }

    void OnDestroy()
    {
        if (spyglassCamera != null && spyglassCamera.targetTexture == renderTexture)
            spyglassCamera.targetTexture = null;

        if (outsideView != null && outsideView.texture == renderTexture)
            outsideView.texture = null;

        if (renderTexture == null)
            return;

        renderTexture.Release();
        Destroy(renderTexture);
    }

    void LateUpdate()
    {
        UpdateRoofCamera();

        bool displayActive =
            spyglassPull == null || spyglassPull.Deployment >= cameraActivationThreshold;

        if (spyglassCamera != null && spyglassCamera.enabled != displayActive)
            spyglassCamera.enabled = displayActive;

        if (outsideView != null && outsideView.enabled != displayActive)
            outsideView.enabled = displayActive;

        UpdateCannonMarker(displayActive);
    }

    void CacheCameraHeading()
    {
        if (spyglassCamera == null || tankRoot == null)
            return;

        Vector3 levelForward = Vector3.ProjectOnPlane(
            spyglassCamera.transform.forward,
            tankRoot.up);

        if (levelForward.sqrMagnitude <= Mathf.Epsilon)
            levelForward = tankRoot.forward;

        Quaternion levelRotation = Quaternion.LookRotation(
            levelForward.normalized,
            tankRoot.up);

        cameraBaseRotationRelativeToTank =
            Quaternion.Inverse(tankRoot.rotation) * levelRotation;
    }

    void UpdateRoofCamera()
    {
        if (spyglassCamera == null || tankRoot == null)
            return;

        Transform cameraTransform = spyglassCamera.transform;

        float spyglassYaw =
            spyglassPull != null
                ? spyglassPull.Yaw * cameraYawMultiplier
                : 0f;

        cameraTransform.rotation =
            tankRoot.rotation *
            Quaternion.AngleAxis(spyglassYaw, Vector3.up) *
            cameraBaseRotationRelativeToTank;
    }

    void CreateRenderTexture()
    {
        if (spyglassCamera == null || outsideView == null)
            return;

        int width = Mathf.Max(16, renderTextureResolution.x);
        int height = Mathf.Max(16, renderTextureResolution.y);

        renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
        {
            name = "Spyglass View",
            useMipMap = false,
            autoGenerateMips = false
        };
        renderTexture.Create();

        spyglassCamera.targetTexture = renderTexture;
        outsideView.texture = renderTexture;
    }

    void UpdateCannonMarker(bool displayActive)
    {
        if (cannonMarker == null || spyglassCamera == null || cannonRayOrigin == null)
            return;

        Vector3 aimDirection = cannonRayOrigin.TransformDirection(localAimAxis).normalized;
        Vector3 aimPoint = cannonRayOrigin.position + aimDirection * maxAimDistance;

        if (Physics.Raycast(
                cannonRayOrigin.position,
                aimDirection,
                out RaycastHit hit,
                maxAimDistance,
                aimLayers,
                QueryTriggerInteraction.Ignore))
        {
            aimPoint = hit.point;
        }

        Vector3 viewportPoint = spyglassCamera.WorldToViewportPoint(aimPoint);
        bool markerVisible =
            displayActive &&
            viewportPoint.z > 0f &&
            viewportPoint.x >= 0f &&
            viewportPoint.x <= 1f &&
            viewportPoint.y >= 0f &&
            viewportPoint.y <= 1f;

        cannonMarker.gameObject.SetActive(markerVisible);

        if (!markerVisible)
            return;

        Vector2 markerPosition = new Vector2(viewportPoint.x, viewportPoint.y);
        cannonMarker.anchorMin = markerPosition;
        cannonMarker.anchorMax = markerPosition;
        cannonMarker.anchoredPosition = Vector2.zero;
    }

    T FindComponentByName<T>(string objectName) where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include);

        foreach (T component in components)
        {
            if (component.gameObject.name == objectName)
                return component;
        }

        return null;
    }

    Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);

        foreach (Transform candidate in transforms)
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }
}
