using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class Tank : MonoBehaviour
{
    [Header("Controls")]
    public XRLever L_Lever;
    public XRLever R_Lever;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    float maxTrackSpeed = 5f;

    [SerializeField, Min(0.01f)]
    float trackWidth = 2f;

    [SerializeField, Min(0f)]
    float leverDeadZone = 2f;

    public float LeftTrackSpeed { get; private set; }
    public float RightTrackSpeed { get; private set; }

    void FixedUpdate()
    {
        if (L_Lever == null || R_Lever == null)
            return;

        LeftTrackSpeed = GetTrackInput(L_Lever) * maxTrackSpeed;
        RightTrackSpeed = GetTrackInput(R_Lever) * maxTrackSpeed;

        float forwardSpeed = (LeftTrackSpeed + RightTrackSpeed) * 0.5f;
        float angularSpeed = (LeftTrackSpeed - RightTrackSpeed) / trackWidth;
        float deltaAngle = angularSpeed * Time.fixedDeltaTime * Mathf.Rad2Deg;

        // Move along the heading halfway through this physics step for smoother arcs.
        Quaternion halfTurn = Quaternion.AngleAxis(deltaAngle * 0.5f, Vector3.up);
        Vector3 movementDirection = halfTurn * transform.forward;

        transform.position += movementDirection * (forwardSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.AngleAxis(deltaAngle, Vector3.up) * transform.rotation;
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
}
