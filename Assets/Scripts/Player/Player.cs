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

        [Header("Movement")] [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Transform _cannon;
        [SerializeField] private float _aimSpeed = 90f;
        [SerializeField] private float _pitchMin = -30f;
        [SerializeField] private float _pitchMax = 60f;
        private float _aimYaw;
        private float _aimPitch;

        [Header("Shooting")]
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Rigidbody _projectilePrefab;
        [SerializeField] private float _muzzleDistance = 0.8f;
        [SerializeField] private float _minShotSpeed = 6f;
        [SerializeField] private float _maxShotSpeed = 18f;
        [SerializeField] private float _chargeSecondsToMax = 1.2f;

        [Header("UI (World Space)")]
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Vector3 _sliderOffset = new Vector3(0f, -0.6f, 0f);
 
        private Rigidbody _rb;
        private Vector2 _moveInput = Vector2.zero;
        private bool _isMyTurn = true;

        private float _charge01 = 0f;
        private bool _charging = false;
        
        public string GetName() => _playerName;

        public void SetTurnActive(bool isActive)
        {
            _isMyTurn = isActive;
            if (!isActive)
            {
                _moveInput = Vector2.zero;
                _charging = false;
                _charge01 = 0f;
                UpdatePowerUI();
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
        }

        void Start()
        {
            _pcm = new PlayerControlManager();
            _playerName = $"Player{_playerCount++}";

            EnsureFirePoint();
            EnsurePowerSlider();
        }

        private void Update()
        {
            if (!_isMyTurn || (TurnManager.Instance != null && TurnManager.Instance.IsWaitingForProjectile()))
            {
                _moveInput = Vector2.zero;
                return;
            }

            _moveInput = _pcm.GetActionValue(MappableAction.Move);
            var aim = _pcm.GetActionValue(MappableAction.Aim);
            
            _aimYaw += aim.x * _aimSpeed * Time.deltaTime;
            _aimPitch -= aim.y * _aimSpeed * Time.deltaTime;
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);
    
            _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);

            HandleShooting();
            
            
            if(_moveInput != Vector2.zero)
                Debug.Log("move: " + _moveInput +  " " + _moveInput.magnitude + " " + _playerName);
            if(aim != Vector2.zero)
                Debug.Log("aim: " + aim +  " " + aim.magnitude + " " + _playerName);
            if(_pcm.GetActionState(MappableAction.Pause) != InputState.Idle)
                Debug.Log("pause " + _playerName);
        }

        private void LateUpdate()
        {
            if (_powerSlider == null) return;

            var canvas = _powerSlider.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.transform.position = transform.position + _sliderOffset;
                if (Camera.main != null)
                    canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - Camera.main.transform.position);
            }
        }

        private void FixedUpdate()
        {
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
            if (_projectilePrefab == null || _cannon == null) return;
            EnsureFirePoint();
            if (_firePoint == null) return;

            float speed = Mathf.Lerp(_minShotSpeed, _maxShotSpeed, Mathf.Clamp01(charge01));
            Rigidbody proj = Instantiate(_projectilePrefab, _firePoint.position, _firePoint.rotation);

            // Ensure it has a Projectile behaviour so TurnManager can track impact.
            var tracker = proj.GetComponent<Projectile>();
            if (tracker == null) tracker = proj.gameObject.AddComponent<Projectile>();

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