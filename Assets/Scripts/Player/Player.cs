using System;
using Controls;
using Controls.InputBinding;
using UnityEngine;

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
 
        private Rigidbody _rb;
        private Vector2 _moveInput = Vector2.zero;
        
        public string GetName() => _playerName;

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
        }

        private void Update()
        {
            _moveInput = _pcm.GetActionValue(MappableAction.Move);
            var aim = _pcm.GetActionValue(MappableAction.Aim);
            
            _aimYaw += aim.x * _aimSpeed * Time.deltaTime;
            _aimPitch -= aim.y * _aimSpeed * Time.deltaTime;
            _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);
    
            _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);
            
            
            if(_moveInput != Vector2.zero)
                Debug.Log("move: " + _moveInput +  " " + _moveInput.magnitude + " " + _playerName);
            if(aim != Vector2.zero)
                Debug.Log("aim: " + aim +  " " + aim.magnitude + " " + _playerName);
            if(_pcm.GetActionState(MappableAction.Shoot) != InputState.Idle)
                Debug.Log("shoot " + _playerName);
            if(_pcm.GetActionState(MappableAction.Pause) != InputState.Idle)
                Debug.Log("pause " + _playerName);
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = new Vector3(_moveInput.x * moveSpeed, _rb.linearVelocity.y, _moveInput.y * moveSpeed);
        }
    }
}