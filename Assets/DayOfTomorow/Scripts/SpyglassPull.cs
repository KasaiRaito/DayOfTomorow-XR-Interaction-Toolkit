using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SpyglassPull : MonoBehaviour
{
    [SerializeField]
    XRBaseInteractable[] grabHandles;

    [SerializeField, Min(0.01f)]
    float maxPullDistance = 1f;

    [SerializeField, Min(0f)]
    float retractSpeed = 1.5f;

    [SerializeField]
    Vector2 yawLimits = new Vector2(-180f, 180f);

    [Header("Deployment Latch")]
    [SerializeField, Range(0f, 1f)]
    float deployedLatchThreshold = 0.7f;

    [Header("Gesture Detection")]
    [FormerlySerializedAs("pullGestureThreshold")]
    [SerializeField, Min(0.001f)]
    float pullSpeedThreshold = 0.03f;

    [FormerlySerializedAs("rotationGestureThreshold")]
    [SerializeField, Min(0.1f)]
    float rotationSpeedThreshold = 5f;

    [SerializeField, Min(1f)]
    float gestureSwitchBias = 1.5f;

    public float Deployment =>
        maxPullDistance > Mathf.Epsilon ? currentPullDistance / maxPullDistance : 0f;

    public bool IsGrabbed => primaryInteractor != null;
    public bool IsPulling => gestureMode == GestureMode.Pulling;
    public bool IsRotating => gestureMode == GestureMode.Rotating;
    public bool IsLatchedDeployed => isLatchedDeployed;
    public float Yaw => currentYaw;

    enum GestureMode
    {
        None,
        Pulling,
        ChoosingTwoHandGesture,
        Rotating
    }

    Vector3 retractedLocalPosition;
    Quaternion initialLocalRotation;
    IXRSelectInteractor primaryInteractor;
    XRBaseInteractable primaryHandle;
    IXRSelectInteractor secondaryInteractor;
    XRBaseInteractable secondaryHandle;
    GestureMode gestureMode;
    float currentPullDistance;
    float currentYaw;
    float gestureStartPullDistance;
    float gestureStartYaw;
    Vector3 singleHandStartPosition;
    Vector3 previousTwoHandMidpoint;
    Vector3 previousTwoHandDirection;
    bool hasPreviousTwoHandDirection;
    bool isLatchedDeployed;

    void Awake()
    {
        retractedLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        if (grabHandles == null || grabHandles.Length == 0)
            grabHandles = GetComponentsInChildren<XRBaseInteractable>(true);
    }

    void OnEnable()
    {
        if (grabHandles == null)
            return;

        foreach (XRBaseInteractable handle in grabHandles)
        {
            if (handle == null)
                continue;

            handle.selectEntered.AddListener(OnGrabbed);
            handle.selectExited.AddListener(OnReleased);
        }

        FindExistingGrabs();
    }

    void OnDisable()
    {
        if (grabHandles != null)
        {
            foreach (XRBaseInteractable handle in grabHandles)
            {
                if (handle == null)
                    continue;

                handle.selectEntered.RemoveListener(OnGrabbed);
                handle.selectExited.RemoveListener(OnReleased);
            }
        }

        ClearGrabs();
    }

    void LateUpdate()
    {
        if (secondaryInteractor != null && secondaryHandle != null)
        {
            UpdateTwoHandGesture();
        }
        else if (primaryInteractor != null && primaryHandle != null)
        {
            UpdateSingleHandPull();
        }
        else
        {
            float targetPullDistance = isLatchedDeployed
                ? maxPullDistance
                : 0f;

            currentPullDistance = Mathf.MoveTowards(
                currentPullDistance,
                targetPullDistance,
                retractSpeed * Time.deltaTime);
        }

        ApplyPosition();
        ApplyRotation();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        AddGrab(
            args.interactorObject,
            args.interactableObject as XRBaseInteractable);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        RemoveGrab(
            args.interactorObject,
            args.interactableObject as XRBaseInteractable);
    }

    void FindExistingGrabs()
    {
        foreach (XRBaseInteractable handle in grabHandles)
        {
            if (handle == null)
                continue;

            foreach (IXRSelectInteractor interactor in handle.interactorsSelecting)
                AddGrab(interactor, handle);
        }
    }

    void AddGrab(IXRSelectInteractor interactor, XRBaseInteractable handle)
    {
        if (interactor == null || handle == null)
            return;

        if (interactor == primaryInteractor || interactor == secondaryInteractor)
            return;

        if (primaryInteractor == null)
        {
            primaryInteractor = interactor;
            primaryHandle = handle;
            BeginSingleHandPull();
            return;
        }

        if (secondaryInteractor == null)
        {
            secondaryInteractor = interactor;
            secondaryHandle = handle;
            BeginTwoHandGesture();
        }
    }

    void RemoveGrab(IXRSelectInteractor interactor, XRBaseInteractable handle)
    {
        if (interactor == secondaryInteractor && handle == secondaryHandle)
        {
            secondaryInteractor = null;
            secondaryHandle = null;
            BeginSingleHandPull();
            return;
        }

        if (interactor != primaryInteractor || handle != primaryHandle)
            return;

        primaryInteractor = secondaryInteractor;
        primaryHandle = secondaryHandle;
        secondaryInteractor = null;
        secondaryHandle = null;

        if (primaryInteractor != null)
            BeginSingleHandPull();
        else
        {
            gestureMode = GestureMode.None;
            UpdateDeploymentLatch();
        }
    }

    void BeginSingleHandPull()
    {
        if (!TryGetHandPosition(primaryInteractor, primaryHandle, out singleHandStartPosition))
        {
            gestureMode = GestureMode.None;
            return;
        }

        gestureStartPullDistance = currentPullDistance;
        gestureStartYaw = currentYaw;
        gestureMode = GestureMode.Pulling;
    }

    void BeginTwoHandGesture()
    {
        if (!TryGetTwoHandPositions(out Vector3 primaryPosition, out Vector3 secondaryPosition))
        {
            gestureMode = GestureMode.None;
            return;
        }

        previousTwoHandMidpoint = (primaryPosition + secondaryPosition) * 0.5f;
        previousTwoHandDirection = GetTwoHandDirection(primaryPosition, secondaryPosition);
        hasPreviousTwoHandDirection =
            previousTwoHandDirection.sqrMagnitude > Mathf.Epsilon;
        gestureMode = GestureMode.ChoosingTwoHandGesture;
    }

    void UpdateSingleHandPull()
    {
        if (!TryGetHandPosition(primaryInteractor, primaryHandle, out Vector3 handPosition))
            return;

        float pullDelta = Vector3.Dot(
            handPosition - singleHandStartPosition,
            GetWorldPullAxis());

        currentPullDistance = Mathf.Clamp(
            gestureStartPullDistance + pullDelta,
            0f,
            maxPullDistance);

        currentYaw = gestureStartYaw;
    }

    void UpdateTwoHandGesture()
    {
        if (!TryGetTwoHandPositions(out Vector3 primaryPosition, out Vector3 secondaryPosition))
            return;

        Vector3 midpoint = (primaryPosition + secondaryPosition) * 0.5f;
        float pullDelta = Vector3.Dot(
            midpoint - previousTwoHandMidpoint,
            GetWorldPullAxis());

        float rotationDelta = 0f;
        Vector3 currentDirection = GetTwoHandDirection(primaryPosition, secondaryPosition);

        if (hasPreviousTwoHandDirection && currentDirection.sqrMagnitude > Mathf.Epsilon)
        {
            rotationDelta = Vector3.SignedAngle(
                previousTwoHandDirection,
                currentDirection,
                GetWorldUpAxis());
        }

        UpdateTwoHandGestureMode(pullDelta, rotationDelta);

        if (gestureMode == GestureMode.Pulling)
        {
            currentPullDistance = Mathf.Clamp(
                currentPullDistance + pullDelta,
                0f,
                maxPullDistance);
        }
        else if (gestureMode == GestureMode.Rotating)
        {
            currentYaw = Mathf.Clamp(
                currentYaw + rotationDelta,
                Mathf.Min(yawLimits.x, yawLimits.y),
                Mathf.Max(yawLimits.x, yawLimits.y));
        }

        previousTwoHandMidpoint = midpoint;

        if (currentDirection.sqrMagnitude > Mathf.Epsilon)
        {
            previousTwoHandDirection = currentDirection;
            hasPreviousTwoHandDirection = true;
        }
    }

    void UpdateTwoHandGestureMode(float pullDelta, float rotationDelta)
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float pullScore =
            Mathf.Abs(pullDelta) /
            (Mathf.Max(0.001f, pullSpeedThreshold) * deltaTime);
        float rotationScore = hasPreviousTwoHandDirection
            ? Mathf.Abs(rotationDelta) /
              (Mathf.Max(0.1f, rotationSpeedThreshold) * deltaTime)
            : 0f;

        float switchBias = Mathf.Max(1f, gestureSwitchBias);

        if (gestureMode == GestureMode.Pulling)
        {
            if (rotationScore >= 1f && rotationScore > pullScore * switchBias)
                gestureMode = GestureMode.Rotating;

            return;
        }

        if (gestureMode == GestureMode.Rotating)
        {
            if (pullScore >= 1f && pullScore > rotationScore * switchBias)
                gestureMode = GestureMode.Pulling;

            return;
        }

        if (pullScore >= 1f || rotationScore >= 1f)
        {
            gestureMode = pullScore >= rotationScore
                ? GestureMode.Pulling
                : GestureMode.Rotating;
        }
    }

    bool TryGetTwoHandPositions(
        out Vector3 primaryPosition,
        out Vector3 secondaryPosition)
    {
        bool hasPrimary =
            TryGetHandPosition(primaryInteractor, primaryHandle, out primaryPosition);
        bool hasSecondary =
            TryGetHandPosition(secondaryInteractor, secondaryHandle, out secondaryPosition);

        return hasPrimary && hasSecondary;
    }

    bool TryGetHandPosition(
        IXRSelectInteractor interactor,
        XRBaseInteractable handle,
        out Vector3 position)
    {
        position = default;

        if (interactor == null || handle == null)
            return false;

        Transform attachTransform = interactor.GetAttachTransform(handle);
        if (attachTransform == null)
            return false;

        position = attachTransform.position;
        return true;
    }

    Vector3 GetTwoHandDirection(Vector3 primaryPosition, Vector3 secondaryPosition)
    {
        return Vector3.ProjectOnPlane(
            secondaryPosition - primaryPosition,
            GetWorldUpAxis()).normalized;
    }

    void ClearGrabs()
    {
        primaryInteractor = null;
        primaryHandle = null;
        secondaryInteractor = null;
        secondaryHandle = null;
        gestureMode = GestureMode.None;
    }

    void UpdateDeploymentLatch()
    {
        isLatchedDeployed =
            maxPullDistance > Mathf.Epsilon &&
            currentPullDistance / maxPullDistance >=
            Mathf.Clamp01(deployedLatchThreshold);
    }

    Vector3 GetWorldUpAxis()
    {
        return transform.parent != null
            ? transform.parent.up
            : Vector3.up;
    }

    Vector3 GetWorldPullAxis()
    {
        return -GetWorldUpAxis();
    }

    void ApplyPosition()
    {
        if (transform.parent == null)
        {
            transform.position = retractedLocalPosition + Vector3.down * currentPullDistance;
            return;
        }

        Vector3 retractedWorldPosition = transform.parent.TransformPoint(retractedLocalPosition);
        Vector3 targetWorldPosition =
            retractedWorldPosition + GetWorldPullAxis() * currentPullDistance;

        transform.localPosition = transform.parent.InverseTransformPoint(targetWorldPosition);
    }

    void ApplyRotation()
    {
        transform.localRotation =
            initialLocalRotation * Quaternion.AngleAxis(currentYaw, Vector3.up);
    }
}
