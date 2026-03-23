using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class BindableStick : IBindableInput
    {
        private readonly IStick _stick;

        public BindableStick(IStick stick)
        {
            _stick = stick;
            if (_stick is StickEmulator emulator)
                InputEmulatorManager.Instance.Register(emulator);
        }

        public InputState GetState()
        {
            return GetAxisValue().Equals(Vector2.zero) ? InputState.Idle : InputState.Held;
        }

        public Vector2 GetAxisValue()
        {
            return _stick.ReadValue();
        }

        public void Dispose()
        {
            if (_stick is StickEmulator emulator)
                InputEmulatorManager.Instance.Unregister(emulator);
        }
    }
}