using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class InputBinder
    {
        private Dictionary<PlayerBind, InputType> bindTypes = new Dictionary<PlayerBind, InputType>
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

        Dictionary<PlayerBind, List<string>> binds = new Dictionary<PlayerBind, List<string>>
        {
            { PlayerBind.KB_MoveLeft, new List<string> { "<Keyboard>/a", "<Keyboard>/leftArrow" } },
            { PlayerBind.KB_MoveRight, new List<string> { "<Keyboard>/d", "<Keyboard>/rightArrow" } },
            { PlayerBind.KB_MoveUp, new List<string> { "<Keyboard>/w", "<Keyboard>/upArrow" } },
            { PlayerBind.KB_MoveDown, new List<string> { "<Keyboard>/s", "<Keyboard>/downArrow" } },
            { PlayerBind.GP_Move, new List<string> { "<Gamepad>/leftStick" } },

            { PlayerBind.KB_AimLeft, new List<string> { "<Keyboard>/leftArrow" } },
            { PlayerBind.KB_AimRight, new List<string> { "<Keyboard>/rightArrow" } },
            { PlayerBind.KB_AimUp, new List<string> { "<Keyboard>/upArrow" } },
            { PlayerBind.KB_AimDown, new List<string> { "<Keyboard>/downArrow" } },
            { PlayerBind.GP_Aim, new List<string> { "<Gamepad>/rightStick" } },

            { PlayerBind.KB_Shoot, new List<string> { "<Keyboard>/space" } },
            { PlayerBind.GP_Shoot, new List<string> { "<Gamepad>/rightTrigger", "<Gamepad>/buttonSouth" } },

            { PlayerBind.KB_Pause, new List<string> { "<Keyboard>/escape" } },
            { PlayerBind.GP_Pause, new List<string> { "<Gamepad>/start" } },
        };

        public void ChangeBind(PlayerBind bind, List<string> values)
        {
            InputType expectedType = bindTypes[bind];

            foreach (string value in values)
            {
                InputType valueType = value.Contains("Stick") ? InputType.Stick : InputType.Button;

                if (valueType != expectedType)
                {
                    Debug.LogWarning($"Bind {bind} expects {expectedType} but got {valueType} for path {value}");
                    return;
                }
            }

            binds[bind] = values;
        }

        public Dictionary<MappableAction, List<IBindableInput>> CreateDeviceBind(params InputDevice[] devices)
        {
            var devicesMap = new Dictionary<MappableAction, List<IBindableInput>>();

            void AppendOrCreate(MappableAction action, List<IBindableInput> inputs)
            {
                if (!devicesMap.TryAdd(action, inputs))
                    devicesMap[action].AddRange(inputs);
            }
            foreach (var inputDevice in devices)
            {
                if (inputDevice is Keyboard keyboard)
                {
                    // Move
                    var moveLefts = new List<ButtonControl>();
                    var moveRights = new List<ButtonControl>();
                    var moveUps = new List<ButtonControl>();
                    var moveDowns = new List<ButtonControl>();
                    foreach (string path in binds[PlayerBind.KB_MoveLeft])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            moveLefts.Add(c);
                    foreach (string path in binds[PlayerBind.KB_MoveRight])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            moveRights.Add(c);
                    foreach (string path in binds[PlayerBind.KB_MoveUp])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            moveUps.Add(c);
                    foreach (string path in binds[PlayerBind.KB_MoveDown])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            moveDowns.Add(c);
                    if (moveLefts.Count > 0 && moveRights.Count > 0 && moveUps.Count > 0 && moveDowns.Count > 0)
                        devicesMap.TryAdd(MappableAction.Move, new List<IBindableInput>())
                            .Add(new CompositeStickInput(moveLefts, moveRights, moveUps, moveDowns));

                    // Aim
                    var aimLefts = new List<ButtonControl>();
                    var aimRights = new List<ButtonControl>();
                    var aimUps = new List<ButtonControl>();
                    var aimDowns = new List<ButtonControl>();
                    foreach (string path in binds[PlayerBind.KB_AimLeft])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            aimLefts.Add(c);
                    foreach (string path in binds[PlayerBind.KB_AimRight])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            aimRights.Add(c);
                    foreach (string path in binds[PlayerBind.KB_AimUp])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            aimUps.Add(c);
                    foreach (string path in binds[PlayerBind.KB_AimDown])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            aimDowns.Add(c);
                    if (aimLefts.Count > 0 && aimRights.Count > 0 && aimUps.Count > 0 && aimDowns.Count > 0)
                        devicesMap.TryAdd(MappableAction.Aim, new List<IBindableInput>())
                            .Add(new CompositeStickInput(aimLefts, aimRights, aimUps, aimDowns));

                    // Shoot
                    foreach (string path in binds[PlayerBind.KB_Shoot])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            devicesMap.TryAdd(MappableAction.Shoot, new List<IBindableInput>()).Add(new ButtonInput(c));

                    // Pause
                    foreach (string path in binds[PlayerBind.KB_Pause])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            devicesMap.TryAdd(MappableAction.Pause, new List<IBindableInput>()).Add(new ButtonInput(c));
                }
                else if (inputDevice is Gamepad gamepad)
                {
                    // Move
                    foreach (string path in binds[PlayerBind.GP_Move])
                        if (gamepad.TryGetChildControl(path) is StickControl c)
                            devicesMap.TryAdd(MappableAction.Move, new List<IBindableInput>()).Add(new StickInput(c));

                    // Aim
                    foreach (string path in binds[PlayerBind.GP_Aim])
                        if (gamepad.TryGetChildControl(path) is StickControl c)
                            devicesMap.TryAdd(MappableAction.Aim, new List<IBindableInput>()).Add(new StickInput(c));

                    // Shoot
                    foreach (string path in binds[PlayerBind.GP_Shoot])
                        if (gamepad.TryGetChildControl(path) is ButtonControl c)
                            devicesMap.TryAdd(MappableAction.Shoot, new List<IBindableInput>()).Add(new ButtonInput(c));

                    // Pause
                    foreach (string path in binds[PlayerBind.GP_Pause])
                        if (gamepad.TryGetChildControl(path) is ButtonControl c)
                            devicesMap.TryAdd(MappableAction.Pause, new List<IBindableInput>()).Add(new ButtonInput(c));
                }
            }

            return devicesMap;
        }

        private IBindableInput GetMoveBindingsKb(Keyboard kb)
        {
            var moveLefts = new List<ButtonControl>();
            var moveRights = new List<ButtonControl>();
            var moveUps = new List<ButtonControl>();
            var moveDowns = new List<ButtonControl>();
            
            foreach (string path in binds[PlayerBind.KB_MoveLeft])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    moveLefts.Add(c);
            foreach (string path in binds[PlayerBind.KB_MoveRight])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    moveRights.Add(c);
            foreach (string path in binds[PlayerBind.KB_MoveUp])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    moveUps.Add(c);
            foreach (string path in binds[PlayerBind.KB_MoveDown])
                if (kb.TryGetChildControl(path) is ButtonControl c)
                    moveDowns.Add(c);
            
            if (moveLefts.Count > 0 && moveRights.Count > 0 && moveUps.Count > 0 && moveDowns.Count > 0)
                return new BindableStick(moveLefts, moveRights, moveUps, moveDowns);
        }
    }

    internal enum InputType
    {
        Button,
        Stick
    }
}