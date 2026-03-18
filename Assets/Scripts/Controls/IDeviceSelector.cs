using UnityEngine.InputSystem;

namespace Controls
{
    public interface IDeviceSelector
    {
        public void BindDevice(InputDevice device);
        public void UnBindDevice();
        public string GetSelectorName();
    }
}
