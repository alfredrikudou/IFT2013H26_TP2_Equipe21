using System.Collections.Generic;

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


        internal enum InputType
        {
            Button,
            Stick,
            Numerical
        }

        private static readonly Dictionary<PlayerSetting, InputType> _bindTypes =
            new Dictionary<PlayerSetting, InputType>
            {
                { PlayerSetting.KB_MoveLeft, InputType.Button },
                { PlayerSetting.KB_MoveRight, InputType.Button },
                { PlayerSetting.KB_MoveUp, InputType.Button },
                { PlayerSetting.KB_MoveDown, InputType.Button },
                { PlayerSetting.GP_Move, InputType.Stick },
                { PlayerSetting.KB_MoveSensitivity,InputType.Numerical },
                { PlayerSetting.KB_MoveDeadZone,InputType.Numerical },
                { PlayerSetting.KB_MoveGravity,InputType.Numerical },
                { PlayerSetting.KB_MoveInverted,InputType.Numerical },
                { PlayerSetting.GP_MoveInverted,InputType.Numerical },

                { PlayerSetting.KB_AimLeft, InputType.Button },
                { PlayerSetting.KB_AimRight, InputType.Button },
                { PlayerSetting.KB_AimUp, InputType.Button },
                { PlayerSetting.KB_AimDown, InputType.Button },
                { PlayerSetting.GP_Aim, InputType.Stick },
                { PlayerSetting.KB_AimSensitivity,InputType.Numerical },
                { PlayerSetting.KB_AimDeadZone,InputType.Numerical },
                { PlayerSetting.KB_AimGravity,InputType.Numerical },
                { PlayerSetting.KB_AimInverted,InputType.Numerical },
                { PlayerSetting.GP_AimInverted,InputType.Numerical },

                { PlayerSetting.KB_Shoot, InputType.Button },
                { PlayerSetting.GP_Shoot, InputType.Button },

                { PlayerSetting.KB_Pause, InputType.Button },
                { PlayerSetting.GP_Pause, InputType.Button },
            };
        
        
        
        public static bool IsValidBindForAction(string action, string controlName)
        {
            PlayerSetting setting = System.Enum.Parse<PlayerSetting>(action);
            if (IsFloatSetting(setting)) return controlName == "" ?  true : false;
            InputType expected = _bindTypes[setting];
            InputType actual = controlName.Contains("Stick") ? InputType.Stick : InputType.Button;
            return expected == actual;
        }
        
        public static bool IsValidBindForAction(PlayerSetting setting, string controlName)
        {
            if (IsFloatSetting(setting)) return float.TryParse(controlName, out _);
            InputType expected = _bindTypes[setting];
            InputType actual = controlName.Contains("Stick") ? InputType.Stick : InputType.Button;
            return expected == actual;
        }

        public static string GetValidBindComparison(PlayerSetting setting, string controlName)
        {
            if (IsFloatSetting(setting)) 
                return float.TryParse(controlName, out _) ? $"Valid {InputType.Numerical}" :  $"Bind {setting} expects {InputType.Numerical} but control name was not a float '{controlName}'";
            InputType expected = _bindTypes[setting];
            InputType actual = controlName.Contains("Stick") ? InputType.Stick : InputType.Button;
            return expected == actual ? $"Valid {expected}" :  $"Bind {setting} expects {expected} but got {actual} for path {controlName}";
        }
        
        public static bool IsFloatSetting(PlayerSetting setting) 
            => _bindTypes[setting] == InputType.Numerical;
    }
}