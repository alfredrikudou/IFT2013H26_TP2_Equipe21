using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    /// <summary>
    /// Ajoute un remplissage "eau" sur les boutons UI Toolkit au survol.
    /// </summary>
    public static class UiButtonWaterFillEffect
    {
        private const string OverlayName = "water-fill-overlay";
        private const string ReadyClass = "water-fill-ready";
        // Même esprit que le fond initial des boutons (blanc translucide), pour un “remplissage” cohérent.
        private static readonly Color FillColor = new(1f, 1f, 1f, 0.18f);

        private sealed class ButtonWaterState
        {
            public VisualElement Overlay;
            public IVisualElementScheduledItem Animator;
            public float Start01;
            public float Target01;
            public float Current01;
            public float StartTime;
            public float Duration;
        }

        private static readonly Dictionary<Button, ButtonWaterState> States = new();

        public static void AttachToAllButtons(VisualElement root, float durationSeconds = 0.22f)
        {
            if (root == null) return;
            foreach (var button in root.Query<Button>().ToList())
                AttachToButton(button, durationSeconds);
        }

        private static void AttachToButton(Button button, float durationSeconds)
        {
            if (button == null || button.ClassListContains(ReadyClass)) return;
            button.AddToClassList(ReadyClass);
            button.style.overflow = Overflow.Hidden;

            var overlay = new VisualElement { name = OverlayName, pickingMode = PickingMode.Ignore };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.bottom = 0f;
            overlay.style.width = Length.Percent(0f);
            overlay.style.backgroundColor = new StyleColor(FillColor);

            button.Insert(0, overlay);

            var state = new ButtonWaterState
            {
                Overlay = overlay,
                Current01 = 0f
            };
            States[button] = state;

            button.RegisterCallback<PointerEnterEvent>(_ => StartTween(button, 1f, durationSeconds));
            button.RegisterCallback<PointerLeaveEvent>(_ => StartTween(button, 0f, durationSeconds * 0.9f));
            button.RegisterCallback<PointerDownEvent>(_ => StartTween(button, 1f, durationSeconds * 0.5f));
        }

        private static void StartTween(Button button, float target01, float durationSeconds)
        {
            if (!States.TryGetValue(button, out var state) || state.Overlay == null) return;
            state.Start01 = state.Current01;
            state.Target01 = Mathf.Clamp01(target01);
            state.StartTime = Time.unscaledTime;
            state.Duration = Mathf.Max(0.02f, durationSeconds);

            state.Animator?.Pause();
            state.Animator = button.schedule.Execute(() => Tick(button)).Every(16);
        }

        private static void Tick(Button button)
        {
            if (!States.TryGetValue(button, out var state) || state.Overlay == null)
                return;

            float t = Mathf.Clamp01((Time.unscaledTime - state.StartTime) / state.Duration);
            // Easing cubic-out pour un effet fluide.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            state.Current01 = Mathf.Lerp(state.Start01, state.Target01, eased);
            state.Overlay.style.width = Length.Percent(state.Current01 * 100f);

            if (t >= 1f)
                state.Animator?.Pause();
        }
    }
}
