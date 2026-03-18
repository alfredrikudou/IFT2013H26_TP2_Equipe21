using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace Controls
{
    public class DeviceManager
    {
        private static Dictionary<IDeviceSelector, InputDevice> mappedDevices { get; set; } = new();

        public static List<InputDevice> GetAllDevices() => InputSystem.devices.Where(x => x is Keyboard || x is Gamepad || x is Joystick) as List<InputDevice>;
    
        public static bool IsDeviceAvailable(InputDevice device) => !mappedDevices.ContainsValue(device);
    
        public static InputDevice GetDevice(IDeviceSelector ds) =>  mappedDevices.ContainsKey(ds) ? mappedDevices[ds] : null;

        public static void UnbindDeviceByDevice(InputDevice device)
        {
            if (!mappedDevices.ContainsValue(device)) return;

            var ds = mappedDevices.First(x => x.Value == device).Key;
            ds.UnBindDevice();
            mappedDevices.Remove(ds);
        }

        public static void BindDevice(IDeviceSelector ds, InputDevice device)
        {
            ds.UnBindDevice();
            UnbindDeviceByDevice(device);
            mappedDevices[ds] = device;
        }
    }
}
