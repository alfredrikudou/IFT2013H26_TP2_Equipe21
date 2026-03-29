using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Agents
{
    /// <summary>
    /// Base class for all controllable agents (human players and AI).
    /// Handles: health, movement, aim, shooting, camera viewport.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Agent : MonoBehaviour
    {
        [SerializeField] protected Camera _camera;
        
        [Header("Movement")] [SerializeField] protected float moveSpeed = 5f;
        protected Vector2 _moveInput = Vector2.zero;
        
        [Header("Aim")] [SerializeField] protected Transform _cannon;
        [SerializeField] protected float _aimSpeed = 90f;
        [SerializeField] protected float _pitchMin = -30f;
        [SerializeField] protected float _pitchMax = 0f;
        protected float _aimYaw;
        protected float _aimPitch;

        [Header("Health")] [SerializeField] protected float _maxHealth = 100f;
        protected float _currentHealth;
        protected AgentHealth AgentHealth;
        public bool IsDead => _currentHealth <= 0f;
        public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;

        [Header("Shooting")] [SerializeField] protected Transform _firePoint;
        [SerializeField] protected Projectile _projectilePrefab;
        [SerializeField] protected float _muzzleDistance = 0.8f;
        [SerializeField] protected float _minShotSpeed = 6f;
        [SerializeField] protected float _maxShotSpeed = 18f;
        [SerializeField] protected float _chargeSecondsToMax = 1.2f;
        [SerializeField] protected float _explosionRadius = 3f;
        [SerializeField] protected float _explosionMaxDamage = 40f;
        [SerializeField] protected bool _explosionDamagesShooter = true;
        protected float _charge01 = 0f;
        protected bool _charging = false;

        [Header("Power UI (World Space)")] [SerializeField]
        protected Slider _powerSlider;

        [SerializeField] protected Vector3 _sliderOffset = new Vector3(0f, -0.6f, 0f);

        [Header("Nom (HUD)")]
        [Tooltip("TextMeshPro ou TextMeshProUGUI dans le prefab : affiche le nom défini par le menu ou SetName.")]
        [SerializeField] protected TMP_Text _nameTextMeshPro;
        [Tooltip("Si vide, le canon sert d’ancre : le nom suit la visée comme la caméra. Sinon, utilisez ex. CameraPivot.")]
        [SerializeField] private Transform _nameLabelAnchor;
        [Tooltip("Position locale du nom par rapport à l’ancre (au-dessus / devant le canon).")]
        [SerializeField] private Vector3 _nameLabelLocalOffset = new Vector3(0f, 0.28f, 0.12f);

        protected Rigidbody _rb;
        private static int nameCount = 0;
        protected string _name = $"Agent {nameCount++}";
        
        public float Charge01 => _charge01;
        public Transform Cannon => _cannon;
        public Transform FirePoint
        {
            get
            {
                EnsureFirePoint();
                return _firePoint;
            }
        }
        
        protected int _slotIndex = -1;
        public int SlotIndex => _slotIndex;

        public void SetSlotIndex(int i) => _slotIndex = i;

        public string GetName() => _name;

        public void SetName(string agentName)
        {
            if (!string.IsNullOrWhiteSpace(agentName)) _name = agentName.Trim();
            RefreshNameDisplay();
        }

        public static void ResetStaticNaming()
        {
            nameCount = 0;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            if (AgentHealth != null)
                amount = AgentHealth.ModifyIncomingDamage(amount);
            if (amount <= 0f)
            {
                AgentHealth?.RefreshHealthUI();
                return;
            }

            _currentHealth -= amount;
            if (_currentHealth < 0f) _currentHealth = 0f;
            AgentHealth?.RefreshHealthUI();
            GameManager.Instance?.OnPlayerHealthChanged(this);
            if (IsDead) AgentHealth?.HandleDeath();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            AgentHealth?.RefreshHealthUI();
        }

        protected void ApplyAim(float yawDelta, float pitchDelta)
        {
            _aimYaw += yawDelta;
            _aimPitch -= pitchDelta;
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);
            UpdateCannonRotation();
        }

        public void AimTowards(float targetYaw, float targetPitch)
        {
            float dt = Time.deltaTime;
            _aimYaw = Mathf.MoveTowardsAngle(_aimYaw, targetYaw, _aimSpeed * dt);
            _aimPitch = Mathf.MoveTowardsAngle(_aimPitch, targetPitch, _aimSpeed * dt);
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);
            UpdateCannonRotation();
        }

        private void UpdateCannonRotation()
        {
            if (_cannon != null)
                _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);
        }

        public void AddCharge(float dt)
        {
            _charging = true;
            _charge01 += dt / Mathf.Max(0.01f, _chargeSecondsToMax);
            _charge01 = Mathf.Clamp01(_charge01);
            UpdatePowerUI();
        }

        public void ResetCharge()
        {
            _charging = false;
            _charge01 = 0f;
            UpdatePowerUI();
        }

        public void FireShot(float charge01)
        {
            if (_projectilePrefab == null || _cannon == null) return;
            EnsureFirePoint();
            if (_firePoint == null) return;

            float speed = Mathf.Lerp(_minShotSpeed, _maxShotSpeed, Mathf.Clamp01(charge01));
            var projectile = Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);
            projectile.Init(this, _explosionRadius, _explosionMaxDamage, _explosionDamagesShooter, _firePoint.forward,
                speed);
        }

        protected void ApplyMovement()
        {
            if (_camera == null) return;

            Vector3 camForward = _camera.transform.forward;
            Vector3 camRight = _camera.transform.right;
            camForward.y = 0f;
            camForward.Normalize();
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = camForward * _moveInput.y + camRight * _moveInput.x;
            _rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, _rb.linearVelocity.y, moveDir.z * moveSpeed);
        }

        protected void StopMovement()
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
        }

        public void SetViewport(Rect rect)
        {
            if (_camera == null) return;
            _camera.rect = rect;
            _camera.enabled = true;
        }

        public void DisableCamera()
        {
            if (_camera == null) return;
            _camera.enabled = false;
        }

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            AgentHealth = GetComponent<AgentHealth>();
            _currentHealth = _maxHealth;

            EnsureFirePoint();
            EnsurePowerSlider();
            ResolveNameTextMeshProIfNeeded();
            RefreshNameDisplay();
            AgentHealth?.RefreshHealthUI();
        }

        protected virtual void FixedUpdate()
        {
            if (GamePauseState.IsPaused || (GameManager.Instance != null && GameManager.Instance.IsMatchOver))
            {
                StopMovement();
                return;
            }

            ApplyMovement();
        }

        protected virtual void LateUpdate()
        {
            if (_powerSlider != null)
            {
                var canvas = _powerSlider.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvas.transform.position = transform.position + _sliderOffset;
            }

            UpdateNameLabelBillboard();
        }

        /// <summary>
        /// Place le nom sur l’ancre (canon par défaut) et l’oriente comme la caméra du slot pour une lecture nette.
        /// </summary>
        private void UpdateNameLabelBillboard()
        {
            if (_nameTextMeshPro == null) return;

            Transform anchor = _nameLabelAnchor != null
                ? _nameLabelAnchor
                : (_cannon != null ? _cannon : transform);

            Transform nameT = _nameTextMeshPro.transform;
            nameT.position = anchor.TransformPoint(_nameLabelLocalOffset);

            if (_camera != null && _camera.isActiveAndEnabled)
                nameT.rotation = _camera.transform.rotation;
        }

        private void EnsureFirePoint()
        {
            if (_firePoint != null || _cannon == null) return;

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

            var canvasGo = new GameObject("PowerUI");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1.6f, 0.25f);

            var sliderGo = new GameObject("PowerSlider");
            sliderGo.transform.SetParent(canvasGo.transform, false);
            var slider = sliderGo.AddComponent<Slider>();
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = Vector2.zero;
            sliderRt.anchorMax = Vector2.one;
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;

            _powerSlider = slider;
            UpdatePowerUI();
        }

        private void UpdatePowerUI()
        {
            if (_powerSlider != null) _powerSlider.value = _charge01;
        }

        /// <summary>Si le champ n’est pas relié dans l’inspecteur, cherche un enfant nommé « NameLabel » (n’importe quelle profondeur).</summary>
        private void ResolveNameTextMeshProIfNeeded()
        {
            if (_nameTextMeshPro != null) return;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "NameLabel") continue;
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    _nameTextMeshPro = tmp;
                    break;
                }
            }
        }

        private void RefreshNameDisplay()
        {
            if (_nameTextMeshPro == null) return;
            _nameTextMeshPro.text = _name;
            _nameTextMeshPro.raycastTarget = false;
        }
    }
}