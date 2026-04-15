using UnityEngine;

namespace Agents
{
    /// <summary>
    /// Logique de tir (charge, cooldown, projectile, audio, obus spécial, estimation d’impact).
    /// À placer sur le même GameObject que <see cref="Agent"/> (Player / IA).
    /// Espace de noms global : Unity retrouve le composant dans « Add Component ».
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerShooting : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _minShotSpeed = 6f;
        [SerializeField] private float _maxShotSpeed = 18f;
        [SerializeField] private float _chargeSecondsToMax = 1.2f;
        [SerializeField] private float _shotCooldown = 1f;

        [Header("Explosion")]
        [SerializeField] private float _explosionRadius = 3f;
        [SerializeField] private float _explosionMaxDamage = 40f;
        [SerializeField] private bool _explosionDamagesShooter = true;

        [Header("Audio (optionnel)")]
        [SerializeField] private AudioSource _shootingAudio;
        [SerializeField] private AudioClip _chargingClip;
        [SerializeField] private AudioClip _fireClip;

        private Agent _agent;
        private float _charge01;
        private bool _charging;
        private float _shotCooldownTimer;
        private bool _hasSpecialShell;
        private float _specialShellMultiplier = 1f;

        public float Charge01 => _charge01;
        public bool IsCharging => _charging;

        private void Awake()
        {
            _agent = GetComponent<Agent>();
            if (_agent == null)
            {
                Debug.LogError("[PlayerShooting] Aucun Agent sur ce GameObject.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (_shotCooldownTimer > 0f)
                _shotCooldownTimer = Mathf.Max(0f, _shotCooldownTimer - Time.fixedDeltaTime);
        }

        public void AddCharge(float dt)
        {
            if (!enabled || _agent == null || _agent.IsDead) return;
            if (GamePauseState.IsPaused || (GameManager.Instance != null && GameManager.Instance.IsMatchOver))
                return;
            if (_shotCooldownTimer > 0f) return;

            if (!_charging)
                PlayChargingAudio();

            _charging = true;
            _charge01 += dt / Mathf.Max(0.01f, _chargeSecondsToMax);
            _charge01 = Mathf.Clamp01(_charge01);
            _agent.NotifyShootingChargeChanged();
        }

        public void ResetCharge()
        {
            _charging = false;
            _charge01 = 0f;
            if (_agent != null)
                _agent.NotifyShootingChargeChanged();
        }

        public bool FireShot(float charge01)
        {
            if (!enabled || _agent == null) return false;
            if (_shotCooldownTimer > 0f) return false;
            if (_projectilePrefab == null || _agent.Cannon == null) return false;

            var fp = _agent.FirePoint;
            if (fp == null) return false;

            float speed = Mathf.Lerp(_minShotSpeed, _maxShotSpeed, Mathf.Clamp01(charge01));
            float damage = _explosionMaxDamage;
            if (_hasSpecialShell)
            {
                damage *= _specialShellMultiplier;
                _hasSpecialShell = false;
                _specialShellMultiplier = 1f;
            }

            var projectile = Instantiate(_projectilePrefab, fp.position, fp.rotation);
            projectile.Init(_agent, _explosionRadius, damage, _explosionDamagesShooter, fp.forward, speed);

            _shotCooldownTimer = Mathf.Max(0f, _shotCooldown);
            PlayFireAudio();
            return true;
        }

        public void EquipSpecialShell(float damageMultiplier)
        {
            _hasSpecialShell = true;
            _specialShellMultiplier = Mathf.Max(1f, damageMultiplier);
        }

        /// <summary>Impact estimé au sol (y = 0), sans obstacles.</summary>
        public Vector3 GetProjectileGroundImpact(float charge01)
        {
            if (_agent == null) return transform.position;
            var fp = _agent.FirePoint;
            if (fp == null) return transform.position;

            float speed = Mathf.Lerp(_minShotSpeed, _maxShotSpeed, Mathf.Clamp01(charge01));
            Vector3 velocity = fp.forward * speed;
            Vector3 origin = fp.position;

            float a = 0.5f * Physics.gravity.y;
            float b = velocity.y;
            float c = origin.y;
            float delta = b * b - 4f * a * c;
            if (delta <= 0f || Mathf.Abs(a) < 1e-6f) return origin;

            float t1 = (-b + Mathf.Sqrt(delta)) / (2f * a);
            float t2 = (-b - Mathf.Sqrt(delta)) / (2f * a);
            float t = t1 > 0f ? t1 : t2;
            if (t <= 0f) return origin;

            Vector3 pos = origin + new Vector3(velocity.x, 0f, velocity.z) * t;
            pos.y = 0f;
            return pos;
        }

        private void PlayChargingAudio()
        {
            if (_shootingAudio == null || _chargingClip == null) return;
            _shootingAudio.clip = _chargingClip;
            _shootingAudio.Play();
        }

        private void PlayFireAudio()
        {
            if (_shootingAudio == null || _fireClip == null) return;
            _shootingAudio.clip = _fireClip;
            _shootingAudio.Play();
        }
    }
}
