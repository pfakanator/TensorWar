using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
namespace TensorWar
{
    [RequireComponent(typeof(VehicleController))]
    public class Mk2Agent : Agent
    {

        public int agentId;
        public Rigidbody m_EnemyRb;
        public Rigidbody ammo;
//        public Rigidbody health;
        public GameObject m_ExplosionPrefab;
        public int startingAmmo = 25;
        public int startingAmmoMin = 1;
        public int startingAmmoMax = 20;
        public float startingHealth = 100f; // The amount of health each tank starts with.

        [Header("STATS")]
        public int currentAmmo;
        public float currentHealth;

        [Header("REWARDS")]
        public float targetKillReward = 1f;
        public float deathPenalty = -0.001f;
        [SerializeField] float touchedAmmoReward = 0.01f;
        // [SerializeField] float touchedHealthReward = 0.05f;
        public float ammoEmptyPenalty = -0.0000001f;
        public float fallPenalty = -0.07f;
        public float wallImpactPenalty = -0.05f;
        public float velocityRewardScalar = 0.0000001f;

        public bool useVectorObs = true;
        Rigidbody m_AgentRb;
        Mk2Agent m_EnemyAgent;
        PlaneEnvironmentController m_PlaneEnvironmentController;
        VehicleController m_VehicleController;
        WeaponController m_WeaponController;

        void OnCollisionEnter(Collision col)
        {
            if (col.gameObject.CompareTag("ammo"))
            {
                AddReward(touchedAmmoReward);
                // Debug.Log("<color=magenta> ---I got AMMO!!--- </color>" + touchedAmmoReward);
                var position = m_PlaneEnvironmentController.ResourceSpawn();
                ammo.transform.position = position;
                currentAmmo = startingAmmo;
            }

            // if (col.gameObject.CompareTag("health"))
            // {
            //     AddReward(touchedHealthReward);
            //     // Debug.Log("<color=white> ---ALL FIXED!!--- </color>" + touchedHealthReward);
            //     var position = m_PlaneEnvironmentController.ResourceSpawn();
            //     health.transform.position = position;
            //     currentHealth = startingHealth;
            //
            // }

            if (col.gameObject.CompareTag("wall"))
            {
                AddReward(wallImpactPenalty);
                // Debug.Log("<color=orange> ---Wall Impact!!--- </color>" + wallImpactPenalty);
            }
        }

        public override void OnEpisodeBegin()
        {
            ResetAgent();
            // Debug.Log("<color=green> <<===THE EPISODE HAS BEGUN===>>>  Off to On Action Received</color>");
        }

        public override void Initialize()
        {
            m_WeaponController = GetComponent<WeaponController>();
            m_VehicleController = GetComponent<VehicleController>();
            m_AgentRb = GetComponent<Rigidbody>();
            m_EnemyAgent = m_EnemyRb.GetComponent<Mk2Agent>();
            m_PlaneEnvironmentController = GetComponentInParent<PlaneEnvironmentController>();

            // Debug.Log("<color=blue>===Environment Initialized--->>> On Episode Begin</color>");

        }

        void InitializeAgent()
        {
            startingAmmo = Random.Range(startingAmmoMin, startingAmmoMax);
            currentAmmo = startingAmmo;
            currentHealth = startingHealth;
            // Debug.Log("<color=magenta> ===AGENT Initialized===</color>");
        }

        public override void CollectObservations(VectorSensor sensor)
        {

            if (m_EnemyAgent.currentHealth <= 0f)
            {
                AddReward(targetKillReward);
                // Debug.Log("<color=red>***---Got that Fool!----***</color>" +targetKillReward);
                // EndEpisode();
            }
            AddReward(-1f / MaxStep);

            if (!useVectorObs)
                return;
            int normalAmmo = currentAmmo / startingAmmo;
            float normalHealth = currentHealth / startingHealth;
            float normalEnemyHealth = m_EnemyAgent.currentHealth / startingHealth;
            sensor.AddObservation(normalAmmo); //Current Ammo Normalized
            sensor.AddObservation(normalHealth); //Current Health Normalized
            sensor.AddObservation(normalEnemyHealth); // Current Enemy Health Normalized
            sensor.AddObservation(transform.InverseTransformDirection(m_AgentRb.velocity));
            sensor.AddObservation(m_EnemyRb.transform.position);
            sensor.AddObservation(ammo.transform.position);

            var position = transform.position;
            var distToTarget = m_EnemyRb.position - position;
            float targetAlignment = Vector3.Dot(distToTarget, Vector3.forward);
            sensor.AddObservation(Mathf.Clamp(targetAlignment, -1, 1));
            AddReward(velocityRewardScalar * targetAlignment);
            sensor.AddObservation(transform.InverseTransformDirection(m_EnemyRb.position - position));
            // Debug.Log("<color=red>TargetAlignment: </color> " + targetAlignment);


        }

        void Battle(ActionSegment<int> act)
        {
            int steering = act[0];
            int accel = act[1];
            int fire = act[2];
            steering = steering switch
            {
                0 => 0,
                1 => 1,
                2 => -1,
                _ => steering
            };
            accel = accel switch
            {
                0 => 0,
                1 => 1,
                2 => -1,
                _ => accel
            };
            fire = fire switch
            {
                0 => 0,
                1 => 1,
                _ => fire
            };
            m_VehicleController.Move(steering, accel, accel, 0f);
            if (currentAmmo >= 1f)
            {
                m_WeaponController.LaunchSequence(fire);
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.DiscreteActions;
            Battle(actions);

            if (transform.localPosition.y < -0.1f)
            {
                AddReward(fallPenalty);
                //Debug.Log("<color=blue> ---I've Fallen And I can't Get up---</color>");
                EndEpisode();
            }
            if (currentHealth <= 0f)
            {
                AddReward(deathPenalty);
                //Debug.Log("<color=magenta>****I Died!****</color>");
                EndEpisode();
            }

            if (currentAmmo <= 0f)
            {
                AddReward(ammoEmptyPenalty);
                // Debug.Log("<color=yellow> ***No Ammo!****</color>" + ammoEmptyPenalty);
                // EndEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            if (Input.GetKey(KeyCode.D)) discreteActionsOut[0] = 1;
            if (Input.GetKey(KeyCode.A)) discreteActionsOut[0] = 2;
            if (Input.GetKey(KeyCode.W)) discreteActionsOut[1] = 1;
            if (Input.GetKey(KeyCode.S)) discreteActionsOut[1] = 2;
            if (Input.GetKey(KeyCode.Space)) discreteActionsOut[2] = 1;
        }

        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            //Debug.Log("<color=red>OUCH HIT: </color>" + damage);
        }

        public void ConsumeAmmo(int amount)
        {
            currentAmmo -= amount;
        }

        void ResetAgent()
        {
            m_VehicleController.Move(0f, 0f, 0f, 0f);
            m_AgentRb.velocity = Vector3.zero;
            m_AgentRb.angularVelocity = Vector3.zero;
            transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            InitializeAgent();
            transform.position = m_PlaneEnvironmentController.RandomSpawn();
            // Debug.Log("<color=green>===AGENT Reset===>>>>  Go To On Episode Begin  </color>");
        }
    }
}
