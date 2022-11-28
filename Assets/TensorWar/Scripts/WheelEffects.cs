using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TensorWar
{
    [RequireComponent(typeof(AudioSource))]
    public class WheelEffects : MonoBehaviour
    {
        static Transform skidTrailsDetachedParent;
        public Transform skidTrailPrefab;
        public ParticleSystem skidParticles;


        AudioSource m_AudioSource;
        Transform m_SkidTrail;
        WheelCollider m_WheelCollider;
        bool Skidding
        {
            get;
            set;
        }
        bool PlayingAudio
        {
            get;
            set;
        }


        void Start()
        {
            skidParticles = transform.root.GetComponentInChildren<ParticleSystem>();

            if (skidParticles == null)
                Debug.LogWarning(" no particle system found on car to generate smoke particles", gameObject);
            else
                skidParticles.Stop();

            m_WheelCollider = GetComponent<WheelCollider>();
            m_AudioSource = GetComponent<AudioSource>();
            PlayingAudio = false;

            if (skidTrailsDetachedParent == null)
                skidTrailsDetachedParent = new GameObject("Skid Trails - Detached").transform;
        }


        public void EmitTyreSmoke()
        {
            var transform1 = transform;
            if (skidParticles.transform != null)
                skidParticles.transform.position = transform1.position + transform1.up * m_WheelCollider.radius;
            skidParticles.Emit(1);
            if (!Skidding) StartCoroutine(StartSkidTrail());
        }


        public void PlayAudio()
        {
            m_AudioSource.Play();
            PlayingAudio = true;
        }


        public void StopAudio()
        {
            m_AudioSource.Stop();
            PlayingAudio = false;
        }


        public IEnumerator StartSkidTrail()
        {
            Skidding = true;
            m_SkidTrail = Instantiate(skidTrailPrefab, transform, true);
            var trail = new List<object>();
            while (m_SkidTrail == null)
            {
                trail.Add(null);
            }

            m_SkidTrail.localPosition = Vector3.up * m_WheelCollider.radius;
            return trail.GetEnumerator();
        }


        public void EndSkidTrail()
        {
            if (!Skidding) return;

            Skidding = false;
            m_SkidTrail.parent = skidTrailsDetachedParent;
            Destroy(m_SkidTrail.gameObject, 10);
        }
    }
}
