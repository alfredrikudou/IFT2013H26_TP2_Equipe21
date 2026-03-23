using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class BindableButton : IBindableInput
    {
        private readonly ButtonControl _button;

        public BindableButton(ButtonControl button) => _button = button;

        public InputState GetState()
        {
            if (_button == null) return InputState.Idle;
            if (_button.wasPressedThisFrame) return InputState.Pressed;
            if (_button.wasReleasedThisFrame) return InputState.Released;
            if (_button.isPressed) return InputState.Held;
            return InputState.Idle;
        }

        public Vector2 GetAxisValue() => Vector2.zero;
        public void Dispose()
        {
            
        }
    }
}