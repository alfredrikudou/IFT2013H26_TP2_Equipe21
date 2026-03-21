using UnityEngine;

namespace Controls.InputBinding
{
    // Interface to physical hardware that an action can be bind to
    public interface IBindableInput : ICustomSerializable
    {
        InputState GetState();
        Vector2 GetAxisValue();
    }
}