using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UI
{
    /// <summary>
    /// Pause au clavier (touche P) + affichage UI (résumé + HP de tous les joueurs).
    /// Le gameplay est gelé via Time.timeScale = 0.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndGameUIEvents : MonoBehaviour
    {
        [SerializeField] private string homeSceneName = "MenuScene";

        private UIDocument _document;
        private VisualElement _pausePanel;
        private Label _endgameLabel;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            var root = _document.rootVisualElement;

            _pausePanel = root.Q<VisualElement>("pause-panel");
            _endgameLabel = root.Q<Label>("endgame_result_label");
        
            root.Q<Button>("pause-home__button")?.RegisterCallback<ClickEvent>(_ => GoHome());
            root.Q<Button>("pause-quit__button")?.RegisterCallback<ClickEvent>(_ => QuitGame());

            HidePanelImmediate();
        }

        public void Pause()
        {
            if (GamePauseState.IsPaused) return;
            GamePauseState.SetPaused(true);
            
            ShowPanel();

            Time.timeScale = 0f;
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }

        public void EndGame(string winner)
        {
            if (_endgameLabel != null)
            {
                _endgameLabel.text = winner;
                _endgameLabel.style.color = new StyleColor(Color.white);
            }

            Pause();
        }

        private void GoHome()
        {
            // Restaurer le temps avant de changer de scène.
            GamePauseState.SetPaused(false);
            Time.timeScale = 1f;
            UnityEngine.Cursor.visible = false;

            if (!string.IsNullOrEmpty(homeSceneName))
                SceneManager.LoadScene(homeSceneName);
            else
                Debug.LogError("[GamePauseController] homeSceneName est vide.");
        }

        private void QuitGame()
        {
            GamePauseState.SetPaused(false);
            Time.timeScale = 1f;
            UnityEngine.Cursor.visible = false;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

        public void ShowPanel()
        {
            if (_pausePanel == null) return;
            _pausePanel.style.display = DisplayStyle.Flex;
        }

        public void HidePanelImmediate()
        {
            if (_pausePanel == null) return;
            _pausePanel.style.display = DisplayStyle.None;
        }
    }
}

