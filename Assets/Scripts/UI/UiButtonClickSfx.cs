using AudioSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// Utilitaire UI Toolkit: joue un SFX quand un bouton est cliqué.
    /// </summary>
    public static class UiButtonClickSfx
    {
        public static void TryPlayForButtonClick(ClickEvent evt, AudioSource source, AudioClip clip, float baseVolume = 1f)
        {
            if (evt == null || clip == null) return;
            if (!IsButtonTarget(evt.target as VisualElement)) return;

            float volume = Mathf.Clamp01(baseVolume) * GameAudioSettings.SfxVolume;
            if (source != null)
            {
                source.volume = volume;
                source.PlayOneShot(clip);
                return;
            }

            if (Camera.main == null) return;
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
        }

        private static bool IsButtonTarget(VisualElement target)
        {
            var current = target;
            while (current != null)
            {
                if (current is Button)
                    return true;
                current = current.parent;
            }

            return false;
        }
    }
}
