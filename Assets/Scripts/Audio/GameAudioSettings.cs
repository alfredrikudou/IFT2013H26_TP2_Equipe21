using System;
using UnityEngine;

namespace AudioSystem
{
    /// <summary>
    /// Réglages audio globaux (musique + SFX) avec persistance locale.
    /// </summary>
    public static class GameAudioSettings
    {
        private const string MenuMusicVolumeKey = "audio.music.menu.volume";
        private const string GameplayMusicVolumeKey = "audio.music.gameplay.volume";
        private const string SfxVolumeKey = "audio.sfx.volume";

        private static float _menuMusicVolume = 0.8f;
        private static float _gameplayMusicVolume = 0.8f;
        private static float _sfxVolume = 0.9f;
        private static bool _initialized;

        public static event Action OnChanged;

        public static float MenuMusicVolume
        {
            get
            {
                EnsureLoaded();
                return _menuMusicVolume;
            }
            set
            {
                EnsureLoaded();
                float v = Mathf.Clamp01(value);
                if (Mathf.Approximately(v, _menuMusicVolume)) return;
                _menuMusicVolume = v;
                PlayerPrefs.SetFloat(MenuMusicVolumeKey, _menuMusicVolume);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        public static float GameplayMusicVolume
        {
            get
            {
                EnsureLoaded();
                return _gameplayMusicVolume;
            }
            set
            {
                EnsureLoaded();
                float v = Mathf.Clamp01(value);
                if (Mathf.Approximately(v, _gameplayMusicVolume)) return;
                _gameplayMusicVolume = v;
                PlayerPrefs.SetFloat(GameplayMusicVolumeKey, _gameplayMusicVolume);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        public static float SfxVolume
        {
            get
            {
                EnsureLoaded();
                return _sfxVolume;
            }
            set
            {
                EnsureLoaded();
                float v = Mathf.Clamp01(value);
                if (Mathf.Approximately(v, _sfxVolume)) return;
                _sfxVolume = v;
                PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
                PlayerPrefs.Save();
                OnChanged?.Invoke();
            }
        }

        public static void EnsureLoaded()
        {
            if (_initialized) return;
            _menuMusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MenuMusicVolumeKey, _menuMusicVolume));
            _gameplayMusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(GameplayMusicVolumeKey, _gameplayMusicVolume));
            _sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, _sfxVolume));
            _initialized = true;
        }
    }
}
