using System;
using System.Collections.Generic;
using System.Linq;
using Controls.InputBinding;
using Player;
using Unity.VisualScripting;
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
            if (freeOrFirst == null && Keyboard.current != null)
                freeOrFirst = Keyboard.current;

            if (freeOrFirst == null)
                _boundDevices = Array.Empty<InputDevice>();
            else
            {
                _boundDevices = new InputDevice[1] { freeOrFirst };
                DeviceManager.Instance.Register(this, freeOrFirst);
            }
        }

        private void UpdateKeybinds()
        {
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
            var sum = Vector2.zero;
            if (!_deviceBindings.TryGetValue(action, out var binding)) return sum;
            foreach (var bind in _deviceBindings[action])
            {
                sum += bind.GetAxisValue();
            }
            return sum;
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

        public void RemoveDevice(InputDevice device, bool updateKeybinds = true)
        {
            if (Array.IndexOf(_boundDevices, device) == -1) return; // Not there to begin with
            var updatedDevices = new InputDevice[_boundDevices.Length - 1];
            for (int i = 0, j = 0; i < _boundDevices.Length; i++)
                if(_boundDevices[i] != device)
                    updatedDevices[j++] = _boundDevices[i];
            _boundDevices = updatedDevices;
            if(updateKeybinds) UpdateKeybinds();
        }
        
        public void AddDevice(InputDevice device, bool updateKeybinds = true)
        {
            if (Array.IndexOf(_boundDevices, device) != -1) return; // Already there
            var updatedDevices = new InputDevice[_boundDevices.Length + 1];
            for (int i = 0; i < _boundDevices.Length; i++)
                updatedDevices[i] = _boundDevices[i];
            updatedDevices[^1] = device;
            _boundDevices = updatedDevices;
            if(updateKeybinds) UpdateKeybinds();
        }

        public string GetBindMapSerialize() => _keybinds.Serialize();
        public string[] GetDevicesSerialize() => 
            System.Array.ConvertAll(_boundDevices, d => 
                d.displayName + ":" + d.deviceId
                );

        public void UpdateControl(PlayerControlDTO dto)
        {
            foreach (var boundDevice in _boundDevices.ToArray())
            {
                DeviceManager.Instance.Unregister(this, boundDevice);
                RemoveDevice(boundDevice, false);
            }
            var allDevices = DeviceManager.Instance.GetAllDevices();
            foreach (var dtoDevice in dto.Devices)
            {
                var device = dtoDevice.Split(':');
                foreach (var inputDevice in allDevices)
                {
                    if (device[0] == inputDevice.displayName && device[1] == inputDevice.deviceId.ToString())
                    {
                        DeviceManager.Instance.Register(this, inputDevice);
                        AddDevice(inputDevice, false);
                    }
                }

            }
            _keybinds.UpdateBind(dto.BindMap);

            UpdateKeybinds();

        }
            
    }
}
