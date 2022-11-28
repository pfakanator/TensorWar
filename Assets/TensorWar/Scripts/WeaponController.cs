using UnityEngine;
namespace TensorWar
{
    public class WeaponController : MonoBehaviour
    {
        public int agentId = 1; // Used to identify the different players.
        public Rigidbody m_Round; // Prefab of the round.
        public Transform m_FireTransform; // A child of the tank where the rounds are spawned.
        public AudioSource
            m_ShootingAudio; // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip; // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip; // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 30f; // The force given to the round if the fire button is not held.
        public float m_MaxLaunchForce = 150f; // The force given to the round if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 2f; // How long the round can charge for before it is fired at max force.
        public float fireRate = 2f;
        float lastShot;
        float m_ChargeSpeed; // How fast the launch force increases, based on the max charge time.
        float m_CurrentLaunchForce; // The force that will be given to the round when the fire button is released.
        bool m_Fired = true; // Whether or not the round has been launched with this button press.
        Mk2Agent m_Mk2Agent;

        void Start()
        {
            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }

        void OnEnable()
        {
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_Mk2Agent = GetComponent<Mk2Agent>();

        }

        public void LaunchSequence(int fire)
        {
            fire = Mathf.Clamp(fire, 0, 1);
            // If the max force has been exceeded and the round hasn't yet been launched...
            if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
            {
                // ... use the max force and launch the round.
                m_CurrentLaunchForce = m_MaxLaunchForce;
                Fire();
            }
            else
                switch (fire)
                {
                    // Otherwise, if the fire button is being held and the round hasn't been launched yet...
                    case 1 when !m_Fired:
                        // Increment the launch force and update the slider.
                        m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                        break;
                    // Otherwise, if the fire button has just started being pressed...
                    case 1:
                        // ... reset the fired flag and reset the launch force.
                        m_Fired = false;
                        m_CurrentLaunchForce = m_MinLaunchForce;

                        // Change the clip to the charging clip and start it playing.
                        m_ShootingAudio.clip = m_ChargingClip;
                        m_ShootingAudio.Play();
                        break;
                    // Otherwise, if the fire button is released and the round hasn't been launched yet...
                    case 0 when !m_Fired:
                        // ... launch the round.
                        Fire();
                        break;
                }
        }

        void Fire()
        {
            // Set the fired flag so only Fire is only called once.
            m_Fired = true;

            if (!(Time.time > fireRate + lastShot))
                return;
            // Create an instance of the round and store a reference to it's rigidbody.
            var roundInstance =
                Instantiate(m_Round, m_FireTransform.position, m_FireTransform.rotation);

            // Set the round's velocity to the launch force in the fire position's forward direction.
            roundInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;
            lastShot = Time.time;
            m_Mk2Agent.ConsumeAmmo(1);
        }
    }
}
