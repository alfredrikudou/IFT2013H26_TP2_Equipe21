using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Controls;

namespace Controls.InputBinding
{
    public static class BindableInputFactory
    {
        public static IBindableInput Deserialize(string raw)
        {
            if (raw.StartsWith("ButtonBinding:"))
                return DeserializeButton(raw.Substring("ButtonBinding:".Length));

            if (raw.StartsWith("StickBinding:"))
                return DeserializeStick(raw.Substring("StickBinding:".Length));

            throw new System.Exception($"[InputBindingFactory] Unknown binding: '{raw}'");
        }

        private static BindableButton DeserializeButton(string data)
        {
            var paths = data.Split(',');
            var controls = new ButtonControl[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                controls[i] = InputSystem.FindControl(paths[i]) as ButtonControl;
            return new BindableButton(controls);
        }

        private static BindableStick DeserializeStick(string data)
        {
            var parts = data.Split(',');
            var sticks = new IStick[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                sticks[i] = DeserializeSingleStick(parts[i]);
            return new BindableStick(sticks);
        }

        private static IStick DeserializeSingleStick(string part)
        {
            if (part.StartsWith("UnityStick:"))
                return DeserializeUnityStick(part.Substring("UnityStick:".Length));

            if (part.StartsWith("StickEmulator:"))
                return DeserializeStickEmulator(part.Substring("StickEmulator:".Length));

            throw new System.Exception($"[InputBindingFactory] Unknown stick type: '{part}'");
        }
        
        
        private static UnityStick DeserializeUnityStick(string data)
        {
            var control = InputSystem.FindControl(data) as StickControl;
            return new UnityStick(control);
        }


        private static StickEmulator DeserializeStickEmulator(string data)
        {
            var axes = data.Split(',');
            var x = DeserializeAxisEmulator(axes[0].Substring("AxisEmulator:".Length));
            var y = DeserializeAxisEmulator(axes[1].Substring("AxisEmulator:".Length));
            return new StickEmulator(x, y);
        }


        private static AxisEmulator DeserializeAxisEmulator(string data)
        {
            float sens = 0.1f, grav = 0.05f, dead = 0.1f;
            MappableAction plus = default, minus = default;

            foreach (var pair in data.Split(','))
            {
                var kv = pair.Split('=');
                switch (kv[0])
                {
                    case "Sensitivity": sens = float.Parse(kv[1]); break;
                    case "Gravity": grav = float.Parse(kv[1]); break;
                    case "DeadZone": dead = float.Parse(kv[1]); break;
                    case "Plus": plus = System.Enum.Parse<MappableAction>(kv[1]); break;
                    case "Minus": minus = System.Enum.Parse<MappableAction>(kv[1]); break;
                }
            }

            return new AxisEmulator(plus, minus, sens, grav, dead);
        }
    }
}