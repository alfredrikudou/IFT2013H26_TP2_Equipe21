using System.Collections.Generic;
using Controls.InputBinding;
using UnityEngine;

namespace Controls
{
    public class ActionMapper
    {
        private Dictionary<MappableAction, IBindableInput> bindings = new();
        public InputState GetActionState(MappableAction action) => bindings[action].GetState();

        public Vector2 GetAxisValue(MappableAction action) => bindings[action].GetAxisValue();

        public void Bind(MappableAction action, IBindableInput @new)
        {
            bindings[action] = @new;
        }

        public void Unbind(MappableAction action)
        {
            bindings.Remove(action);
        }
    }
}
