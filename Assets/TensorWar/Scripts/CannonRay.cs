using UnityEngine;
namespace TensorWar
{
    public class CannonRay : MonoBehaviour
    {
        Mk2Agent m_Mk2Agent;

        // Start is called before the first frame update
        void Start()
        {
            m_Mk2Agent = GetComponentInParent<Mk2Agent>();
        }

        // Update is called once per frame
        void Update()
        {
            if (m_Mk2Agent.agentId == 0)
            {
                Transform transform1;
                var forward = (transform1 = transform).TransformDirection(Vector3.forward) * 10;
                Debug.DrawRay(transform1.position, forward, Color.blue, 0f);
            }
            if (m_Mk2Agent.agentId == 1)
            {
                Transform transform1;
                var forward = (transform1 = transform).TransformDirection(Vector3.forward) * 10;
                Debug.DrawRay(transform1.position, forward, Color.red, 0f);
            }
        }
    }
}
