using Controls;
using Controls.InputBinding;
using UnityEngine;

namespace Agents
{
    public class Player : Agent
    {
        protected static int _playerCount = 0;
        protected PlayerControlManager _pcm;

        [SerializeField] private Transform _cameraPivot;


        public void UpdateControl(PlayerControlDto dto) => _pcm.UpdateControl(dto);

        public PlayerControlDto GetProfileDTO() => new PlayerControlDto
        {
            Name = _name,
            BindMap = _pcm.GetBindMapSerialize(),
            Devices = _pcm.GetDevicesSerialize()
        };

        protected override void Awake()
        {
            base.Awake();
            _currentHealth = _maxHealth;
            SetName($"Player{_playerCount++}");
            _pcm = new PlayerControlManager();
        }

        private void OnDestroy() => _pcm.Dispose();

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
            {
                _moveInput = Vector2.zero;
                return;
            }

            if (_pcm.GetActionState(MappableAction.Pause) == InputState.Pressed)
                FindFirstObjectByType<GamePauseController>()?.Pause();

            if (GamePauseState.IsPaused)
            {
                _moveInput = Vector2.zero;
                return;
            }

            _moveInput = _pcm.GetActionValue(MappableAction.Move);
            var aim = _pcm.GetActionValue(MappableAction.Aim);

            ApplyAim(aim.x * _aimSpeed * Time.deltaTime, aim.y * _aimSpeed * Time.deltaTime);

            if (_cameraPivot != null)
                _cameraPivot.localRotation = Quaternion.Euler(0f, _aimYaw, 0f);

            HandleShooting();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        private void HandleShooting()
        {
            var state = _pcm.GetActionState(MappableAction.Shoot);

            if (state == InputState.Pressed || state == InputState.Held)
                AddCharge(Time.deltaTime);

            if (Charging && state == InputState.Released)
            {
                FireShot(Charge01);
                ResetCharge();
            }
        }
    }
}