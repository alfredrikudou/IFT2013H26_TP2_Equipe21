using System.Collections.Generic;
using UnityEngine;

namespace CustomParticleSystem
{
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance;

        [System.Serializable]
        public enum EntryNames
        {
            DefaultExplosion,
            Death,
            IceExplosion,
            FireExplosion,
            ElectricExplosion,
            LightExplosion,
            RedExplosion,
            YellowExplosion,
            BlueExplosion,
            Fog,
            Rain,
            Scorch
        }
        [System.Serializable]
        public class Entry
        {
            public EntryNames key;
            public GameObject prefab;
            public int initialSize = 10;
        }

        public List<Entry> entries;

        private Dictionary<EntryNames, ParticleSystemPool> _pools;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _pools = new Dictionary<EntryNames, ParticleSystemPool>();

            foreach (var e in entries)
            {
                var pool = new ParticleSystemPool(e.prefab, e.initialSize);
                _pools[e.key] = pool;
            }
        }

        public void Play(EntryNames key, Vector3 position)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                Debug.LogError($"No pool for key {key}");
                return;
            }

            var ps = pool.GetOne();

            var lifetime = ps.GetComponent<ParticleLifetime>();
            if (lifetime == null)
            {
                lifetime = ps.gameObject.AddComponent<ParticleLifetime>();
                lifetime.Init(pool);
            }

            ps.transform.position = position;
            ps.gameObject.SetActive(true);

            lifetime.Play();
        }
    }
}
