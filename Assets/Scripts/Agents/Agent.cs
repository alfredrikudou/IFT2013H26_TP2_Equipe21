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

        [Header("Shooting (point de tir — logique dans PlayerShooting)")]
        [SerializeField] protected Transform _firePoint;
        [SerializeField] protected float _muzzleDistance = 0.8f;

        protected PlayerShooting _playerShooting;

        [Header("Puissance (UI)")]
        [Tooltip("Optionnel. Barre 0–1 (souvent world-space auto). Laissez vide si vous utilisez seulement Aim Power Slider (ex. 5–20).")]
        [SerializeField] protected Slider _powerSlider;

        [Tooltip("HUD joueur recommandé (ex. AimSlider min 5 max 20). Suit la charge au maintien du tir.")]
        [SerializeField] private Slider aimPowerSlider;

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
        
        public float Charge01 => _playerShooting != null ? _playerShooting.Charge01 : 0f;

        /// <summary>Indique si le joueur maintient la charge (pour relâcher le tir).</summary>
        public bool Charging => _playerShooting != null && _playerShooting.IsCharging;
        /// <summary>Caméra du slot (split-screen) — utilisée par le culling logique / silhouette.</summary>
        public Camera ViewCamera => _camera;
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

        public void AddCharge(float dt) => _playerShooting?.AddCharge(dt);

        public void ResetCharge() => _playerShooting?.ResetCharge();

        public bool FireShot(float charge01) => _playerShooting != null && _playerShooting.FireShot(charge01);

        public void EquipSpecialShell(float damageMultiplier) => _playerShooting?.EquipSpecialShell(damageMultiplier);

        public Vector3 GetProjectileGroundImpact(float charge01) =>
            _playerShooting != null ? _playerShooting.GetProjectileGroundImpact(charge01) : transform.position;

        /// <summary>Appelé par <see cref="PlayerShooting"/> quand la charge change (UI).</summary>
        public void NotifyShootingChargeChanged() => UpdatePowerUI();

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

            _playerShooting = GetComponent<PlayerShooting>();
            if (_playerShooting == null)
                Debug.LogWarning(
                    "[Agent] Aucun composant PlayerShooting sur ce GameObject. Ajoutez-le dans l’inspecteur ou sur le prefab.",
                    this);

            EnsureFirePoint();
            EnsurePowerSlider();
            ConfigureAimPowerSlider();
            ResolveNameTextMeshProIfNeeded();
            RefreshNameDisplay();
            if (GetComponent<AgentVisibilityState>() == null)
                gameObject.AddComponent<AgentVisibilityState>();
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
            UpdateAimPowerSliderWorldCanvas();
            UpdateNameLabelBillboard();
        }

        /// <summary>
        /// Canvas world-space de l’AimSlider : position au sol, rotation locale uniquement sur Y (lacet) alignée sur la visée horizontale du canon (<see cref="_cannon"/>).
        /// </summary>
        private void UpdateAimPowerSliderWorldCanvas()
        {
            TryUpdateWorldCanvasForSlider(aimPowerSlider);
            if (_powerSlider != null && _powerSlider != aimPowerSlider)
                TryUpdateWorldCanvasForSlider(_powerSlider);
        }

        private void TryUpdateWorldCanvasForSlider(Slider slider)
        {
            if (slider == null) return;
            Canvas canvas = slider.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) return;
            if (canvas.transform is not RectTransform rt) return;

            rt.position = transform.position + _sliderOffset;
            rt.localRotation = ComputeAimSliderYawOnlyLocalRotation();
        }

        /// <summary>
        /// Même base que le prefab (X = 90° pour plaquer l’UI au sol) + lacet dérivé du canon, sans pitch ni roll.
        /// </summary>
        private Quaternion ComputeAimSliderYawOnlyLocalRotation()
        {
            const float layFlatPitchDeg = 90f;

            if (_cannon == null)
                return Quaternion.Euler(layFlatPitchDeg, 0f, 0f);

            Vector3 flatWorld = _cannon.forward;
            flatWorld.y = 0f;
            if (flatWorld.sqrMagnitude < 1e-6f)
            {
                flatWorld = transform.forward;
                flatWorld.y = 0f;
            }

            if (flatWorld.sqrMagnitude < 1e-6f)
                return Quaternion.Euler(layFlatPitchDeg, 0f, 0f);

            flatWorld.Normalize();
            Vector3 localDir = transform.InverseTransformDirection(flatWorld);
            localDir.y = 0f;
            if (localDir.sqrMagnitude < 1e-6f)
                return Quaternion.Euler(layFlatPitchDeg, 0f, 0f);

            localDir.Normalize();
            float yawDeg = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(layFlatPitchDeg, yawDeg, 0f);
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
            if (_powerSlider != null || aimPowerSlider != null)
            {
                UpdatePowerUI();
                return;
            }

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

        private void ConfigureAimPowerSlider()
        {
            if (aimPowerSlider == null) return;
            aimPowerSlider.wholeNumbers = false;
            aimPowerSlider.interactable = false;
            aimPowerSlider.value = aimPowerSlider.minValue;
        }

        private void UpdatePowerUI()
        {
            float c = Charge01;
            if (aimPowerSlider != null)
                aimPowerSlider.value = Mathf.Lerp(aimPowerSlider.minValue, aimPowerSlider.maxValue, c);
            if (_powerSlider != null && _powerSlider != aimPowerSlider)
                _powerSlider.value = c;
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