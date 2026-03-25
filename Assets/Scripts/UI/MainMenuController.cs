using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Menu principal (UIDocument) : configuration 2–4 joueurs, crédits, quitter.
/// Règle : les joueurs 1 et 2 peuvent être Humain ou IA ; à partir du 3e joueur, toujours IA.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Minigame";
    [Tooltip("Texte multiligne affiché dans Crédits (noms, etc.). La version est ajoutée automatiquement.")]
    [SerializeField] [TextArea(3, 12)] private string creditsBody = "IFT2013 — TP2\nÉquipe 21";

    private UIDocument _document;
    private VisualElement _mainPanel;
    private VisualElement _setupPanel;
    private VisualElement _creditsPanel;
    private DropdownField _playerCountDropdown;
    private readonly Toggle[] _aiToggles = new Toggle[4];
    private readonly VisualElement[] _playerRows = new VisualElement[4];
    private readonly TextField[] _nameFields = new TextField[4];
    private Label _creditsLabel;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;

        _mainPanel = root.Q<VisualElement>("main-menu__panel");
        _setupPanel = root.Q<VisualElement>("setup-panel");
        _creditsPanel = root.Q<VisualElement>("credits-panel");

        _playerCountDropdown = root.Q<DropdownField>("player-count__dropdown");
        if (_playerCountDropdown != null)
        {
            _playerCountDropdown.choices = new List<string> { "2 joueurs", "3 joueurs", "4 joueurs" };
            _playerCountDropdown.index = 0;
            _playerCountDropdown.RegisterValueChangedCallback(_ => RefreshPlayerRowsVisibility());
        }

        for (int i = 0; i < 4; i++)
        {
            _playerRows[i] = root.Q<VisualElement>($"player-row-{i + 1}");
            _aiToggles[i] = root.Q<Toggle>($"p{i + 1}-ai__toggle");
            _nameFields[i] = root.Q<TextField>($"p{i + 1}-name__field");
            if (_nameFields[i] != null)
                _nameFields[i].value = $"Joueur {i + 1}";
            if (_aiToggles[i] == null) continue;
            // Slots 0–1 : humains par défaut ; slots 2–3 : IA obligatoires (3e et 4e joueurs).
            _aiToggles[i].value = i >= 2;
            _aiToggles[i].SetEnabled(i < 2);
        }

        _creditsLabel = root.Q<Label>("credits-body__label");

        root.Q<Button>("start-game__button")?.RegisterCallback<ClickEvent>(_ => ShowSetup());
        root.Q<Button>("credits__button")?.RegisterCallback<ClickEvent>(_ => ShowCredits());
        root.Q<Button>("quit__button")?.RegisterCallback<ClickEvent>(_ => QuitGame());
        root.Q<Button>("setup-launch__button")?.RegisterCallback<ClickEvent>(_ => LaunchGame());
        root.Q<Button>("setup-back__button")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());
        root.Q<Button>("credits-back__button")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());

        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        SetDisplay(_mainPanel, true);
        SetDisplay(_setupPanel, false);
        SetDisplay(_creditsPanel, false);
    }

    private void ShowSetup()
    {
        SetDisplay(_mainPanel, false);
        SetDisplay(_setupPanel, true);
        SetDisplay(_creditsPanel, false);
        RefreshPlayerRowsVisibility();
    }

    private void ShowCredits()
    {
        SetDisplay(_mainPanel, false);
        SetDisplay(_setupPanel, false);
        SetDisplay(_creditsPanel, true);

        if (_creditsLabel != null)
        {
            string ver = Application.version;
            if (string.IsNullOrEmpty(ver))
                ver = "dev";
            _creditsLabel.text = $"Version : {ver}\n\n{creditsBody}";
        }
    }

    private static void SetDisplay(VisualElement el, bool visible)
    {
        if (el == null) return;
        el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshPlayerRowsVisibility()
    {
        int count = 2 + (_playerCountDropdown != null ? _playerCountDropdown.index : 0);
        for (int i = 0; i < 4; i++)
        {
            if (_playerRows[i] != null)
                _playerRows[i].style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
            if (_aiToggles[i] != null)
            {
                _aiToggles[i].SetEnabled(i < 2);
                if (i >= 2)
                    _aiToggles[i].value = true;
            }
        }
    }

    private void LaunchGame()
    {
        int count = 2 + (_playerCountDropdown != null ? _playerCountDropdown.index : 0);
        count = Mathf.Clamp(count, TurnManager.MinPlayers, TurnManager.MaxPlayers);

        var computer = new bool[4];
        var names = new string[4];
        for (int i = 0; i < count; i++)
        {
            // Joueurs 3 et 4 (indices 2 et 3) : toujours IA.
            if (i >= 2)
                computer[i] = true;
            else if (_aiToggles[i] != null)
                computer[i] = _aiToggles[i].value;

            names[i] = _nameFields[i] != null ? _nameFields[i].value : null;
        }

        GameSessionConfig.PrepareFromMenu(count, computer, names);

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[MainMenuController] Aucun nom de scène de jeu défini.");
            return;
        }

        // Déjà dans la scène de jeu (menu + jeu dans la même scène) : masquer l’UI sans recharger.
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            HideMenuForGameplay();
            return;
        }

        // Sinon : changement de scène (le menu est détruit avec l’ancienne scène).
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Masque le UIDocument et désactive ce GameObject pour ne plus bloquer le jeu.</summary>
    private void HideMenuForGameplay()
    {
        if (_document != null)
            _document.rootVisualElement.style.display = DisplayStyle.None;
        gameObject.SetActive(false);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
