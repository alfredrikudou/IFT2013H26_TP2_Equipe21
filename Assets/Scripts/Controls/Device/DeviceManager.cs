using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace Controls.Device
{
    public class DeviceManager
    {
        private static DeviceManager _instance;

        private DeviceManager() { }

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
        private Dictionary<DeviceSelector, InputDevice> MappedDevices { get; set; } = new();

        public List<InputDevice> GetAllDevices() => InputSystem.devices.Where(x => x is Keyboard || x is Gamepad).ToList();
        public List<InputDevice> GetAvailableDevices() => GetAllDevices().Where(IsDeviceAvailable).ToList();
    
        public bool IsDeviceAvailable(InputDevice device) => !MappedDevices.ContainsValue(device);
    
        public InputDevice GetDeviceBySelector(DeviceSelector ds) =>  MappedDevices.ContainsKey(ds) ? MappedDevices[ds] : null;

        public void UnbindDeviceByDevice(InputDevice device)
        {
            if (!MappedDevices.ContainsValue(device)) return;

            var ds = MappedDevices.First(x => x.Value == device).Key;
            ds.UnBindDevice();
            MappedDevices.Remove(ds);
        }

        public void BindDevice(DeviceSelector ds, InputDevice device)
        {
            ds.UnBindDevice();
            UnbindDeviceByDevice(device);
            ds.BindDevice(device);
            MappedDevices[ds] = device;
        }
    }
}
