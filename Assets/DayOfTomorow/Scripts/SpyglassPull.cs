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

    public float Deployment =>
        maxPullDistance > Mathf.Epsilon ? currentPullDistance / maxPullDistance : 0f;

    public bool IsGrabbed => activeInteractor != null;

    Vector3 retractedLocalPosition;
    IXRSelectInteractor activeInteractor;
    XRBaseInteractable activeHandle;
    float currentPullDistance;
    float grabStartPullDistance;
    Vector3 grabStartPosition;

    void Awake()
    {
        retractedLocalPosition = transform.localPosition;

        if (grabHandles == null || grabHandles.Length == 0)
            grabHandles = GetComponentsInChildren<XRBaseInteractable>(true);

        for (int i = 0; i < grabHandles.Length; i++)
        {
            XRBaseInteractable handle = grabHandles[i];

            if (handle == null)
                continue;

            Rigidbody handleBody = handle.GetComponent<Rigidbody>();
            if (handleBody != null)
            {
                handleBody.useGravity = false;
                handleBody.isKinematic = true;
            }
        }
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
        }
        else
        {
            currentPullDistance = Mathf.MoveTowards(
                currentPullDistance,
                0f,
                retractSpeed * Time.deltaTime);
        }

        ApplyPosition();
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        activeInteractor = args.interactorObject;
        activeHandle = args.interactableObject as XRBaseInteractable;

        if (activeInteractor == null || activeHandle == null)
            return;

        grabStartPullDistance = currentPullDistance;
        grabStartPosition = activeInteractor.GetAttachTransform(activeHandle).position;
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

            activeHandle = handle;
            activeInteractor = handle.interactorsSelecting[0];
            grabStartPullDistance = currentPullDistance;
            grabStartPosition = activeInteractor.GetAttachTransform(activeHandle).position;
            return;
        }
    }

    Vector3 GetWorldPullAxis()
    {
        return transform.parent != null
            ? -transform.parent.up
            : Vector3.down;
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
}
