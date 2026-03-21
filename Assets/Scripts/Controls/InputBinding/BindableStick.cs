using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class BindableStick : IBindableInput
    {
        private readonly IStick _stick;

        public BindableStick(IStick stick) => _stick = stick;

        public InputState GetState()
        {
            return GetAxisValue().Equals(Vector2.zero) ? InputState.Idle : InputState.Held;
        }

        public Vector2 GetAxisValue()
        {
            return _stick.ReadValue();
        }
        
        public string Serialize()
        {
            var parts = new string[_sticks.Length];
            for (int i = 0; i < _sticks.Length; i++)
            {
                parts[i] = _sticks[i].Serialize();
            }
            return "StickBinding:" + string.Join(",", parts);
        }
    }
}