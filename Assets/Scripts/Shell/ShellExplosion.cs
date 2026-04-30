using System.Collections.Generic;
using Agents;
using AudioSystem;
using CustomParticleSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace Worm
{
    /// <summary>Explosion d’obus au contact : dégâts en zone (même logique que <see cref="Projectile"/>).</summary>
    public class ShellExplosion : MonoBehaviour
    {
        [Tooltip("Filtre optionnel. Si aucun calque n’est coché, tous les calques sont pris en compte (comme le projectile).")]
        public LayerMask m_TargetMask;
        private List<ParticleManager.EntryNames> _explosionParticleKey =
            new List<ParticleManager.EntryNames>() { ParticleManager.EntryNames.DefaultExplosion,
                ParticleManager.EntryNames.IceExplosion,
                ParticleManager.EntryNames.FireExplosion,
                ParticleManager.EntryNames.ElectricExplosion,
                ParticleManager.EntryNames.LightExplosion,
                ParticleManager.EntryNames.RedExplosion,
                ParticleManager.EntryNames.YellowExplosion,
                ParticleManager.EntryNames.BlueExplosion };
        public AudioSource m_ExplosionAudio;
        [SerializeField] [Range(0f, 1f)] private float _explosionSfxBaseVolume = 1f;
        [HideInInspector] public float m_MaxLifeTime = 2f;

        [HideInInspector] public float m_MaxDamage = 100f;
        [HideInInspector] public float m_ExplosionForce = 50f;
        [HideInInspector] public float m_ExplosionRadius = 5f;

        /// <summary>Rempli au tir par <see cref="PlayerShooting"/> pour ignorer le tireur si besoin.</summary>
        [HideInInspector] public Agent m_Shooter;
        [HideInInspector] public bool m_DamageShooter = true;

        private void Start()
        {
            Destroy(gameObject, m_MaxLifeTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            int mask = m_TargetMask.value == 0 ? ~0 : m_TargetMask.value;
            Collider[] hits = Physics.OverlapSphere(transform.position, m_ExplosionRadius, mask, QueryTriggerInteraction.Ignore);

            var damaged = new HashSet<Agent>();

            foreach (Collider col in hits)
            {
                var agent = col.GetComponentInParent<Agent>();
                if (agent == null || !damaged.Add(agent))
                    continue;

                if (!m_DamageShooter && m_Shooter != null && agent == m_Shooter)
                    continue;

                var rb = agent.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius, 0f, ForceMode.Impulse);

                float dist = Vector3.Distance(transform.position, agent.transform.position);
                if (dist > m_ExplosionRadius)
                    continue;

                float falloff = 1f - dist / m_ExplosionRadius;
                falloff = Mathf.Clamp01(falloff);
                float dmg = m_MaxDamage * falloff;
                agent.TakeDamage(dmg);
            }
            ParticleManager.Instance.Play(_explosionParticleKey[Random.Range(0, _explosionParticleKey.Count)], transform.position);

            if (m_ExplosionAudio != null)
            {
                m_ExplosionAudio.volume = _explosionSfxBaseVolume * GameAudioSettings.SfxVolume;
                m_ExplosionAudio.Play();
            }

            Destroy(gameObject, m_ExplosionAudio.clip.length);
        }
    }
}
