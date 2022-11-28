using System;
using UnityEngine;
namespace TensorWar
{
#pragma warning disable 649
    internal enum CarDriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        AllWheelDrive
    }

    internal enum SpeedType
    {
        Mph,
        Kph
    }

    public class VehicleController : MonoBehaviour
    {
        const int NoOfGears = 5;
        [HideInInspector] public int m_PlayerNumber; // This specifies which player this the manager for.
        [SerializeField] CarDriveType m_CarDriveType = CarDriveType.AllWheelDrive;
        [SerializeField] WheelCollider[] m_WheelColliders = new WheelCollider[6];
        [SerializeField] GameObject[] m_WheelMeshes = new GameObject[6];
        // [SerializeField] private WheelEffects[] m_WheelEffects = new WheelEffects[6];
        [SerializeField] Vector3 m_CentreOfMassOffset;
        [SerializeField] float m_MaximumSteerAngle = 25;

        [Range(0, 1)][SerializeField] float m_SteerHelper; // 0 is raw physics , 1 the car will grip in the direction it is facing

        [Range(0, 1)][SerializeField] float m_TractionControl; // 0 is no traction control, 1 is full interference

        [SerializeField] float m_FullTorqueOverAllWheels = 1000f;
        [SerializeField] float m_ReverseTorque = 500f;
        [SerializeField] float m_MaxHandbrakeTorque = 100000000;
        [SerializeField] float m_Downforce = 100f;
        [SerializeField] SpeedType m_SpeedType = SpeedType.Mph;
        [SerializeField] float m_Topspeed = 40;
        [SerializeField] float m_SlipLimit = 0.8f;
        [SerializeField] float m_BrakeTorque = 20000f;

        [Header("Self Righting")]
        public float m_WaitTime = 3f;
        public float m_VelocityThreshold = 1f;
        float m_CurrentTorque;
        float m_GearFactor;
        int m_GearNum;
        float m_LastOkTime;
        float m_OldRotation;

        Vector3
            m_PrevPos,
            m_Pos;
        readonly float m_RevRangeBoundary = 1f;
        Rigidbody m_Rigidbody;

        Quaternion[] m_WheelMeshLocalRotations;
        public float MaxSteerAngle
        {
            get
            {
                return m_MaximumSteerAngle;
            }
        }
        public float CurrentSteerAngle
        {
            get;
            private set;
        }
        float MaxSpeed
        {
            get
            {
                return m_Topspeed;
            }
        }
        float CurrentSpeed
        {
            get
            {
                return m_Rigidbody.velocity.magnitude * 2.23693629f;
            }
        }
        public float AccelInput { get; private set; }
        public float Revs { get; private set; }

        void Start()
        {
            m_WheelMeshLocalRotations = new Quaternion[6];
            for (int i = 0; i < 6; i++) m_WheelMeshLocalRotations[i] = m_WheelMeshes[i].transform.localRotation;
            m_WheelColliders[0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;
            m_MaxHandbrakeTorque = float.MaxValue;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_CurrentTorque = m_FullTorqueOverAllWheels - m_TractionControl * m_FullTorqueOverAllWheels;
            // m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
            // m_GameManager = GetComponentInParent<GameManager>();
        }

        void Update()
        {
            if (transform.up.y > 0f || m_Rigidbody.velocity.magnitude > m_VelocityThreshold) m_LastOkTime = Time.time;
            if (Time.time > m_LastOkTime + m_WaitTime) RightTank();

        }

        void GearChanging()
        {
            float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
            float upGearLimit = 1 / (float)NoOfGears * (m_GearNum + 1);
            float downGearLimit = 1 / (float)NoOfGears * m_GearNum;
            if (m_GearNum > 0 && f < downGearLimit) m_GearNum--;
            if (f > upGearLimit && m_GearNum < NoOfGears - 1) m_GearNum++;
        }

        // simple function to add a curved bias towards 1 for a value in the 0-1 range
        static float CurveFactor(float factor)
        {
            return 1 - (1 - factor) * (1 - factor);
        }

        // unclamped version of Lerp, to allow value to exceed the from-to range
        static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }

        void CalculateGearFactor()
        {
            float f = 1 / (float)NoOfGears;

            // gear factor is a normalised representation of the current speed within the current gear's range of speeds.
            // We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
            float targetGearFactor =
                Mathf
                    .InverseLerp(f * m_GearNum,
                        f * (m_GearNum + 1),
                        Mathf.Abs(CurrentSpeed / MaxSpeed));
            m_GearFactor =
                Mathf.Lerp(m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
        }

        void CalculateRevs()
        {
            // calculate engine revs (for display / sound)
            // (this is done in retrospect - revs are not used in force/power calculations)
            CalculateGearFactor();
            float gearNumFactor = m_GearNum / (float)NoOfGears;
            float revsRangeMin = ULerp(0f, m_RevRangeBoundary, CurveFactor(gearNumFactor));
            float revsRangeMax = ULerp(m_RevRangeBoundary, 1f, gearNumFactor);
            Revs = ULerp(revsRangeMin, revsRangeMax, m_GearFactor);
        }

        public void Move(float steering, float accel, float footbrake, float handbrake)
        {
            for (int i = 0; i < 6; i++)
            {
                m_WheelColliders[i].GetWorldPose(out var position, out var quat);
                m_WheelMeshes[i].transform.position = position;
                m_WheelMeshes[i].transform.rotation = quat;
            }

            //clamp input values
            steering = Mathf.Clamp(steering, -1, 1);
            AccelInput = accel = Mathf.Clamp(accel, 0, 1);
            footbrake = -1 * Mathf.Clamp(footbrake, -1, 0);
            handbrake = Mathf.Clamp(handbrake, 0, 1);

            //Set the steer on the front wheels.
            //Assuming that wheels 0 and 1 are the front wheels.
            CurrentSteerAngle = steering * m_MaximumSteerAngle;
            m_WheelColliders[0].steerAngle = CurrentSteerAngle;
            m_WheelColliders[1].steerAngle = CurrentSteerAngle;

            SteerHelper();
            ApplyDrive(accel, footbrake);
            CapSpeed();

            //Set the handbrake.
            //Assuming that wheels 4 and 5 are the rear wheels.
            if (handbrake > 0f)
            {
                float hbTorque = handbrake * m_MaxHandbrakeTorque;
                m_WheelColliders[4].brakeTorque = hbTorque;
                m_WheelColliders[5].brakeTorque = hbTorque;
            }

            CalculateRevs();
            GearChanging();
            AddDownForce();
            // CheckForWheelSpin();
            TractionControl();
        }

        void CapSpeed()
        {
            float speed = m_Rigidbody.velocity.magnitude;
            switch (m_SpeedType)
            {
                case SpeedType.Mph:
                    speed *= 2.23693629f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity =
                            m_Topspeed / 2.23693629f *
                            m_Rigidbody.velocity.normalized;
                    break;
                case SpeedType.Kph:
                    speed *= 3.6f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity =
                            m_Topspeed / 3.6f *
                            m_Rigidbody.velocity.normalized;
                    break;
            }
        }

        void ApplyDrive(float accel, float footbrake)
        {
            float thrustTorque;
            switch (m_CarDriveType)
            {
                case CarDriveType.AllWheelDrive:
                    // thrustTorque = accel * (m_CurrentTorque / 4f);
                    thrustTorque = accel * m_CurrentTorque;
                    for (int i = 0; i < 6; i++) m_WheelColliders[i].motorTorque = thrustTorque;

                    break;
                case CarDriveType.FrontWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 2f);
                    m_WheelColliders[0].motorTorque =
                        m_WheelColliders[1].motorTorque = thrustTorque;
                    break;
                case CarDriveType.RearWheelDrive:
                    thrustTorque = accel * (m_CurrentTorque / 2f);
                    m_WheelColliders[2].motorTorque =
                        m_WheelColliders[3].motorTorque = thrustTorque;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < 6; i++)
                if (
                    CurrentSpeed > 5 &&
                    Vector3.Angle(transform.forward, m_Rigidbody.velocity) < 50f
                )
                {
                    m_WheelColliders[i].brakeTorque = m_BrakeTorque * footbrake;
                }
                else if (footbrake > 0)
                {
                    m_WheelColliders[i].brakeTorque = 0f;
                    m_WheelColliders[i].motorTorque =
                        -m_ReverseTorque * footbrake;
                }
        }

        void SteerHelper()
        {
            for (int i = 0; i < 6; i++)
            {
                m_WheelColliders[i].GetGroundHit(out var wheelhit);
                if (wheelhit.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }

            // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
            if (Mathf.Abs(m_OldRotation - transform.eulerAngles.y) < 10f)
            {
                float turnadjust =
                    (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
                var velRotation =
                    Quaternion.AngleAxis(turnadjust, Vector3.up);
                m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
            }

            m_OldRotation = transform.eulerAngles.y;
        }

        // this is used to add more grip in relation to speed
        void AddDownForce()
        {
            m_WheelColliders[0]
                .attachedRigidbody
                .AddForce(-transform.up * (m_Downforce * m_WheelColliders[0].attachedRigidbody.velocity.magnitude));
        }

        // checks if the wheels are spinning and is so does three things
        // 1) emits particles
        // 2) plays tire skidding sounds
        // 3) leaves skidmarks on the ground
        // these effects are controlled through the WheelEffects class
        // private void CheckForWheelSpin()
        // {
        //     // loop through all wheels
        //     for (var i = 0; i < 6; i++)
        //     {
        //         m_WheelColliders[i].GetGroundHit(out var wheelHit);
        //
        //         // is the tire slipping above the given threshhold
        //         if (
        //             Mathf.Abs(wheelHit.forwardSlip) >= m_SlipLimit ||
        //             Mathf.Abs(wheelHit.sidewaysSlip) >= m_SlipLimit
        //         )
        //         {
        //             m_WheelEffects[i].EmitTyreSmoke();
        //
        //             // avoiding all four tires screeching at the same time
        //             // if they do it can lead to some strange audio artefacts
        //             if (!AnySkidSoundPlaying()) m_WheelEffects[i].PlayAudio();
        //
        //             continue;
        //         }
        //
        //         // if it wasn't slipping stop all the audio
        //         if (m_WheelEffects[i].PlayingAudio) m_WheelEffects[i].StopAudio();
        //
        //         // end the trail generation
        //         m_WheelEffects[i].EndSkidTrail();
        //     }
        // }

        // crude traction control that reduces the power to wheel if the car is wheel spinning too much
        void TractionControl()
        {
            WheelHit wheelHit;
            switch (m_CarDriveType)
            {
                case CarDriveType.AllWheelDrive:
                    // loop through all wheels
                    for (int i = 0; i < 6; i++)
                    {
                        m_WheelColliders[i].GetGroundHit(out wheelHit);

                        AdjustTorque(wheelHit.forwardSlip);
                    }

                    break;
                case CarDriveType.RearWheelDrive:
                    m_WheelColliders[2].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[3].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;
                case CarDriveType.FrontWheelDrive:
                    m_WheelColliders[0].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[1].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;
            }
        }

        void AdjustTorque(float forwardSlip)
        {
            if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0)
            {
                m_CurrentTorque -= 10 * m_TractionControl;
            }
            else
            {
                m_CurrentTorque += 10 * m_TractionControl;
                if (m_CurrentTorque > m_FullTorqueOverAllWheels) m_CurrentTorque = m_FullTorqueOverAllWheels;
            }
        }

        // private bool AnySkidSoundPlaying()
        // {
        //     for (var i = 0; i < 6; i++)
        //         if (m_WheelEffects[i].PlayingAudio)
        //             return true;
        //
        //     return false;
        // }

        void RightTank()
        {
            // set the correct orientation for the car, and lift it off the ground a little
            var transform1 = transform;
            transform1.position += Vector3.up;
            transform.rotation = Quaternion.LookRotation(transform1.forward);
        }
    }
}
