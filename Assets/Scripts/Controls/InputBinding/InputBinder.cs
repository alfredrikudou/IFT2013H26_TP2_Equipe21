using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Controls.InputBinding
{
    public class InputBinder
    {
        private readonly Dictionary<PlayerSettings.PlayerSetting, List<string>> _binds =
            new Dictionary<PlayerSettings.PlayerSetting, List<string>>
            {
                { PlayerSettings.PlayerSetting.KB_MoveLeft, new List<string> { "a", "leftArrow" } },
                { PlayerSettings.PlayerSetting.KB_MoveRight, new List<string> { "d", "rightArrow" } },
                { PlayerSettings.PlayerSetting.KB_MoveUp, new List<string> { "w", "upArrow" } },
                { PlayerSettings.PlayerSetting.KB_MoveDown, new List<string> { "s", "downArrow" } },
                { PlayerSettings.PlayerSetting.KB_MoveSensitivity, new List<string> { "1" } },
                { PlayerSettings.PlayerSetting.KB_MoveDeadZone, new List<string> { "0" } },
                { PlayerSettings.PlayerSetting.KB_MoveGravity, new List<string> { "1" } },
                { PlayerSettings.PlayerSetting.KB_MoveInverted, new List<string> { "0" } },
                { PlayerSettings.PlayerSetting.GP_Move, new List<string> { "leftStick" } },
                { PlayerSettings.PlayerSetting.GP_MoveInverted, new List<string> { "0" } },

                { PlayerSettings.PlayerSetting.KB_AimLeft, new List<string> { "leftArrow" } },
                { PlayerSettings.PlayerSetting.KB_AimRight, new List<string> { "rightArrow" } },
                { PlayerSettings.PlayerSetting.KB_AimUp, new List<string> { "upArrow" } },
                { PlayerSettings.PlayerSetting.KB_AimDown, new List<string> { "downArrow" } },
                { PlayerSettings.PlayerSetting.KB_AimSensitivity, new List<string> { "1" } },
                { PlayerSettings.PlayerSetting.KB_AimDeadZone, new List<string> { "0" } },
                { PlayerSettings.PlayerSetting.KB_AimGravity, new List<string> { "1" } },
                { PlayerSettings.PlayerSetting.KB_AimInverted, new List<string> { "0" } },
                { PlayerSettings.PlayerSetting.GP_Aim, new List<string> { "rightStick" } },
                { PlayerSettings.PlayerSetting.GP_AimInverted, new List<string> { "0" } },

                { PlayerSettings.PlayerSetting.KB_Shoot, new List<string> { "space" } },
                { PlayerSettings.PlayerSetting.GP_Shoot, new List<string> { "rightTrigger", "buttonSouth" } },

                { PlayerSettings.PlayerSetting.KB_Pause, new List<string> { "escape" } },
                { PlayerSettings.PlayerSetting.GP_Pause, new List<string> { "start" } },
            };

        public void ChangeBind(PlayerSettings.PlayerSetting bind, List<string> values)
        {
            foreach (string value in values)
            {
                if (!PlayerSettings.IsValidBindForAction(bind, value))
                {
                    Debug.LogWarning(PlayerSettings.GetValidBindComparison(bind, value));
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
                    devicesMap[action] = new List<IBindableInput> { input };
                else
                    devicesMap[action].Add(input);
            }

            foreach (var inputDevice in devices)
            {
                if (inputDevice is Keyboard keyboard)
                {
                    // Move
                    var moveStick = GetStickBindingsKb(keyboard, PlayerSettings.PlayerSetting.KB_MoveLeft,
                        PlayerSettings.PlayerSetting.KB_MoveRight,
                        PlayerSettings.PlayerSetting.KB_MoveDown, PlayerSettings.PlayerSetting.KB_MoveUp,
                        PlayerSettings.PlayerSetting.KB_MoveSensitivity,
                        PlayerSettings.PlayerSetting.KB_MoveGravity,
                        PlayerSettings.PlayerSetting.KB_MoveDeadZone,
                        PlayerSettings.PlayerSetting.KB_MoveInverted);
                    if (moveStick != null)
                        AppendOrCreate(MappableAction.Move, moveStick);

                    // Aim
                    var aimStick = GetStickBindingsKb(keyboard, PlayerSettings.PlayerSetting.KB_AimLeft,
                        PlayerSettings.PlayerSetting.KB_AimRight,
                        PlayerSettings.PlayerSetting.KB_AimDown, PlayerSettings.PlayerSetting.KB_AimUp,
                        PlayerSettings.PlayerSetting.KB_AimSensitivity,
                        PlayerSettings.PlayerSetting.KB_AimGravity,
                        PlayerSettings.PlayerSetting.KB_AimDeadZone,
                        PlayerSettings.PlayerSetting.KB_AimInverted);
                    if (aimStick != null)
                        AppendOrCreate(MappableAction.Aim, moveStick);

                    // Shoot
                    foreach (string path in _binds[PlayerSettings.PlayerSetting.KB_Shoot])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Shoot, new BindableButton(c));

                    // Pause
                    foreach (string path in _binds[PlayerSettings.PlayerSetting.KB_Pause])
                        if (keyboard.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Pause, new BindableButton(c));
                }
                else if (inputDevice is Gamepad gamepad)
                {
                    // Move
                    foreach (string path in _binds[PlayerSettings.PlayerSetting.GP_Move])
                        if (gamepad.TryGetChildControl(path) is StickControl c)
                        {
                            bool invert =
                                _binds.TryGetValue(PlayerSettings.PlayerSetting.GP_MoveInverted, out var invertList) &&
                                float.TryParse(invertList.First(), out float resultInvert) && resultInvert != 0f;
                            AppendOrCreate(MappableAction.Move, new BindableStick(new UnityStick(c, invert)));
                        }

                    // Aim
                    foreach (string path in _binds[PlayerSettings.PlayerSetting.GP_Aim])
                        if (gamepad.TryGetChildControl(path) is StickControl c)
                        {
                            bool invert =
                                _binds.TryGetValue(PlayerSettings.PlayerSetting.GP_AimInverted, out var invertList) &&
                                float.TryParse(invertList.First(), out float resultInvert) && resultInvert != 0f;
                            AppendOrCreate(MappableAction.Aim, new BindableStick(new UnityStick(c, invert)));
                        }

                    // Shoot
                    foreach (string path in _binds[PlayerSettings.PlayerSetting.GP_Shoot])
                        if (gamepad.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Shoot, new BindableButton(c));

                    // Pause
                    foreach (string path in _binds[PlayerSettings.PlayerSetting.GP_Pause])
                        if (gamepad.TryGetChildControl(path) is ButtonControl c)
                            AppendOrCreate(MappableAction.Pause, new BindableButton(c));
                }
            }

            return devicesMap;
        }

        private IBindableInput GetStickBindingsKb(Keyboard kb, PlayerSettings.PlayerSetting negativeX,
            PlayerSettings.PlayerSetting positiveX,
            PlayerSettings.PlayerSetting negativeY, PlayerSettings.PlayerSetting positiveY,
            PlayerSettings.PlayerSetting sensitivity,
            PlayerSettings.PlayerSetting gravity, PlayerSettings.PlayerSetting deadZone,
            PlayerSettings.PlayerSetting isInverted)
        {
            var negativeXs = new List<IBindableInput>();
            var positiveXs = new List<IBindableInput>();
            var negativeYs = new List<IBindableInput>();
            var positiveYs = new List<IBindableInput>();
            float sens =
                _binds.TryGetValue(sensitivity, out var sensList) &&
                float.TryParse(sensList.First(), out float resultSens)
                    ? resultSens
                    : 1f;
            float grav =
                _binds.TryGetValue(gravity, out var gravList) && float.TryParse(gravList.First(), out float resultGrav)
                    ? resultGrav
                    : 1f;
            float dead =
                _binds.TryGetValue(deadZone, out var deadList) && float.TryParse(deadList.First(), out float resultDead)
                    ? resultDead
                    : 0f;
            bool invert = _binds.TryGetValue(isInverted, out var invertList) &&
                          float.TryParse(invertList.First(), out float resultInvert) && resultInvert != 0f;


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
                        new AxisEmulator(positiveXs.ToArray(), negativeXs.ToArray(), sens, grav, dead),
                        new AxisEmulator(positiveYs.ToArray(), negativeYs.ToArray(), sens, grav, dead),
                        invert));
            return null;
        }

        public string Serialize()
        {
            return string.Join(";", _binds.Select(kvp =>
                $"{kvp.Key}:{string.Join(",", kvp.Value)}"
            ));
        }

        public void Deserialize(string data)
        {
            _binds.Clear();
            foreach (string entry in data.Split(';'))
            {
                string[] parts = entry.Split(':');
                PlayerSettings.PlayerSetting bind = System.Enum.Parse<PlayerSettings.PlayerSetting>(parts[0]);
                ChangeBind(bind, parts[1].Split(',').ToList());
            }
        }

        public void UpdateBind(string newBinds) => Deserialize(newBinds);
    }
}