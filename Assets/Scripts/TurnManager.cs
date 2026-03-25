using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Rules")]
    [SerializeField] private float maxProjectileLifeSeconds = 6f;
    [SerializeField] private float postShotDelaySeconds = 0.4f;

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
        RefreshPlayers();
        BeginTurn(0);
    }

    public void RefreshPlayers()
    {
        _players.Clear();
        _players.AddRange(FindObjectsOfType<Player.Player>(true));

        // Stable ordering: Player0, Player1, etc. (based on internal name assigned in Player.cs).
        _players.Sort((a, b) => string.CompareOrdinal(a.GetName(), b.GetName()));
    }

    private void BeginTurn(int index)
    {
        if (_players.Count == 0) return;

        _currentIndex = Mathf.Clamp(index, 0, _players.Count - 1);
        _waitingForProjectile = false;

        for (int i = 0; i < _players.Count; i++)
            _players[i].SetTurnActive(i == _currentIndex);
    }

    private void NextTurn()
    {
        if (_players.Count == 0) return;
        int next = (_currentIndex + 1) % _players.Count;
        BeginTurn(next);
    }

    public bool IsWaitingForProjectile() => _waitingForProjectile;

    public void NotifyShotFired(Projectile projectile)
    {
        if (projectile == null) return;
        if (_waitingForProjectile) return;
        _waitingForProjectile = true;

        StartCoroutine(WaitProjectileThenAdvance(projectile));
    }

    private System.Collections.IEnumerator WaitProjectileThenAdvance(Projectile projectile)
    {
        float start = Time.time;
        while (projectile != null && !projectile.HasImpacted && (Time.time - start) < maxProjectileLifeSeconds)
            yield return null;

        if (projectile != null)
            Destroy(projectile.gameObject);

        yield return new WaitForSeconds(postShotDelaySeconds);
        NextTurn();
    }
}

