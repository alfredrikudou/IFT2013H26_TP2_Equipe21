using System;
using Controls;
using Controls.InputBinding;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class Player : MonoBehaviour
    {
        protected static int _playerCount = 0;
        protected string _playerName = "";
        protected PlayerControlManager _pcm;

        [SerializeField] protected Camera _camera;
        [SerializeField] protected Transform _cameraPivot;

        [Header("Movement")] [SerializeField] protected float moveSpeed = 5f;
        [SerializeField] protected Transform _cannon;
        [SerializeField] protected float _aimSpeed = 90f;
        [SerializeField] protected float _pitchMin = -30f;
        [SerializeField] protected float _pitchMax = 60f;
        protected float _aimYaw;
        protected float _aimPitch;

        [Header("Vie")]
        [SerializeField] protected float _maxHealth = 100f;
        protected float _currentHealth;

        [Header("Shooting")]
        [SerializeField] protected Transform _firePoint;
        [SerializeField] protected Rigidbody _projectilePrefab;
        [SerializeField] protected float _muzzleDistance = 0.8f;
        [SerializeField] protected float _minShotSpeed = 6f;
        [SerializeField] protected float _maxShotSpeed = 18f;
        [SerializeField] protected float _chargeSecondsToMax = 1.2f;
        [SerializeField] protected float _fireCooldown = 3f;
        [SerializeField] protected float _explosionRadius = 3f;
        [SerializeField] protected float _explosionMaxDamage = 40f;
        [SerializeField] protected bool _explosionDamagesShooter = true;

        [Header("UI puissance (World Space)")]
        [SerializeField] protected Slider _powerSlider;
        [SerializeField] protected Vector3 _sliderOffset = new Vector3(0f, -0.6f, 0f);
 
        protected Rigidbody _rb;
        protected Vector2 _moveInput = Vector2.zero;

        protected float _charge01 = 0f;
        protected bool _charging = false;


        public string GetName() => _playerName;

        protected int _slotIndex = -1;
        public int SlotIndex => _slotIndex;

        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }

        private void OnDestroy()
        {
            _pcm.Dispose();
        }

        public void SetPlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;
            _playerName = playerName.Trim();
        }

        /// <summary>Appelé avant instanciation des joueurs (ex. spawn TurnManager) pour repartir à Player0.</summary>
        public static void ResetStaticPlayerNaming()
        {
            _playerCount = 0;
        }
        public bool IsDead => _currentHealth <= 0f;
        public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;

        protected PlayerHealth _playerHealth;

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
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerHealthChanged(this);

            if (IsDead)
                _playerHealth?.HandleDeath();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            _playerHealth?.RefreshHealthUI();
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
            if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
            {
                _moveInput = Vector2.zero;
                return;
            }
            if(_pcm.GetActionState(MappableAction.Pause) == InputState.Pressed)
                FindFirstObjectByType<GamePauseController>()?.Pause();

            if (GamePauseState.IsPaused)
            {
                _moveInput = Vector2.zero;
                return;
            }

            _moveInput = _pcm.GetActionValue(MappableAction.Move);
            var aim = _pcm.GetActionValue(MappableAction.Aim);

            _aimYaw += aim.x * _aimSpeed * Time.deltaTime;
            _aimPitch -= aim.y * _aimSpeed * Time.deltaTime;
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);

            if (_cannon != null)
                _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);
            if (_camera != null)
                _cameraPivot.localRotation = Quaternion.Euler(0f, _aimYaw, 0f);  
            

            HandleShooting();
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
                    // canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - Camera.main.transform.position);
                }
            }
        }

        private void FixedUpdate()
        {
            if (GamePauseState.IsPaused)
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
            {
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
                return;
            }

            Vector3 camForward = _camera.transform.forward;
            Vector3 camRight   = _camera.transform.right;
            camForward.y = 0f; camForward.Normalize();
            camRight.y   = 0f; camRight.Normalize();

            Vector3 moveDir = camForward * _moveInput.y + camRight * _moveInput.x;
            _rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, _rb.linearVelocity.y, moveDir.z * moveSpeed);
        }

        protected void HandleShooting()
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

        protected void FireShot(float charge01)
        {
            if (_projectilePrefab == null || _cannon == null)
            {
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

    }
}
