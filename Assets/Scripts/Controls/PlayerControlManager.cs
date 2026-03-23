using System;
using System.Collections.Generic;
using Controls.Device;
using Controls.InputBinding;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Controls
{
    public class PlayerControlManager
    {
        private readonly InputBinder _keybinds;
        private InputDevice[] _boundDevices;
        private readonly Dictionary<MappableAction, IBindableInput[]> _deviceBindings;

        ~PlayerControlManager()
        {
            foreach (var binds in _deviceBindings.Values)
            {
                foreach (var bind in binds)
                {
                    bind.Dispose();
                }
            }
            DeviceManager.Instance.OnDeviceChangeProcessed -= OnDeviceChange;
        }
        public PlayerControlManager(string playerName)
        {
            
            SetupDefaultDevice();
            _keybinds = new InputBinder();
            _deviceBindings = new Dictionary<MappableAction, IBindableInput[]>();
            UpdateKeybinds();
            DeviceManager.Instance.OnDeviceChangeProcessed += OnDeviceChange;
        }

        private void SetupDefaultDevice()
        {
            var freeOrFirst = DeviceManager.Instance.GetFreeOrFirstDevice();
            if(freeOrFirst == null)
                _boundDevices = Array.Empty<InputDevice>();
            else
            {
                _boundDevices = new InputDevice[1] { freeOrFirst };
                DeviceManager.Instance.Register(this, freeOrFirst);
            }
        }
        
        private void OnDeviceChange(HashSet<DeviceSelector> impactedSelectors)
        {
            if (!impactedSelectors.Contains(_deviceSelector)) return;
            UpdateKeybinds();
        }

        private void UpdateKeybinds()
        {
            Debug.LogWarning("Updating keybinds");
            foreach (var kvp in _deviceBindings)
            {
                foreach (var bind in kvp.Value)
                {
                    bind.Dispose();
                }
            }
            _deviceBindings.Clear();
            var newBinds = _keybinds.CreateDeviceBind(_deviceSelector.BoundDevices);
            foreach (var bind in newBinds)
            {
                _deviceBindings[bind.Key] = bind.Value.ToArray();
            }
        }

        public InputState GetActionState(MappableAction action)
        {
            var best = InputState.Idle;
            if (!_deviceBindings.TryGetValue(action, out var binding)) return best;
            foreach (var bind in binding)
            {
                if (bind.GetState() == InputState.Held) return InputState.Held;
                if (bind.GetState() == InputState.Pressed) best =  InputState.Pressed;
                if (bind.GetState() == InputState.Released && best != InputState.Pressed) best =  InputState.Released;
            }
            return best;
        }
        public Vector2 GetActionValue(MappableAction action)
        {
            var best = Vector2.zero;
            if (!_deviceBindings.TryGetValue(action, out var binding)) return best;
            foreach (var bind in _deviceBindings[action])
            {
                if (bind.GetAxisValue().sqrMagnitude > best.sqrMagnitude) best = bind.GetAxisValue();
            }
            return best;
        }

        public void ListenForDeviceChange()
        {
            DeviceManager.Instance.ListenForDeviceChange(_deviceSelector);
        }
        
        
    }
}
