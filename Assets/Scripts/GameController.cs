using Agents;
using Controls;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Vector3[] spawnPositions;
    
    
    [Header("Player Count")]
    [SerializeField] private int playerCount;

    private Agents.Player[] _players;

    private void Awake()
    {
        InitPlayers();
    }

    private void InitPlayers()
    {
        _players = new Agents.Player[playerCount];
        for(int i = 0, j = 0; i < playerCount; i++)
            _players[i] = SpawnPlayer(playerPrefab, j++ % spawnPositions.Length, $"Player {i}");
    }

    public void Update()
    {
        if(Keyboard.current.tKey.wasPressedThisFrame)
            foreach(var device in DeviceManager.Instance.GetAllDevices())
                Debug.Log($"{device.path} {device.name} {device.deviceId} {device.description}");
    }

    private Agents.Player SpawnPlayer(GameObject prefab, int spawnIndex, string playerName)
    {
        if (prefab == null)
        {
            Debug.LogError($"[GameController] Prefab for {playerName} is not assigned!");
            return null;
        }

        Vector3 position = (spawnPositions != null && spawnIndex < spawnPositions.Length)
            ? spawnPositions[spawnIndex]
            : Vector3.zero;

        GameObject player = Instantiate(prefab, position, Quaternion.identity);
        player.name = playerName;
        return player.GetComponent<Agents.Player>();
    }

    public PlayerControlDto[] GetPlayersProfiles() => _players != null
        ? System.Array.ConvertAll(_players, p => p.GetProfileDTO())
        : System.Array.Empty<PlayerControlDto>();

    public void UpdatePlayerControl(PlayerControlDto dto)
    {
        foreach (var player in _players)
        {
            if (player.GetName() == dto.Name)
                player.UpdateControl(dto);
        }
    }
}