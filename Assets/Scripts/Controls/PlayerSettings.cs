using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace Controls
{
    public static class PlayerSettings
    {
        public enum PlayerSetting
        {
            // Movement
            KB_MoveLeft,
            KB_MoveRight,
            KB_MoveUp,
            KB_MoveDown,
            KB_MoveSensitivity,
            KB_MoveDeadZone,
            KB_MoveGravity,
            KB_MoveInverted,
            GP_Move,
            GP_MoveInverted,

            // Aim
            KB_AimLeft,
            KB_AimRight,
            KB_AimUp,
            KB_AimDown,
            KB_AimSensitivity,
            KB_AimDeadZone,
            KB_AimGravity,
            KB_AimInverted,
            GP_Aim,
            GP_AimInverted,

            // Actions
            KB_Shoot,
            GP_Shoot,

            KB_Pause,
            GP_Pause
        }

        private static readonly Dictionary<PlayerSetting, Type> _bindTypes =
            new Dictionary<PlayerSetting, Type>
            {
                { PlayerSetting.KB_MoveLeft, typeof(ButtonControl) },
                { PlayerSetting.KB_MoveRight, typeof(ButtonControl) },
                { PlayerSetting.KB_MoveUp, typeof(ButtonControl) },
                { PlayerSetting.KB_MoveDown, typeof(ButtonControl) },
                { PlayerSetting.GP_Move, typeof(StickControl) },
                { PlayerSetting.KB_MoveSensitivity, typeof(float) },
                { PlayerSetting.KB_MoveDeadZone, typeof(float) },
                { PlayerSetting.KB_MoveGravity, typeof(float) },
                { PlayerSetting.KB_MoveInverted, typeof(float) },
                { PlayerSetting.GP_MoveInverted, typeof(float) },

                { PlayerSetting.KB_AimLeft, typeof(ButtonControl) },
                { PlayerSetting.KB_AimRight, typeof(ButtonControl) },
                { PlayerSetting.KB_AimUp, typeof(ButtonControl) },
                { PlayerSetting.KB_AimDown, typeof(ButtonControl) },
                { PlayerSetting.GP_Aim, typeof(StickControl) },
                { PlayerSetting.KB_AimSensitivity, typeof(float) },
                { PlayerSetting.KB_AimDeadZone, typeof(float) },
                { PlayerSetting.KB_AimGravity, typeof(float) },
                { PlayerSetting.KB_AimInverted, typeof(float) },
                { PlayerSetting.GP_AimInverted, typeof(float) },

                { PlayerSetting.KB_Shoot, typeof(ButtonControl) },
                { PlayerSetting.GP_Shoot, typeof(ButtonControl) },

                { PlayerSetting.KB_Pause, typeof(ButtonControl) },
                { PlayerSetting.GP_Pause, typeof(ButtonControl) },
            };
        
        
        
        public static bool IsValidBindForAction(string action, string controlName)
        {
            PlayerSetting setting = System.Enum.Parse<PlayerSetting>(action);
            return IsValidBindForAction(setting, controlName);
        }
        
        public static bool IsValidBindForAction(PlayerSetting setting, string controlName)
        {
            if (string.IsNullOrEmpty(controlName)) return false;
            if (IsFloatSetting(setting)) return float.TryParse(controlName, out _);
            Type expected = _bindTypes[setting];
            foreach (var device in DeviceManager.Instance.GetAllDevices())
            {
                var control = device.TryGetChildControl(controlName);
                Debug.Log($"Device: {device.name} | Control: {control?.name ?? "null"} | Type: {control?.GetType().Name ?? "null"}");
                if (control != null && expected.IsInstanceOfType(control))
                {
                    Debug.Log("They matched!");
                    return true;
                }
            }
            Debug.LogWarning($"No match for {setting}");
            return false;
        }

        public static Type GetExpectedType(PlayerSetting setting) => _bindTypes[setting];
        
        public static bool IsFloatSetting(PlayerSetting setting) 
            => _bindTypes[setting] == typeof(float);
    }
}