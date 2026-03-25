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
        }

        private void FixedUpdate()
        {
            _rb.linearVelocity = new Vector3(_moveInput.x * moveSpeed, _rb.linearVelocity.y, _moveInput.y * moveSpeed);
        }
    }
}