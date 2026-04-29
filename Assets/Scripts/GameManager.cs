using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Agents;
using AudioSystem;
using MapGeneration;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int MinPlayers = 0;
    public const int MaxPlayers = 4;
    public const int MinAgent = 2;
    public const int MaxAgent = 4;

    [Header("Spawn")]
    [Tooltip("Si activé, supprime les agents présents et respawn selon le menu (ou 2 humains par défaut en éditeur sans menu).")]
    [SerializeField] private bool spawnPlayersAtStart = true;
    [SerializeField] private Vector3[] spawnPoints = new Vector3[0];

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject aiPrefab;

    [Header("Fin de partie")]
    [SerializeField] private string winnerFormat = "{0} remporte la partie !";
    [SerializeField] private string tieMessage = "Partie terminée — plus aucun survivant";
    [Header("Audio gameplay")]
    [SerializeField] private AudioSource gameplayMusicSource;
    [SerializeField] [Range(0f, 1f)] private float gameplayMusicBaseVolume = 1f;
    [Header("Jingles gameplay")]
    [SerializeField] private AudioSource gameplayJingleSource;
    [SerializeField] private AudioClip lowHealthJingle;
    [SerializeField] private AudioClip deathJingle;
    [SerializeField] [Range(0.05f, 0.95f)] private float lowHealthThreshold = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float lowHealthResetThreshold = 0.45f;
    [SerializeField] [Range(0f, 1f)] private float gameplayJingleBaseVolume = 1f;
    [Header("Fin de partie - SFX")]
    [SerializeField] private AudioSource endMatchSfxSource;
    [SerializeField] private AudioClip winnerSfx;
    [SerializeField] private AudioClip tieSfx;
    [SerializeField] [Range(0f, 1f)] private float endMatchSfxBaseVolume = 1f;

    private readonly List<Agents.Player> _players = new();
    private readonly List<Agents.AiController> _ais = new();
    private readonly List<Agents.Agent> _agents = new();
    private readonly HashSet<int> _lowHealthJinglePlayedFor = new();
    private readonly HashSet<int> _deathJinglePlayedFor = new();
    private bool _matchOver;

    public bool IsMatchOver => _matchOver;

    /// <summary>Agents actuellement en jeu (ordre de spawn). Pour HUD, minimap, etc.</summary>
    public IReadOnlyList<Agents.Agent> ActiveAgents => _agents;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _matchOver = false;
        GameAudioSettings.OnChanged += ApplyGameplayMusicVolume;
        PlayGameplayMusicIfNeeded();
        ApplyGameplayJingleVolume();
        TerrainGenerator terrainGenerator = FindFirstObjectByType<TerrainGenerator>();
        spawnPoints = terrainGenerator.GetSpawnPoints().ToArray();
        if (spawnPlayersAtStart)
            SpawnAllAgents();
        SetupViewports();
    }

    private void OnDestroy()
    {
        GameAudioSettings.OnChanged -= ApplyGameplayMusicVolume;
    }

    private void SetupViewports()
    {
        switch (_agents.Count)
        {
            case 1:
                _agents[0].SetViewport(new Rect(0, 0, 1, 1));
                break;

            case 2:
                _agents[0].SetViewport(new Rect(0f, 0f, 0.5f, 1f));
                _agents[1].SetViewport(new Rect(0.5f, 0f, 0.5f, 1f));
                break;

            case 3:
                _agents[0].SetViewport(new Rect(0f, 0.5f, 0.5f, 0.5f));
                _agents[1].SetViewport(new Rect(0.5f, 0.5f, 0.5f, 0.5f));
                _agents[2].SetViewport(new Rect(0f, 0f, 0.5f, 0.5f));
                Camera.main.rect = new Rect(0.5f, 0f, 0.5f, 0.5f);
                Camera.main.enabled = true;
                break;

            case 4:
                _agents[0].SetViewport(new Rect(0f, 0.5f, 0.5f, 0.5f));
                _agents[1].SetViewport(new Rect(0.5f, 0.5f, 0.5f, 0.5f));
                _agents[2].SetViewport(new Rect(0f, 0f, 0.5f, 0.5f));
                _agents[3].SetViewport(new Rect(0.5f, 0f, 0.5f, 0.5f));
                break;
        }
    }

    private void SpawnAllAgents()
    {
        _players.Clear();
        _ais.Clear();
        _agents.Clear();
        _lowHealthJinglePlayedFor.Clear();
        _deathJinglePlayedFor.Clear();

        foreach (var p in FindObjectsByType<Agents.Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p != null) Destroy(p.gameObject);
        foreach (var a in FindObjectsByType<Agents.AiController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a != null) Destroy(a.gameObject);

        Agents.Agent.ResetStaticNaming();

        if (GameSessionConfig.LoadedFromMenu)
            SpawnAgentsFromMenuSession();
        else
            SpawnEditorDefaultAgents();

        foreach (var cam in FindObjectsByType<CameraController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            cam.RequestTargetsRefreshFromPlayers();
    }

    /// <summary>Slots humains / IA et noms : tout vient du menu principal.</summary>
    private void SpawnAgentsFromMenuSession()
    {
        int n = Mathf.Clamp(GameSessionConfig.PlayerCount, MinAgent, MaxPlayers);
        if (playerPrefab == null || aiPrefab == null)
        {
            Debug.LogError("[GameManager] playerPrefab et aiPrefab sont requis.");
            return;
        }

        if (!ValidateSpawnPoints(n))
            return;

        for (int slot = 0; slot < n; slot++)
        {
            if (GameSessionConfig.IsComputerControlledSlot(slot))
                TrySpawnAiAt(spawnPoints[slot], slot);
            else
                TrySpawnPlayerAt(spawnPoints[slot], slot);
        }
    }

    /// <summary>Lancer Minigame sans passer par le menu : 2 humains (tests éditeur).</summary>
    private void SpawnEditorDefaultAgents()
    {
        int n = MinAgent;
        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] playerPrefab requis.");
            return;
        }

        if (!ValidateSpawnPoints(n))
            return;

        for (int i = 0; i < n; i++)
            TrySpawnPlayerAt(spawnPoints[i], i);
    }

    private bool ValidateSpawnPoints(int requiredCount)
    {
        if (spawnPoints == null || spawnPoints.Length < requiredCount)
        {
            Debug.LogError($"[GameManager] Il faut au moins {requiredCount} spawn points (actuellement {spawnPoints?.Length ?? 0}).");
            return false;
        }

        for (int i = 0; i < requiredCount; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"[GameManager] Spawn point {i} est vide.");
                return false;
            }
        }

        return true;
    }

    private void TrySpawnPlayerAt(Vector3 sp, int slotIndex)
    {
        var go = Instantiate(playerPrefab, sp + Vector3.up * playerPrefab.transform.localScale.y / 2, Quaternion.identity);
        var player = go != null ? go.GetComponent<Agents.Player>() : null;
        if (player == null)
        {
            Debug.LogError("[GameManager] playerPrefab sans Agents.Player.");
            return;
        }

        player.SetSlotIndex(slotIndex);
        if (GameSessionConfig.LoadedFromMenu)
            player.SetName(GameSessionConfig.GetPlayerNameForSlot(slotIndex));
        _players.Add(player);
        _agents.Add(player);
    }

    private void TrySpawnAiAt(Vector3 sp, int slotIndex)
    {
        var go = Instantiate(aiPrefab, sp +Vector3.up * aiPrefab.transform.localScale.y / 2, Quaternion.identity);
        var ai = go != null ? go.GetComponent<Agents.AiController>() : null;
        if (ai == null)
        {
            Debug.LogError("[GameManager] aiPrefab sans Agents.AiController.");
            return;
        }
        ai.SetSlotIndex(slotIndex);
        if (GameSessionConfig.LoadedFromMenu)
            ai.SetName(GameSessionConfig.GetPlayerNameForSlot(slotIndex));
        _ais.Add(ai);
        _agents.Add(ai);
    }

    public void OnPlayerHealthChanged(Agents.Agent agent)
    {
        if (agent == null || _matchOver) return;
        TryPlayDynamicJingles(agent);

        var alive = _agents.Where(p => p != null && !p.IsDead).ToList();

        if (alive.Count <= 1)
        {
            if (alive.Count == 0) EndMatch(tieMessage, false);
            else EndMatch(string.Format(winnerFormat, alive[0].GetName()), true);
        }
    }

    private void EndMatch(string endMessage, bool hasWinner)
    {
        if (_matchOver) return;
        _matchOver = true;
        PauseGameplayMusicForEndMatch();
        PlayEndMatchSfx(hasWinner);

        FindFirstObjectByType<EndGameUIEvents>()?.EndGame(endMessage);
    }

    public PlayerControlDto[] GetPlayersProfiles() => _players != null
        ? _players.Select(p => p.GetProfileDTO()).ToArray()
        : System.Array.Empty<PlayerControlDto>();

    public void UpdatePlayerControl(PlayerControlDto dto)
    {
        foreach (var player in _players)
        {
            if (player.GetName() == dto.Name)
                player.UpdateControl(dto);
        }
    }
    void Update()
    {
        if (_players.Count <= 0)
            if(Keyboard.current.escapeKey.wasPressedThisFrame)
                FindFirstObjectByType<GamePauseController>()?.Pause();
    }

    public List<Vector3> GetAgentsPositions()
    {
        return _agents.ConvertAll(p => p.transform.position);
    }

    /// <summary>
    /// Positions des adversaires encore en vie (exclut <paramref name="self"/>).
    /// À utiliser pour l’IA : un filtre par distance exclurait toutes les cibles si plusieurs agents sont au même endroit.
    /// </summary>
    public List<Vector3> GetOtherAliveAgentPositions(Agents.Agent self)
    {
        if (self == null) return new List<Vector3>();
        return _agents
            .Where(a => a != null && a != self && !a.IsDead)
            .Select(a => a.transform.position)
            .ToList();
    }

    private void PlayGameplayMusicIfNeeded()
    {
        if (gameplayMusicSource == null) return;
        gameplayMusicSource.loop = true;
        ApplyGameplayMusicVolume();
        if (!gameplayMusicSource.isPlaying)
            gameplayMusicSource.Play();
    }

    private void ApplyGameplayMusicVolume()
    {
        if (gameplayMusicSource == null) return;
        gameplayMusicSource.volume = gameplayMusicBaseVolume * GameAudioSettings.GameplayMusicVolume;
        ApplyGameplayJingleVolume();
    }

    private void TryPlayDynamicJingles(Agents.Agent agent)
    {
        if (gameplayJingleSource == null) return;

        int id = agent.GetInstanceID();
        float hp = agent.HealthNormalized;

        if (agent.IsDead)
        {
            if (deathJingle != null && !_deathJinglePlayedFor.Contains(id))
            {
                gameplayJingleSource.PlayOneShot(deathJingle);
                _deathJinglePlayedFor.Add(id);
            }

            _lowHealthJinglePlayedFor.Remove(id);
            return;
        }

        if (hp <= lowHealthThreshold && !_lowHealthJinglePlayedFor.Contains(id))
        {
            if (lowHealthJingle != null)
                gameplayJingleSource.PlayOneShot(lowHealthJingle);
            _lowHealthJinglePlayedFor.Add(id);
        }
        else if (hp >= Mathf.Max(lowHealthThreshold, lowHealthResetThreshold))
        {
            // Permet de rejouer le jingle si le joueur repasse en état critique plus tard.
            _lowHealthJinglePlayedFor.Remove(id);
        }
    }

    private void ApplyGameplayJingleVolume()
    {
        if (gameplayJingleSource == null) return;
        gameplayJingleSource.volume = gameplayJingleBaseVolume * GameAudioSettings.GameplayMusicVolume;
    }

    private void PlayEndMatchSfx(bool hasWinner)
    {
        if (endMatchSfxSource == null) return;
        AudioClip clip = hasWinner ? winnerSfx : tieSfx;
        if (clip == null) return;
        endMatchSfxSource.PlayOneShot(clip, endMatchSfxBaseVolume * GameAudioSettings.SfxVolume);
    }

    private void PauseGameplayMusicForEndMatch()
    {
        if (gameplayMusicSource == null) return;
        if (gameplayMusicSource.isPlaying)
            gameplayMusicSource.Pause();
        if (gameplayJingleSource.isPlaying)
            gameplayJingleSource.Pause();
    }
}
