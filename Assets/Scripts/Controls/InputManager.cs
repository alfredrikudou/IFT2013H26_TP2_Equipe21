using System.Collections.Generic;
using Controls.Device;
using UnityEngine;

namespace Controls
{
    public class InputManager: MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        private Dictionary<DeviceSelector, ActionMapper> _playerMappings = new();
    
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public void RegisterPlayer(DeviceSelector selector, ActionMapper mapper)
        {
            _playerMappings[selector] = mapper;
        }

        public void UnregisterPlayer(DeviceSelector selector)
        {
            _playerMappings.Remove(selector);
        }
    
        public bool GetActionDown(MappableAction actionName)
        {
            return true;
        }
    }
}
