using System;
using Controls;
using Controls.InputBinding;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class Player : MonoBehaviour
    {
        private static int _playerCount = 0;
        private string _playerName = "";
        private PlayerControlManager _pcm;

        [Header("Contrôle")]
        [Tooltip("Si vrai, aucune entrée clavier/manette : l’IA vise les autres joueurs et tire.")]
        [SerializeField] private bool _isComputerControlled;

        [Header("Movement")] [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Transform _cannon;
        [SerializeField] private float _aimSpeed = 90f;
        [SerializeField] private float _pitchMin = -30f;
        [SerializeField] private float _pitchMax = 60f;
        private float _aimYaw;
        private float _aimPitch;

        [Header("Vie")]
        [SerializeField] private float _maxHealth = 100f;
        private float _currentHealth;

        [Header("Shooting")]
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Rigidbody _projectilePrefab;
        [SerializeField] private float _muzzleDistance = 0.8f;
        [SerializeField] private float _minShotSpeed = 6f;
        [SerializeField] private float _maxShotSpeed = 18f;
        [SerializeField] private float _chargeSecondsToMax = 1.2f;
        [SerializeField] private float _explosionRadius = 3f;
        [SerializeField] private float _explosionMaxDamage = 40f;
        [SerializeField] private bool _explosionDamagesShooter = true;

        [Header("IA (si contrôle ordinateur)")]
        [SerializeField] private float _aiAimToleranceDegrees = 10f;
        [SerializeField] private float _aiMaxAimSeconds = 4f;
        [SerializeField] private float _aiChargeMin = 0.72f;
        [SerializeField] private float _aiChargeMax = 1f;
        [Tooltip("Distance horizontale max pour commencer à charger / tirer. Au-delà, l’IA avance vers la cible (portée « confortable »).")]
        [SerializeField] private float _aiComfortShotRange = 14f;
        [Tooltip("Dès que la cible est plus loin que ça (horizontal), l’IA marche vers elle (même proche du joueur).")]
        [SerializeField] private float _aiAlwaysMoveIfFartherThan = 2.5f;
        [Tooltip("Petit déplacement latéral pour ne pas rester statique quand la cible est à portée.")]
        [SerializeField] private float _aiStrafeAmplitude = 0.35f;

        [Header("UI puissance (World Space)")]
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Vector3 _sliderOffset = new Vector3(0f, -0.6f, 0f);
 
        private Rigidbody _rb;
        private Vector2 _moveInput = Vector2.zero;
        private bool _isMyTurn = true;

        private float _charge01 = 0f;
        private bool _charging = false;

        private enum AiPhase { Idle, Aim, Charge }
        private AiPhase _aiPhase = AiPhase.Idle;
        private Player _aiTarget;
        private float _aiChargeGoal;
        private bool _aiNoTargetShotDone;
        private float _aiAimStartTime;

        public string GetName() => _playerName;
        public bool IsComputerControlled => _isComputerControlled;

        /// <summary>Appelé avant instanciation des joueurs (ex. spawn TurnManager) pour repartir à Player0.</summary>
        public static void ResetStaticPlayerNaming()
        {
            _playerCount = 0;
        }
        public bool IsDead => _currentHealth <= 0f;
        public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;

        private PlayerHealth _playerHealth;

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (_playerHealth != null)
                amount = _playerHealth.ModifyIncomingDamage(amount);
            if (amount <= 0f)
            {
                _playerHealth?.RefreshHealthUI();
                return;
            }

            _currentHealth -= amount;
            if (_currentHealth < 0f) _currentHealth = 0f;
            _playerHealth?.RefreshHealthUI();
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerHealthChanged(this);

            if (IsDead)
                _playerHealth?.HandleDeath();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            _playerHealth?.RefreshHealthUI();
        }

        public void SetTurnActive(bool isActive)
        {
            _isMyTurn = isActive;
            if (!isActive)
            {
                _moveInput = Vector2.zero;
                _charging = false;
                _charge01 = 0f;
                _aiPhase = AiPhase.Idle;
                _aiTarget = null;
                _aiNoTargetShotDone = false;
                UpdatePowerUI();
            }
            else if (_isComputerControlled)
            {
                _aiPhase = AiPhase.Aim;
                _aiTarget = null;
                _aiNoTargetShotDone = false;
                _aiAimStartTime = Time.time;
                _aiChargeGoal = UnityEngine.Random.Range(_aiChargeMin, _aiChargeMax);
                PickAiTarget();
            }
        }

        public void UpdateControl(PlayerControlDTO dto) => _pcm.UpdateControl(dto);

        public PlayerControlDTO GetProfileDTO()
        {
            return new PlayerControlDTO{
                Name = _playerName,
                BindMap = _pcm.GetBindMapSerialize(),
                Devices = _pcm.GetDevicesSerialize()
            };
        }
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerHealth = GetComponent<PlayerHealth>();
            // Avant Start : TurnManager peut déjà appeler BeginTurn ; IsDead doit être faux dès Awake.
            _currentHealth = _maxHealth;
            _playerName = $"Player{_playerCount++}";
            _pcm = new PlayerControlManager();

            EnsureFirePoint();
            EnsurePowerSlider();
            _playerHealth?.RefreshHealthUI();
        }

        private void Update()
        {
            if (!_isMyTurn || (TurnManager.Instance != null && TurnManager.Instance.IsWaitingForProjectile()))
            {
                _moveInput = Vector2.zero;
                return;
            }

            if (_isComputerControlled)
            {
                RunComputerTurn();
                return;
            }

            _moveInput = _pcm.GetActionValue(MappableAction.Move);
            var aim = _pcm.GetActionValue(MappableAction.Aim);

            _aimYaw += aim.x * _aimSpeed * Time.deltaTime;
            _aimPitch -= aim.y * _aimSpeed * Time.deltaTime;
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);

            if (_cannon != null)
                _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);

            HandleShooting();
        }

        private void PickAiTarget()
        {
            _aiTarget = null;
            float best = float.MaxValue;
            foreach (var p in FindObjectsOfType<Player>(false))
            {
                if (p == this || p.IsDead) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    _aiTarget = p;
                }
            }
        }

        /// <summary>IA : priorité au plus proche (meilleure chance de toucher), puis visée + tir chargé.</summary>
        private void RunComputerTurn()
        {
            if (_projectilePrefab == null || _cannon == null)
                return;

            EnsureFirePoint();
            if (_firePoint == null) return;

            if (_aiTarget == null || _aiTarget.IsDead)
                PickAiTarget();

            if (_aiTarget == null)
            {
                _moveInput = Vector2.zero;
                if (!_aiNoTargetShotDone)
                {
                    _aiNoTargetShotDone = true;
                    FireShot(0.35f);
                }
                return;
            }

            switch (_aiPhase)
            {
                case AiPhase.Aim:
                    AiAimAtTarget();
                    Vector3 flat = _aiTarget.transform.position - transform.position;
                    flat.y = 0f;
                    float distH = flat.magnitude;
                    if (distH > 0.01f)
                        flat /= distH;

                    // Marche vers la cible si un peu loin ; strafe léger si déjà à portée pour ne pas rester figé.
                    if (distH > _aiAlwaysMoveIfFartherThan)
                        _moveInput = new Vector2(flat.x, flat.z);
                    else if (_aiStrafeAmplitude > 0f)
                    {
                        Vector3 side = Vector3.Cross(Vector3.up, flat);
                        if (side.sqrMagnitude > 0.0001f)
                        {
                            side.Normalize();
                            float w = Mathf.Sin(Time.time * 2.1f) * _aiStrafeAmplitude;
                            _moveInput = new Vector2(side.x, side.z) * w;
                        }
                        else
                            _moveInput = Vector2.zero;
                    }
                    else
                        _moveInput = Vector2.zero;

                    bool aimReady = Time.time - _aiAimStartTime >= _aiMaxAimSeconds || AiIsAimedAtTarget();
                    bool inComfortRange = distH <= _aiComfortShotRange;
                    bool desperateShot = Time.time - _aiAimStartTime >= _aiMaxAimSeconds * 1.75f;
                    if (aimReady && (inComfortRange || desperateShot))
                        _aiPhase = AiPhase.Charge;
                    break;

                case AiPhase.Charge:
                    _moveInput = Vector2.zero;
                    _charging = true;
                    _charge01 += Time.deltaTime / Mathf.Max(0.01f, _chargeSecondsToMax);
                    _charge01 = Mathf.Clamp01(_charge01);
                    UpdatePowerUI();
                    if (_charge01 >= _aiChargeGoal)
                    {
                        FireShot(_charge01);
                        _charging = false;
                        _charge01 = 0f;
                        UpdatePowerUI();
                        _aiPhase = AiPhase.Idle;
                    }
                    break;

                default:
                    _moveInput = Vector2.zero;
                    break;
            }
        }

        /// <summary>Même convention que le joueur : Euler(pitch, yaw, 0) sur le canon (évite un blocage si LookRotation ≠ axe du mesh).</summary>
        private void AiAimAtTarget()
        {
            Vector3 aimPoint = _aiTarget.transform.position + Vector3.up * 0.5f;
            Vector3 dir = (aimPoint - _firePoint.position).normalized;
            Transform parent = _cannon.parent != null ? _cannon.parent : transform;
            Vector3 localDir = parent.InverseTransformDirection(dir);
            float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float horiz = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
            float targetPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.0001f, horiz)) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, _pitchMin, _pitchMax);

            float dt = Time.deltaTime;
            _aimYaw = Mathf.MoveTowardsAngle(_aimYaw, targetYaw, _aimSpeed * dt);
            _aimPitch = Mathf.MoveTowardsAngle(_aimPitch, targetPitch, _aimSpeed * dt);
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);
            _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);
        }

        private bool AiIsAimedAtTarget()
        {
            Vector3 aimPoint = _aiTarget.transform.position + Vector3.up * 0.5f;
            Vector3 desired = (aimPoint - _firePoint.position).normalized;
            return Vector3.Angle(_firePoint.forward, desired) <= _aiAimToleranceDegrees;
        }

        private void LateUpdate()
        {
            if (Camera.main == null) return;

            if (_powerSlider != null)
            {
                var canvas = _powerSlider.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.transform.position = transform.position + _sliderOffset;
                    canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - Camera.main.transform.position);
                }
            }
        }

        private void FixedUpdate()
        {
            bool blocked = !_isMyTurn || (TurnManager.Instance != null && TurnManager.Instance.IsWaitingForProjectile());
            if (blocked)
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                return;
            }

            _rb.linearVelocity = new Vector3(_moveInput.x * moveSpeed, _rb.linearVelocity.y, _moveInput.y * moveSpeed);
        }

        private void HandleShooting()
        {
            var shootState = _pcm.GetActionState(MappableAction.Shoot);

            if (shootState == InputState.Pressed || shootState == InputState.Held)
            {
                _charging = true;
                _charge01 += Time.deltaTime / Mathf.Max(0.01f, _chargeSecondsToMax);
                _charge01 = Mathf.Clamp01(_charge01);
                UpdatePowerUI();
            }

            if (_charging && shootState == InputState.Released)
            {
                FireShot(_charge01);
                _charging = false;
                _charge01 = 0f;
                UpdatePowerUI();
            }
        }

        private void FireShot(float charge01)
        {
            if (_projectilePrefab == null || _cannon == null)
            {
                if (_isComputerControlled)
                    Debug.LogError($"[{name}] IA : Projectile Prefab ou Canon manquant — le tour ne peut pas se terminer.");
                return;
            }
            EnsureFirePoint();
            if (_firePoint == null) return;

            float speed = Mathf.Lerp(_minShotSpeed, _maxShotSpeed, Mathf.Clamp01(charge01));
            Rigidbody proj = Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);

            // Ensure it has a Projectile behaviour so TurnManager can track impact.
            var tracker = proj.GetComponent<Projectile>();
            if (tracker == null) tracker = proj.gameObject.AddComponent<Projectile>();
            tracker.Init(this, _explosionRadius, _explosionMaxDamage, _explosionDamagesShooter);

            proj.linearVelocity = _firePoint.forward * speed;

            if (TurnManager.Instance != null)
                TurnManager.Instance.NotifyShotFired(tracker);
        }

        private void EnsureFirePoint()
        {
            if (_firePoint != null) return;
            if (_cannon == null) return;

            var existing = _cannon.Find("FirePoint");
            if (existing != null)
            {
                _firePoint = existing;
                return;
            }

            var go = new GameObject("FirePoint");
            go.transform.SetParent(_cannon, false);
            go.transform.localPosition = new Vector3(0f, 0f, _muzzleDistance);
            go.transform.localRotation = Quaternion.identity;
            _firePoint = go.transform;
        }

        private void EnsurePowerSlider()
        {
            if (_powerSlider != null) return;

            // Create a very simple world-space slider under the player if none is assigned.
            var canvasGo = new GameObject("PowerUI");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1.6f, 0.25f);

            var sliderGo = new GameObject("PowerSlider");
            sliderGo.transform.SetParent(canvasGo.transform, false);
            var slider = sliderGo.AddComponent<Slider>();
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = Vector2.zero;
            sliderRt.anchorMax = Vector2.one;
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            // Minimal visuals (will still work even if not pretty).
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;

            _powerSlider = slider;
            UpdatePowerUI();
        }

        private void UpdatePowerUI()
        {
            if (_powerSlider != null)
                _powerSlider.value = _charge01;
        }

    }
}
