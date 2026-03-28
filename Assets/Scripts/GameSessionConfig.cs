using System.Linq;
using UnityEngine;

/// <summary>
/// Données passées du menu principal vers la scène de jeu (nombre de joueurs, IA par slot).
/// </summary>
public static class GameSessionConfig
{
    public static bool LoadedFromMenu { get; private set; }

    public static int PlayerCount { get; private set; } = 2;

    private static readonly bool[] s_computerSlot = new bool[4];
    private static readonly string[] s_playerNames = new string[4];

    public static void PrepareFromMenu(int playerCount, bool[] computerControlledPerSlot)
    {
        PrepareFromMenu(playerCount, computerControlledPerSlot, null);
    }

    public static void PrepareFromMenu(int playerCount, bool[] computerControlledPerSlot, string[] playerNamesPerSlot)
    {
        PlayerCount = Mathf.Clamp(playerCount, GameManager.MinAgent, GameManager.MaxPlayers);
        LoadedFromMenu = true;

        for (int i = 0; i < 4; i++)
        {
            bool v = computerControlledPerSlot != null && i < computerControlledPerSlot.Length && computerControlledPerSlot[i];
            s_computerSlot[i] = v;

            // Stocke tel quel : le fallback "Joueur X" se fera via GetPlayerNameForSlot.
            if (playerNamesPerSlot != null && i < playerNamesPerSlot.Length)
                s_playerNames[i] = playerNamesPerSlot[i];
            else
                s_playerNames[i] = null;
        }
    }

    public static string GetPlayerNameForSlot(int slotIndex)
    {
        // Fallback : "Joueur x" (x = numéro de slot + 1)
        if (slotIndex < 0 || slotIndex >= 4)
            return "Joueur";

        string s = s_playerNames[slotIndex];
        if (string.IsNullOrWhiteSpace(s))
            return $"Joueur {slotIndex + 1}";

        return s.Trim();
    }

    // public static void ApplyComputerFlagsToSpawnedPlayers(System.Collections.Generic.IReadOnlyList<Player.Player> sortedPlayers)
    // {
    //     if (!LoadedFromMenu) return;
    //     for (int i = 0; i < sortedPlayers.Count && i < 4; i++)
    //         sortedPlayers[i].SetComputerControlled(s_computerSlot[i]);
    //     LoadedFromMenu = false;
    // }
    public static int GetComputerCount()
    {
        return s_computerSlot.Count(x => x);
    }

    /// <summary>True si le slot (0–3) est une IA selon le menu principal.</summary>
    public static bool IsComputerControlledSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return false;
        return s_computerSlot[slotIndex];
    }
}
