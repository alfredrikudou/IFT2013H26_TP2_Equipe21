using UnityEngine;

namespace UI
{
    public class UINavigationManager : MonoBehaviour
    {
        [SerializeField] private GamePauseController _pauseController;
        [SerializeField] private KeybindMenuEvents _keybindMenu;

        private void Start()
        {
            _pauseController.OnSettingsRequested += () =>
            {
                _pauseController.HidePanelImmediate();
                _keybindMenu.Show();
            };

            _keybindMenu.OnBackRequested += () =>
            {
                _pauseController.ShowPanel();
            };
        }
    }
}
