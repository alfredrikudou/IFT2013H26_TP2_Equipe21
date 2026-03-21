using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class BindableButton : IBindableInput
    {
        private readonly ButtonControl[] _buttons;

        public BindableButton(params ButtonControl[] buttons) => _buttons = buttons;

        public InputState GetState()
        {
            var best = InputState.Idle;
            foreach (var b in _buttons)
            {
                if (b == null) continue;
                if (b.wasPressedThisFrame) return InputState.Pressed;
                if (b.wasReleasedThisFrame) best = InputState.Released;
                if (b.isPressed && best == InputState.Idle) best = InputState.Held;
            }

            return best;
        }

        public Vector2 GetAxisValue() => Vector2.zero;
        
        public string Serialize()
        {
            var paths = System.Array.ConvertAll(_buttons, b => b?.path ?? "");
            return "ButtonBinding:" + string.Join(",", paths);
        }
    }
}