using UnityEngine;
namespace TensorWar
{
    public class CannonRound : MonoBehaviour
    {
        public LayerMask m_DamageMask; // Used to filter what the explosion affects, this should be set to "Players".
        public ParticleSystem m_ExplosionParticles; // Reference to the particles that will play on explosion.
        public AudioSource m_ExplosionAudio; // Reference to the audio that will play on explosion.
        public float m_MaxDamage = 100f; // The amount of damage done if the explosion is centred on a tank.
        public float m_ExplosionForce = 1000f; // The amount of force added to a tank at the centre of the explosion.
        public float velocityDamageMultiplier = 0.01f;
        public float m_MaxLifeTime = 2f; // The time in seconds before the shell is removed.
        public float m_ExplosionRadius = 5f; // The maximum distance away from the explosion tanks can be and are still affected.

        void Start()
        {
            // If it isn't destroyed by then, destroy
            // the shell after it's lifetime.
            Destroy(gameObject, m_MaxLifeTime);
        }


        void OnTriggerEnter(Collider col)
        {
            // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius.
            var colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_DamageMask);

            // Go through all the colliders...
            foreach (var t in colliders)
            {
                // ... and find their rigidbody.
                var targetRigidbody = t.GetComponent<Rigidbody>();

                // If they don't have a rigidbody, go on to the next collider.
                if (!targetRigidbody)
                    continue;

                // Add an explosion force.
                targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

                // Find the TankHealth script associated with the rigidbody.
                var mk2Agent = targetRigidbody.GetComponent<Mk2Agent>();

                // If there is no TankHealth script attached to the game-object, go on to the next collider.
                if (!mk2Agent)
                    continue;

                // Calculate the amount of damage the target should take based on it's distance from the shell.
                float damage = CalculateDamage(targetRigidbody.position);
                if (damage > 0f)
                {
                    // Debug.Log("<color=magenta>DAMAGE  </color>" + damage);
                }
                // Deal this damage to the tank.

                mk2Agent.TakeDamage(damage);
            }

            // Unparent the particles from the shell.
            m_ExplosionParticles.transform.parent = null;

            // Play the particle system.
            m_ExplosionParticles.Play();

            // Play the explosion sound effect.
            m_ExplosionAudio.Play();

            // Once the particles have finished, destroy the game-object they are on.
            var mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);

            // Destroy the shell.
            Destroy(gameObject);
        }


        float CalculateDamage(Vector3 targetPosition)
        {
            // Create a vector from the shell to the target.
            var explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target.
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away.
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage.
            float damage = relativeDistance * m_MaxDamage * velocityDamageMultiplier;

            // Make sure that the minimum damage is always 0.
            damage = Mathf.Max(0f, damage);

            return damage;
        }
    }
}
