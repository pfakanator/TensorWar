using UnityEngine;
namespace TensorWar
{
    public class TerrainEnvironmentController : MonoBehaviour
    {
        public Terrain terrain;
        public float spawnAreaMarginMultiplier = 1f;
        public float maxY = 2;
        public float minY = 2;
        [Tooltip("Number of random points to generate on the surface.")]
        public int numPoints = 100;
        [Tooltip("Maximal number of iterations to find the points.")]
        public int maxIterations = 1000;
        Bounds areaBounds;

        [Header("SPAWN")]
        Bounds terrainBounds;

        void Start()
        {
            // terrainBounds = terrain.GetComponent<TerrainCollider>().bounds;
            // RandomSpawn();
        }

        void OnDrawGizmos()
        {
            var bounds = terrain.GetComponent<TerrainCollider>().bounds;

            // Draw the bounding box of the transform
            Gizmos.color = new Color(1, 0, 0, 1f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        public Vector3 RandomResourceSpawn()
        {
            var pointOnSurface = RandomSpawn();
            pointOnSurface = new Vector3(pointOnSurface.x, pointOnSurface.y + 1f, pointOnSurface.z);
            return pointOnSurface;
        }

        public Vector3 RandomSpawn()
        {
            Vector3 pointRandom;
            var pointOnSurface = Vector3.zero;
            bool pointFound = false;
            int indexPoints = 0;
            int indexLoops = 0;
            do
            {
                indexLoops++;
                pointRandom = RandomPointInBounds(terrainBounds);
                pointFound = GetRandomPointOnTerrainSurface(pointRandom, out pointOnSurface);

                if (pointFound)
                {
                    indexPoints++;
                }
            }
            while (indexPoints < numPoints && indexLoops < maxIterations);
            if (Physics.CheckBox(pointOnSurface, new Vector3(3f, 0f, 3f)) == false)
            {
                return pointOnSurface;
            }
            return pointOnSurface;
        }

        bool GetRandomPointOnTerrainSurface(Vector3 point, out Vector3 pointSurface)
        {

            var pointOnSurface = Vector3.zero;
            RaycastHit hit;
            bool pointFound = false;
            // Raycast against the surface of the transform
            Debug.DrawRay(point, transform.up, Color.green, 5f);
            if (Physics.Raycast(point, transform.up, out hit, Mathf.Infinity))
            {
                //Debug.Log("Found point up");
                pointOnSurface = hit.point;
                pointFound = true;
            }
            else
            {
                Debug.DrawRay(point, -transform.up, Color.red, 5f);
                if (Physics.Raycast(point, -transform.up, out hit, Mathf.Infinity))
                {
                    //Debug.Log("Found point -up");
                    pointOnSurface = hit.point;
                    pointFound = true;
                }
            }

            pointSurface = pointOnSurface;
            return pointFound;
        }

        Vector3 RandomPointInBounds(Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y + minY, bounds.max.y - maxY),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }
    }
}
