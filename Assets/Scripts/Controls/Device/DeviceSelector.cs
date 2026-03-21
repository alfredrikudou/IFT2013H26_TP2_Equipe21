using System.Linq;
using UnityEngine.InputSystem;

namespace Controls.Device
{
    public class DeviceSelector
    {
        private readonly string _selectorName;

        public DeviceSelector(string ownerName = "")
        {
            _selectorName = ownerName;
        }
        public InputDevice BoundDevice { get; private set; }
        public bool IsBound => BoundDevice != null;
        public void BindDevice(InputDevice device)
        {
            BoundDevice = device;
        }

        public void UnBindDevice()
        {
            BoundDevice = null;
        }

        public string GetSelectorName()
        {
            return _selectorName;
        }
    }
}
