using UnityEngine;
namespace TensorWar
{
    public class Indicator : MonoBehaviour
    {
        public bool updatedByAgent; //should this be updated by the agent? If not, it will use local settings
        public Transform transformToFollow; //ex: hips or body
        public Transform targetToLookAt; //target in the scene the indicator will point to
        public float heightOffset;
        float m_StartingYPos;

        void Update()
        {
            if (updatedByAgent)
                return;
            var position = transformToFollow.position;
            var transform1 = transform;
            transform1.position = new Vector3(position.x, m_StartingYPos + heightOffset,
                position.z);
            var targetDir = targetToLookAt.position - transform1.position;
            targetDir.y = 0; //flatten dir on the y
            transform.rotation = Quaternion.LookRotation(targetDir);
        }

        void OnEnable()
        {
            m_StartingYPos = transform.position.y;
        }

        //Public method to allow an agent to directly update this component
        public void MatchOrientation(Transform t)
        {
            var transform1 = transform;
            var position = t.position;
            transform1.position = new Vector3(position.x, m_StartingYPos + heightOffset, position.z);
            transform1.rotation = t.rotation;
        }
    }
}
