using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Controls
{
    public class DeviceManager
    {
        private static DeviceManager _instance;
        private static readonly Type[] SupportedDeviceTypes = { typeof(Keyboard), typeof(Gamepad) };
        private Dictionary<InputDevice, HashSet<PlayerControlManager>> MappedDevices { get; set; } = new();
        private Dictionary<InputDevice, HashSet<PlayerControlManager>> DisconnectedDevices { get; set; } = new();

        public bool IsDeviceSupported(InputDevice device) => SupportedDeviceTypes.Any(t => t.IsAssignableFrom(device.GetType()));

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


        public List<InputDevice> GetAllDevices() =>
            InputSystem.devices.Where(x => x is Keyboard or Gamepad).ToList();
        
        public bool IsDeviceBound(InputDevice device) => MappedDevices.ContainsKey(device);

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
            if (!MappedDevices.TryAdd(device, new HashSet<PlayerControlManager>() { control }))
                MappedDevices[device].Add(control);
        }
        
        public void Unregister(PlayerControlManager control, InputDevice device)
        {
            if (!MappedDevices.ContainsKey(device)) return;
            if (!MappedDevices[device].Remove(control)) return;
            if (MappedDevices[device].Count == 0) MappedDevices.Remove(device);
        }
        
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Removed:
                    if(!MappedDevices.ContainsKey(device)) return;
                    DisconnectedDevices[device] = new HashSet<PlayerControlManager>(MappedDevices[device]);
                    foreach (var controlManager in MappedDevices[device].ToArray())
                    {
                        controlManager.RemoveDevice(device);
                        Unregister(controlManager, device);
                    }
                    break;
                case InputDeviceChange.Reconnected:
                    if(!DisconnectedDevices.ContainsKey(device)) return;
                    foreach (var controlManager in DisconnectedDevices[device])
                    {
                        Register(controlManager, device);
                        controlManager.AddDevice(device);
                    }
                    DisconnectedDevices.Remove(device);
                    break;
            }
        }

    }
}