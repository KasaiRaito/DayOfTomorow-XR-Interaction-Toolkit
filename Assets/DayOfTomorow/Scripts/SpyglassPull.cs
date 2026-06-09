using UnityEngine;
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

    public float Deployment =>
        maxPullDistance > Mathf.Epsilon ? currentPullDistance / maxPullDistance : 0f;

    public bool IsGrabbed => activeInteractor != null;
    public float Yaw => currentYaw;

    Vector3 retractedLocalPosition;
    Quaternion initialLocalRotation;
    IXRSelectInteractor activeInteractor;
    XRBaseInteractable activeHandle;
    float currentPullDistance;
    float grabStartPullDistance;
    float currentYaw;
    float grabStartYaw;
    Vector3 grabStartPosition;
    Vector3 grabStartTwistDirection;
    bool hasGrabStartTwistDirection;

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
    }

    void OnDisable()
    {
        if (grabHandles == null)
            return;

        foreach (XRBaseInteractable handle in grabHandles)
        {
            if (handle == null)
                continue;

            handle.selectEntered.RemoveListener(OnGrabbed);
            handle.selectExited.RemoveListener(OnReleased);
        }

        activeInteractor = null;
        activeHandle = null;
    }

    void LateUpdate()
    {
        if (activeInteractor != null && activeHandle != null)
        {
            Transform handAttach = activeInteractor.GetAttachTransform(activeHandle);
            Vector3 pullAxis = GetWorldPullAxis();
            float handMovement = Vector3.Dot(handAttach.position - grabStartPosition, pullAxis);

            currentPullDistance = Mathf.Clamp(
                grabStartPullDistance + handMovement,
                0f,
                maxPullDistance);

            UpdateTwist(handAttach.position);
        }
        else
        {
            currentPullDistance = Mathf.MoveTowards(
                currentPullDistance,
                0f,
                retractSpeed * Time.deltaTime);
        }

        ApplyPosition();
        ApplyRotation();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        BeginGrab(
            args.interactorObject,
            args.interactableObject as XRBaseInteractable);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (args.interactorObject != activeInteractor)
            return;

        activeInteractor = null;
        activeHandle = null;

        FindRemainingGrab();
    }

    void FindRemainingGrab()
    {
        foreach (XRBaseInteractable handle in grabHandles)
        {
            if (handle == null || handle.interactorsSelecting.Count == 0)
                continue;

            BeginGrab(handle.interactorsSelecting[0], handle);
            return;
        }
    }

    void BeginGrab(IXRSelectInteractor interactor, XRBaseInteractable handle)
    {
        activeInteractor = interactor;
        activeHandle = handle;

        if (activeInteractor == null || activeHandle == null)
            return;

        grabStartPullDistance = currentPullDistance;
        grabStartYaw = currentYaw;

        Vector3 handPosition =
            activeInteractor.GetAttachTransform(activeHandle).position;

        grabStartPosition = handPosition;
        grabStartTwistDirection = GetTwistDirection(handPosition);
        hasGrabStartTwistDirection =
            grabStartTwistDirection.sqrMagnitude > Mathf.Epsilon;
    }

    void UpdateTwist(Vector3 handPosition)
    {
        if (!hasGrabStartTwistDirection)
            return;

        Vector3 currentDirection = GetTwistDirection(handPosition);
        if (currentDirection.sqrMagnitude <= Mathf.Epsilon)
            return;

        float twistDelta = Vector3.SignedAngle(
            grabStartTwistDirection,
            currentDirection,
            GetWorldUpAxis());

        currentYaw = Mathf.Clamp(
            grabStartYaw + twistDelta,
            Mathf.Min(yawLimits.x, yawLimits.y),
            Mathf.Max(yawLimits.x, yawLimits.y));
    }

    Vector3 GetTwistDirection(Vector3 handPosition)
    {
        return Vector3.ProjectOnPlane(
            handPosition - transform.position,
            GetWorldUpAxis()).normalized;
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
