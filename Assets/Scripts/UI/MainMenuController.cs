using System.Collections;
using System.Collections.Generic;
using AudioSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Menu principal (UIDocument) : configuration 2–4 joueurs, crédits, quitter.
/// Chaque slot visible (2 à 4 joueurs) peut être Humain ou IA, y compris les joueurs 3 et 4.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Minigame";
    [Tooltip("Texte multiligne affiché dans Crédits (noms, etc.). La version est ajoutée automatiquement.")]
    [SerializeField] [TextArea(3, 12)] private string creditsBody = "IFT2013 — TP2\nÉquipe 21";
    [Tooltip("Optionnel : image plein écran pendant le chargement (sinon même visuel que l’arrière-plan du menu).")]
    [SerializeField] private Texture2D loadingSplashOverride;
    [Tooltip("Durée minimale d’affichage du splash (temps réel), même si la scène est déjà chargée.")]
    [SerializeField] [Min(0f)] private float minimumSplashDurationSeconds = 5f;
    [Header("Audio menu")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] [Range(0f, 1f)] private float menuMusicBaseVolume = 1f;
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private AudioClip uiButtonClickClip;
    [SerializeField] [Range(0f, 1f)] private float uiButtonClickBaseVolume = 1f;
    [Header("Animation UI")]
    [SerializeField] [Min(0.05f)] private float panelFadeDuration = 0.24f;

    private UIDocument _document;
    private VisualElement _mainPanel;
    private VisualElement _setupPanel;
    private VisualElement _creditsPanel;
    private VisualElement _soundPanel;
    private VisualElement _loadingPanel;
    private VisualElement _loadingSplashBg;
    private VisualElement _loadingProgressFill;
    private Label _loadingStatusLabel;
    private Label _loadingDetailLabel;
    private Label _loadingPercentLabel;
    private DropdownField _playerCountDropdown;
    private readonly Toggle[] _aiToggles = new Toggle[4];
    private readonly VisualElement[] _playerRows = new VisualElement[4];
    private readonly TextField[] _nameFields = new TextField[4];
    private Label _creditsLabel;
    private Slider _menuMusicVolumeSlider;
    private Slider _gameplayMusicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Coroutine _panelTransitionRoutine;

    private void OnEnable()
    {
        // Après Minigame (curseur masqué en jeu / reprise), garantir un pointeur utilisable au menu.
        // Qualification explicite : UI Toolkit expose aussi une classe Cursor.
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        GameAudioSettings.OnChanged += OnAudioSettingsChanged;
        ApplyMenuMusicVolume();
    }

    private void OnDisable()
    {
        GameAudioSettings.OnChanged -= OnAudioSettingsChanged;
    }

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;
        root.RegisterCallback<ClickEvent>(evt =>
            UI.UiButtonClickSfx.TryPlayForButtonClick(evt, uiSfxSource, uiButtonClickClip, uiButtonClickBaseVolume));

        _mainPanel = root.Q<VisualElement>("main-menu__panel");
        _setupPanel = root.Q<VisualElement>("setup-panel");
        _creditsPanel = root.Q<VisualElement>("credits-panel");
        _soundPanel = root.Q<VisualElement>("sound-panel");
        _loadingPanel = root.Q<VisualElement>("loading-panel");
        _loadingSplashBg = root.Q<VisualElement>("loading-splash__bg");
        _loadingProgressFill = root.Q<VisualElement>("loading-progress__fill");
        _loadingStatusLabel = root.Q<Label>("loading-status__label");
        _loadingDetailLabel = root.Q<Label>("loading-detail__label");
        _loadingPercentLabel = root.Q<Label>("loading-percent__label");

        if (loadingSplashOverride != null && _loadingSplashBg != null)
            _loadingSplashBg.style.backgroundImage = new StyleBackground(loadingSplashOverride);

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
            // Slots 0–1 : humains par défaut ; 3–4 : IA par défaut (modifiable).
            _aiToggles[i].value = i >= 2;
            _aiToggles[i].SetEnabled(true);
        }

        _creditsLabel = root.Q<Label>("credits-body__label");
        _menuMusicVolumeSlider = root.Q<Slider>("menu-music-volume__slider");
        _gameplayMusicVolumeSlider = root.Q<Slider>("gameplay-music-volume__slider");
        _sfxVolumeSlider = root.Q<Slider>("sfx-volume__slider");

        root.Q<Button>("start-game__button")?.RegisterCallback<ClickEvent>(_ => ShowSetup());
        root.Q<Button>("credits__button")?.RegisterCallback<ClickEvent>(_ => ShowCredits());
        root.Q<Button>("sound-setting__button")?.RegisterCallback<ClickEvent>(_ => ShowSoundSettings());
        root.Q<Button>("quit__button")?.RegisterCallback<ClickEvent>(_ => QuitGame());
        root.Q<Button>("setup-launch__button")?.RegisterCallback<ClickEvent>(_ => LaunchGame());
        root.Q<Button>("setup-back__button")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());
        root.Q<Button>("credits-back__button")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());
        root.Q<Button>("sound-back__button")?.RegisterCallback<ClickEvent>(_ => ShowMainMenu());

        ConfigureSoundSliders();
        PlayMenuMusicIfNeeded();

        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        ShowPanelAnimated(_mainPanel, _setupPanel, _creditsPanel, _soundPanel);
    }

    private void ShowSetup()
    {
        ShowPanelAnimated(_setupPanel, _mainPanel, _creditsPanel, _soundPanel);
        RefreshPlayerRowsVisibility();
    }

    private void ShowCredits()
    {
        ShowPanelAnimated(_creditsPanel, _mainPanel, _setupPanel, _soundPanel);

        if (_creditsLabel != null)
        {
            string ver = Application.version;
            if (string.IsNullOrEmpty(ver))
                ver = "dev";
            _creditsLabel.text = $"Version : {ver}\n\n{creditsBody}";
        }
    }

    private void ShowSoundSettings()
    {
        ShowPanelAnimated(_soundPanel, _mainPanel, _setupPanel, _creditsPanel);
    }

    private static void SetDisplay(VisualElement el, bool visible)
    {
        if (el == null) return;
        el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowPanelAnimated(VisualElement shown, params VisualElement[] hidden)
    {
        if (_panelTransitionRoutine != null)
            StopCoroutine(_panelTransitionRoutine);
        _panelTransitionRoutine = StartCoroutine(AnimatePanelTransition(shown, hidden));
    }

    private IEnumerator AnimatePanelTransition(VisualElement shown, VisualElement[] hidden)
    {
        foreach (var el in hidden)
        {
            if (el == null) continue;
            el.style.opacity = 0f;
            el.style.display = DisplayStyle.None;
        }

        if (shown == null) yield break;
        shown.style.display = DisplayStyle.Flex;
        shown.style.opacity = 0f;

        float t = 0f;
        float duration = Mathf.Max(0.01f, panelFadeDuration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(t / duration);
            // Easing cubic pour adoucir l'entrée du panneau.
            float eased = 1f - Mathf.Pow(1f - x, 3f);
            shown.style.opacity = eased;
            yield return null;
        }

        shown.style.opacity = 1f;
        _panelTransitionRoutine = null;
    }

    private void RefreshPlayerRowsVisibility()
    {
        int count = 2 + (_playerCountDropdown != null ? _playerCountDropdown.index : 0);
        for (int i = 0; i < 4; i++)
        {
            if (_playerRows[i] != null)
                _playerRows[i].style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
            if (_aiToggles[i] != null)
                _aiToggles[i].SetEnabled(i < count);
        }
    }

    private void LaunchGame()
    {
        if (menuMusicSource != null)
            menuMusicSource.Stop();

        int count = 2 + (_playerCountDropdown != null ? _playerCountDropdown.index : 0);
        count = Mathf.Clamp(count, GameManager.MinAgent, GameManager.MaxPlayers);

        var computer = new bool[4];
        var names = new string[4];
        for (int i = 0; i < count; i++)
        {
            computer[i] = _aiToggles[i] != null && _aiToggles[i].value;
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

        // Sinon : chargement asynchrone (splash / attente jusqu’à ce que la scène soit prête).
        StartCoroutine(LoadGameSceneAsync(gameSceneName));
    }

    private void ConfigureSoundSliders()
    {
        GameAudioSettings.EnsureLoaded();

        if (_menuMusicVolumeSlider != null)
        {
            _menuMusicVolumeSlider.lowValue = 0f;
            _menuMusicVolumeSlider.highValue = 1f;
            _menuMusicVolumeSlider.value = GameAudioSettings.MenuMusicVolume;
            _menuMusicVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                GameAudioSettings.MenuMusicVolume = evt.newValue;
                ApplyMenuMusicVolume();
            });
        }

        if (_gameplayMusicVolumeSlider != null)
        {
            _gameplayMusicVolumeSlider.lowValue = 0f;
            _gameplayMusicVolumeSlider.highValue = 1f;
            _gameplayMusicVolumeSlider.value = GameAudioSettings.GameplayMusicVolume;
            _gameplayMusicVolumeSlider.RegisterValueChangedCallback(evt =>
            {
                GameAudioSettings.GameplayMusicVolume = evt.newValue;
            });
        }

        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.lowValue = 0f;
            _sfxVolumeSlider.highValue = 1f;
            _sfxVolumeSlider.value = GameAudioSettings.SfxVolume;
            _sfxVolumeSlider.RegisterValueChangedCallback(evt => { GameAudioSettings.SfxVolume = evt.newValue; });
        }
    }

    private void PlayMenuMusicIfNeeded()
    {
        if (menuMusicSource == null) return;
        menuMusicSource.loop = true;
        ApplyMenuMusicVolume();
        if (!menuMusicSource.isPlaying)
            menuMusicSource.Play();
    }

    private void OnAudioSettingsChanged()
    {
        if (_menuMusicVolumeSlider != null)
            _menuMusicVolumeSlider.SetValueWithoutNotify(GameAudioSettings.MenuMusicVolume);
        if (_gameplayMusicVolumeSlider != null)
            _gameplayMusicVolumeSlider.SetValueWithoutNotify(GameAudioSettings.GameplayMusicVolume);
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.SetValueWithoutNotify(GameAudioSettings.SfxVolume);
        ApplyMenuMusicVolume();
    }

    private void ApplyMenuMusicVolume()
    {
        if (menuMusicSource == null) return;
        menuMusicSource.volume = menuMusicBaseVolume * GameAudioSettings.MenuMusicVolume;
    }

    private IEnumerator LoadGameSceneAsync(string sceneName)
    {
        float splashStart = Time.unscaledTime;
        SetLoadingVisible(true);
        SetLoadingProgressUI(0f);
        SetLoadingHeadline("Préparation du chargement");
        SetLoadingDetail(
            "Demande asynchrone au moteur Unity : la scène, ses GameObjects, composants, maillages, matériaux et colliders seront instanciés en mémoire.");

        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[MainMenuController] Impossible de charger la scène « {sceneName} » (nom ou Build Settings).");
            SetLoadingVisible(false);
            yield break;
        }

        op.allowSceneActivation = false;

        // Phase 1 : flux Unity (progress 0 → 0,9 tant que la scène n’est pas prête à s’activer).
        while (op.progress < 0.9f)
        {
            float async01 = Mathf.Clamp01(op.progress / 0.9f);
            SetLoadingProgressUI(async01 * 0.88f);
            SetLoadingHeadline($"Chargement de « {sceneName} »");
            SetLoadingDetail(
                $"Étape : désérialisation et intégration des ressources de la scène (hiérarchie, prefabs référencés, assets liés). " +
                $"Progression interne du chargeur : {Mathf.RoundToInt(async01 * 100)} %.");
            yield return null;
        }

        float loadReadyTime = Time.unscaledTime;

        // Phase 2 : respecter la durée minimale du splash (temps réel, indépendant de timeScale).
        SetLoadingHeadline("Scène prête en mémoire");
        while (Time.unscaledTime - splashStart < minimumSplashDurationSeconds)
        {
            float elapsed = Time.unscaledTime - splashStart;
            float remaining = Mathf.Max(0f, minimumSplashDurationSeconds - elapsed);
            float sinceLoadReady = Time.unscaledTime - loadReadyTime;
            float padWindow = Mathf.Max(0.01f, minimumSplashDurationSeconds - (loadReadyTime - splashStart));
            float pad01 = Mathf.Clamp01(sinceLoadReady / padWindow);
            SetLoadingProgressUI(Mathf.Lerp(0.88f, 1f, pad01));
            SetLoadingDetail(
                "Étape : synchronisation affichage — la scène est chargée mais le lancement attend encore " +
                $"{remaining:F1} s (durée minimale du splash). Ensuite, activation de la nouvelle scène.");
            yield return null;
        }

        SetLoadingProgressUI(1f);
        SetLoadingHeadline("Lancement");
        SetLoadingDetail("Activation de la scène : passage au jeu.");
        op.allowSceneActivation = true;
        yield return op;
    }

    private void SetLoadingHeadline(string text)
    {
        if (_loadingStatusLabel != null)
            _loadingStatusLabel.text = text;
    }

    private void SetLoadingDetail(string text)
    {
        if (_loadingDetailLabel != null)
            _loadingDetailLabel.text = text;
    }

    private void SetLoadingProgressUI(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);
        if (_loadingProgressFill != null)
            _loadingProgressFill.style.width = Length.Percent(progress01 * 100f);
        if (_loadingPercentLabel != null)
            _loadingPercentLabel.text = $"{Mathf.RoundToInt(progress01 * 100f)} %";
    }

    private void SetLoadingVisible(bool visible)
    {
        if (_loadingPanel == null) return;
        _loadingPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (!visible)
        {
            SetLoadingProgressUI(0f);
            SetLoadingDetail(string.Empty);
        }
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
