using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Content.Interaction;

public class Tank : MonoBehaviour
{
    [Header("Player")] public GameObject Player;
    
    [Header("Displays")]
    [FormerlySerializedAs("LocationDisplay")]
    public TMP_Text LeftLeverDisplay;

    public TMP_Text RightLeverDisplay;

    [Header("Controls")]
    // Movement
    public XRLever LeftCaterpilar_Lever;
    public XRLever RightCaterpilar_Lever;
    // Canon
    public XRJoystick Canon_Joystick;
    public XRPushButton Shoot_Button;

    [Header("Canon")]
    [SerializeField]
    Transform canonAimSphere;

    [SerializeField, Min(0f)]
    float canonYawSpeed = 45f;

    [SerializeField, Min(0f)]
    float canonPitchSpeed = 30f;

    [SerializeField]
    Vector2 canonPitchLimits = new Vector2(-10f, 45f);

    [Header("Shooting")]
    [SerializeField]
    SpyglassDisplay spyglassDisplay;

    [SerializeField]
    GameObject explosionFxPrefab;

    [SerializeField, Min(0f)]
    float explosionFxLifetime = 5f;

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

        DirectOnlyInteractionFilter.ApplyTo(LeftCaterpilar_Lever);
        DirectOnlyInteractionFilter.ApplyTo(RightCaterpilar_Lever);
        DirectOnlyInteractionFilter.ApplyTo(Canon_Joystick);

        if (Shoot_Button == null)
            Shoot_Button = FindChildComponent<XRPushButton>("Shoot_Button");

        if (Shoot_Button == null)
            Shoot_Button = GetComponentInChildren<XRPushButton>(true);

        if (canonAimSphere == null)
            canonAimSphere = FindChildTransform("Orbiit");

        if (spyglassDisplay == null)
            spyglassDisplay = GetComponentInChildren<SpyglassDisplay>(true);

        if (spyglassDisplay == null)
            spyglassDisplay = FindAnyObjectByType<SpyglassDisplay>(FindObjectsInactive.Include);

        if (canonAimSphere != null)
            initialCanonRotation = canonAimSphere.localRotation;
        
        if(Player != null)
            Player.transform.position = new Vector3(0, 0, 0);
    }

    void OnEnable()
    {
        if (Shoot_Button != null)
            Shoot_Button.onPress.AddListener(Shoot);
    }

    void OnDisable()
    {
        if (Shoot_Button != null)
            Shoot_Button.onPress.RemoveListener(Shoot);
    }

    void Update()
    {
        AimCanon();
        UpdateLeverDisplays();
    }

    void FixedUpdate()
    {
        if (LeftCaterpilar_Lever == null || RightCaterpilar_Lever == null)
            return;

        float leftInput = GetTrackInput(LeftCaterpilar_Lever);
        float rightInput = GetTrackInput(RightCaterpilar_Lever);

        LeftTrackSpeed = leftInput * maxTrackSpeed;
        RightTrackSpeed = rightInput * maxTrackSpeed;

        float forwardSpeed = (LeftTrackSpeed + RightTrackSpeed) * 0.5f;
        float angularSpeed = (LeftTrackSpeed - RightTrackSpeed) / trackWidth;
        float deltaAngle = angularSpeed * Time.fixedDeltaTime * Mathf.Rad2Deg;

        // Move along the heading halfway through this physics step for smoother arcs.
        Quaternion halfTurn = Quaternion.AngleAxis(deltaAngle * 0.5f, Vector3.up);
        Vector3 movementDirection = halfTurn * transform.forward;

        transform.position += movementDirection * (forwardSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.AngleAxis(deltaAngle, Vector3.up) * transform.rotation;
    }

    void UpdateLeverDisplays()
    {
        if (LeftLeverDisplay != null)
        {
            float leftValue = LeftCaterpilar_Lever != null
                ? GetTrackInput(LeftCaterpilar_Lever)
                : 0f;
            LeftLeverDisplay.SetText("L: {0:0.00}", leftValue);
        }

        if (RightLeverDisplay != null)
        {
            float rightValue = RightCaterpilar_Lever != null
                ? GetTrackInput(RightCaterpilar_Lever)
                : 0f;
            RightLeverDisplay.SetText("R: {0:0.00}", rightValue);
        }
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

    public void Shoot()
    {
        if (spyglassDisplay == null ||
            !spyglassDisplay.TryGetCannonHit(out RaycastHit hit))
        {
            return;
        }

        if (explosionFxPrefab != null)
        {
            GameObject explosionFx = Instantiate(
                explosionFxPrefab,
                hit.point,
                Quaternion.identity);

            if (explosionFxLifetime > 0f)
                Destroy(explosionFx, explosionFxLifetime);
        }

        Target target = hit.collider.GetComponentInParent<Target>();

        if (target != null)
            target.Hit();
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
