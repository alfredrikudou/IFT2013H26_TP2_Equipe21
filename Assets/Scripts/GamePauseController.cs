using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

/// <summary>
/// Pause au clavier (touche P) + affichage UI (résumé + HP de tous les joueurs).
/// Le gameplay est gelé via Time.timeScale = 0.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GamePauseController : MonoBehaviour
{
    [SerializeField] private string homeSceneName = "MenuScene";
    
    public event Action OnSettingsRequested;

    private UIDocument _document;
    private VisualElement _pausePanel;
    private VisualElement _playersContainer;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;

        _pausePanel = root.Q<VisualElement>("pause-panel");
        _playersContainer = root.Q<VisualElement>("pause-players__container");

        root.Q<Button>("pause-resume__button")?.RegisterCallback<ClickEvent>(_ => Resume());
        root.Q<Button>("pause-setting__button")?.RegisterCallback<ClickEvent>(_ => OnSettingsRequested?.Invoke());
        root.Q<Button>("pause-home__button")?.RegisterCallback<ClickEvent>(_ => GoHome());
        root.Q<Button>("pause-quit__button")?.RegisterCallback<ClickEvent>(_ => QuitGame());

        HidePanelImmediate();
    }

    public void SetPause(bool pause)
    {
        if (GamePauseState.IsPaused && !pause)
            Resume();
        else if(!GamePauseState.IsPaused && pause)
            Pause();
    }

    public void Pause()
    {
        if (GamePauseState.IsPaused) return;
        GamePauseState.SetPaused(true);

        RefreshPlayersUI();
        ShowPanel();

        Time.timeScale = 0f;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        if (!GamePauseState.IsPaused) return;
        GamePauseState.SetPaused(false);

        HidePanelImmediate();
        Time.timeScale = 1f;
        UnityEngine.Cursor.visible = false;
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

    private void RefreshPlayersUI()
    {
        if (_playersContainer == null) return;

        _playersContainer.Clear();

        var players = Object.FindObjectsOfType<Player.Player>(false)
            .Where(p => p != null)
            .ToList();

        // Si SlotIndex est renseigné, on trie par slot (Player0..), sinon par nom.
        bool anySlots = players.Any(p => p.SlotIndex >= 0);
        if (anySlots)
            players = players.OrderBy(p => p.SlotIndex >= 0 ? p.SlotIndex : int.MaxValue).ToList();
        else
            players = players.OrderBy(p => p.GetName()).ToList();

        foreach (var p in players)
        {
            int current = Mathf.RoundToInt(p.CurrentHealth);
            int max = Mathf.RoundToInt(p.MaxHealth);
            var line = new Label($"{p.GetName()} : {current}/{max}");
            line.AddToClassList("pause-player-line");
            if (p.IsDead) line.AddToClassList("dead");
            _playersContainer.Add(line);
        }
    }
}

