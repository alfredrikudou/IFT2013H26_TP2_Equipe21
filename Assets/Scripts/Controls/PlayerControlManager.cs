using System;
using System.Collections.Generic;
using Controls.Device;
using Controls.InputBinding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

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
        }
        public PlayerControlManager()
        {
            
            SetupDefaultDevice();
            _keybinds = new InputBinder();
            _deviceBindings = new Dictionary<MappableAction, IBindableInput[]>();
            UpdateKeybinds();
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
            var newBinds = _keybinds.CreateDeviceBind(_boundDevices);
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
            InputSystem.onAnyButtonPress.CallOnce(DeviceChange);
        }
        private void DeviceChange(InputControl control)
        {
            if (!DeviceManager.Instance.IsDeviceSupported(control.device))
            {
                InputSystem.onAnyButtonPress.CallOnce(DeviceChange);
                return;
            }
            if (Array.IndexOf(_boundDevices, control.device) == -1)
            {
                foreach (var boundDevice in _boundDevices)
                {
                    DeviceManager.Instance.Unregister(this,  boundDevice);
                }
                _boundDevices = Array.Empty<InputDevice>();
                DeviceManager.Instance.Register(this, control.device);
                AddDevice(control.device);
            }
        }

        public void RemoveDevice(InputDevice device)
        {
            if (Array.IndexOf(_boundDevices, device) == -1) return; // Not there to begin with
            var updatedDevices = new InputDevice[_boundDevices.Length - 1];
            for (int i = 0, j = 0; i < _boundDevices.Length; i++)
                if(_boundDevices[i] != device)
                    updatedDevices[j++] = _boundDevices[i];
            _boundDevices = updatedDevices;
            UpdateKeybinds();
        }
        
        public void AddDevice(InputDevice device)
        {
            if (Array.IndexOf(_boundDevices, device) != -1) return; // Already there
            var updatedDevices = new InputDevice[_boundDevices.Length + 1];
            for (int i = 0; i < _boundDevices.Length; i++)
                updatedDevices[i] = _boundDevices[i];
            updatedDevices[^1] = device;
            _boundDevices = updatedDevices;
            UpdateKeybinds();
        }
        
        
    }
}
