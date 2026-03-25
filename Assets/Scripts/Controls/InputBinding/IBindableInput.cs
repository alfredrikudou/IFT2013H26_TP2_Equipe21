using UnityEngine;

namespace Controls.InputBinding
{
    public interface IBindableInput
    {
        public InputState GetState();
        public Vector2 GetAxisValue();
        public void Dispose();
    }
}