using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Controls.Device
{
    // public class DeviceSelector
    // {
    //     private readonly string _selectorName;
    //
    //     public DeviceSelector(string ownerName = "")
    //     {
    //         _selectorName = ownerName;
    //         var dm = DeviceManager.Instance;
    //         var freeDevice = dm.GetAllDevices().Find(x => !dm.IsDeviceBound(x));
    //         if(freeDevice != null)
    //             dm.BindDevice(this, freeDevice);
    //         else if(dm.GetAllDevices().Any())
    //             dm.BindDevice(this, dm.GetAllDevices().First());
    //     }
    //     public InputDevice[] BoundDevices { get; private set; }
    //     public bool IsBound => BoundDevices is { Length: > 0 };
    //     public void BindDevice(InputDevice device)
    //     {
    //         BoundDevices ??= new InputDevice[] { };
    //         var boundDevicesList = BoundDevices.ToList();
    //         if (boundDevicesList.Contains(device)) return;
    //         boundDevicesList.Add(device);
    //         BoundDevices = boundDevicesList.ToArray();
    //     }
    //
    //     public void UnBindDevice(InputDevice device)
    //     {
    //         if (BoundDevices == null) return;
    //         var boundDevicesList = BoundDevices.ToList();
    //         boundDevicesList.Remove(device);
    //         BoundDevices = boundDevicesList.ToArray();
    //     }
    //
    //     public string GetSelectorName()
    //     {
    //         return _selectorName;
    //     }
    // }
}
