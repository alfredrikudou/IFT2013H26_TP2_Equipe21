using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CustomParticleSystem
{
    public class ParticleSystemPool
    {
        private readonly GameObject _prefab;
        private readonly Queue<ParticleSystem> _freePool;
        private readonly Dictionary<ParticleSystem, int> _usedPool;
        private int _maxPoolSize;
        private readonly GameObject _parent;

        public ParticleSystemPool(GameObject prefab, int size)
        {
            if (size <= 0) throw new ArgumentException("Size of pool must be greater than zero");
            _maxPoolSize = size;
            _prefab = prefab;
            _freePool = new Queue<ParticleSystem>(size);
            _usedPool = new Dictionary<ParticleSystem, int>(size);
            _parent = new GameObject(prefab.name + " pool");

            for (int i = 0; i < _maxPoolSize; i++)
                _freePool.Enqueue(Create());
        }

        public void AdjustPoolSize(int newSize)
        {
            if (newSize <= 0) throw new ArgumentException("Size of pool must be greater than zero");
            for (int i = 0; i < _maxPoolSize - newSize; i++) _freePool.Dequeue();
            for(int i = 0; i < newSize - _maxPoolSize; i++) _freePool.Enqueue(Create());
            _maxPoolSize = newSize;
        }

        private ParticleSystem Create()
        {
            GameObject go = Object.Instantiate(_prefab, Vector3.zero, Quaternion.identity);
            var lifetime = go.GetComponent<ParticleLifetime>();
            if (lifetime == null)
            {
                lifetime = go.gameObject.AddComponent<ParticleLifetime>();
            }
            lifetime.Init(this);
            go.SetActive(false);
            go.transform.SetParent(_parent.transform);
            return go.GetComponent<ParticleSystem>();
        }

        public ParticleSystem GetOne()
        {
            if (_freePool.Count > 0)
            {
                var ps =  _freePool.Dequeue();
                _usedPool.Add(ps, Time.frameCount);
                return ps;
            }
            var oldest = _usedPool.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            _usedPool[oldest] = Time.frameCount;
            return oldest;
        }

        public List<ParticleSystem> GetMultiple(int count)
        {
            if(count > _maxPoolSize) AdjustPoolSize(count);
            List<ParticleSystem> ps = new List<ParticleSystem>(count);
            for(int i = 0; i <  count; i++)
                ps.Add(GetOne());
            return ps;
        }
        
        
        public void Return(ParticleSystem ps)
        {
            ps.gameObject.SetActive(false);
            _usedPool.Remove(ps);
            _freePool.Enqueue(ps);
        }
    }
}
