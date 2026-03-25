using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Player;
using TMPro;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public const int MinPlayers = 2;
    public const int MaxPlayers = 4;

    [Header("Spawn (2 à 4 joueurs)")]
    [Tooltip("Si activé, les joueurs présents dans la scène sont retirés et des prefabs sont instanciés aux positions indiquées.")]
    [SerializeField] private bool spawnPlayersAtStart = true;
    [Tooltip("Nombre de joueurs pour cette partie (entre 2 et 4).")]
    [SerializeField] [Range(MinPlayers, MaxPlayers)] private int numberOfPlayers = 2;
    [Tooltip("Points d’apparition (utiliser les N premiers selon le nombre de joueurs).")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [Tooltip("Une variante de prefab par slot (1 à 4). Le joueur i utilise le slot i (ou le premier prefab non-null si le slot est vide).")]
    [SerializeField] private GameObject[] wormPrefabSlots = new GameObject[4];

    [Header("Turn Rules")]
    [SerializeField] private float maxProjectileLifeSeconds = 6f;
    [SerializeField] private float postShotDelaySeconds = 0.4f;

    [Header("UI joueur actif")]
    [SerializeField] private TextMeshProUGUI activePlayerLabel;
    [SerializeField] private string activePlayerFormat = "Joueur actif : {0}";
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color waitingProjectileColor = new Color(1f, 0.7f, 0.3f);

    private readonly List<Player.Player> _players = new();
    private int _currentIndex = 0;
    private bool _waitingForProjectile = false;

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
        if (spawnPlayersAtStart)
            SpawnPlayersFromConfig();

        RefreshPlayers();
        BeginTurn(FindFirstAliveIndex(0));
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
        int n = Mathf.Clamp(numberOfPlayers, MinPlayers, MaxPlayers);

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

        GameObject fallbackPrefab = null;
        if (wormPrefabSlots != null)
        {
            foreach (var p in wormPrefabSlots)
            {
                if (p != null) { fallbackPrefab = p; break; }
            }
        }

        if (fallbackPrefab == null)
        {
            Debug.LogError("[TurnManager] Aucun prefab de worm / joueur dans les variantes (slots 1 à 4).");
            return;
        }

        var existing = FindObjectsOfType<Player.Player>(true);
        foreach (var p in existing)
        {
            if (p != null && p.gameObject != null)
                Destroy(p.gameObject);
        }

        Player.Player.ResetStaticPlayerNaming();

        for (int i = 0; i < n; i++)
        {
            Transform sp = spawnPoints[i];
            GameObject prefab = (wormPrefabSlots != null && i < wormPrefabSlots.Length && wormPrefabSlots[i] != null)
                ? wormPrefabSlots[i]
                : fallbackPrefab;

            Instantiate(prefab, sp.position, sp.rotation);
        }

        foreach (var cam in FindObjectsOfType<CameraController>(false))
            cam.RequestTargetsRefreshFromPlayers();
    }

    public void RefreshPlayers()
    {
        _players.Clear();
        // Ne pas inclure les joueurs désactivés : sinon le tour peut rester sur un GameObject inactif.
        _players.AddRange(FindObjectsOfType<Player.Player>(false));
        _players.Sort((a, b) => string.CompareOrdinal(a.GetName(), b.GetName()));
    }

    private int FindFirstAliveIndex(int startIndex)
    {
        if (_players.Count == 0) return 0;
        for (int k = 0; k < _players.Count; k++)
        {
            int i = (startIndex + k) % _players.Count;
            if (!_players[i].IsDead) return i;
        }
        return startIndex;
    }

    private void BeginTurn(int index)
    {
        if (_players.Count == 0)
        {
            UpdateActivePlayerUI();
            return;
        }

        _currentIndex = FindFirstAliveIndex(index);
        _waitingForProjectile = false;

        for (int i = 0; i < _players.Count; i++)
            _players[i].SetTurnActive(i == _currentIndex && !_players[i].IsDead);

        UpdateActivePlayerUI();
    }

    private void NextTurn()
    {
        if (_players.Count == 0) return;
        int next = FindFirstAliveIndex(_currentIndex + 1);
        BeginTurn(next);
    }

    public bool IsWaitingForProjectile() => _waitingForProjectile;

    public void NotifyShotFired(Projectile projectile)
    {
        if (projectile == null) return;
        if (_waitingForProjectile) return;
        _waitingForProjectile = true;
        UpdateActivePlayerUI();
        StartCoroutine(WaitProjectileThenAdvance(projectile));
    }

    public void OnPlayerHealthChanged(Player.Player player)
    {
        if (player == null) return;
        if (player.IsDead)
            player.SetTurnActive(false);

        if (_players.Count > 0 && _players.TrueForAll(p => p.IsDead))
        {
            if (activePlayerLabel != null)
            {
                activePlayerLabel.color = activeColor;
                activePlayerLabel.text = "Partie terminée";
            }
            return;
        }

        if (!_waitingForProjectile && _players.Count > 0 && _currentIndex >= 0 && _currentIndex < _players.Count &&
            _players[_currentIndex].IsDead)
            BeginTurn(FindFirstAliveIndex(_currentIndex + 1));

        UpdateActivePlayerUI();
    }

    private IEnumerator WaitProjectileThenAdvance(Projectile projectile)
    {
        float start = Time.time;
        while (projectile != null && !projectile.HasImpacted && (Time.time - start) < maxProjectileLifeSeconds)
            yield return null;

        if (projectile != null)
            Destroy(projectile.gameObject);

        _waitingForProjectile = false;
        UpdateActivePlayerUI();

        yield return new WaitForSeconds(postShotDelaySeconds);
        NextTurn();
    }

    private void UpdateActivePlayerUI()
    {
        if (activePlayerLabel == null) return;

        if (_waitingForProjectile)
        {
            activePlayerLabel.color = waitingProjectileColor;
            activePlayerLabel.text = string.Format(activePlayerFormat, "Projectile en vol…");
            return;
        }

        activePlayerLabel.color = activeColor;
        if (_players.Count == 0 || _currentIndex < 0 || _currentIndex >= _players.Count)
        {
            activePlayerLabel.text = string.Format(activePlayerFormat, "—");
            return;
        }

        var p = _players[_currentIndex];
        if (p.IsDead)
        {
            activePlayerLabel.text = string.Format(activePlayerFormat, "—");
            return;
        }

        activePlayerLabel.text = string.Format(activePlayerFormat, p.GetName());
    }
}
