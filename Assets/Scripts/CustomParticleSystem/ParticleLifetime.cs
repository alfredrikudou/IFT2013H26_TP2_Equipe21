using UnityEngine;

namespace CustomParticleSystem
{
    public class ParticleLifetime : MonoBehaviour
    {
        private ParticleSystem _ps;
        private ParticleSystemPool _pool;
        private bool _isPlaying;

        public void Init(ParticleSystemPool pool)
        {
            _pool = pool;
            _ps = GetComponent<ParticleSystem>();
        }

        public void Play()
        {
            _isPlaying = true;
            _ps.Play();
        }

        void Update()
        {
            if (!_isPlaying) return;

            if (!_ps.IsAlive(true))
            {
                _isPlaying = false;
                _pool.Return(_ps);
            }
        }
    }
}
