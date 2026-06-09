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

        if (TryGetCannonHit(out RaycastHit hit))
            aimPoint = hit.point;

        Vector3 viewportPoint = spyglassCamera.WorldToViewportPoint(aimPoint);
        cannonMarker.gameObject.SetActive(displayActive);

        if (!displayActive)
            return;

        Vector2 markerPosition = new Vector2(viewportPoint.x, viewportPoint.y);

        bool aimPointIsOnScreen =
            viewportPoint.z > 0f &&
            markerPosition.x >= 0f &&
            markerPosition.x <= 1f &&
            markerPosition.y >= 0f &&
            markerPosition.y <= 1f;

        if (!aimPointIsOnScreen)
        {
            Vector2 directionFromCenter = markerPosition - Vector2.one * 0.5f;

            // Viewport coordinates are mirrored when the target is behind the camera.
            if (viewportPoint.z < 0f)
                directionFromCenter = -directionFromCenter;

            markerPosition = GetViewportEdgePosition(directionFromCenter);
        }

        cannonMarker.anchorMin = markerPosition;
        cannonMarker.anchorMax = markerPosition;
        cannonMarker.anchoredPosition = Vector2.zero;
    }

    public bool TryGetCannonHit(out RaycastHit hit)
    {
        hit = default;

        if (cannonRayOrigin == null)
            return false;

        Vector3 aimDirection = cannonRayOrigin.TransformDirection(localAimAxis).normalized;

        if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
            return false;

        return Physics.Raycast(
            cannonRayOrigin.position,
            aimDirection,
            out hit,
            maxAimDistance,
            aimLayers,
            QueryTriggerInteraction.Ignore);
    }

    Vector2 GetViewportEdgePosition(Vector2 directionFromCenter)
    {
        if (directionFromCenter.sqrMagnitude <= Mathf.Epsilon)
            directionFromCenter = Vector2.down;

        RectTransform markerParent = cannonMarker.parent as RectTransform;
        Vector2 markerHalfSize = cannonMarker.rect.size * 0.5f;
        Vector2 parentSize = markerParent != null
            ? markerParent.rect.size
            : Vector2.one;

        Vector2 viewportMargin = new Vector2(
            parentSize.x > Mathf.Epsilon ? markerHalfSize.x / parentSize.x : 0f,
            parentSize.y > Mathf.Epsilon ? markerHalfSize.y / parentSize.y : 0f);

        Vector2 availableHalfSize = new Vector2(
            Mathf.Max(0f, 0.5f - viewportMargin.x),
            Mathf.Max(0f, 0.5f - viewportMargin.y));

        float horizontalScale = Mathf.Abs(directionFromCenter.x) > Mathf.Epsilon
            ? availableHalfSize.x / Mathf.Abs(directionFromCenter.x)
            : float.PositiveInfinity;
        float verticalScale = Mathf.Abs(directionFromCenter.y) > Mathf.Epsilon
            ? availableHalfSize.y / Mathf.Abs(directionFromCenter.y)
            : float.PositiveInfinity;

        float edgeScale = Mathf.Min(horizontalScale, verticalScale);
        return Vector2.one * 0.5f + directionFromCenter * edgeScale;
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
