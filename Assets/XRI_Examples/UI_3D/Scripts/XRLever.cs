using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// An interactable lever that snaps into an on or off position by a direct interactor
    /// </summary>
    public class XRLever : XRBaseInteractable
    {
        const float k_LeverDeadZone = 0.1f; // Prevents rapid switching between on and off states when right in the middle

        [SerializeField]
        [Tooltip("The object that is visually grabbed and manipulated")]
        Transform m_Handle = null;

        [SerializeField]
        [Tooltip("The value of the lever")]
        float m_Value = 0.0f;

        [SerializeField]
        [Tooltip("If enabled, the lever will snap to the value position when released")]
        bool m_LockToValue;
        
        [SerializeField]
        [Tooltip("List of Stable positions")]
        List<float> m_StablePositions = new List<float>();

        [SerializeField]
        [Tooltip("Angle of the lever in the 'on' position")]
        [Range(-90.0f, 90.0f)]
        float m_MaxAngle = 90.0f;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'off' position")]
        [Range(-90.0f, 90.0f)]
        float m_MinAngle = -90.0f;
        
        [SerializeField]
        [Tooltip("Events to trigger when the lever activates")]
        UnityEvent m_OnLeverChangeValue = new UnityEvent();

        [SerializeField]
        [Tooltip("Events to trigger when the lever deactivates")]
        UnityEvent m_OnLeverDeactivate = new UnityEvent();

        IXRSelectInteractor m_Interactor;

        /// <summary>
        /// The object that is visually grabbed and manipulated
        /// </summary>
        public Transform handle
        {
            get => m_Handle;
            set => m_Handle = value;
        }

        /// <summary>
        /// The value of the lever
        /// </summary>
        public float value
        {
            get => m_Value;
            set => SetValue(value, true);
        }

        /// <summary>
        /// If enabled, the lever will snap to the value position when released
        /// </summary>
        public bool lockToValue { get; set; }

        /// <summary>
        /// Angle of the lever in the 'on' position
        /// </summary>
        public float maxAngle
        {
            get => m_MaxAngle;
            set => m_MaxAngle = value;
        }

        /// <summary>
        /// Angle of the lever in the 'off' position
        /// </summary>
        public float minAngle
        {
            get => m_MinAngle;
            set => m_MinAngle = value;
        }

        /// <summary>
        /// Events to trigger when the lever activates
        /// </summary>
        public UnityEvent onLeverActivate => m_OnLeverChangeValue;

        /// <summary>
        /// Events to trigger when the lever deactivates
        /// </summary>
        public UnityEvent onLeverDeactivate => m_OnLeverDeactivate;

        void Start()
        {
            SetValue(m_Value, true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(StartGrab);
            selectExited.AddListener(EndGrab);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(StartGrab);
            selectExited.RemoveListener(EndGrab);
            base.OnDisable();
        }

        void StartGrab(SelectEnterEventArgs args)
        {
            m_Interactor = args.interactorObject;
        }

        void EndGrab(SelectExitEventArgs args)
        {
            SetValue(m_Value, true);
            m_Interactor = null;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (isSelected)
                {
                    UpdateValue();
                }
            }
        }

        Vector3 GetLookDirection()
        {
            Vector3 direction = m_Interactor.GetAttachTransform(this).position - m_Handle.position;
            direction = transform.InverseTransformDirection(direction);
            direction.x = 0;

            return direction.normalized;
        }

        void UpdateValue()
        {
            var lookDirection = GetLookDirection();
            var lookAngle = Mathf.Atan2(lookDirection.z, lookDirection.y) * Mathf.Rad2Deg;

            if (m_MinAngle < m_MaxAngle)
                lookAngle = Mathf.Clamp(lookAngle, m_MinAngle, m_MaxAngle);
            else
                lookAngle = Mathf.Clamp(lookAngle, m_MaxAngle, m_MinAngle);

            var newValue = lookAngle;//(maxAngleDistance < minAngleDistance);

            SetHandleAngle(lookAngle);

            SetValue(newValue);
             
            Debug.Log(newValue);
        }

        void SetValue(float angle, bool forceRotation = false)
        {
            m_Value = angle;
            SetHandleAngle(m_Value);
            
            m_OnLeverChangeValue.Invoke();
            
            if (!isSelected && (m_LockToValue || forceRotation))
            {
                SetHandleAngle(FindNearestStableAngle());
            }
        }

        float FindNearestStableAngle()
        {
            int closestIndex = -1;
            float smallestDT = float.MaxValue;
            
            float closestDistance = float.MaxValue;

            for (int i = 0; i < m_StablePositions.Count; i++)
            {
                float dt = Mathf.Abs(m_StablePositions[i] - m_Handle.localRotation.x);
                
                if(dt < smallestDT)
                {
                    closestIndex = i;
                    smallestDT = dt;
                }
            }
            
            if(closestIndex != -1)
                return m_StablePositions[closestIndex];
            
            else
                return m_StablePositions[0];
        }

        void SetHandleAngle(float angle)
        {
            if (m_Handle != null)
                m_Handle.localRotation = Quaternion.Euler(angle, 0.0f, 0.0f);
        }

        void OnDrawGizmosSelected()
        {
            var angleStartPoint = transform.position;

            if (m_Handle != null)
                angleStartPoint = m_Handle.position;

            const float k_AngleLength = 0.25f;

            var angleMaxPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_MaxAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;
            var angleMinPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_MinAngle, 0.0f, 0.0f) * Vector3.up) * k_AngleLength;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(angleStartPoint, angleMaxPoint);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(angleStartPoint, angleMinPoint);
        }

        void OnValidate()
        {
            SetHandleAngle(m_Value);
        }
    }
}
