using System;
using System.Linq;
using Controls;
using Controls.Device;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class Player : MonoBehaviour
    {
        DeviceSelector _deviceSelector;
        
        void Start()
        {
            _deviceSelector = new DeviceSelector("PlayerName");
            var dm = DeviceManager.Instance;
            dm.BindDevice(_deviceSelector, dm.GetAvailableDevices().First());
        }

        void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                var newdevice = DeviceManager.Instance.GetAvailableDevices().First();
                // DeviceManager.Instance.UnbindDeviceByDevice(_deviceSelector.BoundDevice);
                DeviceManager.Instance.BindDevice(_deviceSelector, newdevice);
                Debug.Log(_deviceSelector.BoundDevice.path);
            }
        }
    }
}
