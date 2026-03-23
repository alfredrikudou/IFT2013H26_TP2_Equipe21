using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class InputBinder
    {
        private readonly Dictionary<PlayerBind, InputType> _bindTypes = new Dictionary<PlayerBind, InputType>
        {
            { PlayerBind.KB_MoveLeft, InputType.Button },
            { PlayerBind.KB_MoveRight, InputType.Button },
            { PlayerBind.KB_MoveUp, InputType.Button },
            { PlayerBind.KB_MoveDown, InputType.Button },
            { PlayerBind.GP_Move, InputType.Stick },

            { PlayerBind.KB_AimLeft, InputType.Button },
            { PlayerBind.KB_AimRight, InputType.Button },
            { PlayerBind.KB_AimUp, InputType.Button },
            { PlayerBind.KB_AimDown, InputType.Button },
            { PlayerBind.GP_Aim, InputType.Stick },

            { PlayerBind.KB_Shoot, InputType.Button },
            { PlayerBind.GP_Shoot, InputType.Button },

            { PlayerBind.KB_Pause, InputType.Button },
            { PlayerBind.GP_Pause, InputType.Button },
        };

        private readonly Dictionary<PlayerBind, List<string>> _binds = new Dictionary<PlayerBind, List<string>>
        {
            { PlayerBind.KB_MoveLeft,  new List<string> { "a", "leftArrow" } },
            { PlayerBind.KB_MoveRight, new List<string> { "d", "rightArrow" } },
            { PlayerBind.KB_MoveUp,    new List<string> { "w", "upArrow" } },
            { PlayerBind.KB_MoveDown,  new List<string> { "s", "downArrow" } },
            { PlayerBind.GP_Move,      new List<string> { "leftStick" } },

            { PlayerBind.KB_AimLeft,   new List<string> { "leftArrow" } },
            { PlayerBind.KB_AimRight,  new List<string> { "rightArrow" } },
            { PlayerBind.KB_AimUp,     new List<string> { "upArrow" } },
            { PlayerBind.KB_AimDown,   new List<string> { "downArrow" } },
            { PlayerBind.GP_Aim,       new List<string> { "rightStick" } },

            { PlayerBind.KB_Shoot,     new List<string> { "space" } },
            { PlayerBind.GP_Shoot,     new List<string> { "rightTrigger", "buttonSouth" } },

            { PlayerBind.KB_Pause,     new List<string> { "escape" } },
            { PlayerBind.GP_Pause,     new List<string> { "start" } },
        };

        public void ChangeBind(PlayerBind bind, List<string> values)
        {
            InputType expectedType = _bindTypes[bind];

            foreach (string value in values)
            {
                InputType valueType = value.Contains("Stick") ? InputType.Stick : InputType.Button;

                if (valueType != expectedType)
                {
                    Debug.LogWarning($"Bind {bind} expects {expectedType} but got {valueType} for path {value}");
                    return;
                }
            }

            _binds[bind] = values;
        }

        public Dictionary<MappableAction, List<IBindableInput>> CreateDeviceBind(params InputDevice[] devices)
        {
            var devicesMap = new Dictionary<MappableAction, List<IBindableInput>>();

            void AppendOrCreate(MappableAction action, IBindableInput input)
            {
                if (!devicesMap.ContainsKey(action))
                    devicesMap[action] = new List<IBindableInput>{input};
                else
                    devicesMap[action].Add(input);
            }

            foreach (var inputDevice in devices)
            {
                if (inputDevice is Keyboard keyboard)
                {
                    // Move
                    var moveStick = GetStickBindingsKb(keyboard, PlayerBind.KB_MoveLeft, PlayerBind.KB_MoveRight,
                        PlayerBind.KB_MoveDown, PlayerBind.KB_MoveUp);
                    if(moveStick != null)
                        AppendOrCreate(MappableAction.Move, moveStick);

                    // Aim
                    var aimStick = GetStickBindingsKb(keyboard, PlayerBind.KB_AimLeft, PlayerBind.KB_AimRight,
                        PlayerBind.KB_AimDown, PlayerBind.KB_AimUp);
                    if(aimStick != null)
                        AppendOrCreate(MappableAction.Aim, moveStick);
                    
                    // Shoot
                    foreach (string path in _binds[PlayerBind.KB_Shoot])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Shoot, new BindableButton(c));
                    
                    // Pause
                    foreach (string path in _binds[PlayerBind.KB_Pause])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Pause, new BindableButton(c));
                }
                else if (inputDevice is Gamepad gamepad)
                {
                    // Move
                    foreach (string path in _binds[PlayerBind.GP_Move])
                        if (gamepad.TryGetChildControl(path) is StickControl c)
                            AppendOrCreate(MappableAction.Move, new BindableStick(new UnityStick(c)));

                    // Aim
                    foreach (string path in _binds[PlayerBind.GP_Aim])
                        if (gamepad.TryGetChildControl(path) is StickControl c)
                            AppendOrCreate(MappableAction.Aim, new BindableStick(new UnityStick(c)));

                    // Shoot
                    foreach (string path in _binds[PlayerBind.GP_Shoot])
                        if (gamepad.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Shoot, new BindableButton(c));

                    // Pause
                    foreach (string path in _binds[PlayerBind.GP_Pause])
                        if (gamepad.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Pause, new BindableButton(c));
                }
            }

            return devicesMap;
        }

        private IBindableInput GetStickBindingsKb(Keyboard kb, PlayerBind negativeX, PlayerBind positiveX,
            PlayerBind negativeY, PlayerBind positiveY)
        {
            var negativeXs = new List<IBindableInput>();
            var positiveXs = new List<IBindableInput>();
            var negativeYs = new List<IBindableInput>();
            var positiveYs = new List<IBindableInput>();

            foreach (string path in _binds[negativeX])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    negativeXs.Add(new BindableButton(c));
            foreach (string path in _binds[positiveX])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    positiveXs.Add(new BindableButton(c));
            foreach (string path in _binds[negativeY])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    negativeYs.Add(new BindableButton(c));
            foreach (string path in _binds[positiveY])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    positiveYs.Add(new BindableButton(c));

            if (negativeXs.Count > 0 && positiveXs.Count > 0 && negativeYs.Count > 0 && positiveYs.Count > 0)
                return new BindableStick(
                    new StickEmulator(
                        new AxisEmulator(positiveXs.ToArray(), negativeXs.ToArray()),
                        new AxisEmulator(positiveYs.ToArray(), negativeYs.ToArray())));
            return null;
        }
    }

    internal enum InputType
    {
        Button,
        Stick
    }
}