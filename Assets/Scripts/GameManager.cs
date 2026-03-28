using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agents;
using TMPro;
using UI;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int MinPlayers = 0;
    public const int MaxPlayers = 4;
    public const int MinAgent = 2;
    public const int MaxAgent = 4;
    public const int MinAis = 0;
    public const int MaxAis = 4;

    [Header("Spawn (2 à 4 joueurs)")]
    [Tooltip("Si activé, les joueurs présents dans la scène sont retirés et des prefabs sont instanciés aux positions indiquées.")]
    [SerializeField] private bool spawnPlayersAtStart = true;
    [Tooltip("Nombre de joueurs pour cette partie (entre 0 et 4).")]
    [SerializeField] [Range(MinPlayers, MaxPlayers)] private int numberOfPlayers = 2;
    [Tooltip("Nombre de ai pour cette partie (entre 0 et 4).")]
    [SerializeField] [Range(MinAis, MaxAis)] private int numberOfAi = 0;
    [Tooltip("Points d’apparition (utiliser les N premiers selon le nombre de joueurs).")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [Tooltip("Une variante de prefab par slot (1 à 4). Le joueur i utilise le slot i (ou le premier prefab non-null si le slot est vide).")]
    
    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject aiPrefab;

    [Header("Turn Rules")]
    [SerializeField] private float maxProjectileLifeSeconds = 6f;
    [SerializeField] private float postShotDelaySeconds = 0.4f;

    [Header("UI joueur actif")]
    [SerializeField] private TextMeshProUGUI activePlayerLabel;
    [SerializeField] private string activePlayerFormat = "Joueur actif : {0}";
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color waitingProjectileColor = new Color(1f, 0.7f, 0.3f);

    [Header("Fin de partie")]
    [SerializeField] private string winnerFormat = "{0} remporte la partie !";
    [SerializeField] private Color winnerColor = new Color(0.4f, 1f, 0.5f);
    [SerializeField] private string tieMessage = "Partie terminée — plus aucun survivant";

    private readonly List<Agents.Player> _players = new();
    private readonly List<Agents.AiController> _ais = new();
    private readonly List<Agents.Agent> _agents = new();
    private bool _matchOver;

    public bool IsMatchOver => _matchOver;

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
        if (spawnPlayersAtStart)
            SpawnPlayersFromConfig();
        SpawnAisFromConfig();
        SetupViewports();
    }
    
    private void SetupViewports()
    {
        switch (_agents.Count)
        {
            case 1:
                _agents[0].SetViewport(new Rect(0, 0, 1, 1));
                break;

            case 2:
                _agents[0].SetViewport(new Rect(0f,   0f, 0.5f, 1f));
                _agents[1].SetViewport(new Rect(0.5f, 0f, 0.5f, 1f));
                break;

            case 3:
                _agents[0].SetViewport(new Rect(0f,   0.5f, 0.5f, 0.5f));
                _agents[1].SetViewport(new Rect(0.5f, 0.5f, 0.5f, 0.5f));
                _agents[2].SetViewport(new Rect(0f,   0f,   0.5f, 0.5f));
                Camera.main.rect    = new Rect(0.5f, 0f, 0.5f, 0.5f);
                Camera.main.enabled = true;
                break;

            case 4:
                _agents[0].SetViewport(new Rect(0f,   0.5f, 0.5f, 0.5f));
                _agents[1].SetViewport(new Rect(0.5f, 0.5f, 0.5f, 0.5f));
                _agents[2].SetViewport(new Rect(0f,   0f,   0.5f, 0.5f));
                _agents[3].SetViewport(new Rect(0.5f, 0f,   0.5f, 0.5f));
                break;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        numberOfPlayers = Mathf.Clamp(numberOfPlayers, MinPlayers, MaxPlayers);
    }
#endif

    /// <summary>Retire les joueurs de la scène et instancie les variantes aux spawn points (noms Player0…).</summary>
    private void SpawnPlayersFromConfig()
    {
        int n = Mathf.Clamp(GameSessionConfig.GetEffectivePlayerCount(numberOfPlayers), MinPlayers, MaxPlayers);

        if (spawnPoints == null || spawnPoints.Length < n)
        {
            Debug.LogError($"[TurnManager] Il faut au moins {n} spawn points (actuellement {spawnPoints?.Length ?? 0}).");
            return;
        }

        for (int i = 0; i < n; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"[TurnManager] Spawn point {i} est vide.");
                return;
            }
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[TurnManager] Aucun prefab de worm / joueur dans les variantes.");
            return;
        }

        var existing = FindObjectsOfType<Agents.Player>(true);
        foreach (var p in existing)
        {
            if (p != null && p.gameObject != null)
                Destroy(p.gameObject);
        }

        Agents.Agent.ResetStaticNaming();

        for (int i = 0; i < n; i++)
        {
            Transform sp = spawnPoints[i];
            var go = Instantiate(playerPrefab, sp.position, sp.rotation);
            var player = go != null ? go.GetComponent<Agents.Player>() : null;
            if (player != null)
            {
                player.SetSlotIndex(i);
                if (GameSessionConfig.LoadedFromMenu)
                    player.SetName(GameSessionConfig.GetPlayerNameForSlot(i));
                _players.Add(player);
                _agents.Add(player);
            }
        }

        foreach (var cam in FindObjectsOfType<CameraController>(false))
            cam.RequestTargetsRefreshFromPlayers();
    }
    
    private void SpawnAisFromConfig()
    {
        int n = Mathf.Clamp(numberOfAi, MinAis, MaxAgent);
        if (n + numberOfPlayers > MaxAgent) n = Mathf.Max(MaxAgent - numberOfPlayers, 0);
        if (n + numberOfPlayers < MinAgent) n = MinAgent - numberOfPlayers;
        Debug.Log($"I'm going to spawn {n} ais");
        if (spawnPoints == null || spawnPoints.Length < n)
        {
            Debug.LogError($"[TurnManager] Il faut au moins {n} spawn points (actuellement {spawnPoints?.Length ?? 0}).");
            return;
        }

        for (int i = numberOfPlayers; i < n + numberOfPlayers; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"[TurnManager] Spawn point {i} est vide.");
                return;
            }
        }

        if (aiPrefab == null)
        {
            Debug.LogError("[TurnManager] Aucun prefab de worm / ai dans les variantes.");
            return;
        }

        var existing = FindObjectsOfType<Agents.AiController>(true);
        foreach (var p in existing)
        {
            if (p != null && p.gameObject != null)
                Destroy(p.gameObject);
        }

        Agents.Agent.ResetStaticNaming();

        for (int i = 0; i < n; i++)
        {
            Debug.Log("Creating ai");
            Transform sp = spawnPoints[i + numberOfPlayers];
            var go = Instantiate(aiPrefab, sp.position, sp.rotation);
            var ai = go != null ? go.GetComponent<Agents.AiController>() : null;
            if (ai != null)
            {
                ai.SetSlotIndex(i);
                if (GameSessionConfig.LoadedFromMenu)
                    ai.SetName(GameSessionConfig.GetPlayerNameForSlot(i));
                _ais.Add(ai);
                _agents.Add(ai);
            }
        }

        foreach (var cam in FindObjectsOfType<CameraController>(false))
            cam.RequestTargetsRefreshFromPlayers();
    }

    public void OnPlayerHealthChanged(Agents.Agent agent)
    {
        if (agent == null || _matchOver) return;

        var alive = _agents.Where(p => p != null && !p.IsDead).ToList();

        if (alive.Count <= 1)
        {
            if (alive.Count == 0) EndMatch("The game ended in a tie!");
            else EndMatch($"{alive[0].GetName()} won the game!");
        }
    }

    private void EndMatch(string endMessage = "This game ended!")
    {
        if (_matchOver) return;
        _matchOver = true;

        // StopAllCoroutines();

        FindFirstObjectByType<EndGameUIEvents>().EndGame(endMessage);
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

    public List<Vector3> GetAgentsPositions()
    {
        return _agents.ConvertAll(p => p.transform.position);
    }
}
