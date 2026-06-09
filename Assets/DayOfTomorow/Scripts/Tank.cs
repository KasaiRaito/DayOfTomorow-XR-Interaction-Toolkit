using TMPro;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class Tank : MonoBehaviour
{
    [Header("Displays")]
    public TextMeshProUGUI LocationDisplay;

    [Header("Controls")]
    // Leavers
    public XRLever LeftCaterpilar_Lever;
    public XRLever RightCaterpilar_Lever;
    //JoyStick
    public XRJoystick Canon_Joystick;

    [Header("Canon")]
    [SerializeField]
    Transform canonAimSphere;

    [SerializeField, Min(0f)]
    float canonYawSpeed = 45f;

    [SerializeField, Min(0f)]
    float canonPitchSpeed = 30f;

    [SerializeField]
    Vector2 canonPitchLimits = new Vector2(-10f, 45f);

    [Header("Movement")]
    [SerializeField, Min(0f)]
    float maxTrackSpeed = 5f;

    [SerializeField, Min(0.01f)]
    float trackWidth = 2f;

    [SerializeField, Min(0f)]
    float leverDeadZone = 2f;

    public float LeftTrackSpeed { get; private set; }
    public float RightTrackSpeed { get; private set; }

    Quaternion initialCanonRotation;
    float canonYaw;
    float canonPitch;

    void Awake()
    {
        if (LeftCaterpilar_Lever == null)
            LeftCaterpilar_Lever = FindChildComponent<XRLever>("L_Lever");

        if (RightCaterpilar_Lever == null)
            RightCaterpilar_Lever = FindChildComponent<XRLever>("R_Lever");

        if (Canon_Joystick == null)
            Canon_Joystick = GetComponentInChildren<XRJoystick>(true);

        if (canonAimSphere == null)
            canonAimSphere = FindChildTransform("Orbiit");

        if (canonAimSphere != null)
            initialCanonRotation = canonAimSphere.localRotation;
    }

    void Update()
    {
        AimCanon();
    }

    void FixedUpdate()
    {
        if (LeftCaterpilar_Lever == null || RightCaterpilar_Lever == null)
            return;

        LeftTrackSpeed = GetTrackInput(LeftCaterpilar_Lever) * maxTrackSpeed;
        RightTrackSpeed = GetTrackInput(RightCaterpilar_Lever) * maxTrackSpeed;

        float forwardSpeed = (LeftTrackSpeed + RightTrackSpeed) * 0.5f;
        float angularSpeed = (LeftTrackSpeed - RightTrackSpeed) / trackWidth;
        float deltaAngle = angularSpeed * Time.fixedDeltaTime * Mathf.Rad2Deg;

        // Move along the heading halfway through this physics step for smoother arcs.
        Quaternion halfTurn = Quaternion.AngleAxis(deltaAngle * 0.5f, Vector3.up);
        Vector3 movementDirection = halfTurn * transform.forward;

        transform.position += movementDirection * (forwardSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.AngleAxis(deltaAngle, Vector3.up) * transform.rotation;

        string PostionText = ("X: " + this.gameObject.transform.position.x + ", Y: " + this.gameObject.transform.position.y + "Z: " + this.gameObject.transform.position.y);
        LocationDisplay.text = PostionText;
    }

    float GetTrackInput(XRLever lever)
    {
        float angle = lever.value;

        if (Mathf.Abs(angle) <= leverDeadZone)
            return 0f;

        float angleRange = angle > 0f
            ? Mathf.Abs(lever.maxAngle)
            : Mathf.Abs(lever.minAngle);

        if (angleRange <= Mathf.Epsilon)
            return 0f;

        return Mathf.Clamp(angle / angleRange, -1f, 1f);
    }

    void AimCanon()
    {
        if (Canon_Joystick == null || canonAimSphere == null)
            return;

        Vector2 joystickInput = Canon_Joystick.value;

        canonYaw = Mathf.Repeat(
            canonYaw + joystickInput.x * canonYawSpeed * Time.deltaTime,
            360f);

        float minPitch = Mathf.Min(canonPitchLimits.x, canonPitchLimits.y);
        float maxPitch = Mathf.Max(canonPitchLimits.x, canonPitchLimits.y);
        canonPitch = Mathf.Clamp(
            canonPitch - joystickInput.y * canonPitchSpeed * Time.deltaTime,
            minPitch,
            maxPitch);

        canonAimSphere.localRotation =
            initialCanonRotation * Quaternion.Euler(0f, canonYaw, canonPitch);
    }

    T FindChildComponent<T>(string objectName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        foreach (T component in components)
        {
            if (component.gameObject.name == objectName)
                return component;
        }

        return null;
    }

    Transform FindChildTransform(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }
}
