using UnityEngine;
namespace TensorWar
{
    public class PlaneEnvironmentController : MonoBehaviour
    {
        public float spawnAreaMarginMultiplier = 1f;
        public GameObject ground;

        [Header("SPAWN")] Bounds areaBounds;

        void Start()
        {
            areaBounds = ground.GetComponent<Collider>().bounds;
        }

        /// <summary>
        ///     Use the ground's bounds to pick a random spawn position.
        /// </summary>
        public Vector3 RandomSpawn()
        {
            bool foundNewSpawnLocation = false;
            var randomSpawnPos = Vector3.zero;
            while (foundNewSpawnLocation == false)
            {
                float randomPosX = Random.Range(-areaBounds.extents.x * spawnAreaMarginMultiplier,
                    areaBounds.extents.x * spawnAreaMarginMultiplier);

                float randomPosZ = Random.Range(-areaBounds.extents.z * spawnAreaMarginMultiplier,
                    areaBounds.extents.z * spawnAreaMarginMultiplier);
                randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 0f, randomPosZ);
                var transform1 = transform;
                var up = transform1.up;
                // Debug.DrawRay(randomSpawnPos - up, up, Color.green, 10f);

                if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0f, 2.5f)) == false)
                {
                    foundNewSpawnLocation = true;
                }
            }
            return randomSpawnPos;
        }

        public Vector3 ResourceSpawn()
        {
            var position = RandomSpawn();
            position = new Vector3(position.x, position.y + 1, position.z);
            return position;
        }
    }
}
