using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Controls.Device
{
    public class DeviceManager
    {
        private static DeviceManager _instance;
        private Dictionary<InputDevice, HashSet<DeviceSelector>> MappedDevices { get; set; } = new();
        public event Action<HashSet<DeviceSelector>> OnDeviceChangeProcessed; // impacted selectors

        private DeviceManager()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }
        ~DeviceManager()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        public static DeviceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DeviceManager();
                }

                return _instance;
            }
        }
        
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Keyboard or Gamepad)) return;
            Debug.LogWarning($"OnDeviceChange: {device.path} bound:{MappedDevices.ContainsKey(device)}");
            if (!MappedDevices.TryGetValue(device, out var impactedSelectors))
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Removed:
                    UnbindAllSelectorFromDevice(device);
                    break;
            }
            OnDeviceChangeProcessed?.Invoke(impactedSelectors);
        }


        public List<InputDevice> GetAllDevices() =>
            InputSystem.devices.Where(x => x is Keyboard or Gamepad).ToList();
        
        public bool IsDeviceBound(InputDevice device) => MappedDevices.ContainsKey(device);

        public void UnbindSelectorToDevice(DeviceSelector selector, InputDevice device)
        {
            selector.UnBindDevice(device);
            if (!MappedDevices.ContainsKey(device)) return;
            if (!MappedDevices[device].Remove(selector)) return;
            if (MappedDevices[device].Count == 0) MappedDevices.Remove(device);
        }

        public void UnbindAllSelectorFromDevice(InputDevice device)
        {
            if (!MappedDevices.ContainsKey(device)) return;
            foreach (var selector in MappedDevices[device].ToArray())
                UnbindSelectorToDevice(selector, device);
        }

        public void BindDevice(DeviceSelector ds, InputDevice device)
        {
            if (MappedDevices.ContainsKey(device))
                MappedDevices[device].Add(ds);
            else
                MappedDevices[device] = new HashSet<DeviceSelector>() { ds };
            ds.BindDevice(device);
            Debug.Log($"BindDevice: {device.path} bound:{MappedDevices.ContainsKey(device)}");
        }

        public InputDevice GetFreeOrFirstDevice()
        {
            var devices = InputSystem.devices.Where(x => x is Keyboard or Gamepad).ToList();
            foreach (var device in devices)
                if (!MappedDevices.ContainsKey(device))
                    return device;
            if(devices.Count >= 1) return devices.First();
            return null;
        }

        public void Register(PlayerControlManager control, InputDevice device)
        {
            
        }
        
        

        public void ListenForDeviceChange(DeviceSelector deviceSelector)
        {
            InputSystem.onAnyButtonPress.CallOnce(DeviceChange);
            void DeviceChange(InputControl control)
            {
                foreach (var deviceSelectorBoundDevice in deviceSelector.BoundDevices)
                {
                    UnbindSelectorToDevice(deviceSelector, deviceSelectorBoundDevice);
                }
                BindDevice(deviceSelector, control.device);
                OnDeviceChangeProcessed?.Invoke(new HashSet<DeviceSelector>(){deviceSelector});
            }
        }

        public void test()
        {
            foreach (var mappedDevice in MappedDevices)
            {
                Debug.Log(mappedDevice.Key.path);
            }
        }

    }
}